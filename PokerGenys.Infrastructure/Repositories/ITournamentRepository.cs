// Infrastructure/Repositories/ITournamentRepository.cs
using PokerGenys.Domain.Models.Tournaments;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerGenys.Infrastructure.Repositories
{
    public interface ITournamentRepository
    {
        Task<List<Tournament>> GetAllAsync();
        Task<Tournament?> GetByIdAsync(Guid id);
        Task<Tournament> CreateAsync(Tournament tournament);
        Task<Tournament> UpdateAsync(Tournament tournament);
        Task<bool> DeleteAsync(Guid id);
        Task UpdateClockStateAsync(Guid tournamentId, ClockState clockState, int currentLevel, TournamentStatus status);
        Task<List<Tournament>> GetRunningTournamentsAsync();
    }
}
