using MongoDB.Driver;
using PokerGenys.Domain.Models;
using PokerGenys.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerGenys.Infrastructure.Repositories
{
    public class TableRepository : ITableRepository
    {
        private readonly MongoContext _context;

        public TableRepository(MongoContext context) => _context = context;

        public async Task<List<TableInstance>> GetByDayAsync(Guid dayId) =>
            await _context.Tables.Find(t => t.DayId == dayId).ToListAsync();

        public async Task<TableInstance?> GetByIdAsync(Guid id) =>
            await _context.Tables.Find(t => t.Id == id).FirstOrDefaultAsync();

        public async Task<TableInstance> CreateAsync(TableInstance table)
        {
            await _context.Tables.InsertOneAsync(table);
            return table;
        }

        public async Task UpdateAsync(TableInstance table) =>
            await _context.Tables.ReplaceOneAsync(t => t.Id == table.Id, table);
    }
}