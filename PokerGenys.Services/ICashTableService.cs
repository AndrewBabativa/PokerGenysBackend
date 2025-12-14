using PokerGenys.Domain.Models.CashGame;

namespace PokerGenys.Services
{
    public interface ICashTableService
    {
        Task<List<CashTable>> GetByDayAsync(Guid workingDayId);
        Task<CashTable> CreateAsync(CashTable table);
        Task<CashTable?> UpdateAsync(CashTable table);
    }
}