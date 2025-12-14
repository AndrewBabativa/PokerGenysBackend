using PokerGenys.Domain.Models.Core;

namespace PokerGenys.Infrastructure.Repositories
{
    public interface IWorkingDayRepository
    {
        Task<List<WorkingDay>> GetAllAsync();
        Task<WorkingDay?> GetOpenDayAsync();
        Task<WorkingDay?> GetByIdAsync(Guid id);
        Task<WorkingDay> CreateAsync(WorkingDay day);
        Task UpdateAsync(WorkingDay day);
        Task<WorkingDay?> GetByDateAsync(DateTime date);
    }
}