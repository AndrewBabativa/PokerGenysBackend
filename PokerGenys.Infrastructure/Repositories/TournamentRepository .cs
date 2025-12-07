// Infrastructure/Repositories/TournamentRepository.cs
using MongoDB.Driver;
using PokerGenys.Domain.Models.Tournaments;
using PokerGenys.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerGenys.Infrastructure.Repositories
{
    public class TournamentRepository : ITournamentRepository
    {
        private readonly MongoContext _context;
        public TournamentRepository(MongoContext context)
        {
            _context = context;
        }

        public async Task<List<Tournament>> GetAllAsync() =>
            await _context.Tournaments.Find(_ => true).ToListAsync();

        public async Task<Tournament?> GetByIdAsync(Guid id) =>
            await _context.Tournaments.Find(t => t.Id == id).FirstOrDefaultAsync();

        public async Task<Tournament> CreateAsync(Tournament tournament)
        {
            await _context.Tournaments.InsertOneAsync(tournament);
            return tournament;
        }

        public async Task<Tournament> UpdateAsync(Tournament tournament)
        {
            await _context.Tournaments.ReplaceOneAsync(t => t.Id == tournament.Id, tournament);
            return tournament;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var result = await _context.Tournaments.DeleteOneAsync(t => t.Id == id);
            return result.DeletedCount > 0;
        }

        public async Task<List<Tournament>> GetRunningTournamentsAsync()
        {
            return await _context.Tournaments
                .Find(t => t.Status == TournamentStatus.Running && t.ClockState.IsPaused == false)
                .ToListAsync();
        }

        public async Task UpdateClockStateAsync(Guid tournamentId, ClockState clockState, int currentLevel, TournamentStatus status)
        {
            var updateDef = Builders<Tournament>.Update
                .Set(t => t.ClockState, clockState)
                .Set(t => t.CurrentLevel, currentLevel)
                .Set(t => t.Status, status);

            await _context.Tournaments.UpdateOneAsync(t => t.Id == tournamentId, updateDef);
        }

        public async Task<List<Tournament>> GetByWorkingDayIdAsync(Guid workingDayId)
        {
            return await _context.Tournaments
                .Find(t => t.WorkingDayId == workingDayId)
                .ToListAsync();
        }
    }
}
