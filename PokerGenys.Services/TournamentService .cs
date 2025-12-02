using PokerGenys.Domain.Models;
using PokerGenys.Infrastructure.Repositories;

namespace PokerGenys.Services
{
    public class TournamentService : ITournamentService
    {
        private readonly ITournamentRepository _repo;
        public TournamentService(ITournamentRepository repo) => _repo = repo;

        public Task<List<Tournament>> GetAllAsync() => _repo.GetAllAsync();
        public Task<Tournament?> GetByIdAsync(Guid id) => _repo.GetByIdAsync(id);
        public Task<Tournament> CreateAsync(Tournament tournament) => _repo.CreateAsync(tournament);
        public Task<Tournament> UpdateAsync(Tournament tournament) => _repo.UpdateAsync(tournament);
        public Task<bool> DeleteAsync(Guid id) => _repo.DeleteAsync(id);

        // =============================================================
        // REGISTRATIONS 
        // =============================================================

        public async Task<List<TournamentRegistration>> GetRegistrationsAsync(Guid id)
        {
            var t = await _repo.GetByIdAsync(id);
            return t?.Registrations ?? new();
        }

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

        public async Task<bool> RemoveRegistrationAsync(Guid id, Guid regId)
        {
            var t = await _repo.GetByIdAsync(id);
            if (t == null) return false;

            var removed = t.Registrations.RemoveAll(r => r.Id == regId) > 0;
            if (!removed) return false;

            await _repo.UpdateAsync(t);
            return true;
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

                if (!t.StartTime.HasValue)
                    return new TournamentState
                    {
                        CurrentLevel = t.CurrentLevel,
                        TimeRemaining = 0,
                        Status = t.Status,
                        RegisteredCount = t.Registrations.Count,
                        PrizePool = t.PrizePool
                    };

                // Calcular nivel actual y tiempo restante
                var elapsedMs = (DateTime.UtcNow - t.StartTime.Value).TotalMilliseconds;
                int currentLevel = t.CurrentLevel;
                double levelStartMs = 0;
                double timeRemaining = 0;

                foreach (var lvl in t.Levels.OrderBy(l => l.LevelNumber))
                {
                    double durationMs = lvl.DurationSeconds * 1000;
                    if (elapsedMs < levelStartMs + durationMs)
                    {
                        timeRemaining = (levelStartMs + durationMs - elapsedMs) / 1000;
                        break;
                    }
                    currentLevel++;
                    levelStartMs += durationMs;
                }

                if (currentLevel > t.Levels.Count)
                {
                    currentLevel = t.Levels.Count;
                    timeRemaining = 0;
                }

                t.CurrentLevel = currentLevel;
                await _repo.UpdateAsync(t);

                return new TournamentState
                {
                    CurrentLevel = t.CurrentLevel,
                    TimeRemaining = (int)Math.Ceiling(timeRemaining),
                    Status = t.Status,
                    RegisteredCount = t.Registrations.Count,
                    PrizePool = t.PrizePool
                };
            }


        public async Task<TournamentRegistration?> RegisterPlayerAsync(Guid id, string playerName)
        {
            var t = await _repo.GetByIdAsync(id);
            if (t == null) return null;

            var reg = new TournamentRegistration
            {
                Id = Guid.NewGuid(),
                PlayerName = playerName,
                TournamentId = t.Id,
                RegisteredAt = DateTime.UtcNow
            };

            t.Registrations.Add(reg);
            t.PrizePool += t.BuyIn + t.Fee;

            await _repo.UpdateAsync(t);
            return reg;
        }


    }

}
