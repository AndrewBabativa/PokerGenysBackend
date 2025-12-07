using PokerGenys.Domain.Models.Reports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerGenys.Services
{
    public interface IReportService
    {
        Task<DailyReportDto> GetDailyReportAsync(Guid workingDayId);
    }
}
