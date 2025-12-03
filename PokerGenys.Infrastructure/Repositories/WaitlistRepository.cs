using MongoDB.Driver;
using PokerGenys.Domain.Models;
using PokerGenys.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerGenys.Infrastructure.Repositories
{
    public class WaitlistRepository : IWaitlistRepository
    {
        private readonly MongoContext _context;

        public WaitlistRepository(MongoContext context) => _context = context;

        public async Task<List<WaitlistItem>> GetAllAsync() =>
            await _context.Waitlist.Find(_ => true).ToListAsync();

        public async Task<List<WaitlistItem>> GetByTableAsync(Guid tableId) =>
            await _context.Waitlist.Find(w => w.TableId == tableId).SortBy(w => w.Priority).ToListAsync();

        public async Task<WaitlistItem> AddAsync(WaitlistItem item)
        {
            await _context.Waitlist.InsertOneAsync(item);
            return item;
        }

        public async Task DeleteAsync(Guid id) =>
            await _context.Waitlist.DeleteOneAsync(w => w.Id == id);

        public async Task<WaitlistItem?> GetByIdAsync(Guid id) =>
            await _context.Waitlist.Find(w => w.Id == id).FirstOrDefaultAsync();
    }
}