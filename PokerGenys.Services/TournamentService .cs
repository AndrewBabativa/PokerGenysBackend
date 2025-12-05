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
            // Inicializar ClockState
            tournament.ClockState = new TournamentClockState
            {
                IsPaused = true,
                SecondsRemaining = 0 // Se calculará al iniciar
            };

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
                PaymentMethod = ParsePaymentMethod(paymentMethod),
                RegistrationType = RegistrationType.Standard
            };

            var seatResult = AssignSmartSeat(t, reg);

            // Log Transaction
            RecordInternalTransaction(t, reg.Id, TransactionType.BuyIn, t.BuyIn, reg.PaymentMethod, bank, reference, $"Buy-In: {reg.PlayerName}");
            if (t.Fee > 0)
                RecordInternalTransaction(t, null, TransactionType.HouseRake, t.Fee, reg.PaymentMethod, bank, reference, "Rake Buy-In");

            t.PrizePool += t.BuyIn;
            t.TotalEntries++;
            t.ActivePlayers++;
            t.Registrations.Add(reg);

            await _repo.UpdateAsync(t);

            return new RegistrationResult
            {
                Registration = reg,
                NewStats = new TournamentStatsDto
                {
                    Entries = t.TotalEntries,
                    Active = t.ActivePlayers,
                    PrizePool = t.PrizePool
                }
            };
        }

        public async Task<RegistrationResult?> RebuyPlayerAsync(Guid tournamentId, Guid registrationId, string paymentMethod, string? bank = null, string? reference = null)
        {
            var t = await _repo.GetByIdAsync(tournamentId);
            if (t == null) return null;

            if (t.RebuyConfig.UntilLevel > 0 && t.CurrentLevel > t.RebuyConfig.UntilLevel) return null;
            if (!t.RebuyConfig.Enabled) return null;

            EnsureListsInitialized(t);
            var reg = t.Registrations.FirstOrDefault(r => r.Id == registrationId);
            if (reg == null) return null;

            reg.Status = RegistrationStatus.Active;
            reg.EliminatedAt = null;
            reg.Chips = t.RebuyConfig.RebuyChips;
            t.ActivePlayers++;

            RecordInternalTransaction(t, reg.Id, TransactionType.ReBuy, t.RebuyConfig.RebuyCost, ParsePaymentMethod(paymentMethod), bank, reference, $"Rebuy Nivel {t.CurrentLevel}");
            if (t.RebuyConfig.RebuyHouseFee > 0)
                RecordInternalTransaction(t, null, TransactionType.HouseRake, t.RebuyConfig.RebuyHouseFee, ParsePaymentMethod(paymentMethod), bank, reference, "Rake Rebuy");

            t.PrizePool += t.RebuyConfig.RebuyCost;
            reg.PaidAmount += (t.RebuyConfig.RebuyCost + t.RebuyConfig.RebuyHouseFee);

            var seatResult = AssignSmartSeat(t, reg);
            await _repo.UpdateAsync(t);

            return new RegistrationResult { Registration = reg, SystemMessage = $"Rebuy Exitoso: {seatResult.Message}" };
        }

        public async Task<RegistrationResult?> AddOnPlayerAsync(Guid tournamentId, Guid registrationId, string paymentMethod, string? bank = null, string? reference = null)
        {
            var t = await _repo.GetByIdAsync(tournamentId);
            if (t == null) return null;
            if (!t.AddonConfig.Enabled) return null;
            if (t.AddonConfig.AllowedLevel > 0 && t.CurrentLevel > t.AddonConfig.AllowedLevel) return null;

            EnsureListsInitialized(t);
            var reg = t.Registrations.FirstOrDefault(r => r.Id == registrationId);
            if (reg == null || reg.Status != RegistrationStatus.Active) return null;

            reg.Chips += t.AddonConfig.AddonChips;

            RecordInternalTransaction(t, reg.Id, TransactionType.AddOn, t.AddonConfig.AddonCost, ParsePaymentMethod(paymentMethod), bank, reference, "Add-On");
            if (t.AddonConfig.AddonHouseFee > 0)
                RecordInternalTransaction(t, null, TransactionType.HouseRake, t.AddonConfig.AddonHouseFee, ParsePaymentMethod(paymentMethod), bank, reference, "Rake Add-On");

            t.PrizePool += t.AddonConfig.AddonCost;
            reg.PaidAmount += (t.AddonConfig.AddonCost + t.AddonConfig.AddonHouseFee);

            await _repo.UpdateAsync(t);
            return new RegistrationResult { Registration = reg, SystemMessage = "Add-On Procesado con éxito" };
        }

        // =============================================================
        // 3. CONTROL DE JUEGO (OPTIMIZADO)
        // =============================================================

        public async Task<Tournament?> StartTournamentAsync(Guid id)
        {
            var t = await _repo.GetByIdAsync(id);
            if (t == null) return null;

            // Si es la primera vez que inicia
            if (!t.StartTime.HasValue)
            {
                t.StartTime = DateTime.UtcNow;
                t.CurrentLevel = 1;
                // Cargar tiempo del nivel 1
                var level1 = t.Levels.FirstOrDefault(l => l.LevelNumber == 1);
                t.ClockState.SecondsRemaining = level1 != null ? level1.DurationSeconds : 1200; // Default 20min
            }

            // Lógica de RESUME (Reanudar)
            t.Status = TournamentStatus.Running;
            t.ClockState.IsPaused = false;
            t.ClockState.LastUpdatedAt = DateTime.UtcNow; // Marcamos cuándo arrancó el reloj

            return await _repo.UpdateAsync(t);
        }

        public async Task<Tournament?> PauseTournamentAsync(Guid id)
        {
            var t = await _repo.GetByIdAsync(id);
            if (t == null) return null;

            if (t.Status == TournamentStatus.Running && t.ClockState.LastUpdatedAt.HasValue)
            {
                // Calcular tiempo transcurrido desde el último inicio
                var elapsedSeconds = (DateTime.UtcNow - t.ClockState.LastUpdatedAt.Value).TotalSeconds;

                // Restar al tiempo que quedaba
                t.ClockState.SecondsRemaining = Math.Max(0, t.ClockState.SecondsRemaining - elapsedSeconds);

                t.Status = TournamentStatus.Paused;
                t.ClockState.IsPaused = true;
                t.ClockState.LastUpdatedAt = DateTime.UtcNow;

                await _repo.UpdateAsync(t);
            }

            return t;
        }

        public async Task<TournamentState?> GetTournamentStateAsync(Guid id)
        {
            var t = await _repo.GetByIdAsync(id);
            if (t == null) return null;

            // Lógica de cálculo en tiempo real
            double timeRemaining = t.ClockState?.SecondsRemaining ?? 0;
            int currentLevel = t.CurrentLevel;

            // Si está corriendo, calculamos el delta en vivo sin guardar en BD (solo lectura)
            if (t.Status == TournamentStatus.Running && t.ClockState?.LastUpdatedAt != null)
            {
                var elapsedSinceLastUpdate = (DateTime.UtcNow - t.ClockState.LastUpdatedAt.Value).TotalSeconds;
                timeRemaining = Math.Max(0, t.ClockState.SecondsRemaining - elapsedSinceLastUpdate);

                // Auto-Level-Up Logic (Solo lectura, si llega a 0 el frontend o un job deberían llamar a AdvanceLevel)
                if (timeRemaining <= 0)
                {
                    // Nota: Aquí podrías implementar la lógica para cambiar de nivel automáticamente
                    // Por ahora devolvemos 0 para que el cliente sepa que acabó el nivel
                    timeRemaining = 0;
                }
            }

            return new TournamentState
            {
                CurrentLevel = currentLevel,
                TimeRemaining = (int)Math.Ceiling(timeRemaining),
                Status = t.Status,
                RegisteredCount = t.Registrations?.Count ?? t.TotalEntries,
                PrizePool = t.PrizePool
            };
        }

        // =============================================================
        // 4. VENTAS Y TRANSACCIONES
        // =============================================================

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
                PlayerId = playerId,
                Type = TransactionType.ServiceSale,
                Amount = amount,
                PaymentMethod = ParsePaymentMethod(paymentMethod),
                Bank = ParsePaymentProvider(bank),
                PaymentReference = reference,
                Description = description,
                Metadata = items,
                Timestamp = DateTime.UtcNow
            };

            t.Transactions.Add(tx);
            await _repo.UpdateAsync(t);
            return tx;
        }

        public async Task<TournamentTransaction?> RecordTransactionAsync(Guid tournamentId, TournamentTransaction transaction)
        {
            // Implementación genérica simple
            var t = await _repo.GetByIdAsync(tournamentId);
            if (t == null) return null;
            EnsureListsInitialized(t);
            transaction.Id = Guid.NewGuid();
            transaction.Timestamp = DateTime.UtcNow;
            t.Transactions.Add(transaction);
            await _repo.UpdateAsync(t);
            return transaction;
        }

        // =============================================================
        // OTROS Y HELPERS
        // =============================================================

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
            CleanupEmptyTables(t);
            await _repo.UpdateAsync(t);
            return reg;
        }

        public Task<decimal> GetTotalPrizePoolAsync(Guid id) => _repo.GetByIdAsync(id).ContinueWith(t => t.Result?.PrizePool ?? 0);

        // --- HELPERS PRIVADOS ---

        private void RecordInternalTransaction(Tournament t, Guid? playerId, TransactionType type, decimal amount, PaymentMethod method, string? bank, string? refId, string desc)
        {
            t.Transactions.Add(new TournamentTransaction
            {
                Id = Guid.NewGuid(),
                TournamentId = t.Id,
                WorkingDayId = t.WorkingDayId,
                PlayerId = playerId,
                Type = type,
                Amount = amount,
                PaymentMethod = method,
                Bank = ParsePaymentProvider(bank),
                PaymentReference = refId,
                Description = desc,
                Timestamp = DateTime.UtcNow
            });
        }

        private void EnsureListsInitialized(Tournament t)
        {
            if (t.Tables == null) t.Tables = new List<TournamentTable>();
            if (t.Registrations == null) t.Registrations = new List<TournamentRegistration>();
            if (t.Transactions == null) t.Transactions = new List<TournamentTransaction>();
            if (t.ClockState == null) t.ClockState = new TournamentClockState(); // Asegurar ClockState
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
            var activePlayers = t.Registrations.Where(r => r.Status == RegistrationStatus.Active).ToList();
            var activeTables = t.Tables.Where(tb => tb.Status == TournamentTableStatus.Active).ToList();

            int seatsPerTable = t.Seating.SeatsPerTable > 0 ? t.Seating.SeatsPerTable : 9;

            // 1. Verificar si necesitamos abrir una mesa nueva
            // (Si la capacidad actual está al 100% llena)
            int currentCapacity = activeTables.Count * seatsPerTable;
            // Contamos los activos + el nuevo que va a entrar
            int totalActiveWithNew = activePlayers.Count + (activePlayers.Any(x => x.Id == reg.Id) ? 0 : 1);

            if (totalActiveWithNew > currentCapacity)
            {
                // LÓGICA DE APERTURA DE MESA Y BALANCEO
                int nextNum = t.Tables.Any() ? t.Tables.Max(x => x.TableNumber) + 1 : 1;
                var newTable = new TournamentTable
                {
                    Id = Guid.NewGuid(),
                    TournamentId = t.Id,
                    TableNumber = nextNum,
                    Name = $"Mesa {nextNum}",
                    Status = TournamentTableStatus.Active
                };
                t.Tables.Add(newTable);
                activeTables.Add(newTable);

                // Movemos a la mitad de la mesa más llena para balancear
                // (Estrategia simple: Tomar la mesa con más gente y mover la mitad)
                var fullestTable = activeTables
                    .OrderByDescending(tb => activePlayers.Count(p => p.TableId == tb.Id.ToString()))
                    .First();

                var victims = activePlayers
                    .Where(p => p.TableId == fullestTable.Id.ToString())
                    .OrderBy(x => Guid.NewGuid()) // Randomizar quién se mueve
                    .Take(activePlayers.Count(p => p.TableId == fullestTable.Id.ToString()) / 2)
                    .ToList();

                // Asignar víctimas a la nueva mesa (Sillas 1, 2, 3...)
                int seatCounter = 1;
                foreach (var v in victims)
                {
                    v.TableId = newTable.Id.ToString();
                    v.SeatId = seatCounter.ToString();
                    seatCounter++;
                }

                // Asignar al NUEVO jugador a la nueva mesa también
                reg.TableId = newTable.Id.ToString();
                reg.SeatId = seatCounter.ToString();

                return ("INFO_ALERT", $"Se abrió la Mesa {nextNum} y se balancearon jugadores.");
            }

            // 2. Asignación Normal (Buscar hueco en mesas existentes)
            // Buscamos la mesa con más huecos para mantener balance, o simplemente la primera con espacio
            var targetTable = activeTables
                .OrderBy(tb => activePlayers.Count(p => p.TableId == tb.Id.ToString())) // Llenar la más vacía primero
                .FirstOrDefault(tb => activePlayers.Count(p => p.TableId == tb.Id.ToString()) < seatsPerTable);

            if (targetTable == null) return (null, "Error crítico: No hay mesas disponibles.");

            // 3. ENCONTRAR EL PRIMER 'SEAT_ID' DISPONIBLE (Esto arregla la superposición)
            var occupiedSeats = activePlayers
                .Where(p => p.TableId == targetTable.Id.ToString() && int.TryParse(p.SeatId, out _))
                .Select(p => int.Parse(p.SeatId!))
                .ToHashSet();

            int freeSeat = 1;
            while (occupiedSeats.Contains(freeSeat))
            {
                freeSeat++;
            }

            reg.TableId = targetTable.Id.ToString();
            reg.SeatId = freeSeat.ToString();

            return (null, $"Asignado a {targetTable.Name}, Silla {freeSeat}");
        }

        private void CleanupEmptyTables(Tournament t) { /* Tu lógica existente */ }
        private RemoveResult CheckForTableBalancing(Tournament t) { return new RemoveResult { Success = true }; }

        private void CalculateFixedPayouts(Tournament t)
        {
            decimal totalPrize = Math.Max(t.Guaranteed, t.PrizePool);
            if (totalPrize <= 0 || t.Payouts == null) return;
            foreach (var tier in t.Payouts) tier.FixedAmount = (tier.Percentage / 100m) * totalPrize;
        }

        private PaymentMethod ParsePaymentMethod(string method)
        {
            return Enum.TryParse<PaymentMethod>(method, true, out var result) ? result : PaymentMethod.Cash;
        }

        private PaymentProvider? ParsePaymentProvider(string? provider)
        {
            if (string.IsNullOrEmpty(provider)) return null;
            return Enum.TryParse<PaymentProvider>(provider, true, out var result) ? result : null;
        }

        public async Task<TournamentStatsDto?> GetTournamentStatsAsync(Guid id)
        {
            var t = await _repo.GetByIdAsync(id);
            if (t == null) return null;

            return new TournamentStatsDto
            {
                Entries = t.TotalEntries,
                Active = t.ActivePlayers,
                PrizePool = t.PrizePool
            };
        }
    }
}