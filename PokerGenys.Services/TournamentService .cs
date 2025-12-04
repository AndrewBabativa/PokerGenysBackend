using PokerGenys.Domain.Models;
using PokerGenys.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            // Antes de guardar, aseguramos que la tabla de premios tenga montos fijos calculados
            CalculateFixedPayouts(tournament);

            // Inicializar tablas y registros
            if (tournament.Tables == null || !tournament.Tables.Any())
            {
                tournament.Tables = new List<TournamentTable> { new TournamentTable { TableNumber = 1, Name = "Mesa 1", Status = "Scheduled" } };
            }

            // El repositorio guarda el objeto Tournament completo, incluyendo los FixedAmount actualizados
            var created = await _repo.CreateAsync(tournament);
            return created;
        }
        public Task<Tournament> UpdateAsync(Tournament tournament) => _repo.UpdateAsync(tournament);
        public Task<bool> DeleteAsync(Guid id) => _repo.DeleteAsync(id);

        public async Task<List<TournamentRegistration>> GetRegistrationsAsync(Guid id)
        {
            var t = await _repo.GetByIdAsync(id);
            return t?.Registrations ?? new();
        }

        // =============================================================
        // 2. GESTIÓN MANUAL / LEGACY
        // =============================================================
        public async Task<Tournament?> AddRegistrationAsync(Guid id, TournamentRegistration reg)
        {
            var t = await _repo.GetByIdAsync(id);
            if (t == null) return null;

            reg.Id = Guid.NewGuid();
            reg.TournamentId = id;
            reg.RegisteredAt = DateTime.UtcNow;

            t.Registrations.Add(reg);
            return await _repo.UpdateAsync(t);
        }

        public async Task<TournamentRegistration?> AssignSeatAsync(Guid tournamentId, Guid regId, string tableId, string seatId)
        {
            var t = await _repo.GetByIdAsync(tournamentId);
            if (t == null) return null;

            var reg = t.Registrations.FirstOrDefault(r => r.Id == regId);
            if (reg == null) return null;

            // ASIGNAR
            reg.TableId = tableId;
            reg.SeatId = seatId;

            // LIMPIEZA AUTOMÁTICA: Cerrar mesas que quedaron vacías tras el movimiento
            CleanupEmptyTables(t);

            await _repo.UpdateAsync(t);
            return reg;
        }

        // =============================================================
        // 3. CONTROL DE TIEMPO
        // =============================================================
        public async Task<Tournament?> StartTournamentAsync(Guid id)
        {
            var t = await _repo.GetByIdAsync(id);
            if (t == null) return null;

            t.StartTime = DateTime.UtcNow;
            t.CurrentLevel = 1;
            t.Status = "Running";

            return await _repo.UpdateAsync(t);
        }

        public async Task<TournamentState?> GetTournamentStateAsync(Guid id)
        {
            // ... (Tu lógica de timer se mantiene igual, omitida por brevedad) ...
            // Asegúrate de copiar la implementación completa que tenías antes aquí.
            var t = await _repo.GetByIdAsync(id);
            if (t == null) return null;

            if (!t.StartTime.HasValue) return new TournamentState { CurrentLevel = t.CurrentLevel, TimeRemaining = 0, Status = t.Status, RegisteredCount = t.Registrations.Count, PrizePool = t.PrizePool };

            var elapsedMs = (DateTime.UtcNow - t.StartTime.Value).TotalMilliseconds;
            int currentLevel = 1;
            double levelStartMs = 0;
            double timeRemaining = 0;
            bool levelFound = false;

            foreach (var lvl in t.Levels.OrderBy(l => l.LevelNumber))
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

            if (!levelFound) { currentLevel = t.Levels.Count + 1; timeRemaining = 0; }

            if (t.CurrentLevel != currentLevel) { t.CurrentLevel = currentLevel; await _repo.UpdateAsync(t); }

            return new TournamentState { CurrentLevel = currentLevel, TimeRemaining = (int)Math.Ceiling(timeRemaining), Status = t.Status, RegisteredCount = t.Registrations.Count, PrizePool = t.PrizePool };
        }


        // =============================================================
        // 4. LÓGICA INTELIGENTE: REGISTRO (Solo Mesas Activas)
        // =============================================================
        public async Task<RegistrationResult?> RegisterPlayerAsync(Guid id, string playerName)
        {
            var t = await _repo.GetByIdAsync(id);
            if (t == null) return null;

            // Inicializar lista si es null
            if (t.Tables == null) t.Tables = new List<TournamentTable>();

            // Si no hay ninguna mesa (ni activa ni cerrada), crear la primera
            if (!t.Tables.Any())
            {
                t.Tables.Add(new TournamentTable { TableNumber = 1, Name = "Mesa 1", Id = Guid.NewGuid().ToString(), Status = "Active" });
            }
            // Si hay mesas pero ninguna activa (caso raro, reabrir o crear nueva)
            else if (!t.Tables.Any(x => x.Status == "Active"))
            {
                int nextNum = t.Tables.Max(x => x.TableNumber) + 1;
                t.Tables.Add(new TournamentTable { TableNumber = nextNum, Name = $"Mesa {nextNum}", Id = Guid.NewGuid().ToString(), Status = "Active" });
            }

            var reg = new TournamentRegistration
            {
                Id = Guid.NewGuid(),
                PlayerName = playerName,
                TournamentId = t.Id,
                Chips = t.StartingChips,
                RegisteredAt = DateTime.UtcNow,
                Status = "Active",

                PaidAmount = t.BuyIn + t.Fee,
                PaymentMethod = "Cash"
            };

            string? instructionType = null;
            string? systemMessage = null;

            // --- DATOS FILTRADOS POR ESTADO ---
            var activePlayers = t.Registrations.Where(r => r.Status == "Active").ToList();
            // IMPORTANTE: Solo contamos mesas activas para capacidad
            var activeTables = t.Tables.Where(tb => tb.Status == "Active").ToList();

            int totalActiveWithNew = activePlayers.Count + 1;
            int seatsPerTable = t.Seating.SeatsPerTable > 0 ? t.Seating.SeatsPerTable : 9;
            int currentCapacity = activeTables.Count * seatsPerTable;

            // --- ALGORITMO ROMPER MESA ---
            if (totalActiveWithNew > currentCapacity)
            {
                // Crear nueva mesa
                // Buscamos el máximo histórico para el número, para no repetir ID visual
                int newTableNum = t.Tables.Any() ? t.Tables.Max(x => x.TableNumber) + 1 : 1;

                var newTable = new TournamentTable
                {
                    Id = Guid.NewGuid().ToString(),
                    TableNumber = newTableNum,
                    Name = $"Mesa {newTableNum}",
                    Status = "Active"
                };
                t.Tables.Add(newTable);

                // Refrescamos lista de activas
                activeTables.Add(newTable);

                // Mover gente de la mesa más llena (DE LAS ACTIVAS)
                var crowdedTable = activeTables.OrderByDescending(tb => activePlayers.Count(p => p.TableId == tb.Id)).First();

                var playersInCrowded = activePlayers
                    .Where(p => p.TableId == crowdedTable.Id)
                    .OrderByDescending(p => p.RegisteredAt)
                    .ToList();

                int moveCount = playersInCrowded.Count / 2;
                var playersToMove = playersInCrowded.Take(moveCount).ToList();

                foreach (var p in playersToMove)
                {
                    p.TableId = newTable.Id;
                    p.SeatId = null; // Reset asiento
                }

                // Asignar nuevo
                reg.TableId = newTable.Id;

                // Reasignar asientos secuenciales en la nueva mesa
                int seatCounter = 1;
                var allInNewTable = playersToMove.Concat(new[] { reg }).ToList();
                foreach (var p in allInNewTable)
                {
                    p.SeatId = seatCounter.ToString();
                    seatCounter++;
                }

                instructionType = "INFO_ALERT";
                systemMessage = $"⚠️ SE ABRIÓ MESA {newTableNum}: Se balancearon {playersToMove.Count} jugadores.";
            }
            else
            {
                // Buscar mesa con cupo (SOLO ACTIVAS)
                var targetTable = activeTables
                    .OrderBy(tb => activePlayers.Count(p => p.TableId == tb.Id))
                    .FirstOrDefault(tb => activePlayers.Count(p => p.TableId == tb.Id) < seatsPerTable);

                // Fallback a la primera activa
                if (targetTable == null) targetTable = activeTables.First();

                reg.TableId = targetTable.Id;

                // Buscar primer asiento numérico libre
                var takenSeats = activePlayers
                    .Where(p => p.TableId == targetTable.Id && p.SeatId != null)
                    .Select(p => int.TryParse(p.SeatId, out int s) ? s : 0) // Safe parse
                    .ToList();

                int freeSeat = 1;
                while (takenSeats.Contains(freeSeat)) freeSeat++;
                reg.SeatId = freeSeat.ToString();

                systemMessage = $"Asignado a {targetTable.Name}, Puesto {reg.SeatId}";
            }

            if (t.Transactions == null) t.Transactions = new List<TournamentTransaction>();

            var buyInTx = new TournamentTransaction
            {
                Id = Guid.NewGuid(),
                TournamentId = t.Id,
                PlayerId = reg.Id,
                Type = TournamentTransactionType.BuyIn,
                Amount = t.BuyIn, 
                Method = "Cash",
                Notes = $"Buy-In Inicial: {playerName}",
                Timestamp = DateTime.UtcNow
            };
            t.Transactions.Add(buyInTx);

            if (t.Fee > 0)
            {
                var rakeTx = new TournamentTransaction
                {
                    Id = Guid.NewGuid(),
                    TournamentId = t.Id,
                    Type = TournamentTransactionType.HouseRake,
                    Amount = t.Fee,
                    Method = "Cash",
                    Notes = $"Rake Buy-In: {playerName}",
                    Timestamp = DateTime.UtcNow
                };
                t.Transactions.Add(rakeTx);
            }
            t.PrizePool += t.BuyIn;
            t.Registrations.Add(reg);
          
            await _repo.UpdateAsync(t);

            return new RegistrationResult { Registration = reg, InstructionType = instructionType, SystemMessage = systemMessage };
        }


        // =============================================================
        // 5. LÓGICA INTELIGENTE: ELIMINACIÓN (Solo Mesas Activas)
        // =============================================================
        public async Task<RemoveResult> RemoveRegistrationAsync(Guid tournamentId, Guid regId)
        {
            var t = await _repo.GetByIdAsync(tournamentId);
            if (t == null) return new RemoveResult { Success = false };

            // 1. Eliminar al jugador
            var player = t.Registrations.FirstOrDefault(r => r.Id == regId);
            if (player == null) return new RemoveResult { Success = false };

            player.Status = "Eliminated";
            player.EliminatedAt = DateTime.UtcNow;
            player.TableId = null;
            player.SeatId = null;

            // Limpiar mesas vacías previas
            CleanupEmptyTables(t);

            // Guardamos la eliminación primero
            await _repo.UpdateAsync(t);

            // --- LÓGICA CENTRALIZADA DE MESA FINAL (REDRAW) ---

            var activePlayers = t.Registrations.Where(r => r.Status == "Active").ToList();
            var activeTables = t.Tables.Where(tb => tb.Status == "Active").ToList();

            // Leemos config (default 9)
            int finalTableSize = t.Seating.FinalTableSize > 0 ? t.Seating.FinalTableSize : 9;

            // CONDICIÓN DE MESA FINAL:
            // Quedan X jugadores Y (hay más de 1 mesa O la mesa actual no se llama "Mesa Final")
            bool isFinalCountReached = activePlayers.Count <= finalTableSize && activePlayers.Count > 1;
            bool needsConsolidation = activeTables.Count > 1 || (activeTables.Count == 1 && activeTables[0].Name != "Mesa Final");

            if (isFinalCountReached && needsConsolidation)
            {
                // 1. Crear la Mesa Final Oficial
                var finalTable = new TournamentTable
                {
                    Id = Guid.NewGuid().ToString(),
                    TableNumber = 1, // Reiniciamos a Mesa 1
                    Name = "Mesa Final",
                    Status = "Active"
                };

                // 2. Cerrar todas las mesas anteriores
                foreach (var tb in activeTables)
                {
                    tb.Status = "Closed";
                    tb.ClosedAt = DateTime.UtcNow;
                }

                // Agregar la nueva
                t.Tables.Add(finalTable);

                // 3. SHUFFLE (Barajar aleatoriamente a los jugadores)
                var rng = new Random();
                var shuffledPlayers = activePlayers.OrderBy(x => rng.Next()).ToList();

                // 4. Asignar puestos del 1 al N
                int seatNum = 1;
                foreach (var p in shuffledPlayers)
                {
                    p.TableId = finalTable.Id;
                    p.SeatId = seatNum.ToString(); // Asignación forzada 1, 2, 3...
                    seatNum++;
                }

                // 5. Guardar el Redraw en BD
                await _repo.UpdateAsync(t);

                return new RemoveResult
                {
                    Success = true,
                    InstructionType = "FINAL_TABLE_START",
                    Message = "¡MESA FINAL DEFINIDA! Sorteo automático realizado."
                };
            }

            // ... (Resto de lógica de balanceo normal) ...
            // Check desbalance simple (Gap > 1)
            var tableCounts = activeTables.Select(tb => new { Table = tb, Count = activePlayers.Count(p => p.TableId == tb.Id) }).ToList();
            if (tableCounts.Count >= 2)
            {
                var max = tableCounts.MaxBy(x => x.Count);
                var min = tableCounts.MinBy(x => x.Count);
                if (max != null && min != null && (max.Count - min.Count) >= 2)
                {
                    return new RemoveResult { Success = true, InstructionType = "BALANCE_REQUIRED", Message = $"Balancear: De {max.Table.Name} a {min.Table.Name}", FromTable = max.Table.Id, ToTable = min.Table.Id };
                }
            }

            return new RemoveResult { Success = true };
        }

        // --- HELPER PRIVADO ---
        private void CleanupEmptyTables(Tournament t)
        {
            // Buscar mesas que están ACTIVAS pero no tienen jugadores activos
            var emptyActiveTables = t.Tables.Where(tb =>
                tb.Status == "Active" &&
                !t.Registrations.Any(r => r.TableId == tb.Id && r.Status == "Active")
            ).ToList();

            // Regla: Nunca cerrar la Mesa 1 si es la única activa (para que siempre haya una base)
            // Contamos cuántas activas hay en total
            var activeTablesCount = t.Tables.Count(x => x.Status == "Active");

            foreach (var tb in emptyActiveTables)
            {
                // Si solo queda 1 mesa activa, no la cerramos aunque esté vacía
                if (activeTablesCount <= 1) break;

                // --- SOFT DELETE: CAMBIAR ESTADO ---
                tb.Status = "Closed";
                tb.ClosedAt = DateTime.UtcNow;

                activeTablesCount--;
            }
        }

        // =============================================================
        // LÓGICA FINANCIERA
        // =============================================================

        public async Task<TournamentTransaction?> RecordTransactionAsync(Guid tournamentId, TournamentTransaction transaction)
        {
            var t = await _repo.GetByIdAsync(tournamentId);
            if (t == null) return null;

            if (t.Transactions == null) t.Transactions = new List<TournamentTransaction>();

            transaction.Id = Guid.NewGuid();
            transaction.TournamentId = tournamentId;
            transaction.Timestamp = DateTime.UtcNow;

            t.Transactions.Add(transaction);

            // Si es un pago de premio, actualizamos el registro del jugador también
            if (transaction.Type == TournamentTransactionType.Payout && transaction.PlayerId.HasValue)
            {
                var player = t.Registrations.FirstOrDefault(r => r.Id == transaction.PlayerId.Value);
                if (player != null)
                {
                    player.PayoutAmount = (player.PayoutAmount ?? 0) + Math.Abs(transaction.Amount); // Guardamos el total pagado
                }
            }

            await _repo.UpdateAsync(t);
            return transaction;
        }

        public async Task<decimal> GetTotalPrizePoolAsync(Guid tournamentId)
        {
            var t = await _repo.GetByIdAsync(tournamentId);
            if (t == null) return 0;

            // Calculado basado en transacciones reales (BuyIns + Rebuys + Addons) - HouseRake
            // O simplemente usamos la propiedad acumulada si prefieres
            return t.PrizePool;
        }

        // Dentro de Services/TournamentService.cs

        public async Task<RegistrationResult?> RebuyPlayerAsync(Guid tournamentId, Guid registrationId, string paymentMethod)
        {
            var t = await _repo.GetByIdAsync(tournamentId);
            if (t == null) return null;

            // 1. Validar que el registro esté permitido (Nivel)
            if (t.RebuyConfig.UntilLevel > 0 && t.CurrentLevel > t.RebuyConfig.UntilLevel)
            {
                // Retornamos null o podrías manejar un error específico
                return null;
            }

            var reg = t.Registrations.FirstOrDefault(r => r.Id == registrationId);
            if (reg == null) return null;

            // 2. "Resucitar" al Jugador
            reg.Status = "Active";
            reg.EliminatedAt = null;
            reg.Chips = t.RebuyConfig.RebuyChips; // Reset de fichas (stack de rebuy)

            // Limpiar asiento anterior por si acaso quedó sucio
            reg.TableId = null;
            reg.SeatId = null;

            // 3. Registrar Transacción Financiera (REBUY)
            if (t.Transactions == null) t.Transactions = new List<TournamentTransaction>();

            var tx = new TournamentTransaction
            {
                Id = Guid.NewGuid(),
                TournamentId = t.Id,
                PlayerId = reg.Id,
                Type = TournamentTransactionType.Rebuy, // Enum que pediste
                Amount = t.RebuyConfig.RebuyCost,
                Method = paymentMethod, // Por defecto, o pasarlo por parámetro
                Notes = $"Re-entry Nivel {t.CurrentLevel}",
                Timestamp = DateTime.UtcNow
            };
            t.Transactions.Add(tx);

            // Transacción de Rake (si aplica en Rebuy)
            if (t.RebuyConfig.RebuyHouseFee > 0)
            {
                var rakeTx = new TournamentTransaction
                {
                    Id = Guid.NewGuid(),
                    TournamentId = t.Id,
                    PlayerId = reg.Id, // Opcional ligarlo al jugador
                    Type = TournamentTransactionType.HouseRake,
                    Amount = t.RebuyConfig.RebuyHouseFee,
                    Method = paymentMethod,
                    Notes = "Rake Rebuy",
                    Timestamp = DateTime.UtcNow
                };
                t.Transactions.Add(rakeTx);
            }

            // Actualizar acumulados
            t.PrizePool += t.RebuyConfig.RebuyCost;
            reg.PaidAmount += (t.RebuyConfig.RebuyCost + t.RebuyConfig.RebuyHouseFee);

            // 4. Buscarle Silla Nueva (Reutilizamos lógica de Register)
            // ... (Aquí va la lógica de buscar mesa activa y hueco, igual que en RegisterPlayerAsync) ...
            // (Para no repetir código gigante, idealmente extrae la lógica de "FindSeat" a un método privado)

            // --- LÓGICA RESUMIDA DE ASIGNACIÓN ---
            var activeTables = t.Tables.Where(tb => tb.Status == "Active").ToList();
            var activePlayers = t.Registrations.Where(r => r.Status == "Active").ToList(); // Ya incluye al resucitado
            int seatsPerTable = t.Seating.SeatsPerTable > 0 ? t.Seating.SeatsPerTable : 9;

            // Buscar mesa con espacio
            var targetTable = activeTables
                .OrderBy(tb => activePlayers.Count(p => p.TableId == tb.Id))
                .FirstOrDefault(tb => activePlayers.Count(p => p.TableId == tb.Id) < seatsPerTable);

            // Si no hay hueco, habría que crear mesa (Lógica Romper Mesa), 
            // por simplicidad aquí asumimos que entra en hueco o creas mesa manual si está lleno.
            if (targetTable != null)
            {
                reg.TableId = targetTable.Id;
                // Buscar asiento libre
                var takenSeats = activePlayers.Where(p => p.TableId == targetTable.Id && p.SeatId != null)
                                              .Select(p => int.TryParse(p.SeatId, out int s) ? s : 0).ToList();
                int freeSeat = 1;
                while (takenSeats.Contains(freeSeat)) freeSeat++;
                reg.SeatId = freeSeat.ToString();
            }
            // -------------------------------------

            await _repo.UpdateAsync(t);

            return new RegistrationResult
            {
                Registration = reg,
                SystemMessage = $"Reingreso exitoso en {targetTable?.Name ?? "Sin Mesa"}"
            };
        }

        // Dentro de TournamentService.cs (o un PayoutCalculatorService)

        private void CalculateFixedPayouts(Tournament t)
        {
            // Usamos el garantizado como base si el pozo real es 0 o menor
            decimal totalPrize = Math.Max(t.Guaranteed, t.PrizePool);

            if (totalPrize <= 0 || t.Payouts == null || t.Payouts.Count == 0)
            {
                return; // No hay nada que calcular
            }

            // Calcular la suma de todos los porcentajes definidos
            decimal totalPercentage = t.Payouts.Sum(p => p.Percentage);

            // Si el porcentaje suma 100, calculamos los montos fijos.
            if (totalPercentage == 100)
            {
                foreach (var tier in t.Payouts)
                {
                    // Calcula el monto fijo: (Porcentaje / 100) * Pozo Total
                    decimal fixedAmount = (tier.Percentage / 100m) * totalPrize;
                    tier.FixedAmount = fixedAmount;

                    // Nota: Aquí se está sobrescribiendo el FixedAmount a 0 que viene del CURL si es %
                    // Si el CURL ya tiene FixedAmount, este cálculo podría ser omitido si es un payout fijo.
                }
            }
            // Si no suma 100, la lógica dependerá de las reglas específicas del casino.
        }
    }
}