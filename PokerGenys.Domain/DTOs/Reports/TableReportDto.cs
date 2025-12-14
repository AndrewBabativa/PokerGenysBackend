namespace PokerGenys.Domain.DTOs.Reports
{
    public class TableReportDto
    {
        public Guid TableId { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // 1. MÉTRICAS OPERATIVAS
        public decimal TotalBuyIns { get; set; }
        public decimal TotalCashOuts { get; set; }
        public decimal TotalRestaurantSales { get; set; }
        public decimal TotalJackpotAllocated { get; set; }

        // 2. TESORERÍA (Reutilizamos el DTO que ya tienes en ReportDtos.cs)
        public TreasurySummaryDto Treasury { get; set; } = new TreasurySummaryDto();

        // 3. DETALLE POR JUGADOR Y DEALER
        public List<PlayerReportDto> Players { get; set; } = new List<PlayerReportDto>();
        public List<DealerReportDto> Dealers { get; set; } = new List<DealerReportDto>();
    }

    // Detalle de un jugador en esa mesa específica
    public class PlayerReportDto
    {
        public Guid PlayerId { get; set; }
        public string PlayerName { get; set; } = "Jugador";
        public string Duration { get; set; } // "5h 30m"
        public int TotalMinutes { get; set; }

        public decimal BuyIn { get; set; }
        public decimal CashOut { get; set; }
        public decimal Restaurant { get; set; }
        public decimal NetResult { get; set; } // CashOut - (BuyIn + Restaurant)

        public bool HasPendingDebt { get; set; }
    }

    // Detalle de un dealer en esa mesa (para calcular pago)
    public class DealerReportDto
    {
        public Guid DealerId { get; set; }
        public string DealerName { get; set; } = string.Empty;
        public int TotalMinutes { get; set; }
        public decimal TotalPayable { get; set; }
        public decimal CostPerHour { get; set; }
    }
}