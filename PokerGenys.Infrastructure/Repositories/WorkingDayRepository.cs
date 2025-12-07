using MongoDB.Driver;
using PokerGenys.Domain.Models;
using PokerGenys.Infrastructure.Data;
using PokerGenys.Domain.Models.CashGame;

namespace PokerGenys.Infrastructure.Repositories
{
    public class WorkingDayRepository : IWorkingDayRepository
    {
        private readonly MongoContext _context;

        public WorkingDayRepository(MongoContext context) => _context = context;

        public async Task<List<WorkingDay>> GetAllAsync() =>
            await _context.WorkingDays.Find(_ => true).SortByDescending(d => d.StartAt).ToListAsync();

        public async Task<WorkingDay?> GetOpenDayAsync() =>
            await _context.WorkingDays.Find(d => d.Status == WorkingDayStatus.Open).FirstOrDefaultAsync();

        public async Task<WorkingDay?> GetByIdAsync(Guid id) =>
            await _context.WorkingDays.Find(d => d.Id == id).FirstOrDefaultAsync();

        public async Task<WorkingDay> CreateAsync(WorkingDay day)
        {
            await _context.WorkingDays.InsertOneAsync(day);
            return day;
        }

        public async Task UpdateAsync(WorkingDay day) =>
            await _context.WorkingDays.ReplaceOneAsync(d => d.Id == day.Id, day);

        public async Task<WorkingDay?> GetByDateAsync(DateTime date)
        {
            var start = date.Date; // 2025-12-07 00:00:00
            var end = start.AddDays(1); // 2025-12-08 00:00:00

            // Asumiendo que tu modelo WorkingDay tiene un campo 'Date' o 'CreatedAt'
            return await _context.WorkingDays
                .Find(w => w.CreatedAt >= start && w.ClosedAt < end)
                .FirstOrDefaultAsync();
        }
    }
}