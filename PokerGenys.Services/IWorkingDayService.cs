using PokerGenys.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerGenys.Services
{
    public interface IWorkingDayService
    {
        Task<List<WorkingDay>> GetAllAsync();
        Task<WorkingDay> CreateAsync();
        Task CloseDayAsync(Guid id);
    }
}