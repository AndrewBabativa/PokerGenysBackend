using PokerGenys.Domain.Models.CashGame;

namespace PokerGenys.Services
{
    public interface IWaitlistService
    {
        Task<List<WaitlistItem>> GetAllAsync();
        Task<List<WaitlistItem>> GetByTableAsync(Guid tableId);
        Task<WaitlistItem> AddToWaitlistAsync(Guid tableId, Guid playerId);
        Task RemoveFromWaitlistAsync(Guid id);
        Task<CashSession?> SeatPlayerAsync(Guid waitlistItemId);
    }
}