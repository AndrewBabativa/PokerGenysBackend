using PokerGenys.Domain.Models;
using PokerGenys.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerGenys.Services
{
    public class TableService : ITableService
    {
        private readonly ITableRepository _repo;

        public TableService(ITableRepository repo) => _repo = repo;

        public Task<List<TableInstance>> GetByDayAsync(Guid dayId) => _repo.GetByDayAsync(dayId);

        public Task<TableInstance> CreateAsync(TableInstance table)
        {
            if (table.Id == Guid.Empty) table.Id = Guid.NewGuid();
            return _repo.CreateAsync(table);
        }

        public async Task<TableInstance?> UpdateAsync(TableInstance table)
        {
            var existing = await _repo.GetByIdAsync(table.Id);
            if (existing == null) return null;
            await _repo.UpdateAsync(table);
            return table;
        }
    }
}