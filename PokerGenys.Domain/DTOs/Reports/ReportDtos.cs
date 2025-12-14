using System;
using System.Collections.Generic;

namespace PokerGenys.Domain.DTOs.Reports
{
    public class DailyReportDto
    {
        public DateTime Date { get; set; }
        public decimal TotalNetProfit { get; set; }
        public TreasurySummaryDto GlobalTreasury { get; set; } = new();
        public CashGameDailySummaryDto CashGames { get; set; } = new();
        public TournamentDailySummaryDto Tournaments { get; set; } = new();
        public decimal RestaurantTotalSales { get; set; }
        public bool IsProvisional { get; set; }
    }

    public class TreasurySummaryDto
    {
        public decimal TotalCashReceived { get; set; }
        public decimal TotalTransfersReceived { get; set; }
        public Dictionary<string, decimal> BankBreakdown { get; set; } = new()
        {
            { "Bancolombia", 0 }, { "Nequi", 0 }, { "Daviplata", 0 }, { "Other", 0 }
        };
        public decimal TotalCourtesies { get; set; }
        public decimal TotalInternalBalanceUsed { get; set; }
        public decimal TotalPendingDebt { get; set; }
        public decimal TotalPayoutsPaid { get; set; }
    }

    public class CashGameDailySummaryDto
    {
        public int TotalTables { get; set; }
        public double TotalHours { get; set; }
        public decimal TotalBuyIns { get; set; }
        public decimal TotalCashOuts { get; set; }
        public decimal TotalRake { get; set; }
        public decimal NetCashFlow { get; set; }
    }

    public class TournamentDailySummaryDto
    {
        public int TotalTournaments { get; set; }
        public int TotalEntries { get; set; }
        public int TotalRebuys { get; set; }
        public int TotalAddons { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal TotalPrizePool { get; set; }
        public decimal TotalPayouts { get; set; }
        public decimal TotalStaffFee { get; set; }
        public decimal TotalRake { get; set; }
        public decimal TotalOverlay { get; set; }
        public List<TournamentEventDto> Events { get; set; } = new();
    }

    public class TournamentEventDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Entries { get; set; }
        public decimal Guaranteed { get; set; }
        public decimal PrizePool { get; set; }
        public decimal Overlay { get; set; }
        public string Status { get; set; }
    }
}