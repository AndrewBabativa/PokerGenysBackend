using PokerGenys.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerGenys.Infrastructure.Repositories
{
    public interface ITableRepository
    {
        Task<List<TableInstance>> GetByDayAsync(Guid dayId);
        Task<TableInstance?> GetByIdAsync(Guid id);
        Task<TableInstance> CreateAsync(TableInstance table);
        Task UpdateAsync(TableInstance table);
    }
}