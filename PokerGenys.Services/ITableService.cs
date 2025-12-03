using PokerGenys.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerGenys.Services
{
    public interface ITableService
    {
        Task<List<TableInstance>> GetByDayAsync(Guid dayId);
        Task<TableInstance> CreateAsync(TableInstance table);
        Task<TableInstance?> UpdateAsync(TableInstance table);
    }
}