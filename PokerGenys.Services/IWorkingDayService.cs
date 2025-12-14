using PokerGenys.Domain.Models.Core;

namespace PokerGenys.Services
{
    public interface IWorkingDayService
    {
        Task<List<WorkingDay>> GetAllAsync();
        Task<WorkingDay> CreateAsync(decimal initialCash, string notes);
        Task<WorkingDay> CloseDayAsync(Guid id, decimal finalCount, decimal expenses, string notes);
    }
}