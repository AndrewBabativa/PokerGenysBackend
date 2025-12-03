using PokerGenys.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerGenys.Services
{
    public interface IDealerService
    {
        Task<List<Dealer>> GetAllDealersAsync();
        Task<List<DealerShift>> GetShiftsAsync(Guid dayId, Guid? tableId);
        Task<DealerShift> AddShiftAsync(DealerShift shift);
        Task<DealerShift?> UpdateShiftAsync(DealerShift shift);
    }
}