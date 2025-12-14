using PokerGenys.Domain.Models.CashGame;

namespace PokerGenys.Infrastructure.Repositories
{
    public interface ISessionRepository
    {
        Task<List<CashSession>> GetAllActiveAsync();
        Task<List<CashSession>> GetByDayAsync(Guid dayId);
        Task<CashSession?> GetByIdAsync(Guid id);
        Task<CashSession> CreateAsync(CashSession session);
        Task UpdateAsync(CashSession session);

        Task<List<CashSession>> GetByTableIdAsync(Guid tableId);

        Task<List<CashSession>> GetByDayIdAsync(Guid dayId);

        Task<List<CashSession>> GetAllByTableIdAsync(Guid tableId);
    }
}