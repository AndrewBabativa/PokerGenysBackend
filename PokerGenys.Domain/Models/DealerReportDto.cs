using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerGenys.Domain.Models
{
    public class DealerReportDto
    {
        public Guid DealerId { get; set; }
        public string DealerName { get; set; } = string.Empty;
        public int TotalMinutes { get; set; }
        public decimal TotalPayable { get; set; } // Valor a pagar calculado
        public decimal CostPerHour { get; set; }  // Tarifa base
    }
}
