using Microsoft.AspNetCore.Mvc;
using PokerGenys.Domain.Models;
using PokerGenys.Services;
using System;
using System.Threading.Tasks;

namespace PokerGenys.API.Controllers
{
    [ApiController]
    public class DealersController : ControllerBase
    {
        private readonly IDealerService _service;

        public DealersController(IDealerService service)
        {
            _service = service;
        }

        // --- DEALERS (Personas) ---

        [HttpGet("api/dealers")]
        public async Task<IActionResult> GetDealers()
        {
            var dealers = await _service.GetAllDealersAsync();
            return Ok(dealers);
        }

        // --- DEALER SHIFTS (Turnos) ---

        [HttpGet("api/dealer-shifts")]
        public async Task<IActionResult> GetShifts([FromQuery] Guid dayId, [FromQuery] Guid? tableId)
        {
            if (dayId == Guid.Empty)
                return BadRequest("El parámetro 'dayId' es obligatorio.");

            var shifts = await _service.GetShiftsAsync(dayId, tableId);
            return Ok(shifts);
        }

        [HttpPost("api/dealer-shifts")]
        public async Task<IActionResult> AddShift([FromBody] DealerShift shift)
        {
            var created = await _service.AddShiftAsync(shift);
            return Ok(created);
        }

        [HttpPut("api/dealer-shifts/{id}")]
        public async Task<IActionResult> UpdateShift(Guid id, [FromBody] DealerShift shift)
        {
            if (id != shift.Id)
                return BadRequest("ID mismatch");

            var updated = await _service.UpdateShiftAsync(shift);

            if (updated == null) return NotFound();

            return Ok(updated);
        }
    }
}