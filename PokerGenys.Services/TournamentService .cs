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
    }
}
