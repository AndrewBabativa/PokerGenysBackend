using Microsoft.AspNetCore.Mvc;
using PokerGenys.Domain.Models;
using PokerGenys.Services;
using System;
using System.Threading.Tasks;

namespace PokerGenys.API.Controllers
{
    [ApiController]
    [Route("api/waitlist")]
    public class WaitlistController : ControllerBase
    {
        private readonly IWaitlistService _service;

        public WaitlistController(IWaitlistService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] Guid? tableId)
        {
            if (tableId.HasValue)
            {
                var list = await _service.GetByTableAsync(tableId.Value);
                return Ok(list);
            }

            var all = await _service.GetAllAsync();
            return Ok(all);
        }

        // DTO pequeño para recibir los datos del body limpiamente
        public class AddWaitlistRequest
        {
            public Guid TableId { get; set; }
            public Guid PlayerId { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] AddWaitlistRequest req)
        {
            try
            {
                var item = await _service.AddToWaitlistAsync(req.TableId, req.PlayerId);
                return Ok(item);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Remove(Guid id)
        {
            await _service.RemoveFromWaitlistAsync(id);
            return NoContent();
        }

        // Endpoint especial que ejecuta la "transacción" de sentar jugador
        [HttpPost("{id}/seat")]
        public async Task<IActionResult> Seat(Guid id)
        {
            var session = await _service.SeatPlayerAsync(id);

            if (session == null)
                return BadRequest("No se pudo sentar al jugador. Verifique ID o configuración de mesa.");

            return Ok(session);
        }
    }
}