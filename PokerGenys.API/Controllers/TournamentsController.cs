using Microsoft.AspNetCore.Mvc;
using PokerGenys.Domain.Models;
using PokerGenys.Services;
using System;
using System.Threading.Tasks;

namespace PokerGenys.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TournamentsController : ControllerBase
    {
        private readonly ITournamentService _service;

        public TournamentsController(ITournamentService service) => _service = service;

        // CRUD TORNEOS
        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var t = await _service.GetByIdAsync(id);
            return t == null ? NotFound() : Ok(t);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Tournament t)
        {
            var created = await _service.CreateAsync(t);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] Tournament t)
        {
            if (id != t.Id) return BadRequest("ID mismatch");
            return Ok(await _service.UpdateAsync(t));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var ok = await _service.DeleteAsync(id);
            return ok ? NoContent() : NotFound();
        }

        // ============================================================
        // REGISTRATIONS
        // ============================================================

        [HttpGet("{id}/registrations")]
        public async Task<IActionResult> GetRegistrations(Guid id)
        {
            var regs = await _service.GetRegistrationsAsync(id);
            return Ok(regs);
        }

        [HttpPost("{id}/registrations")]
        public async Task<IActionResult> AddRegistration(Guid id, [FromBody] TournamentRegistration reg)
        {
            var t = await _service.AddRegistrationAsync(id, reg);
            return t == null ? NotFound() : Ok(t);
        }

        [HttpDelete("{id}/registrations/{regId}")]
        public async Task<IActionResult> RemoveRegistration(Guid id, Guid regId)
        {
            var ok = await _service.RemoveRegistrationAsync(id, regId);
            return ok ? NoContent() : NotFound();
        }

        // ============================================================
        // SEATING
        // ============================================================

        public class SeatRequest { public string TableId { get; set; } = ""; public string SeatId { get; set; } = ""; }

        [HttpPost("{id}/registrations/{regId}/seat")]
        public async Task<IActionResult> AssignSeat(Guid id, Guid regId, [FromBody] SeatRequest req)
        {
            var reg = await _service.AssignSeatAsync(id, regId, req.TableId, req.SeatId);
            return reg == null ? NotFound() : Ok(reg);
        }
    }
}
