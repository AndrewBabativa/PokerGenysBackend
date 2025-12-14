using MongoDB.Driver;
using PokerGenys.Domain.Models.CashGame;
using PokerGenys.Infrastructure.Data;

namespace PokerGenys.Infrastructure.Repositories
{
    public class SessionRepository : ISessionRepository
    {
        private readonly MongoContext _context;

        public SessionRepository(MongoContext context) => _context = context;

        public async Task<List<CashSession>> GetAllActiveAsync() =>
            await _context.Sessions.Find(s => s.EndTime == null).ToListAsync();

        public async Task<List<CashSession>> GetAllByTableIdAsync(Guid tableId)
        {
            return await _context.Sessions
                .Find(s => s.TableId == tableId) 
                .SortBy(s => s.StartTime)        
                .ToListAsync();
        }

        public async Task<List<CashSession>> GetByDayAsync(Guid workingDayId) =>
            await _context.Sessions.Find(s => s.WorkingDayId == workingDayId).ToListAsync();

        public async Task<CashSession?> GetByIdAsync(Guid id) =>
            await _context.Sessions.Find(s => s.Id == id).FirstOrDefaultAsync();

        public async Task<CashSession> CreateAsync(CashSession session)
        {
            await _context.Sessions.InsertOneAsync(session);
            return session;
        }

        public async Task UpdateAsync(CashSession session) =>
            await _context.Sessions.ReplaceOneAsync(s => s.Id == session.Id, session);

        public async Task<List<CashSession>> GetByTableIdAsync(Guid tableId)
        {
            return await _context.Sessions
                .Find(s => s.TableId == tableId) 
                .SortBy(s => s.StartTime)        
                .ToListAsync();
        }

        public async Task<List<CashSession>> GetByDayIdAsync(Guid workingDayId)
        {
            return await _context.Sessions
                .Find(s => s.WorkingDayId == workingDayId)
                .ToListAsync();
        }
    }
}