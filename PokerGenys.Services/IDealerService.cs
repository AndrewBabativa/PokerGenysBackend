using PokerGenys.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerGenys.Services
{
    public interface IDealerService
    {
        // CRUD
        Task<List<Dealer>> GetAllDealersAsync();
        Task<Dealer?> GetByIdAsync(Guid id);
        Task<Dealer> CreateAsync(Dealer dealer);
        Task<Dealer?> UpdateAsync(Dealer dealer);
        Task DeleteAsync(Guid id);

        // SHIFTS
        Task<List<DealerShift>> GetShiftsAsync(Guid dayId, Guid? tableId);
        Task<DealerShift> AddShiftAsync(DealerShift shift);
        Task<DealerShift?> UpdateShiftAsync(DealerShift shift);
    }
}