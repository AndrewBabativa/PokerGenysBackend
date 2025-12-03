using PokerGenys.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerGenys.Infrastructure.Repositories
{
    public interface IWorkingDayRepository
    {
        Task<List<WorkingDay>> GetAllAsync();
        Task<WorkingDay?> GetOpenDayAsync();
        Task<WorkingDay?> GetByIdAsync(Guid id);
        Task<WorkingDay> CreateAsync(WorkingDay day);
        Task UpdateAsync(WorkingDay day);
    }
}