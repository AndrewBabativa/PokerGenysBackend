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
        public Task<Tournament> CreateAsync(Tournament tournament) => _repo.CreateAsync(tournament);
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
                Status = "Active"
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

            t.Registrations.Add(reg);
            t.PrizePool += t.BuyIn + t.Fee;
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

            var player = t.Registrations.FirstOrDefault(r => r.Id == regId);
            if (player == null) return new RemoveResult { Success = false };

            player.Status = "Eliminated";
            player.EliminatedAt = DateTime.UtcNow;
            player.TableId = null;
            player.SeatId = null;

            // LIMPIEZA: Cerrar mesas vacías
            CleanupEmptyTables(t);

            await _repo.UpdateAsync(t);

            // --- LÓGICA MESA FINAL Y BALANCEO ---
            var activePlayers = t.Registrations.Where(r => r.Status == "Active").ToList();
            // IMPORTANTE: Solo miramos mesas activas para decidir si hay balanceo o final
            var activeTables = t.Tables.Where(tb => tb.Status == "Active").ToList();

            int finalTableSize = t.Seating.FinalTableSize > 0 ? t.Seating.FinalTableSize : 9;

            // Si hay más de 1 mesa activa y caben en una sola -> MESA FINAL
            if (activeTables.Count > 1 && activePlayers.Count <= finalTableSize)
            {
                return new RemoveResult { Success = true, InstructionType = "FINAL_TABLE_START", Message = "¡MESA FINAL DEFINIDA!" };
            }

            // Check desbalance simple (Gap > 1) entre mesas activas
            var tableCounts = activeTables.Select(tb => new
            {
                Table = tb,
                Count = activePlayers.Count(p => p.TableId == tb.Id)
            }).ToList();

            if (tableCounts.Count >= 2)
            {
                var max = tableCounts.MaxBy(x => x.Count);
                var min = tableCounts.MinBy(x => x.Count);

                if (max != null && min != null && (max.Count - min.Count) >= 2)
                {
                    return new RemoveResult
                    {
                        Success = true,
                        InstructionType = "BALANCE_REQUIRED",
                        Message = $"Balancear: De {max.Table.Name} a {min.Table.Name}",
                        FromTable = max.Table.Id,
                        ToTable = min.Table.Id
                    };
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
    }
}