using Microsoft.AspNetCore.Mvc;
using PokerGenys.Domain.Models.CashGame;
using PokerGenys.Services;

namespace PokerGenys.API.Controllers
{
    [ApiController]
    [Route("api/sessions")]
    public class SessionsController : ControllerBase
    {
        private readonly ISessionService _service;

        public SessionsController(ISessionService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var sessions = await _service.GetAllAsync();
            return Ok(sessions);
        }

        [HttpGet("{tableId}")]
        public async Task<IActionResult> GetAllByTableId(Guid tableId)
        {
            // Validación opcional por si envían basura
            if (tableId == Guid.Empty) return BadRequest("ID inválido");

            var sessions = await _service.GetAllByTableIdAsync(tableId);
            return Ok(sessions);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CashSession session)
        {
            var created = await _service.CreateAsync(session);
            return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CashSession session)
        {
            if (id != session.Id)
                return BadRequest("ID mismatch");

            var updated = await _service.UpdateAsync(session);

            if (updated == null) return NotFound();

            return Ok(updated);
        }

        [HttpGet("report/{tableId}")]
        public async Task<IActionResult> GetTableReport(Guid tableId)
        {
            if (tableId == Guid.Empty)
                return BadRequest("El ID de la mesa no es válido.");

            var report = await _service.GetTableReportAsync(tableId);

            return Ok(report);
        }
    }
}