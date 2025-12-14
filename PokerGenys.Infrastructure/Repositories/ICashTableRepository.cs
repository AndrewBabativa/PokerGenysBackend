using PokerGenys.Domain.Models.CashGame;

namespace PokerGenys.Infrastructure.Repositories
{
    public interface ICashTableRepository
    {
        Task<List<CashTable>> GetByDayAsync(Guid workingDayId);
        Task<CashTable?> GetByIdAsync(Guid id);
        Task<CashTable> CreateAsync(CashTable cashTable);
        Task UpdateAsync(CashTable cashTable);
    }
}