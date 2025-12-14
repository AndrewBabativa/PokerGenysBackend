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
        public async Task<IActionResult> GetDailyReport([FromQuery] DateTime date) 
        {
            try
            {
                var report = await _service.GetDailyReportByDateAsync(date);

                if (report == null)
                    return NotFound(new { message = "No se encontró una jornada de trabajo para esta fecha." });

                return Ok(report);
            }
            catch (Exception ex)
            {
                // Loguear el error real para verlo en la consola de Render
                Console.WriteLine($"Error generando reporte: {ex.Message}");
                return StatusCode(500, "Error interno generando el reporte.");
            }
        }
    }
}