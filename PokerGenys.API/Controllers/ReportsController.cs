using Microsoft.AspNetCore.Mvc;
using PokerGenys.Services;

namespace PokerGenys.API.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _service;

        public ReportsController(IReportService service)
        {
            _service = service;
        }

        [HttpGet("daily")]
        public async Task<IActionResult> GetDailyReport([FromQuery] Guid date)
        {

            if (date == Guid.Empty) return BadRequest("WorkingDayId es requerido");

            var report = await _service.GetDailyReportAsync(date);
            return Ok(report);
        }
    }
}