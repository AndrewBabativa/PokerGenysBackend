using PokerGenys.Domain.Models.Core;

namespace PokerGenys.Infrastructure.Repositories
{
    public interface IDealerRepository
    {
        Task<List<Dealer>> GetAllAsync();
        Task<Dealer?> GetByIdAsync(Guid id);
        Task<Dealer> CreateAsync(Dealer dealer);
        Task UpdateAsync(Dealer dealer);
        Task DeleteAsync(Guid id);
        Task<List<DealerShift>> GetShiftsAsync(Guid tableId);
        Task<DealerShift> AddShiftAsync(DealerShift shift);
        Task UpdateShiftAsync(DealerShift shift);
        Task<DealerShift?> GetShiftByIdAsync(Guid id);
    }
}