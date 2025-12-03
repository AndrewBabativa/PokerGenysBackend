using PokerGenys.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerGenys.Infrastructure.Repositories
{
    public interface IDealerRepository
    {
        Task<List<Dealer>> GetAllAsync();
        Task<List<DealerShift>> GetShiftsAsync(Guid dayId, Guid? tableId);
        Task<DealerShift> AddShiftAsync(DealerShift shift);
        Task UpdateShiftAsync(DealerShift shift);
        Task<DealerShift?> GetShiftByIdAsync(Guid id);
    }
}