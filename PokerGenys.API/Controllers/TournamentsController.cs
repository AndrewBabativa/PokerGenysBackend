// API/Controllers/TournamentsController.cs
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

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var t = await _service.GetByIdAsync(id);
            if (t == null) return NotFound();
            return Ok(t);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Tournament tournament)
        {
            var t = await _service.CreateAsync(tournament);
            return CreatedAtAction(nameof(Get), new { id = t.Id }, t);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] Tournament tournament)
        {
            if (id != tournament.Id) return BadRequest("ID mismatch");
            var t = await _service.UpdateAsync(tournament);
            return Ok(t);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }
    }
}
