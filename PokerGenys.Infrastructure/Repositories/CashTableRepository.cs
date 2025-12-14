using MongoDB.Driver;
using PokerGenys.Domain.Models.CashGame;
using PokerGenys.Infrastructure.Data;

namespace PokerGenys.Infrastructure.Repositories
{
    public class CashTableRepository : ICashTableRepository
    {
        private readonly MongoContext _context;

        public CashTableRepository(MongoContext context) => _context = context;

        public async Task<List<CashTable>> GetByDayAsync(Guid workingDayId) =>
            await _context.Tables.Find(t => t.WorkingDayId == workingDayId).ToListAsync();

        public async Task<CashTable?> GetByIdAsync(Guid id) =>
            await _context.Tables.Find(t => t.Id == id).FirstOrDefaultAsync();

        public async Task<CashTable> CreateAsync(CashTable table)
        {
            await _context.Tables.InsertOneAsync(table);
            return table;
        }

        public async Task UpdateAsync(CashTable table) =>
            await _context.Tables.ReplaceOneAsync(t => t.Id == table.Id, table);
    }
}