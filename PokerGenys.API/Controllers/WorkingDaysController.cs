using Microsoft.AspNetCore.Mvc;
using PokerGenys.Domain.Models.Core; // ✅ Aquí están WorkingDay y los Requests
using PokerGenys.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PokerGenys.API.Controllers
{
    [ApiController]
    [Route("api/working-days")]
    public class WorkingDaysController : ControllerBase
    {
        private readonly IWorkingDayService _service;

        public WorkingDaysController(IWorkingDayService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var days = await _service.GetAllAsync();

                // Mapeo a anónimo para el frontend
                var response = days.Select(d => MapToDto(d));
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateWorkingDayRequest request)
        {
            try
            {
                var newDay = await _service.CreateAsync(request.InitialCash, request.Notes ?? "");
                return CreatedAtAction(nameof(GetAll), new { id = newDay.Id }, MapToDto(newDay));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno al crear la jornada." });
            }
        }

        [HttpPost("{id}/close")]
        public async Task<IActionResult> Close(Guid id, [FromBody] CloseWorkingDayRequest request)
        {
            try
            {
                var closedDay = await _service.CloseDayAsync(
                    id,
                    request.FinalCashCount,
                    request.Expenses,
                    request.Notes ?? ""
                );
                return Ok(MapToDto(closedDay));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // --- MAPPER PRIVADO ---
        // Adapta el modelo de Dominio (C#) al JSON que espera React
        private static object MapToDto(WorkingDay d)
        {
            return new
            {
                id = d.Id,
                date = d.StartAt,
                status = d.Status.ToString(),

                // Nombres que espera el Frontend (camelCase)
                initialCash = d.InitialCapita,
                finalCashCount = d.FinalCapitaDeclared,
                expenses = d.OperationalExpenses,
                notes = d.Notes,

                expectedCash = d.SystemExpectedCash,
                variance = d.CashVariance,

                // Métricas adicionales
                cashGameBuyIns = d.CashGameBuyIns,
                tournamentCollected = d.TournamentCollected
            };
        }
    }
}