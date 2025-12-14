using PokerGenys.Domain.Models.Core;

namespace PokerGenys.Services
{
    public interface IDealerService
    {
        Task<List<Dealer>> GetAllDealersAsync();
        Task<Dealer?> GetByIdAsync(Guid id);
        Task<Dealer> CreateAsync(Dealer dealer);
        Task<Dealer?> UpdateAsync(Dealer dealer);
        Task DeleteAsync(Guid id);
        Task<List<DealerShift>> GetShiftsAsync(Guid tableId);
        Task<DealerShift> AddShiftAsync(DealerShift shift);
        Task<DealerShift?> UpdateShiftAsync(DealerShift shift);
    }
}