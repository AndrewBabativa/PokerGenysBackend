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

        public TournamentService(ITournamentRepository repo) => _repo = repo;

        // ... CRUD BÁSICO (GetAll, GetById, Create, Update, Delete) SE MANTIENEN IGUAL ...
        public Task<List<Tournament>> GetAllAsync() => _repo.GetAllAsync();
        public Task<Tournament?> GetByIdAsync(Guid id) => _repo.GetByIdAsync(id);
        public Task<Tournament> CreateAsync(Tournament tournament) => _repo.CreateAsync(tournament);
        public Task<Tournament> UpdateAsync(Tournament tournament) => _repo.UpdateAsync(tournament);
        public Task<bool> DeleteAsync(Guid id) => _repo.DeleteAsync(id);
        public async Task<List<TournamentRegistration>> GetRegistrationsAsync(Guid id) { var t = await _repo.GetByIdAsync(id); return t?.Registrations ?? new(); }
        public async Task<Tournament?> StartTournamentAsync(Guid id) { var t = await _repo.GetByIdAsync(id); if (t == null) return null; t.StartTime = DateTime.UtcNow; t.CurrentLevel = 1; t.Status = "Running"; return await _repo.UpdateAsync(t); }
        public async Task<TournamentState?> GetTournamentStateAsync(Guid id) { /* ... Tu lógica de timer existente ... */ return null; } // Resumido para brevedad

        // =============================================================
        // LÓGICA DE GESTIÓN DE JUGADORES
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

            // LIMPIEZA AUTOMÁTICA: Si la mesa antigua quedó vacía, borrarla
            CleanupEmptyTables(t);

            await _repo.UpdateAsync(t);
            return reg;
        }

        public async Task<RegistrationResult?> RegisterPlayerAsync(Guid id, string playerName)
        {
            var t = await _repo.GetByIdAsync(id);
            if (t == null) return null;

            if (t.Tables == null) t.Tables = new List<TournamentTable>();
            if (!t.Tables.Any()) t.Tables.Add(new TournamentTable { TableNumber = 1, Name = "Mesa 1", Id = Guid.NewGuid().ToString() });

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

            var activePlayers = t.Registrations.Where(r => r.Status == "Active").ToList();
            int totalActive = activePlayers.Count + 1;
            int seatsPerTable = t.Seating.SeatsPerTable > 0 ? t.Seating.SeatsPerTable : 9;
            int capacity = t.Tables.Count * seatsPerTable;

            // --- ALGORITMO ROMPER MESA ---
            if (totalActive > capacity)
            {
                int newTableNum = t.Tables.Max(x => x.TableNumber) + 1;
                var newTable = new TournamentTable { Id = Guid.NewGuid().ToString(), TableNumber = newTableNum, Name = $"Mesa {newTableNum}" };
                t.Tables.Add(newTable);

                // Mover gente de la mesa más llena
                var crowdedTable = t.Tables.OrderByDescending(tb => activePlayers.Count(p => p.TableId == tb.Id)).First();
                var playersToMove = activePlayers.Where(p => p.TableId == crowdedTable.Id).OrderByDescending(p => p.RegisteredAt).Take(seatsPerTable / 2).ToList();

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
                // Buscar mesa con cupo
                var targetTable = t.Tables
                    .OrderBy(tb => activePlayers.Count(p => p.TableId == tb.Id))
                    .FirstOrDefault(tb => activePlayers.Count(p => p.TableId == tb.Id) < seatsPerTable);

                if (targetTable == null) targetTable = t.Tables.First(); // Fallback

                reg.TableId = targetTable.Id;

                // Buscar primer asiento numérico libre (1, 2, 3...)
                var takenSeats = activePlayers.Where(p => p.TableId == targetTable.Id && p.SeatId != null).Select(p => int.Parse(p.SeatId)).ToList();
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

            // LIMPIEZA: Verificar mesas vacías
            CleanupEmptyTables(t);

            await _repo.UpdateAsync(t);

            // --- LÓGICA MESA FINAL Y BALANCEO ---
            var activePlayers = t.Registrations.Where(r => r.Status == "Active").ToList();
            int finalTableSize = t.Seating.FinalTableSize > 0 ? t.Seating.FinalTableSize : 9;

            if (t.Tables.Count > 1 && activePlayers.Count <= finalTableSize)
            {
                return new RemoveResult { Success = true, InstructionType = "FINAL_TABLE_START", Message = "¡MESA FINAL DEFINIDA!" };
            }

            // Check desbalance simple (Gap > 1)
            var tableCounts = t.Tables.Select(tb => new { Table = tb, Count = activePlayers.Count(p => p.TableId == tb.Id) }).ToList();
            if (tableCounts.Count >= 2)
            {
                var max = tableCounts.MaxBy(x => x.Count);
                var min = tableCounts.MinBy(x => x.Count);
                if ((max.Count - min.Count) >= 2)
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
            // Mesas que NO tienen ningún jugador Activo
            var emptyTables = t.Tables.Where(tb =>
                !t.Registrations.Any(r => r.TableId == tb.Id && r.Status == "Active")
            ).ToList();

            // Nunca borrar la Mesa 1 si es la única
            if (t.Tables.Count == 1) return;

            foreach (var tb in emptyTables)
            {
                // Si borramos mesas, hay que tener cuidado de no borrar la Mesa Final si ya estamos en ella
                t.Tables.Remove(tb);
            }
        }
    }
}