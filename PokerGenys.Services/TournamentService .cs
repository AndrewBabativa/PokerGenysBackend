using PokerGenys.Domain.DTOs.Audit;
using PokerGenys.Domain.Enums;
using PokerGenys.Domain.Models.Core;
using PokerGenys.Domain.Models.Tournaments;
using PokerGenys.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PokerGenys.Services
{
    public class TournamentService : ITournamentService
    {
        private readonly ITournamentRepository _repo;
        private readonly IPlayerRepository _playerRepo;
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

        public TournamentService(ITournamentRepository repo, IPlayerRepository playerRepo)
        {
            _repo = repo;
            _playerRepo = playerRepo;
        }

        // ==================================================================================
        // 1. CRUD BÁSICO
        // ==================================================================================
        public Task<List<Tournament>> GetAllAsync() => _repo.GetAllAsync();
        public Task<Tournament?> GetByIdAsync(Guid id) => _repo.GetByIdAsync(id);


        public async Task<Tournament> CreateAsync(Tournament tournament)
        {
            CalculateFixedPayouts(tournament);
            tournament.ClockState = new ClockState { IsPaused = true, SecondsRemaining = 0 };

            if (tournament.Tables == null || !tournament.Tables.Any())
            {
                tournament.Tables = new List<TournamentTable>
                {
                    new TournamentTable { Id = Guid.NewGuid(), TournamentId = tournament.Id, TableNumber = 1, Name = "Mesa 1", Status = TournamentTableStatus.Active, CreatedAt = DateTime.UtcNow }
                };
            }
            return await _repo.CreateAsync(tournament);
        }

        public Task<Tournament> UpdateAsync(Tournament tournament) => _repo.UpdateAsync(tournament);
        public Task<bool> DeleteAsync(Guid id) => _repo.DeleteAsync(id);

        public async Task<List<TournamentRegistration>> GetRegistrationsAsync(Guid id)
        {
            var t = await _repo.GetByIdAsync(id);
            return t?.Registrations ?? new List<TournamentRegistration>();
        }

        // ==================================================================================
        // 2. GESTIÓN DE JUGADORES
        // ==================================================================================

        public async Task<RegistrationResult?> RegisterPlayerAsync(
             Guid tournamentId, string playerName, string paymentMethodStr, string? bankStr = null, string? reference = null, Guid? existingPlayerId = null)
        {
            var tournamentLock = _locks.GetOrAdd(tournamentId, _ => new SemaphoreSlim(1, 1));
            await tournamentLock.WaitAsync();

            try
            {
                var t = await _repo.GetByIdAsync(tournamentId);
                if (t == null) return null;

                EnsureListsInitialized(t);
                EnsureActiveTableExists(t);

                if (!Enum.TryParse<PaymentMethod>(paymentMethodStr, true, out var pMethod)) pMethod = PaymentMethod.Cash;
                PaymentProvider? pBank = null;
                if (!string.IsNullOrEmpty(bankStr) && Enum.TryParse<PaymentProvider>(bankStr, true, out var parsedBank)) pBank = parsedBank;

                Guid finalPlayerId;
                if (existingPlayerId.HasValue)
                {
                    finalPlayerId = existingPlayerId.Value;
                }
                else
                {
                    var newPlayer = new Player { FirstName = playerName, Type = PlayerType.Guest, InternalNotes = $"Creado autom. desde Torneo {t.Name}" };
                    await _playerRepo.CreateAsync(newPlayer);
                    finalPlayerId = newPlayer.Id;
                }

                if (t.Registrations.Any(r => r.PlayerId == finalPlayerId && r.Status == RegistrationStatus.Active))
                    throw new Exception("El jugador ya está activo en este torneo.");

                var reg = new TournamentRegistration
                {
                    Id = Guid.NewGuid(),
                    TournamentId = t.Id,
                    WorkingDayId = t.WorkingDayId,
                    PlayerId = finalPlayerId,
                    PlayerName = playerName,
                    Chips = t.StartingChips,
                    PaidAmount = t.BuyIn + t.Fee,
                    PaymentMethod = pMethod,
                    RegistrationType = RegistrationType.Standard,
                    RegisteredAt = DateTime.UtcNow,
                    Status = RegistrationStatus.Active
                };

                var seatResult = AssignSmartSeat(t, reg);

                RecordFinancialTransaction(t, finalPlayerId, TransactionType.BuyIn, t.BuyIn, pMethod, pBank, reference, $"Buy-In: {playerName}");
                if (t.Fee > 0) RecordFinancialTransaction(t, null, TransactionType.Rake, t.Fee, pMethod, pBank, reference, "Rake Buy-In");

                t.PrizePool += t.BuyIn;
                t.TotalEntries++;
                t.ActivePlayers++;
                t.Registrations.Add(reg);

                await _repo.UpdateAsync(t);

                return new RegistrationResult
                {
                    Registration = reg,
                    SystemMessage = seatResult.Message,
                    InstructionType = seatResult.InstructionType,
                    NewStats = new TournamentStatsDto { Entries = t.TotalEntries, Active = t.ActivePlayers, PrizePool = t.PrizePool }
                };
            }
            finally { tournamentLock.Release(); }
        }

        public async Task<RegistrationResult?> RebuyPlayerAsync(Guid tournamentId, Guid regId, string paymentMethodStr, string? bankStr = null, string? reference = null)
        {
            var t = await _repo.GetByIdAsync(tournamentId);
            if (t == null) return null;
            if (!t.RebuyConfig.Enabled) return null;
            if (t.RebuyConfig.UntilLevel > 0 && t.CurrentLevel > t.RebuyConfig.UntilLevel) throw new Exception("Periodo de Rebuy finalizado.");

            EnsureListsInitialized(t);
            var reg = t.Registrations.FirstOrDefault(r => r.Id == regId);
            if (reg == null) return null;

            bool wasEliminated = reg.Status == RegistrationStatus.Eliminated;
            reg.Status = RegistrationStatus.Active;
            reg.EliminatedAt = null;
            reg.Chips += t.RebuyConfig.RebuyChips;
            if (wasEliminated) t.ActivePlayers++;

            if (!Enum.TryParse<PaymentMethod>(paymentMethodStr, true, out var pMethod)) pMethod = PaymentMethod.Cash;
            PaymentProvider? pBank = null;
            if (!string.IsNullOrEmpty(bankStr) && Enum.TryParse<PaymentProvider>(bankStr, true, out var parsedBank)) pBank = parsedBank;

            RecordFinancialTransaction(t, reg.PlayerId, TransactionType.ReBuy, t.RebuyConfig.RebuyCost, pMethod, pBank, reference, $"Rebuy Nivel {t.CurrentLevel}");
            if (t.RebuyConfig.RebuyHouseFee > 0) RecordFinancialTransaction(t, null, TransactionType.Rake, t.RebuyConfig.RebuyHouseFee, pMethod, pBank, reference, "Rake Rebuy");

            t.PrizePool += t.RebuyConfig.RebuyCost;
            reg.PaidAmount += (t.RebuyConfig.RebuyCost + t.RebuyConfig.RebuyHouseFee);

            string msg = "Rebuy exitoso.";
            if (wasEliminated)
            {
                var seatResult = AssignSmartSeat(t, reg);
                msg += " " + seatResult.Message;
            }

            await _repo.UpdateAsync(t);
            return new RegistrationResult { Registration = reg, SystemMessage = msg };
        }

        public async Task<RegistrationResult?> AddOnPlayerAsync(Guid tournamentId, Guid regId, string paymentMethodStr, string? bankStr = null, string? reference = null)
        {
            var t = await _repo.GetByIdAsync(tournamentId);
            if (t == null) return null;
            if (!t.AddonConfig.Enabled) return null;
            if (t.AddonConfig.AllowedLevel > 0 && t.CurrentLevel > t.AddonConfig.AllowedLevel) throw new Exception("Periodo de Add-On finalizado.");

            EnsureListsInitialized(t);
            var reg = t.Registrations.FirstOrDefault(r => r.Id == regId);
            if (reg == null || reg.Status != RegistrationStatus.Active) return null;

            reg.Chips += t.AddonConfig.AddonChips;

            if (!Enum.TryParse<PaymentMethod>(paymentMethodStr, true, out var pMethod)) pMethod = PaymentMethod.Cash;
            PaymentProvider? pBank = null;
            if (!string.IsNullOrEmpty(bankStr) && Enum.TryParse<PaymentProvider>(bankStr, true, out var parsedBank)) pBank = parsedBank;

            RecordFinancialTransaction(t, reg.PlayerId, TransactionType.AddOn, t.AddonConfig.AddonCost, pMethod, pBank, reference, "Add-On");
            if (t.AddonConfig.AddonHouseFee > 0) RecordFinancialTransaction(t, null, TransactionType.Rake, t.AddonConfig.AddonHouseFee, pMethod, pBank, reference, "Rake Add-On");

            t.PrizePool += t.AddonConfig.AddonCost;
            reg.PaidAmount += (t.AddonConfig.AddonCost + t.AddonConfig.AddonHouseFee);

            await _repo.UpdateAsync(t);
            return new RegistrationResult { Registration = reg, SystemMessage = "Add-On Procesado." };
        }

        // ==================================================================================
        // 3. CONTROL DE JUEGO
        // ==================================================================================

        public async Task<Tournament?> StartTournamentAsync(Guid id)
        {
            var t = await _repo.GetByIdAsync(id);
            if (t == null) return null;
            if (t.Status == TournamentStatus.Running) return t;

            if (t.ClockState.SecondsRemaining <= 0.1)
            {
                var level1 = t.Levels.FirstOrDefault(l => l.LevelNumber == 1);
                t.ClockState.SecondsRemaining = level1?.DurationSeconds ?? 0;
                t.CurrentLevel = 1;
            }

            t.Status = TournamentStatus.Running;
            t.ClockState.IsPaused = false;
            t.ClockState.LastUpdatedAt = DateTime.UtcNow;
            if (!t.StartTime.HasValue) t.StartTime = DateTime.UtcNow;

            await _repo.UpdateAsync(t);
            return t;
        }

        public async Task<Tournament?> PauseTournamentAsync(Guid id)
        {
            var t = await _repo.GetByIdAsync(id);
            if (t == null) return null;

            if (!t.ClockState.IsPaused && t.ClockState.LastUpdatedAt.HasValue)
            {
                var elapsed = (DateTime.UtcNow - t.ClockState.LastUpdatedAt.Value).TotalSeconds;
                t.ClockState.SecondsRemaining -= elapsed;
                if (t.ClockState.SecondsRemaining < 0) t.ClockState.SecondsRemaining = 0;
            }

            t.Status = TournamentStatus.Paused;
            t.ClockState.IsPaused = true;
            t.ClockState.LastUpdatedAt = DateTime.UtcNow;

            await _repo.UpdateAsync(t);
            return t;
        }

        public async Task<TournamentState?> GetTournamentStateAsync(Guid id)
        {
            var t = await _repo.GetByIdAsync(id);
            if (t == null) return null;

            double projectedSeconds = t.ClockState.SecondsRemaining;
            bool needsDbUpdate = false;

            if (t.Status == TournamentStatus.Running && !t.ClockState.IsPaused && t.ClockState.LastUpdatedAt.HasValue)
            {
                var elapsed = (DateTime.UtcNow - t.ClockState.LastUpdatedAt.Value).TotalSeconds;
                projectedSeconds -= elapsed;

                if (projectedSeconds <= 0)
                {
                    AdvanceLevelInternal(t);
                    projectedSeconds = t.ClockState.SecondsRemaining;
                    needsDbUpdate = true;
                }
            }

            if (needsDbUpdate) await _repo.UpdateAsync(t);

            var blinds = t.Levels.FirstOrDefault(l => l.LevelNumber == t.CurrentLevel);
            return new TournamentState
            {
                CurrentLevel = t.CurrentLevel,
                TimeRemaining = (int)Math.Max(0, Math.Ceiling(projectedSeconds)),
                Status = t.Status,
                RegisteredCount = t.TotalEntries,
                PrizePool = t.PrizePool,
                Blinds = blinds != null ? $"{blinds.SmallBlind}/{blinds.BigBlind}" : "-",
                IsFinalTable = t.ActivePlayers <= t.Seating.FinalTableSize && t.ActivePlayers > 0 && (t.Levels.Find(l => l.LevelNumber == t.CurrentLevel).AllowRebuy == false)
            };
        }

        // ==================================================================================
        // 4. VENTAS Y TRANSACCIONES EXTRAS
        // ==================================================================================

        public async Task<FinancialTransaction?> RecordServiceSaleAsync(Guid tournamentId, Guid? playerId, decimal amount, string description, Dictionary<string, object> items, string paymentMethodStr, string? bankStr = null, string? reference = null)
        {
            var t = await _repo.GetByIdAsync(tournamentId);
            if (t == null) return null;
            if (t.Transactions == null) t.Transactions = new List<FinancialTransaction>();

            if (!Enum.TryParse<PaymentMethod>(paymentMethodStr, true, out var pMethod)) pMethod = PaymentMethod.Cash;
            PaymentProvider? pBank = null;
            if (!string.IsNullOrEmpty(bankStr) && Enum.TryParse<PaymentProvider>(bankStr, true, out var parsedBank)) pBank = parsedBank;

            var tx = new FinancialTransaction
            {
                Id = Guid.NewGuid(),
                WorkingDayId = t.WorkingDayId,
                Source = TransactionSource.Restaurant,
                SourceId = t.Id,
                PlayerId = playerId,
                Type = TransactionType.ServiceSale,
                Amount = amount,
                PaymentMethod = pMethod,
                Bank = pBank,
                ReferenceCode = reference,
                Description = description,
                Metadata = items,
                Timestamp = DateTime.UtcNow,
                Status = PaymentStatus.Paid
            };

            t.Transactions.Add(tx);
            await _repo.UpdateAsync(t);
            return tx;
        }

        public async Task<FinancialTransaction?> RecordTransactionAsync(Guid tournamentId, FinancialTransaction transaction)
        {
            var t = await _repo.GetByIdAsync(tournamentId);
            if (t == null) return null;
            if (t.Transactions == null) t.Transactions = new List<FinancialTransaction>();

            transaction.Id = Guid.NewGuid();
            transaction.Timestamp = DateTime.UtcNow;
            transaction.WorkingDayId = t.WorkingDayId;
            transaction.SourceId = t.Id;

            t.Transactions.Add(transaction);
            await _repo.UpdateAsync(t);
            return transaction;
        }

        // ==================================================================================
        // 5. GESTIÓN DE MESAS Y ESTRUCTURA
        // ==================================================================================

        public async Task<RemoveResult> RemoveRegistrationAsync(Guid tournamentId, Guid regId)
        {
            var t = await _repo.GetByIdAsync(tournamentId);
            if (t == null) return new RemoveResult { Success = false };
            EnsureListsInitialized(t);

            var player = t.Registrations.FirstOrDefault(r => r.Id == regId);
            if (player == null) return new RemoveResult { Success = false };

            player.Status = RegistrationStatus.Eliminated;
            player.EliminatedAt = DateTime.UtcNow;
            player.TableId = null;
            player.SeatId = null;

            t.ActivePlayers = t.Registrations.Count(r => r.Status == RegistrationStatus.Active);
            CleanupEmptyTables(t);
            var result = CheckForStructureEvents(t);

            await _repo.UpdateAsync(t);
            return result;
        }

        public async Task<TournamentRegistration?> AssignSeatAsync(Guid tournamentId, Guid regId, string tableId, string seatId)
        {
            var t = await _repo.GetByIdAsync(tournamentId);
            if (t == null) return null;
            EnsureListsInitialized(t);

            var reg = t.Registrations.FirstOrDefault(r => r.Id == regId);
            if (reg == null) return null;

            if (t.Registrations.Any(r => r.TableId == tableId && r.SeatId == seatId && r.Status == RegistrationStatus.Active))
                throw new Exception("El asiento ya está ocupado.");

            reg.TableId = tableId;
            reg.SeatId = seatId;

            CleanupEmptyTables(t);
            await _repo.UpdateAsync(t);
            return reg;
        }

        public Task<decimal> GetTotalPrizePoolAsync(Guid id) => _repo.GetByIdAsync(id).ContinueWith(t => t.Result?.PrizePool ?? 0);

        public async Task<TournamentAuditResult> GetFinancialAuditAsync(Guid workingDayId)
        {
            var allTournaments = await _repo.GetAllAsync();
            var dayTournaments = allTournaments.Where(t => t.WorkingDayId == workingDayId).ToList();
            var audit = new TournamentAuditResult();

            foreach (var t in dayTournaments)
            {
                if (t.Transactions == null) continue;
                foreach (var tx in t.Transactions)
                {
                    if (tx.Type == TransactionType.BuyIn || tx.Type == TransactionType.ReBuy || tx.Type == TransactionType.AddOn || tx.Type == TransactionType.LateRegistration)
                    {
                        audit.TotalCollected += tx.Amount;
                        string methodKey = tx.PaymentMethod.ToString();
                        if (!audit.PaymentMethodBreakdown.ContainsKey(methodKey)) audit.PaymentMethodBreakdown[methodKey] = 0;
                        audit.PaymentMethodBreakdown[methodKey] += tx.Amount;
                    }
                    if (tx.Type == TransactionType.Payout)
                    {
                        audit.TotalPayouts += tx.Amount;
                        string methodKey = tx.PaymentMethod.ToString();
                        if (audit.PaymentMethodBreakdown.ContainsKey(methodKey)) audit.PaymentMethodBreakdown[methodKey] -= tx.Amount;
                    }
                    if (tx.Type == TransactionType.Rake) audit.TotalFeesGenerated += tx.Amount;
                }
            }
            return audit;
        }

        // --- HELPERS ---
        private void RecordFinancialTransaction(Tournament t, Guid? playerId, TransactionType type, decimal amount, PaymentMethod method, PaymentProvider? bank, string? refId, string desc)
        {
            t.Transactions.Add(new FinancialTransaction
            {
                Id = Guid.NewGuid(),
                WorkingDayId = t.WorkingDayId,
                Source = TransactionSource.Tournament,
                SourceId = t.Id,
                PlayerId = playerId,
                Type = type,
                Amount = amount,
                PaymentMethod = method,
                Bank = bank,
                ReferenceCode = refId,
                Description = desc,
                Timestamp = DateTime.UtcNow
            });
        }

        private void EnsureListsInitialized(Tournament t)
        {
            if (t.Tables == null) t.Tables = new List<TournamentTable>();
            if (t.Registrations == null) t.Registrations = new List<TournamentRegistration>();
            if (t.Transactions == null) t.Transactions = new List<FinancialTransaction>();
            if (t.ClockState == null) t.ClockState = new ClockState();
        }

        private void EnsureActiveTableExists(Tournament t)
        {
            if (!t.Tables.Any(x => x.Status == TournamentTableStatus.Active))
            {
                int nextNum = t.Tables.Any() ? t.Tables.Max(x => x.TableNumber) + 1 : 1;
                t.Tables.Add(new TournamentTable
                {
                    Id = Guid.NewGuid(),
                    TournamentId = t.Id,
                    TableNumber = nextNum,
                    Name = $"Mesa {nextNum}",
                    Status = TournamentTableStatus.Active,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        private void AdvanceLevelInternal(Tournament t)
        {
            var nextLevelNum = t.CurrentLevel + 1;
            var nextLevelConfig = t.Levels.FirstOrDefault(l => l.LevelNumber == nextLevelNum);
            if (nextLevelConfig != null)
            {
                t.CurrentLevel = nextLevelNum;
                t.ClockState.SecondsRemaining = nextLevelConfig.DurationSeconds;
            }
            else
            {
                t.ClockState.SecondsRemaining = 0;
                t.Status = TournamentStatus.Paused;
                t.ClockState.IsPaused = true;
            }
            t.ClockState.LastUpdatedAt = DateTime.UtcNow;
        }

        private void CleanupEmptyTables(Tournament t)
        {
            var activeTableIds = t.Registrations.Where(r => r.Status == RegistrationStatus.Active && !string.IsNullOrEmpty(r.TableId)).Select(r => r.TableId).Distinct().ToList();
            foreach (var table in t.Tables.Where(tb => tb.Status == TournamentTableStatus.Active).ToList())
            {
                if (!activeTableIds.Contains(table.Id.ToString())) table.Status = TournamentTableStatus.Broken;
            }
        }

        private (string? InstructionType, string Message) AssignSmartSeat(Tournament t, TournamentRegistration reg)
        {
            int maxSeats = t.Seating.SeatsPerTable > 0 ? t.Seating.SeatsPerTable : 9;
            var targetTable = t.Tables.Where(tb => tb.Status == TournamentTableStatus.Active).OrderBy(tb => t.Registrations.Count(r => r.TableId == tb.Id.ToString() && r.Status == RegistrationStatus.Active)).FirstOrDefault(tb => t.Registrations.Count(r => r.TableId == tb.Id.ToString() && r.Status == RegistrationStatus.Active) < maxSeats);

            if (targetTable == null)
            {
                int nextNum = t.Tables.Any() ? t.Tables.Max(x => x.TableNumber) + 1 : 1;
                targetTable = new TournamentTable { Id = Guid.NewGuid(), TournamentId = t.Id, TableNumber = nextNum, Name = $"Mesa {nextNum}", Status = TournamentTableStatus.Active };
                t.Tables.Add(targetTable);
            }

            var occupiedSeats = t.Registrations.Where(r => r.TableId == targetTable.Id.ToString() && r.Status == RegistrationStatus.Active).Select(r => int.TryParse(r.SeatId, out int s) ? s : 0).ToHashSet();
            int seat = 1;
            for (int i = 1; i <= maxSeats; i++) { if (!occupiedSeats.Contains(i)) { seat = i; break; } }

            reg.TableId = targetTable.Id.ToString();
            reg.SeatId = seat.ToString();
            return (null, $"Asignado a {targetTable.Name}, Silla {seat}");
        }

        private void CalculateFixedPayouts(Tournament t)
        {
            decimal totalPrize = Math.Max(t.Guaranteed, t.PrizePool);
            if (totalPrize <= 0 || t.Payouts == null) return;
            foreach (var tier in t.Payouts) if (tier.Percentage > 0) tier.FixedAmount = (tier.Percentage / 100m) * totalPrize;
        }

        private RemoveResult CheckForStructureEvents(Tournament t)
        {
            var activePlayers = t.Registrations.Where(r => r.Status == RegistrationStatus.Active).ToList();
            var activeTables = t.Tables.Where(tb => tb.Status == TournamentTableStatus.Active || tb.Status == TournamentTableStatus.FinalTable).ToList();
            int ftSize = t.Seating.FinalTableSize > 0 ? t.Seating.FinalTableSize : 9;

            if (activePlayers.Count <= ftSize && activeTables.Count > 1)
            {
                var finalTable = t.Tables.FirstOrDefault(tb => tb.Status == TournamentTableStatus.Active);
                if (finalTable == null) return new RemoveResult { Success = true };

                finalTable.Name = "Mesa Final";
                finalTable.Status = TournamentTableStatus.FinalTable;
                foreach (var table in activeTables.Where(tb => tb.Id != finalTable.Id)) table.Status = TournamentTableStatus.Broken;

                var rng = new Random();
                var shuffledPlayers = activePlayers.OrderBy(x => rng.Next()).ToList();
                int seat = 1;
                foreach (var p in shuffledPlayers) { p.TableId = finalTable.Id.ToString(); p.SeatId = seat.ToString(); seat++; }

                return new RemoveResult { Success = true, InstructionType = "FINAL_TABLE_START", Message = "¡Mesa Final Formada! Se han sorteado los puestos.", FromTable = finalTable.Id.ToString() };
            }

            if (activePlayers.Count == 1)
            {
                var winner = activePlayers.First();
                t.Status = TournamentStatus.Finished;
                return new RemoveResult { Success = true, InstructionType = "TOURNAMENT_WINNER", Message = $"¡Torneo Finalizado! Ganador: {winner.PlayerName}", Data = winner };
            }
            return new RemoveResult { Success = true };
        }
    }
}