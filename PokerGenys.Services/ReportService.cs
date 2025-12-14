using PokerGenys.Domain.DTOs.Reports;
using PokerGenys.Domain.Enums;
using PokerGenys.Infrastructure.Repositories;

namespace PokerGenys.Services
{
    public class ReportService : IReportService
    {
        private readonly ISessionRepository _sessionRepo;
        private readonly ITournamentRepository _tournamentRepo;
        private readonly IWorkingDayRepository _workingDayRepo;

        public ReportService(
            ISessionRepository sessionRepo,
            ITournamentRepository tournamentRepo,
            IWorkingDayRepository workingDayRepo)
        {
            _sessionRepo = sessionRepo;
            _tournamentRepo = tournamentRepo;
            _workingDayRepo = workingDayRepo;
        }

        public async Task<DailyReportDto> GetDailyReportAsync(Guid workingDayId)
        {
            var sessions = await _sessionRepo.GetByDayIdAsync(workingDayId);
            var tournaments = await _tournamentRepo.GetByWorkingDayIdAsync(workingDayId);
            var day = await _workingDayRepo.GetByIdAsync(workingDayId);

            var report = new DailyReportDto { Date = day?.StartAt ?? DateTime.UtcNow };

            // -- CASH --
            foreach (var s in sessions)
            {
                if (s.Transactions == null) continue;
                foreach (var tx in s.Transactions)
                {
                    if (tx.Type == TransactionType.BuyIn || tx.Type == TransactionType.ReBuy)
                        report.CashGames.TotalBuyIns += tx.Amount;

                    if (tx.Type == TransactionType.CashOut)
                        report.CashGames.TotalCashOuts += tx.Amount;

                    if (tx.Type == TransactionType.ServiceSale)
                        report.RestaurantTotalSales += tx.Amount;

                    // Tesorería
                    if (tx.Type != TransactionType.CashOut)
                        AddTreasuryEntry(report.GlobalTreasury, tx.PaymentMethod, tx.Amount, tx.Status);
                    else
                        SubtractTreasuryEntry(report.GlobalTreasury, tx.PaymentMethod, tx.Amount);
                }
            }
            report.CashGames.NetCashFlow = report.CashGames.TotalBuyIns - report.CashGames.TotalCashOuts;

            // -- TORNEOS --
            foreach (var t in tournaments)
            {
                if (t.Transactions == null) continue;
                foreach (var tx in t.Transactions)
                {
                    if (tx.Type == TransactionType.BuyIn || tx.Type == TransactionType.ReBuy || tx.Type == TransactionType.AddOn)
                        report.Tournaments.TotalCollected += tx.Amount;

                    if (tx.Type == TransactionType.Payout)
                    {
                        report.Tournaments.TotalPayouts += tx.Amount;
                        SubtractTreasuryEntry(report.GlobalTreasury, tx.PaymentMethod, tx.Amount);
                    }
                    else
                    {
                        AddTreasuryEntry(report.GlobalTreasury, tx.PaymentMethod, tx.Amount, tx.Status);
                    }
                }
            }

            // Profit Final (Simplificado)
            report.TotalNetProfit = report.CashGames.NetCashFlow
                                  + (report.Tournaments.TotalCollected - report.Tournaments.TotalPayouts)
                                  + report.RestaurantTotalSales
                                  - (day?.OperationalExpenses ?? 0);

            return report;
        }

        private void AddTreasuryEntry(TreasurySummaryDto t, PaymentMethod method, decimal amount, PaymentStatus status)
        {
            if (status == PaymentStatus.Pending) { t.TotalPendingDebt += amount; return; }
            if (method == PaymentMethod.Cash) t.TotalCashReceived += amount;
            if (method == PaymentMethod.Transfer) t.TotalTransfersReceived += amount;
        }

        private void SubtractTreasuryEntry(TreasurySummaryDto t, PaymentMethod method, decimal amount)
        {
            t.TotalPayoutsPaid += amount;
            if (method == PaymentMethod.Cash) t.TotalCashReceived -= amount;
        }

        public async Task<DailyReportDto?> GetDailyReportByDateAsync(DateTime date)
        {
            // Implementación similar a la anterior, buscando ID por fecha
            var day = await _workingDayRepo.GetByDateAsync(date);
            if (day == null) return null;
            return await GetDailyReportAsync(day.Id);
        }
    }
}