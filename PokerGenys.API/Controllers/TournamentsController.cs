using Microsoft.AspNetCore.Mvc;
using PokerGenys.Domain.Models;
using PokerGenys.Domain.Models.Tournaments; // Aquí están tus modelos (ServiceSaleRequest, etc.)
using PokerGenys.Services;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace PokerGenys.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TournamentsController : ControllerBase
    {
        private readonly ITournamentService _service;
        private readonly IHttpClientFactory _httpClientFactory;
        private const string NODE_SERVER_URL = "https://pokersocketserver.onrender.com/api/webhook/emit";
        private readonly SocketNotificationService _notifier; 

        public TournamentsController(ITournamentService service, IHttpClientFactory httpClientFactory, SocketNotificationService notifier)
        {
            _service = service;
            _httpClientFactory = httpClientFactory;
            _notifier = notifier;
        }

        private async Task NotifyNodeServerSafe(Guid tournamentId, string eventName, object payload)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var client = _httpClientFactory.CreateClient("NodeServer"); // Configurado en Startup
                    var body = new { tournamentId, @event = eventName, data = payload };
                    var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

                    var response = await client.PostAsync("/api/webhook/emit", content);
                    if (!response.IsSuccessStatusCode)
                    {
                        // Loguear error real
                        Console.WriteLine($"[Webhook Fail] {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Webhook Crash] {ex.Message}");
                }
            });
        }

        private async Task NotifyWithStats(Guid tournamentId, string action, object payload)
        {
            var t = await _service.GetByIdAsync(tournamentId);

            if (t != null)
            {
                var stats = new
                {
                    entries = t.TotalEntries,
                    active = t.ActivePlayers,
                    prizePool = t.PrizePool
                };

                await NotifyNodeServerSafe(tournamentId, "player-action", new { action, payload, stats });
            }
        }

        // ============================================================
        // 1. CONTROL DE JUEGO (Start / Pause)
        // ============================================================
        [HttpPost("{id}/start")]
        public async Task<IActionResult> StartTournament(Guid id)
        {
            var t = await _service.StartTournamentAsync(id);
            if (t == null) return NotFound();

            // USAMOS EL NUEVO NOTIFIER
            await _notifier.QueueNotificationAsync(id, "tournament-control", new
            {
                type = "start",
                data = new
                {
                    level = t.CurrentLevel,
                    timeLeft = t.ClockState.SecondsRemaining,
                    lastUpdatedAt = t.ClockState.LastUpdatedAt,
                    status = "Running"
                }
            });

            return Ok(t);
        }

        [HttpPost("{id}/pause")]
        public async Task<IActionResult> PauseTournament(Guid id)
        {
            var t = await _service.PauseTournamentAsync(id);
            if (t == null) return NotFound();

            await _notifier.QueueNotificationAsync(id, "tournament-control", new
            {
                type = "pause",
                data = new { level = t.CurrentLevel, timeLeft = t.ClockState?.SecondsRemaining ?? 0 }
            });
            return Ok(t);
        }

        [HttpGet("{id}/state")]
        public async Task<IActionResult> GetState(Guid id)
        {
            var state = await _service.GetTournamentStateAsync(id);
            return state == null ? NotFound() : Ok(state);
        }

        // ============================================================
        // 2. CRUD BÁSICO
        // ============================================================

        [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

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
        // 3. JUGADORES Y ACCIONES
        // ============================================================

        [HttpGet("{id}/registrations")]
        public async Task<IActionResult> GetRegistrations(Guid id) => Ok(await _service.GetRegistrationsAsync(id));

        [HttpPost("{id}/register")]
        public async Task<IActionResult> RegisterPlayer(Guid id, [FromBody] RegisterRequest req)
        {
            var result = await _service.RegisterPlayerAsync(id, req.PlayerName, req.PaymentMethod, req.Bank, req.Reference);
            if (result == null) return BadRequest("No se pudo registrar.");

            await _notifier.QueueNotificationAsync(id, "player-action", new
            {
                action = "add",
                payload = result.Registration,
                stats = result.NewStats 
            });

            return Ok(result);
        }

        // ... imports

        [HttpDelete("{id}/registrations/{regId}")]
        public async Task<IActionResult> RemoveRegistration(Guid id, Guid regId)
        {
            var result = await _service.RemoveRegistrationAsync(id, regId);
            if (!result.Success) return NotFound();

            // Notificar eliminación
            await _notifier.QueueNotificationAsync(id, "player-action", new
            {
                action = "remove",
                payload = new { id = regId, status = "Eliminated" }
            });

            // Notificar instrucciones especiales (Mesa final, Ganador)
            if (!string.IsNullOrEmpty(result.InstructionType))
            {
                await _notifier.QueueNotificationAsync(id, "tournament-instruction", new
                {
                    type = result.InstructionType,
                    message = result.Message,
                    // data = extraData
                });
            }

            return Ok(result);
        }

        // Usa SeatRequest definido abajo
        [HttpPost("{id}/registrations/{regId}/seat")]
        public async Task<IActionResult> AssignSeat(Guid id, Guid regId, [FromBody] SeatRequest req)
        {
            var reg = await _service.AssignSeatAsync(id, regId, req.TableId, req.SeatId);
            if (reg == null) return NotFound();
            await NotifyNodeServerSafe(id, "player-action", new { action = "move", payload = reg });
            return Ok(reg);
        }

        [HttpGet("{id}/tables")]
        public async Task<IActionResult> GetTables(Guid id)
        {
            var t = await _service.GetByIdAsync(id);
            if (t == null) return NotFound();
            return Ok(t.Tables ?? new List<TournamentTable>());
        }

        // Usa GamePaymentRequest (Que YA existe en tus modelos de dominio)
        [HttpPost("{id}/registrations/{regId}/rebuy")]
        public async Task<IActionResult> RebuyPlayer(Guid id, Guid regId, [FromBody] GamePaymentRequest req)
        {
            var result = await _service.RebuyPlayerAsync(id, regId, req.PaymentMethod, req.Bank, req.Reference);
            if (result == null) return BadRequest("Rebuy no permitido");
            await NotifyWithStats(id, "rebuy", result.Registration);
            return Ok(result);
        }

        [HttpPost("{id}/registrations/{regId}/addon")]
        public async Task<IActionResult> AddOnPlayer(Guid id, Guid regId, [FromBody] GamePaymentRequest req)
        {
            var result = await _service.AddOnPlayerAsync(id, regId, req.PaymentMethod, req.Bank, req.Reference);
            if (result == null) return BadRequest("Add-on no disponible");
            await NotifyNodeServerSafe(id, "player-action", new { action = "addon", payload = result.Registration });
            return Ok(result);
        }

        // Usa ServiceSaleRequest (Que YA existe en tus modelos de dominio)
        [HttpPost("{id}/sales")]
        public async Task<IActionResult> RecordSale(Guid id, [FromBody] ServiceSaleRequest req)
        {
            var tx = await _service.RecordServiceSaleAsync(id, req.PlayerId, req.Amount, req.Description, req.Items, req.PaymentMethod, req.Bank, req.Reference);
            if (tx == null) return NotFound();
            return Ok(tx);
        }

        [HttpPost("{id}/transactions")]
        public async Task<IActionResult> RecordGenericTransaction(Guid id, [FromBody] TournamentTransaction tx)
        {
            if (tx.TournamentId != Guid.Empty && tx.TournamentId != id) return BadRequest("ID mismatch");
            var result = await _service.RecordTransactionAsync(id, tx);
            return result == null ? NotFound() : Ok(result);
        }

    }
}