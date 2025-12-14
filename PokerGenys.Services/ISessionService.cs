using PokerGenys.Domain.Models.CashGame;
using PokerGenys.Domain.DTOs.Audit;
using PokerGenys.Domain.DTOs.Reports;

namespace PokerGenys.Services
{
    public interface ISessionService
    {
        Task<List<CashSession>> GetAllAsync();
        Task<CashSession> CreateAsync(CashSession session);
        Task<CashSession?> UpdateAsync(CashSession session);
        Task<TableReportDto> GetTableReportAsync(Guid tableId);
        Task<List<CashSession>> GetAllByTableIdAsync(Guid tableId);
        Task<CashAuditResult> GetFinancialAuditAsync(Guid workingDayId);
    }

}
