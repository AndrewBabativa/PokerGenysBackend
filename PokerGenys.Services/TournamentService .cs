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

        // Configuración Hardcoded (Idealmente mover al modelo Tournament en el futuro)
        private const int MAX_PLAYERS_PER_TABLE = 10;
        private const int FINAL_TABLE_SIZE = 9;

        public TournamentService(ITournamentRepository repo)
        {
            _repo = repo;
        }

        // =============================================================
        // 1. CRUD BÁSICO (Pasamanos al Repo)
        // =============================================================
        public Task<List<Tournament>> GetAllAsync() => _repo.GetAllAsync();
        public Task<Tournament?> GetByIdAsync(Guid id) => _repo.GetByIdAsync(id);
        public Task<Tournament> CreateAsync(Tournament tournament) => _repo.CreateAsync(tournament);
        public Task<Tournament> UpdateAsync(Tournament tournament) => _repo.UpdateAsync(tournament);
        public Task<bool> DeleteAsync(Guid id) => _repo.DeleteAsync(id);

        // =============================================================
        // 2. GESTIÓN MANUAL / LEGACY
        // =============================================================
        public async Task<List<TournamentRegistration>> GetRegistrationsAsync(Guid id)
        {
            var t = await _repo.GetByIdAsync(id);
            return t?.Registrations ?? new();
        }

        // Método "tonto" para registros masivos o manuales sin lógica de mesa
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

            reg.TableId = tableId;
            reg.SeatId = seatId;

            await _repo.UpdateAsync(t);
            return reg;
        }

        // =============================================================
        // 3. CONTROL DE TIEMPO Y ESTADO (Timer)
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
            var t = await _repo.GetByIdAsync(id);
            if (t == null) return null;

            // Si no ha empezado, retornar estado base
            if (!t.StartTime.HasValue)
                return new TournamentState
                {
                    CurrentLevel = t.CurrentLevel,
                    TimeRemaining = 0,
                    Status = t.Status,
                    RegisteredCount = t.Registrations.Count,
                    PrizePool = t.PrizePool
                };

            // Cálculo matemático del nivel basado en el tiempo transcurrido
            var elapsedMs = (DateTime.UtcNow - t.StartTime.Value).TotalMilliseconds;
            int currentLevel = 1; // Empezamos en 1 y sumamos según niveles pasados
            double levelStartMs = 0;
            double timeRemaining = 0;
            bool levelFound = false;

            // Ordenamos los niveles para iterar cronológicamente
            foreach (var lvl in t.Levels.OrderBy(l => l.LevelNumber))
            {
                double durationMs = lvl.DurationSeconds * 1000;

                // Si el tiempo actual cae dentro de este nivel
                if (elapsedMs < levelStartMs + durationMs)
                {
                    timeRemaining = (levelStartMs + durationMs - elapsedMs) / 1000;
                    currentLevel = lvl.LevelNumber;
                    levelFound = true;
                    break;
                }

                // Si no, pasamos al siguiente
                levelStartMs += durationMs;
            }

            // Si pasamos todos los niveles, el torneo terminó o está en el último
            if (!levelFound)
            {
                currentLevel = t.Levels.Count + 1; // O el último nivel
                timeRemaining = 0;
            }

            // Actualizamos DB solo si el nivel cambió (Optimización)
            if (t.CurrentLevel != currentLevel)
            {
                t.CurrentLevel = currentLevel;
                await _repo.UpdateAsync(t);
            }

            return new TournamentState
            {
                CurrentLevel = currentLevel,
                TimeRemaining = (int)Math.Ceiling(timeRemaining),
                Status = t.Status,
                RegisteredCount = t.Registrations.Count,
                PrizePool = t.PrizePool
            };
        }

        // =============================================================
        // 4. LÓGICA INTELIGENTE: REGISTRO (Jugador 11 y Balanceo)
        // =============================================================
        public async Task<RegistrationResult?> RegisterPlayerAsync(Guid id, string playerName)
        {
            var t = await _repo.GetByIdAsync(id);
            if (t == null) return null;

            // A. Asegurar que existen mesas (Fix para torneos creados antes del update)
            if (t.Tables == null) t.Tables = new List<TournamentTable>();
            if (!t.Tables.Any())
            {
                t.Tables.Add(new TournamentTable { TableNumber = 1, Name = "Mesa 1" });
            }

            // B. Preparar el objeto del nuevo jugador
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

            // C. Datos para el cálculo
            var activePlayers = t.Registrations.Where(r => r.Status == "Active").ToList();
            int totalActiveWithNew = activePlayers.Count + 1;
            int currentCapacity = t.Tables.Count * MAX_PLAYERS_PER_TABLE;

            // 

            // --- ESCENARIO: FALTA ESPACIO (Ej: 10/10 -> Llega el 11) ---
            if (totalActiveWithNew > currentCapacity)
            {
                // 1. Crear nueva mesa
                int newTableNum = t.Tables.Count + 1;
                var newTable = new TournamentTable { TableNumber = newTableNum, Name = $"Mesa {newTableNum}" };
                t.Tables.Add(newTable);

                // 2. Identificar la mesa más llena para sacar gente de ahí
                var crowdedTable = t.Tables
                    .OrderByDescending(tb => activePlayers.Count(p => p.TableId == tb.Id))
                    .First();

                var playersInCrowded = activePlayers
                    .Where(p => p.TableId == crowdedTable.Id)
                    .OrderByDescending(p => p.RegisteredAt) // Movemos a los últimos (o lógica random)
                    .ToList();

                // 3. Calcular cuántos mover (aprox la mitad para equilibrar 6 y 5)
                int moveCount = playersInCrowded.Count / 2;
                var playersToMove = playersInCrowded.Take(moveCount).ToList();

                foreach (var p in playersToMove)
                {
                    p.TableId = newTable.Id;
                    p.SeatId = null; // Reset asiento o asignar nuevo ID
                }

                // 4. Asignar al NUEVO jugador a la mesa nueva también
                reg.TableId = newTable.Id;
                reg.SeatId = (playersToMove.Count + 1).ToString();

                instructionType = "INFO_ALERT";
                systemMessage = $"⚠️ SE ABRIÓ MESA {newTableNum}: Se movieron {playersToMove.Count} jugadores de Mesa {crowdedTable.TableNumber} para balancear.";
            }
            // --- ESCENARIO: HAY ESPACIO (Normal) ---
            else
            {
                // Buscar la mesa más vacía para mantener el balance natural
                var targetTable = t.Tables
                    .OrderBy(tb => activePlayers.Count(p => p.TableId == tb.Id))
                    .First();

                reg.TableId = targetTable.Id;

                // Asignar siguiente asiento disponible (lógica simple)
                int seatsTaken = activePlayers.Count(p => p.TableId == targetTable.Id);
                reg.SeatId = (seatsTaken + 1).ToString();

                systemMessage = $"Asignado a {targetTable.Name}, Asiento {reg.SeatId}";
            }

            // D. Guardar cambios
            t.Registrations.Add(reg);
            t.PrizePool += t.BuyIn + t.Fee;

            await _repo.UpdateAsync(t);

            return new RegistrationResult
            {
                Registration = reg,
                InstructionType = instructionType,
                SystemMessage = systemMessage
            };
        }

        // =============================================================
        // 5. LÓGICA INTELIGENTE: ELIMINACIÓN (Gap Detection)
        // =============================================================
        public async Task<RemoveResult> RemoveRegistrationAsync(Guid tournamentId, Guid regId)
        {
            var t = await _repo.GetByIdAsync(tournamentId);
            if (t == null) return new RemoveResult { Success = false };

            var player = t.Registrations.FirstOrDefault(r => r.Id == regId);
            if (player == null) return new RemoveResult { Success = false };

            // A. Soft Delete
            player.Status = "Eliminated";
            player.EliminatedAt = DateTime.UtcNow;
            player.TableId = null; // Liberar asiento
            player.SeatId = null;

            await _repo.UpdateAsync(t); // Guardar estado base inmediatamente

            // B. Análisis de Balance ("El Cerebro")
            var activePlayers = t.Registrations.Where(r => r.Status == "Active").ToList();

            // 1. CHEQUEO DE MESA FINAL
            if (t.Tables.Count > 1 && activePlayers.Count <= FINAL_TABLE_SIZE)
            {
                return new RemoveResult
                {
                    Success = true,
                    InstructionType = "FINAL_TABLE_START",
                    Message = "¡MESA FINAL DEFINIDA! Se requiere unificar y re-sortear asientos."
                };
            }

            // 2. CHEQUEO DE DESBALANCE (Gap >= 2)
            // Calculamos cuántos hay en cada mesa
            var tableCounts = t.Tables.Select(tb => new
            {
                Table = tb,
                Count = activePlayers.Count(p => p.TableId == tb.Id)
            }).ToList();

            var maxTable = tableCounts.OrderByDescending(x => x.Count).FirstOrDefault();
            var minTable = tableCounts.OrderBy(x => x.Count).FirstOrDefault();

            if (maxTable != null && minTable != null)
            {
                // Si la diferencia es 2 o más (Ej: Mesa 1 tiene 9, Mesa 2 tiene 7) -> ILEGAL
                if ((maxTable.Count - minTable.Count) >= 2)
                {
                    return new RemoveResult
                    {
                        Success = true,
                        InstructionType = "BALANCE_REQUIRED",
                        Message = $"⚠️ DESBALANCE: Mover jugador de {maxTable.Table.Name} a {minTable.Table.Name} (Mover UTG+1).",
                        FromTable = maxTable.Table.Id,
                        ToTable = minTable.Table.Id
                    };
                }
            }

            // Si todo está correcto, solo devolvemos éxito
            return new RemoveResult { Success = true };
        }
    }
}