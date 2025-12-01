using PokerGenys.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerGenys.Services
{
    public interface ITournamentService
    {
        Task<List<Tournament>> GetAllAsync();
        Task<Tournament?> GetByIdAsync(Guid id);
        Task<Tournament> CreateAsync(Tournament tournament);
        Task<Tournament> UpdateAsync(Tournament tournament);
        Task<bool> DeleteAsync(Guid id);

        Task<List<TournamentRegistration>> GetRegistrationsAsync(Guid id);
        Task<Tournament?> AddRegistrationAsync(Guid id, TournamentRegistration reg);
        Task<bool> RemoveRegistrationAsync(Guid id, Guid regId);
        Task<TournamentRegistration?> AssignSeatAsync(Guid tournamentId, Guid regId, string tableId, string seatId);
    }
}
