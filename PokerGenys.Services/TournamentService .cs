using PokerGenys.Domain.Models;
using PokerGenys.Domain.Models.Tournaments;
using PokerGenys.Infrastructure.Repositories;

namespace PokerGenys.Services
{
    public class TournamentService : ITournamentService
    {
        private readonly ITournamentRepository _repo;

        public TournamentService(ITournamentRepository repo)
        {
            _repo = repo;
        }

        // =============================================================
        // 1. CRUD BÁSICO
        // =============================================================
        public Task<List<Tournament>> GetAllAsync() => _repo.GetAllAsync();
        public Task<Tournament?> GetByIdAsync(Guid id) => _repo.GetByIdAsync(id);

        public async Task<Tournament> CreateAsync(Tournament tournament)
        {
            CalculateFixedPayouts(tournament);
            if (tournament.Tables == null || !tournament.Tables.Any())
            {
                tournament.Tables = new List<TournamentTable>
                {
                    new TournamentTable
                    {
                        Id = Guid.NewGuid(),
                        TournamentId = tournament.Id,
                        TableNumber = 1,
                        Name = "Mesa 1",
                        Status = TournamentTableStatus.Active
                    }
                };
            }
            return await _repo.CreateAsync(tournament);
        }

        public Task<Tournament> UpdateAsync(Tournament tournament) => _repo.UpdateAsync(tournament);
        public Task<bool> DeleteAsync(Guid id) => _repo.DeleteAsync(id);

        // =============================================================
        // 2. GESTIÓN DE JUGADORES
        // =============================================================
        public async Task<List<TournamentRegistration>> GetRegistrationsAsync(Guid id)
        {
            var t = await _repo.GetByIdAsync(id);
            return t?.Registrations ?? new List<TournamentRegistration>();
        }

        public async Task<Tournament?> AddRegistrationAsync(Guid id, TournamentRegistration reg)
        {
            var t = await _repo.GetByIdAsync(id);
            if (t == null) return null;
            if (t.Registrations == null) t.Registrations = new List<TournamentRegistration>();

            reg.Id = Guid.NewGuid();
            reg.TournamentId = id;
            reg.RegisteredAt = DateTime.UtcNow;
            t.Registrations.Add(reg);
            t.TotalEntries++;

            return await _repo.UpdateAsync(t);
        }

        // -------------------------------------------------------------
        // REGISTRO (BUY-IN) CON PAGO DETALLADO
        // -------------------------------------------------------------
        public async Task<RegistrationResult?> RegisterPlayerAsync(Guid id, string playerName, string paymentMethod, string? bank = null, string? reference = null)
        {
            var t = await _repo.GetByIdAsync(id);
            if (t == null) return null;

            EnsureListsInitialized(t);
            EnsureActiveTableExists(t);

            var reg = new TournamentRegistration
            {
                Id = Guid.NewGuid(),
                TournamentId = t.Id,
                WorkingDayId = t.WorkingDayId,
                PlayerName = playerName,
                Chips = t.StartingChips,
                RegisteredAt = DateTime.UtcNow,
                Status = RegistrationStatus.Active,
                PaidAmount = t.BuyIn + t.Fee,
                PaymentMethod = ParsePaymentMethod(paymentMethod), // Enum
                RegistrationType = RegistrationType.Standard
            };

            var seatResult = AssignSmartSeat(t, reg);

            // Transacción detallada
            var tx = new TournamentTransaction
            {
                Id = Guid.NewGuid(),
                TournamentId = t.Id,
                WorkingDayId = t.WorkingDayId,
                PlayerId = reg.Id,
                Type = TransactionType.BuyIn,
                Amount = t.BuyIn,
                PaymentMethod = reg.PaymentMethod,
                Bank = ParsePaymentProvider(bank),       // <--- NUEVO: Guarda el banco
                PaymentReference = reference,            // <--- NUEVO: Guarda el recibo
                Description = $"Buy-In: {reg.PlayerName}",
                Timestamp = DateTime.UtcNow
            };
            t.Transactions.Add(tx);

            if (t.Fee > 0)
            {
                t.Transactions.Add(new TournamentTransaction
                {
                    Id = Guid.NewGuid(),
                    TournamentId = t.Id,
                    WorkingDayId = t.WorkingDayId,
                    Type = TransactionType.HouseRake, // StaffFee
                    Amount = t.Fee,
                    PaymentMethod = reg.PaymentMethod,
                    Bank = ParsePaymentProvider(bank),
                    PaymentReference = reference,
                    Description = "Rake Buy-In",
                    Timestamp = DateTime.UtcNow
                });
            }

            t.PrizePool += t.BuyIn;
            t.TotalEntries++;
            t.ActivePlayers++;
            t.Registrations.Add(reg);

            await _repo.UpdateAsync(t);

            return new RegistrationResult { Registration = reg, InstructionType = seatResult.InstructionType, SystemMessage = seatResult.Message };
        }

        // -------------------------------------------------------------
        // RECOMPRA (REBUY) CON PAGO DETALLADO
        // -------------------------------------------------------------
        public async Task<RegistrationResult?> RebuyPlayerAsync(Guid tournamentId, Guid registrationId, string paymentMethod, string? bank = null, string? reference = null)
        {
            var t = await _repo.GetByIdAsync(tournamentId);
            if (t == null) return null;

            if (t.RebuyConfig.UntilLevel > 0 && t.CurrentLevel > t.RebuyConfig.UntilLevel) return null;
            if (!t.RebuyConfig.Enabled) return null;

            EnsureListsInitialized(t);

            var reg = t.Registrations.FirstOrDefault(r => r.Id == registrationId);
            if (reg == null) return null;

            // Lógica de Juego
            reg.Status = RegistrationStatus.Active;
            reg.EliminatedAt = null;
            reg.Chips = t.RebuyConfig.RebuyChips;
            t.ActivePlayers++;

            // Transacción detallada
            var tx = new TournamentTransaction
            {
                Id = Guid.NewGuid(),
                TournamentId = t.Id,
                WorkingDayId = t.WorkingDayId,
                PlayerId = reg.Id,
                Type = TransactionType.ReBuy,
                Amount = t.RebuyConfig.RebuyCost,
                PaymentMethod = ParsePaymentMethod(paymentMethod),
                Bank = ParsePaymentProvider(bank),       // <--- NUEVO
                PaymentReference = reference,            // <--- NUEVO
                Description = $"Rebuy Nivel {t.CurrentLevel}",
                Timestamp = DateTime.UtcNow
            };
            t.Transactions.Add(tx);

            if (t.RebuyConfig.RebuyHouseFee > 0)
            {
                t.Transactions.Add(new TournamentTransaction
                {
                    Id = Guid.NewGuid(),
                    TournamentId = t.Id,
                    WorkingDayId = t.WorkingDayId,
                    Type = TransactionType.HouseRake,
                    Amount = t.RebuyConfig.RebuyHouseFee,
                    PaymentMethod = ParsePaymentMethod(paymentMethod),
                    Bank = ParsePaymentProvider(bank),
                    PaymentReference = reference,
                    Description = "Rake Rebuy",
                    Timestamp = DateTime.UtcNow
                });
            }

            t.PrizePool += t.RebuyConfig.RebuyCost;
            reg.PaidAmount += (t.RebuyConfig.RebuyCost + t.RebuyConfig.RebuyHouseFee);

            var seatResult = AssignSmartSeat(t, reg);
            await _repo.UpdateAsync(t);

            return new RegistrationResult { Registration = reg, SystemMessage = $"Rebuy Exitoso: {seatResult.Message}" };
        }

        // -------------------------------------------------------------
        // ADD-ON (NUEVO MÉTODO)
        // -------------------------------------------------------------
        public async Task<RegistrationResult?> AddOnPlayerAsync(Guid tournamentId, Guid registrationId, string paymentMethod, string? bank = null, string? reference = null)
        {
            var t = await _repo.GetByIdAsync(tournamentId);
            if (t == null) return null;

            // Validar Reglas de Addon
            if (!t.AddonConfig.Enabled) return null;
            // Generalmente el addon es en el break del nivel X, o hasta el nivel X
            if (t.AddonConfig.AllowedLevel > 0 && t.CurrentLevel > t.AddonConfig.AllowedLevel) return null;

            EnsureListsInitialized(t);

            var reg = t.Registrations.FirstOrDefault(r => r.Id == registrationId);
            if (reg == null || reg.Status != RegistrationStatus.Active) return null; // Solo activos pueden hacer addon

            // Sumar fichas
            reg.Chips += t.AddonConfig.AddonChips;

            // Transacción
            var tx = new TournamentTransaction
            {
                Id = Guid.NewGuid(),
                TournamentId = t.Id,
                WorkingDayId = t.WorkingDayId,
                PlayerId = reg.Id,
                Type = TransactionType.AddOn, // Enum
                Amount = t.AddonConfig.AddonCost,
                PaymentMethod = ParsePaymentMethod(paymentMethod),
                Bank = ParsePaymentProvider(bank),
                PaymentReference = reference,
                Description = "Add-On",
                Timestamp = DateTime.UtcNow
            };
            t.Transactions.Add(tx);

            if (t.AddonConfig.AddonHouseFee > 0)
            {
                t.Transactions.Add(new TournamentTransaction
                {
                    Id = Guid.NewGuid(),
                    TournamentId = t.Id,
                    WorkingDayId = t.WorkingDayId,
                    Type = TransactionType.HouseRake,
                    Amount = t.AddonConfig.AddonHouseFee,
                    PaymentMethod = ParsePaymentMethod(paymentMethod),
                    Bank = ParsePaymentProvider(bank),
                    PaymentReference = reference,
                    Description = "Rake Add-On",
                    Timestamp = DateTime.UtcNow
                });
            }

            t.PrizePool += t.AddonConfig.AddonCost;
            reg.PaidAmount += (t.AddonConfig.AddonCost + t.AddonConfig.AddonHouseFee);

            await _repo.UpdateAsync(t);

            return new RegistrationResult { Registration = reg, SystemMessage = "Add-On Procesado con éxito" };
        }

        // -------------------------------------------------------------
        // VENTAS RESTAURANTE / SERVICIOS (NUEVO MÉTODO)
        // -------------------------------------------------------------
        public async Task<TournamentTransaction?> RecordServiceSaleAsync(Guid tournamentId, Guid? playerId, decimal amount, string description, Dictionary<string, object> items, string paymentMethod, string? bank = null, string? reference = null)
        {
            var t = await _repo.GetByIdAsync(tournamentId);
            if (t == null) return null;
            EnsureListsInitialized(t);

            var tx = new TournamentTransaction
            {
                Id = Guid.NewGuid(),
                TournamentId = t.Id,
                WorkingDayId = t.WorkingDayId,
                PlayerId = playerId, // Puede ser null si es venta a público general
                Type = TransactionType.ServiceSale, // Enum
                Amount = amount,
                PaymentMethod = ParsePaymentMethod(paymentMethod),
                Bank = ParsePaymentProvider(bank),
                PaymentReference = reference,
                Description = description, // Ej: "Cena + Bebidas"
                Metadata = items,          // Ej: { "Hamburguesa": 1, "CocaCola": 2 }
                Timestamp = DateTime.UtcNow
            };

            t.Transactions.Add(tx);
            // Nota: Las ventas de restaurante NO suman al PrizePool del torneo, 
            // pero sí suman a la caja del WorkingDay (vía la transacción).

            await _repo.UpdateAsync(t);
            return tx;
        }

        // -------------------------------------------------------------
        // OTROS MÉTODOS
        // -------------------------------------------------------------

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
            t.ActivePlayers--;

            CleanupEmptyTables(t);
            var balanceResult = CheckForTableBalancing(t);
            await _repo.UpdateAsync(t);
            return balanceResult;
        }

        public async Task<TournamentRegistration?> AssignSeatAsync(Guid tournamentId, Guid regId, string tableId, string seatId)
        {
            var t = await _repo.GetByIdAsync(tournamentId);
            if (t == null) return null;
            EnsureListsInitialized(t);

            var reg = t.Registrations.FirstOrDefault(r => r.Id == regId);
            if (reg == null) return null;

            reg.TableId = tableId;
            reg.SeatId = seatId;
            if (t.Tables != null) CleanupEmptyTables(t);

            await _repo.UpdateAsync(t);
            return reg;
        }

        public async Task<Tournament?> StartTournamentAsync(Guid id)
        {
            var t = await _repo.GetByIdAsync(id);
            if (t == null) return null;
            t.StartTime = DateTime.UtcNow;
            t.CurrentLevel = 1;
            t.Status = TournamentStatus.Running;
            return await _repo.UpdateAsync(t);
        }

        public async Task<TournamentState?> GetTournamentStateAsync(Guid id)
        {
            var t = await _repo.GetByIdAsync(id);
            if (t == null) return null;

            var levels = t.Levels ?? new List<BlindLevel>();
            int regCount = t.Registrations?.Count ?? t.TotalEntries;

            if (!t.StartTime.HasValue)
                return new TournamentState { CurrentLevel = t.CurrentLevel, TimeRemaining = 0, Status = t.Status, RegisteredCount = regCount, PrizePool = t.PrizePool };

            var elapsedMs = (DateTime.UtcNow - t.StartTime.Value).TotalMilliseconds;
            int currentLevel = 1;
            double levelStartMs = 0;
            double timeRemaining = 0;
            bool levelFound = false;

            foreach (var lvl in levels.OrderBy(l => l.LevelNumber))
            {
                double durationMs = lvl.DurationSeconds * 1000;
                if (elapsedMs < levelStartMs + durationMs)
                {
                    timeRemaining = (levelStartMs + durationMs - elapsedMs) / 1000;
                    currentLevel = lvl.LevelNumber;
                    levelFound = true;
                    break;
                }
                levelStartMs += durationMs;
            }

            if (!levelFound) { currentLevel = levels.Count + 1; timeRemaining = 0; }

            if (t.CurrentLevel != currentLevel)
            {
                t.CurrentLevel = currentLevel;
                if (t.Status != TournamentStatus.Finished) await _repo.UpdateAsync(t);
            }

            return new TournamentState { CurrentLevel = currentLevel, TimeRemaining = (int)Math.Ceiling(timeRemaining), Status = t.Status, RegisteredCount = regCount, PrizePool = t.PrizePool };
        }

        public async Task<TournamentTransaction?> RecordTransactionAsync(Guid tournamentId, TournamentTransaction transaction)
        {
            var t = await _repo.GetByIdAsync(tournamentId);
            if (t == null) return null;
            EnsureListsInitialized(t);

            transaction.Id = Guid.NewGuid();
            transaction.TournamentId = tournamentId;
            transaction.WorkingDayId = t.WorkingDayId;
            transaction.Timestamp = DateTime.UtcNow;

            t.Transactions.Add(transaction);
            await _repo.UpdateAsync(t);
            return transaction;
        }

        public async Task<decimal> GetTotalPrizePoolAsync(Guid tournamentId)
        {
            var t = await _repo.GetByIdAsync(tournamentId);
            return t?.PrizePool ?? 0;
        }

        // =============================================================
        // HELPERS
        // =============================================================

        private void EnsureListsInitialized(Tournament t)
        {
            if (t.Tables == null) t.Tables = new List<TournamentTable>();
            if (t.Registrations == null) t.Registrations = new List<TournamentRegistration>();
            if (t.Transactions == null) t.Transactions = new List<TournamentTransaction>();
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
                    Status = TournamentTableStatus.Active
                });
            }
        }

        private (string? InstructionType, string Message) AssignSmartSeat(Tournament t, TournamentRegistration reg)
        {
            var activeTables = t.Tables.Where(tb => tb.Status == TournamentTableStatus.Active).ToList();
            var activePlayers = t.Registrations.Where(r => r.Status == RegistrationStatus.Active).ToList();
            int seatsPerTable = t.Seating.SeatsPerTable > 0 ? t.Seating.SeatsPerTable : 9;
            int currentCapacity = activeTables.Count * seatsPerTable;
            int totalActiveWithNew = activePlayers.Count + (activePlayers.Contains(reg) ? 0 : 1);

            if (totalActiveWithNew > currentCapacity)
            {
                int newNum = t.Tables.Max(x => x.TableNumber) + 1;
                var newTable = new TournamentTable { Id = Guid.NewGuid(), TournamentId = t.Id, TableNumber = newNum, Name = $"Mesa {newNum}", Status = TournamentTableStatus.Active };
                t.Tables.Add(newTable);
                activeTables.Add(newTable);

                var crowdedTable = activeTables.OrderByDescending(tb => activePlayers.Count(p => p.TableId == tb.Id.ToString())).First();
                var victims = activePlayers.Where(p => p.TableId == crowdedTable.Id.ToString()).OrderByDescending(p => p.RegisteredAt).Take(activePlayers.Count(p => p.TableId == crowdedTable.Id.ToString()) / 2).ToList();

                foreach (var v in victims) { v.TableId = newTable.Id.ToString(); v.SeatId = null; }
                reg.TableId = newTable.Id.ToString();
                int s = 1;
                foreach (var p in victims.Append(reg)) { p.SeatId = s.ToString(); s++; }

                return ("INFO_ALERT", $"Se abrió Mesa {newNum} y se movieron jugadores.");
            }

            var targetTable = activeTables.OrderBy(tb => activePlayers.Count(p => p.TableId == tb.Id.ToString())).FirstOrDefault(tb => activePlayers.Count(p => p.TableId == tb.Id.ToString()) < seatsPerTable) ?? activeTables.First();
            reg.TableId = targetTable.Id.ToString();
            var usedSeats = activePlayers.Where(p => p.TableId == targetTable.Id.ToString() && p.SeatId != null).Select(p => int.Parse(p.SeatId!)).ToList();
            int freeSeat = 1;
            while (usedSeats.Contains(freeSeat)) freeSeat++;
            reg.SeatId = freeSeat.ToString();

            return (null, $"Asignado a {targetTable.Name}, Puesto {freeSeat}");
        }

        private RemoveResult CheckForTableBalancing(Tournament t)
        {
            var activeTables = t.Tables.Where(tb => tb.Status == TournamentTableStatus.Active).ToList();
            if (activeTables.Count < 2) return new RemoveResult { Success = true };
            var activePlayers = t.Registrations.Where(r => r.Status == RegistrationStatus.Active).ToList();
            var counts = activeTables.Select(tb => new { Table = tb, Count = activePlayers.Count(p => p.TableId == tb.Id.ToString()) }).ToList();
            var max = counts.MaxBy(x => x.Count);
            var min = counts.MinBy(x => x.Count);

            if (max != null && min != null && (max.Count - min.Count) >= 2)
            {
                return new RemoveResult { Success = true, InstructionType = "BALANCE_REQUIRED", Message = $"Desbalance detectado. Mover de {max.Table.Name} a {min.Table.Name}", FromTable = max.Table.Id.ToString(), ToTable = min.Table.Id.ToString() };
            }
            return new RemoveResult { Success = true };
        }

        private void CleanupEmptyTables(Tournament t)
        {
            var activeTables = t.Tables.Where(tb => tb.Status == TournamentTableStatus.Active).ToList();
            if (activeTables.Count <= 1) return;
            foreach (var tb in activeTables)
            {
                if (!t.Registrations.Any(r => r.TableId == tb.Id.ToString() && r.Status == RegistrationStatus.Active))
                {
                    tb.Status = TournamentTableStatus.Broken; tb.ClosedAt = DateTime.UtcNow;
                }
            }
        }

        private void CalculateFixedPayouts(Tournament t)
        {
            decimal totalPrize = Math.Max(t.Guaranteed, t.PrizePool);
            if (totalPrize <= 0 || t.Payouts == null || t.Payouts.Sum(p => p.Percentage) != 100) return;
            foreach (var tier in t.Payouts) tier.FixedAmount = (tier.Percentage / 100m) * totalPrize;
        }

        private PaymentMethod ParsePaymentMethod(string method)
        {
            if (Enum.TryParse<PaymentMethod>(method, true, out var result)) return result;
            return PaymentMethod.Cash;
        }

        // Helper para convertir string a Enum nullable
        private PaymentProvider? ParsePaymentProvider(string? provider)
        {
            if (string.IsNullOrEmpty(provider)) return null;
            if (Enum.TryParse<PaymentProvider>(provider, true, out var result)) return result;
            return null;
        }
    }
}