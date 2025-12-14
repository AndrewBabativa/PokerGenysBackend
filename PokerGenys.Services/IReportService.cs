using PokerGenys.Domain.DTOs.Reports;

namespace PokerGenys.Services
{
    public interface IReportService
    {
        Task<DailyReportDto> GetDailyReportAsync(Guid workingDayId);
        Task<DailyReportDto?> GetDailyReportByDateAsync(DateTime date);
    }
}
