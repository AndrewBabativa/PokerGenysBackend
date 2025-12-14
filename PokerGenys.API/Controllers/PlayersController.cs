using Microsoft.AspNetCore.Mvc;
using PokerGenys.Domain.Models.Core;
using PokerGenys.Services;

namespace PokerGenys.API.Controllers
{
    [ApiController]
    [Route("api/players")]
    public class PlayersController : ControllerBase
    {
        private readonly IPlayerService _service;

        public PlayersController(IPlayerService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var players = await _service.GetAllAsync();
            return Ok(players);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var player = await _service.GetByIdAsync(id);
            return player == null ? NotFound() : Ok(player);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Player player)
        {
            try
            {
                var created = await _service.CreateAsync(player);
                return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] Player player)
        {
            if (id != player.Id) return BadRequest("ID mismatch");

            var updated = await _service.UpdateAsync(player);
            return updated == null ? NotFound() : Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
    }
}