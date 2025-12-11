using Microsoft.AspNetCore.Mvc;
using PokerGenys.Domain.Models;
using PokerGenys.Services;
using System;
using System.Threading.Tasks;

namespace PokerGenys.API.Controllers
{
    [ApiController]
    // OJO: Quitamos la ruta base global para definirla por método y ser más claros
    public class DealersController : ControllerBase
    {
        private readonly IDealerService _service;

        public DealersController(IDealerService service)
        {
            _service = service;
        }

        // ============================================================
        // 1. GESTIÓN DE DEALERS (CRUD)
        // ============================================================

        [HttpGet("api/dealers")]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _service.GetAllDealersAsync());
        }

        [HttpGet("api/dealers/{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var dealer = await _service.GetByIdAsync(id);
            return dealer == null ? NotFound() : Ok(dealer);
        }

        [HttpPost("api/dealers")]
        public async Task<IActionResult> Create([FromBody] Dealer dealer)
        {
            // Validaciones básicas
            if (string.IsNullOrWhiteSpace(dealer.FirstName))
                return BadRequest("El nombre es obligatorio.");

            var created = await _service.CreateAsync(dealer);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("api/dealers/{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] Dealer dealer)
        {
            if (id != dealer.Id) return BadRequest("ID mismatch");

            var updated = await _service.UpdateAsync(dealer);
            return updated == null ? NotFound() : Ok(updated);
        }

        [HttpDelete("api/dealers/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }

        // ============================================================
        // 2. GESTIÓN DE TURNOS (DEALER SHIFTS)
        // ============================================================

        [HttpGet("api/dealer-shifts")]
        public async Task<IActionResult> GetShifts([FromQuery] Guid tableId)
        {
            if (tableId == Guid.Empty)
                return BadRequest("El parámetro 'tableId' es obligatorio.");

            var shifts = await _service.GetShiftsAsync(tableId);
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
            if (id != shift.Id) return BadRequest("ID mismatch");

            var updated = await _service.UpdateShiftAsync(shift);
            return updated == null ? NotFound() : Ok(updated);
        }
    }
}