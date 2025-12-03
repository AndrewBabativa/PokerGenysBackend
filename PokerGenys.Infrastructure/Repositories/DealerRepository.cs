using MongoDB.Driver;
using PokerGenys.Domain.Models;
using PokerGenys.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerGenys.Infrastructure.Repositories
{
    public class DealerRepository : IDealerRepository
    {
        private readonly MongoContext _context;

        public DealerRepository(MongoContext context) => _context = context;

        public async Task<List<Dealer>> GetAllAsync() =>
            await _context.Dealers.Find(_ => true).ToListAsync();

        public async Task<List<DealerShift>> GetShiftsAsync(Guid dayId, Guid? tableId)
        {
            var filter = Builders<DealerShift>.Filter.Eq(s => s.DayId, dayId);
            if (tableId.HasValue)
                filter &= Builders<DealerShift>.Filter.Eq(s => s.TableId, tableId.Value);
            return await _context.DealerShifts.Find(filter).ToListAsync();
        }

        public async Task<DealerShift> AddShiftAsync(DealerShift shift)
        {
            await _context.DealerShifts.InsertOneAsync(shift);
            return shift;
        }

        public async Task UpdateShiftAsync(DealerShift shift) =>
            await _context.DealerShifts.ReplaceOneAsync(s => s.Id == shift.Id, shift);

        public async Task<DealerShift?> GetShiftByIdAsync(Guid id) =>
            await _context.DealerShifts.Find(s => s.Id == id).FirstOrDefaultAsync();
    }
}