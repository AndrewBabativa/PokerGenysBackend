using PokerGenys.Domain.Models;
using PokerGenys.Domain.Models.Tournaments;
using PokerGenys.Infrastructure.Repositories;
using System.Reflection;

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
            tournament.ClockState = new ClockState
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
            if (t.Status == TournamentStatus.Running) return t;

            // Recuperar configuración del nivel actual
            var currentLevelConfig = t.Levels.FirstOrDefault(l => l.LevelNumber == t.CurrentLevel);
            double levelDuration = currentLevelConfig?.DurationSeconds ?? 0;

            // Corrección: Si el tiempo es 0 o negativo (recién creado o nivel acabado), reiniciamos al tiempo del nivel
            if (t.ClockState.SecondsRemaining <= 0.1)
            {
                t.ClockState.SecondsRemaining = levelDuration;
            }

            // MARCA DE TIEMPO UTC REAL
            t.Status = TournamentStatus.Running;
            t.ClockState.IsPaused = false;
            t.ClockState.LastUpdatedAt = DateTime.UtcNow; // Punto de anclaje para todos los clientes

            if (!t.StartTime.HasValue) t.StartTime = DateTime.UtcNow;

            return await _repo.UpdateAsync(t);
        }

        public async Task<Tournament?> PauseTournamentAsync(Guid id)
        {
            var t = await _repo.GetByIdAsync(id);
            if (t == null) return null;

            // 1. "Congelar" el tiempo: Calcular cuánto sobraba exactamente ahora mismo
            if (!t.ClockState.IsPaused && t.ClockState.LastUpdatedAt.HasValue)
            {
                var elapsed = (DateTime.UtcNow - t.ClockState.LastUpdatedAt.Value).TotalSeconds;
                t.ClockState.SecondsRemaining -= elapsed;

                // Protección contra negativos visuales
                if (t.ClockState.SecondsRemaining < 0) t.ClockState.SecondsRemaining = 0;
            }

            t.Status = TournamentStatus.Paused;
            t.ClockState.IsPaused = true;
            t.ClockState.LastUpdatedAt = DateTime.UtcNow; // Referencia de cuándo se pausó

            return await _repo.UpdateAsync(t);
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

        // ============================================================
        // LÓGICA DE ELIMINACIÓN Y GESTIÓN DE ESTRUCTURA
        // ============================================================
        public async Task<RemoveResult> RemoveRegistrationAsync(Guid tournamentId, Guid regId)
        {
            var t = await _repo.GetByIdAsync(tournamentId);
            if (t == null) return new RemoveResult { Success = false };
            EnsureListsInitialized(t);

            var player = t.Registrations.FirstOrDefault(r => r.Id == regId);
            if (player == null) return new RemoveResult { Success = false };

            // 1. Marcar como Eliminado y guardar timestamp
            player.Status = RegistrationStatus.Eliminated;
            player.EliminatedAt = DateTime.UtcNow;
            player.TableId = null;
            player.SeatId = null;

            // Actualizar contadores
            t.ActivePlayers = t.Registrations.Count(r => r.Status == RegistrationStatus.Active);

            // 2. Limpiar mesas vacías (Regla de negocio)
            CleanupEmptyTables(t);

            // 3. Verificar eventos importantes (Mesa Final o Ganador)
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
            if (t.ClockState == null) t.ClockState = new ClockState(); // Asegurar ClockState
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

        private void CleanupEmptyTables(Tournament t)
        {
            var activeTableIds = t.Registrations
                .Where(r => r.Status == RegistrationStatus.Active && !string.IsNullOrEmpty(r.TableId))
                .Select(r => r.TableId)
                .Distinct()
                .ToList();

            foreach (var table in t.Tables.Where(tb => tb.Status == TournamentTableStatus.Active).ToList())
            {
                if (!activeTableIds.Contains(table.Id.ToString()))
                {
                    table.Status = TournamentTableStatus.Broken; // O "Closed"
                }
            }
        }

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

        public async Task<TournamentState?> GetTournamentStateAsync(Guid id)
        {
            var t = await _repo.GetByIdAsync(id);
            if (t == null) return null;

            double projectedSeconds = t.ClockState.SecondsRemaining;
            bool needsUpdate = false;

            // LÓGICA DE PROYECCIÓN (No guardamos en BD cada segundo, solo calculamos)
            if (t.Status == TournamentStatus.Running && !t.ClockState.IsPaused && t.ClockState.LastUpdatedAt.HasValue)
            {
                var elapsed = (DateTime.UtcNow - t.ClockState.LastUpdatedAt.Value).TotalSeconds;
                projectedSeconds -= elapsed;

                // SI EL TIEMPO SE ACABÓ -> Cambiar Nivel (Aquí sí escribimos en BD)
                if (projectedSeconds <= 0)
                {
                    AdvanceLevelInternal(t); // Esto resetea SecondsRemaining al nuevo nivel
                    projectedSeconds = t.ClockState.SecondsRemaining; // Actualizar proyección
                    needsUpdate = true;
                }
            }

            if (needsUpdate)
            {
                t.ClockState.LastUpdatedAt = DateTime.UtcNow; // Nuevo anclaje para el nuevo nivel
                await _repo.UpdateAsync(t);
            }

            var IsFinalTable = t.ActivePlayers <= t.Seating.FinalTableSize ? true : false;

            return new TournamentState
            {
                CurrentLevel = t.CurrentLevel,
                TimeRemaining = (int)Math.Max(0, Math.Ceiling(projectedSeconds)), // Enviar siempre positivo
                Status = t.Status,
                RegisteredCount = t.TotalEntries,
                PrizePool = t.PrizePool,
                Blinds = GetBlindsInfo(t),
                IsFinalTable = IsFinalTable
            };
        }

        private RemoveResult CheckForStructureEvents(Tournament t)
        {
            var activePlayers = t.Registrations.Where(r => r.Status == RegistrationStatus.Active).ToList();
            var activeTables = t.Tables.Where(tb => tb.Status == TournamentTableStatus.Active || tb.Status == TournamentTableStatus.FinalTable).ToList(); // IMPORTANTE: Incluir FinalTable en la cuenta

            int ftSize = t.Seating.FinalTableSize > 0 ? t.Seating.FinalTableSize : 9;

            // CASO: MESA FINAL
            // Solo entramos si hay más de 1 mesa activa y llegamos al número de jugadores
            if (activePlayers.Count <= ftSize)
            {
                // 1. Elegir la mesa destino (La mesa 1, o reutilizar una existente)
                var finalTable = t.Tables.OrderBy(tb => tb.TableNumber).FirstOrDefault(tb => tb.Status != TournamentTableStatus.Broken);
                if (activePlayers.Count == 1)
                {
                    finalTable.Name = "Mesa Final";
                    finalTable.Status = TournamentTableStatus.Finished;
                    return new RemoveResult
                    {
                        Success = true,
                        InstructionType = "FINAL_TABLE_FINISHED",
                        Message = "¡Mesa Final Terminda!",
                        FromTable = finalTable.Id.ToString(),
                        Data = new
                        {
                            winnerName = activePlayers[0].PlayerName
                        }
                    };
                }

                if (finalTable == null) return new RemoveResult { Success = true };

                finalTable.Name = "Mesa Final";
                finalTable.Status = TournamentTableStatus.FinalTable; // Ojo con este estado en el Frontend

                // 2. Romper las demás mesas
                foreach (var table in activeTables.Where(tb => tb.Id != finalTable.Id))
                {
                    table.Status = TournamentTableStatus.Broken;
                }

                // 3. Reseat aleatorio
                var rng = new Random();
                var shuffledPlayers = activePlayers.OrderBy(x => rng.Next()).ToList();
                int seat = 1;

                foreach (var p in shuffledPlayers)
                {
                    p.TableId = finalTable.Id.ToString();
                    p.SeatId = seat.ToString();
                    seat++;
                }

                return new RemoveResult
                {
                    Success = true,
                    InstructionType = "FINAL_TABLE_START",
                    Message = "¡Mesa Final Formada!",
                    FromTable = finalTable.Id.ToString()
                };
            }

            return new RemoveResult { Success = true };
        }

        private void UpdateClockStateInternal(Tournament t)
        {
            if (t.Status != TournamentStatus.Running || !t.ClockState.LastUpdatedAt.HasValue) return;

            var now = DateTime.UtcNow;
            var elapsed = (now - t.ClockState.LastUpdatedAt.Value).TotalSeconds;

            // Restamos lo que ha pasado
            t.ClockState.SecondsRemaining -= elapsed;

            // Actualizamos la marca de tiempo para que la próxima resta sea pequeña
            t.ClockState.LastUpdatedAt = now;
        }

        private void AdvanceLevelInternal(Tournament t)
        {
            var nextLevelNum = t.CurrentLevel + 1;
            var nextLevelConfig = t.Levels.FirstOrDefault(l => l.LevelNumber == nextLevelNum);

            if (nextLevelConfig != null)
            {
                t.CurrentLevel = nextLevelNum;
                t.ClockState.SecondsRemaining = nextLevelConfig.DurationSeconds;
                // No cambiamos IsPaused, sigue corriendo
            }
            else
            {
                // Fin del torneo o estructura
                t.ClockState.SecondsRemaining = 0;
                t.Status = TournamentStatus.Paused;
                t.ClockState.IsPaused = true;
            }
        }

        private string GetBlindsInfo(Tournament t)
        {
            var l = t.Levels.FirstOrDefault(x => x.LevelNumber == t.CurrentLevel);
            return l != null ? $"{l.SmallBlind}/{l.BigBlind}" : "-/-";
        }

    }
}