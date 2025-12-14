using System;

namespace PokerGenys.Domain.Models.Core
{
    public class CreateWorkingDayRequest
    {
        public decimal InitialCash { get; set; }
        public string? Notes { get; set; }
    }

    public class CloseWorkingDayRequest
    {
        public decimal FinalCashCount { get; set; }
        public decimal Expenses { get; set; }
        public string? Notes { get; set; }
    }
}