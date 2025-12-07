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

        public async Task<TableInstance?> UpdateAsync(TableInstance incoming)
        {
            var existing = await _repo.GetByIdAsync(incoming.Id);

            if (existing == null) return null;


            if (incoming.Status != default)
                existing.Status = incoming.Status;

            if (incoming.ClosedAt.HasValue)
                existing.ClosedAt = incoming.ClosedAt;


            existing.TotalRake = incoming.TotalRake;
            existing.CloseNotes = incoming.CloseNotes;

            if (incoming.Metadata != null)
            {
                if (existing.Metadata == null) existing.Metadata = new Dictionary<string, object>();

                foreach (var item in incoming.Metadata)
                {
                    existing.Metadata[item.Key] = item.Value;
                }
            }

            await _repo.UpdateAsync(existing);

            return existing;
        }
    }
}