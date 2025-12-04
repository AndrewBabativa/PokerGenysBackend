using Microsoft.AspNetCore.Mvc;
using PokerGenys.Domain.Models;
using PokerGenys.Services;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;

namespace PokerGenys.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TournamentsController : ControllerBase
    {
        private readonly ITournamentService _service;
        private readonly IHttpClientFactory _httpClientFactory;

        // VERIFICA QUE ESTA URL SEA EXACTA Y NO TENGA ESPACIOS AL FINAL
        private const string NODE_SERVER_URL = "https://pokersocketserver.onrender.com/api/webhook/emit";

        public TournamentsController(ITournamentService service, IHttpClientFactory httpClientFactory)
        {
            _service = service;
            _httpClientFactory = httpClientFactory;
        }

        // --- MÉTODO HELPER (OBLIGATORIO USAR AWAIT) ---
        private async Task NotifyNodeServer(Guid tournamentId, string eventName, object payload)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();

                var body = new
                {
                    tournamentId = tournamentId,
                    @event = eventName,
                    data = payload
                };

                var json = JsonSerializer.Serialize(body);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Console.WriteLine($"[C#] Enviando Webhook a Node: {eventName}..."); // LOG PARA DEPURAR

                // ⚠️ CAMBIO CRÍTICO: Usamos 'await' para asegurar que el mensaje salga antes de cerrar el request
                var response = await client.PostAsync(NODE_SERVER_URL, content);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[C#] Error Webhook: {response.StatusCode}");
                }
                else
                {
                    Console.WriteLine($"[C#] Webhook Enviado OK.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[C#] Excepción conectando a Node: {ex.Message}");
            }
        }

        // ... [GET, POST Create, PUT Update se mantienen igual] ...

        // ============================================================
        // GESTIÓN DE JUGADORES (MODIFICADO)
        // ============================================================

        [HttpDelete("{id}/registrations/{regId}")]
        public async Task<IActionResult> RemoveRegistration(Guid id, Guid regId)
        {
            var result = await _service.RemoveRegistrationAsync(id, regId);

            if (!result.Success) return NotFound();

            // 1. Notificar eliminación (AWAIT OBLIGATORIO)
            await NotifyNodeServer(id, "player-action", new
            {
                action = "remove",
                payload = new { registrationId = regId }
            });

            // 2. Notificar instrucciones
            if (!string.IsNullOrEmpty(result.InstructionType))
            {
                await NotifyNodeServer(id, "tournament-instruction", new
                {
                    type = result.InstructionType,
                    message = result.Message,
                    payload = new { fromTable = result.FromTable, toTable = result.ToTable }
                });
            }

            return Ok(result);
        }

        [HttpPost("{id}/register")]
        public async Task<IActionResult> RegisterPlayer(Guid id, [FromBody] string playerName)
        {
            var result = await _service.RegisterPlayerAsync(id, playerName);

            if (result == null) return NotFound();

            await NotifyNodeServer(id, "player-action", new
            {
                action = "add",
                payload = result.Registration
            });

            if (!string.IsNullOrEmpty(result.InstructionType))
            {
                await NotifyNodeServer(id, "tournament-instruction", new
                {
                    type = result.InstructionType,
                    message = result.SystemMessage
                });
            }

            return Ok(result);
        }

        [HttpPost("{id}/start")]
        public async Task<IActionResult> StartTournament(Guid id)
        {
            var tournament = await _service.StartTournamentAsync(id);
            if (tournament == null) return NotFound();

            await NotifyNodeServer(id, "tournament-control", new
            {
                type = "start",
                data = new { level = tournament.CurrentLevel }
            });

            return Ok(tournament);
        }

        // ============================================================
        // REBUY (Con lógica de validación básica en backend)
        // ============================================================
        [HttpPost("{id}/registrations/{regId}/rebuy")]
        public async Task<IActionResult> RebuyPlayer(Guid id, Guid regId)
        {
            // Nota: La validación fuerte de nivel debe estar en _service.RebuyPlayerAsync
            var result = await _service.RebuyPlayerAsync(id, regId);

            if (result == null) return BadRequest("No se pudo realizar el rebuy (Verifique nivel o ID)");

            await NotifyNodeServer(id, "player-action", new
            {
                action = "add",
                payload = result.Registration
            });

            if (!string.IsNullOrEmpty(result.SystemMessage))
            {
                await NotifyNodeServer(id, "tournament-instruction", new
                {
                    type = "INFO_ALERT",
                    message = result.SystemMessage
                });
            }

            return Ok(result);
        }

        // ... (Payouts y Expenses igual, recuerda poner await NotifyNodeServer si agregas notificaciones ahí)
    }
}