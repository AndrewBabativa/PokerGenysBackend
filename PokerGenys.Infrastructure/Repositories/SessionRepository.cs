using MongoDB.Driver;
using PokerGenys.Domain.Models;
using PokerGenys.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerGenys.Infrastructure.Repositories
{
    public class SessionRepository : ISessionRepository
    {
        private readonly MongoContext _context;

        public SessionRepository(MongoContext context) => _context = context;

        public async Task<List<Session>> GetAllActiveAsync() =>
            await _context.Sessions.Find(s => s.EndTime == null).ToListAsync();

        public async Task<List<Session>> GetByDayAsync(Guid dayId) =>
            await _context.Sessions.Find(s => s.DayId == dayId).ToListAsync();

        public async Task<Session?> GetByIdAsync(Guid id) =>
            await _context.Sessions.Find(s => s.Id == id).FirstOrDefaultAsync();

        public async Task<Session> CreateAsync(Session session)
        {
            await _context.Sessions.InsertOneAsync(session);
            return session;
        }

        public async Task UpdateAsync(Session session) =>
            await _context.Sessions.ReplaceOneAsync(s => s.Id == session.Id, session);

        public async Task<List<Session>> GetByTableIdAsync(Guid tableId)
        {
            // Usamos _context.Sessions en lugar de _collection
            return await _context.Sessions
                .Find(s => s.TableId == tableId) // Filtro Lambda directo (más limpio)
                .SortBy(s => s.StartTime)        // Ordenamos por hora de inicio
                .ToListAsync();
        }
    }
}