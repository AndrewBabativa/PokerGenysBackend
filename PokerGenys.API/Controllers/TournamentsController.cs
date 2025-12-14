using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using PokerGenys.Domain.Enums;
using PokerGenys.Domain.Models.Core;
using PokerGenys.Domain.Models.Tournaments; // Aquí están ahora tus Requests
using PokerGenys.Services;
using System;
using System.Collections.Generic; // Necesario para Dictionary
using System.Threading.Tasks;

namespace PokerGenys.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TournamentsController : ControllerBase
    {
        private readonly ITournamentService _service;

        public TournamentsController(ITournamentService service)
        {
            _service = service;
        }

        // ==================================================================================
        // 1. CRUD BÁSICO
        // ==================================================================================

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tournaments = await _service.GetAllAsync();
            return Ok(tournaments);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var tournament = await _service.GetByIdAsync(id);
            if (tournament == null) return NotFound("Torneo no encontrado.");
            return Ok(tournament);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Tournament tournament)
        {
            try
            {
                var created = await _service.CreateAsync(tournament);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] Tournament tournament)
        {
            if (id != tournament.Id) return BadRequest("ID mismatch.");
            try
            {
                var updated = await _service.UpdateAsync(tournament);
                return Ok(updated);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var success = await _service.DeleteAsync(id);
                if (!success) return NotFound();
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==================================================================================
        // 2. CONTROL DE JUEGO
        // ==================================================================================

        [HttpPost("{id}/start")]
        public async Task<IActionResult> Start(Guid id)
        {
            var t = await _service.StartTournamentAsync(id);
            if (t == null) return NotFound();
            return Ok(t);
        }

        [HttpPost("{id}/pause")]
        public async Task<IActionResult> Pause(Guid id)
        {
            var t = await _service.PauseTournamentAsync(id);
            if (t == null) return NotFound();
            return Ok(t);
        }

        [HttpGet("{id}/state")]
        public async Task<IActionResult> GetState(Guid id)
        {
            var state = await _service.GetTournamentStateAsync(id);
            if (state == null) return NotFound();
            return Ok(state);
        }

        // ==================================================================================
        // 3. JUGADORES (REGISTRO, REBUYS, ADDONS)
        // ==================================================================================

        [HttpGet("{id}/registrations")]
        public async Task<IActionResult> GetRegistrations(Guid id)
        {
            var regs = await _service.GetRegistrationsAsync(id);
            return Ok(regs);
        }

        [HttpPost("{id}/register")]
        public async Task<IActionResult> RegisterPlayer(Guid id, [FromBody] RegisterRequest request)
        {
            try
            {
                var result = await _service.RegisterPlayerAsync(
                    id,
                    request.PlayerName,
                    request.PaymentMethod.ToString(),
                    request.Bank,
                    request.Reference,
                    request.PlayerId
                );

                if (result == null) return NotFound("Torneo no encontrado.");
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/rebuy")]
        public async Task<IActionResult> RebuyPlayer(Guid id, [FromBody] RebuyRequest request)
        {
            try
            {
                var result = await _service.RebuyPlayerAsync(
                    id,
                    request.RegistrationId,
                    request.PaymentMethod.ToString(),
                    request.Bank,
                    request.Reference
                );

                if (result == null) return BadRequest("No se pudo procesar el Rebuy.");
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/addon")]
        public async Task<IActionResult> AddOnPlayer(Guid id, [FromBody] AddonRequest request)
        {
            try
            {
                var result = await _service.AddOnPlayerAsync(
                    id,
                    request.RegistrationId,
                    request.PaymentMethod.ToString(),
                    request.Bank,
                    request.Reference
                );

                if (result == null) return BadRequest("No se pudo procesar el Add-On.");
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}/players/{regId}")]
        public async Task<IActionResult> RemovePlayer(Guid id, Guid regId)
        {
            try
            {
                var result = await _service.RemoveRegistrationAsync(id, regId);
                if (!result.Success) return NotFound("Registro no encontrado.");
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/seat")]
        public async Task<IActionResult> AssignSeat(Guid id, [FromBody] SeatRequest request)
        {
            try
            {
                // Validación básica de IDs
                if (!Guid.TryParse(request.RegistrationId, out Guid regGuid))
                    return BadRequest("RegistrationId inválido.");

                var reg = await _service.AssignSeatAsync(
                    id,
                    regGuid,
                    request.TableId,
                    request.SeatId // En el Request del dominio le pusimos SeatId para coincidir con el servicio
                );

                if (reg == null) return BadRequest("Fallo al asignar silla.");
                return Ok(reg);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==================================================================================
        // 4. VENTAS Y SERVICIOS
        // ==================================================================================

        [HttpPost("{id}/sales")]
        public async Task<IActionResult> RecordSale(Guid id, [FromBody] ServiceSaleRequest request)
        {
            try
            {
                var metadata = new Dictionary<string, object>();
                if (request.Items != null)
                {
                    foreach (var item in request.Items) metadata.Add(item.Key, item.Value);
                }

                var tx = await _service.RecordServiceSaleAsync(
                    id,
                    request.PlayerId,
                    request.Amount,
                    request.Description,
                    metadata,
                    request.PaymentMethod.ToString(),
                    request.Bank,
                    request.Reference
                );

                if (tx == null) return NotFound("Torneo no encontrado.");
                return Ok(tx);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(Guid id, [FromBody] JsonPatchDocument<Tournament> patchDoc)
        {
            if (patchDoc == null) return BadRequest("No patch document provided.");

            try
            {
                // 1. Obtener el torneo existente de la BD
                // Asumo que tienes un método GetByIdAsync en tu servicio
                var tournament = await _service.GetByIdAsync(id);

                if (tournament == null) return NotFound($"Tournament with id {id} not found.");

                // 2. Aplicar los cambios al objeto en memoria
                // ModelState captura errores si intentan parchear propiedades que no existen o tipos incorrectos
                patchDoc.ApplyTo(tournament, error =>
                {
                    ModelState.AddModelError(error.Operation.path ?? "JsonPatch", error.ErrorMessage);
                });
                // 3. Validar si el modelo resultante es válido
                if (!ModelState.IsValid) return BadRequest(ModelState);

                // 4. Guardar los cambios usando tu método Update existente
                var updated = await _service.UpdateAsync(tournament);

                return Ok(updated);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}