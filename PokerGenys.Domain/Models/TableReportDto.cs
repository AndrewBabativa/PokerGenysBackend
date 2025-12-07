using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerGenys.Domain.Models
{
    public class TableReportDto
    {
        public Guid TableId { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // 1. MÉTRICAS OPERATIVAS (Volumen de negocio)
        public decimal TotalBuyIns { get; set; }        // Fichas vendidas
        public decimal TotalCashOuts { get; set; }      // Fichas devueltas
        public decimal TotalRestaurantSales { get; set; } // Ventas totales (Bar/Cocina)
        public decimal TotalJackpotAllocated { get; set; } // Dinero que salió del juego al pozo (si aplica)

        // 2. TESORERÍA (¿Dónde está la plata?)
        public TreasurySummaryDto Treasury { get; set; } = new TreasurySummaryDto();

        // 3. DETALLE POR JUGADOR
        public List<PlayerReportDto> Players { get; set; } = new List<PlayerReportDto>();
    }
}
