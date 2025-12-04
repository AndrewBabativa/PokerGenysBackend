using Microsoft.AspNetCore.Mvc;
using PokerGenys.Domain.Models;
using PokerGenys.Services;
using System;
using System.Reflection;
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

        // URL de tu servidor Node.js
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

        // ============================================================
        // CRUD BÁSICO TORNEOS
        // ============================================================
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

        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(Guid id, [FromBody] JsonElement patch)
        {
            var tournament = await _service.GetByIdAsync(id);
            if (tournament == null) return NotFound();

            foreach (var prop in patch.EnumerateObject())
            {
                var property = typeof(Tournament).GetProperty(prop.Name,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (property != null)
                {
                    var value = prop.Value.Deserialize(property.PropertyType);
                    property.SetValue(tournament, value);
                }
            }
            if (tournament.Status.Equals("Running", StringComparison.OrdinalIgnoreCase) && tournament.CurrentLevel == 1)
                tournament.StartTime = DateTime.Now;

            await _service.UpdateAsync(tournament);

            // OPCIONAL: Notificar cambios de estado generales si es necesario
            return Ok(tournament);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var ok = await _service.DeleteAsync(id);
            return ok ? NoContent() : NotFound();
        }

        // ============================================================
        // GESTIÓN DE JUGADORES (CON NOTIFICACIONES A NODE)
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

        // ⚠️ CAMBIO: Eliminación + Notificación
        [HttpDelete("{id}/registrations/{regId}")]
        public async Task<IActionResult> RemoveRegistration(Guid id, Guid regId)
        {
            var result = await _service.RemoveRegistrationAsync(id, regId);

            if (!result.Success) return NotFound();

            // 1. Notificar eliminación
            await NotifyNodeServer(id, "player-action", new
            {
                action = "remove",
                payload = new { registrationId = regId }
            });

            // 2. Notificar instrucciones (Mesa final / Balanceo)
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

        // ⚠️ CAMBIO: Registro + Notificación
        [HttpPost("{id}/register")]
        public async Task<IActionResult> RegisterPlayer(Guid id, [FromBody] string playerName)
        {
            var result = await _service.RegisterPlayerAsync(id, playerName);

            if (result == null) return NotFound();

            // 1. Notificar nuevo jugador
            await NotifyNodeServer(id, "player-action", new
            {
                action = "add",
                payload = result.Registration
            });

            // 2. Notificar mensaje de sistema (ej: "Se abrió mesa 2")
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

        // ============================================================
        // SEATING & CONTROL
        // ============================================================

        public class SeatRequest { public string TableId { get; set; } = ""; public string SeatId { get; set; } = ""; }

        [HttpPost("{id}/registrations/{regId}/seat")]
        public async Task<IActionResult> AssignSeat(Guid id, Guid regId, [FromBody] SeatRequest req)
        {
            var reg = await _service.AssignSeatAsync(id, regId, req.TableId, req.SeatId);
            // El movimiento de asiento suele refrescarse solo por el front al recibir updates, 
            // pero podrías agregar un NotifyNodeServer aquí si quisieras animación instantánea en todos.
            return reg == null ? NotFound() : Ok(reg);
        }

        // ⚠️ CAMBIO: Start + Notificación
        [HttpPost("{id}/start")]
        public async Task<IActionResult> StartTournament(Guid id)
        {
            var tournament = await _service.StartTournamentAsync(id);
            if (tournament == null) return NotFound();

            // Avisar a Node que inicie el timer
            await NotifyNodeServer(id, "tournament-control", new
            {
                type = "start",
                data = new { level = tournament.CurrentLevel }
            });

            return Ok(tournament);
        }

        [HttpGet("{id}/state")]
        public async Task<IActionResult> GetTournamentState(Guid id)
        {
            var state = await _service.GetTournamentStateAsync(id);
            return state == null ? NotFound() : Ok(state);
        }

        // ============================================================
        // FINANZAS & REBUY
        // ============================================================

        public class PayoutRequest
        {
            public decimal Amount { get; set; }
            public string Method { get; set; } = "Cash";
            public string Notes { get; set; } = "";
        }

        [HttpPost("{id}/registrations/{regId}/payout")]
        public async Task<IActionResult> RecordPayout(Guid id, Guid regId, [FromBody] PayoutRequest req)
        {
            var tx = new TournamentTransaction
            {
                PlayerId = regId,
                Type = TournamentTransactionType.Payout,
                Amount = -Math.Abs(req.Amount),
                Method = req.Method,
                Notes = req.Notes
            };

            var result = await _service.RecordTransactionAsync(id, tx);
            if (result == null) return NotFound();

            return Ok(result);
        }

        [HttpPost("{id}/expenses")]
        public async Task<IActionResult> RecordExpense(Guid id, [FromBody] PayoutRequest req)
        {
            var tx = new TournamentTransaction
            {
                Type = TournamentTransactionType.Expense,
                Amount = -Math.Abs(req.Amount),
                Method = req.Method,
                Notes = req.Notes
            };

            var result = await _service.RecordTransactionAsync(id, tx);
            return Ok(result);
        }

        // ⚠️ CAMBIO: Rebuy + Notificación
        [HttpPost("{id}/registrations/{regId}/rebuy")]
        public async Task<IActionResult> RebuyPlayer(Guid id, Guid regId)
        {
            var result = await _service.RebuyPlayerAsync(id, regId);
            if (result == null) return BadRequest("No se pudo realizar el rebuy (Verifique nivel o ID)");

            // 1. Notificar resurrección del jugador
            await NotifyNodeServer(id, "player-action", new
            {
                action = "add", // Usamos add/update para que vuelva a aparecer
                payload = result.Registration
            });

            // 2. Notificar alerta
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
    }
}