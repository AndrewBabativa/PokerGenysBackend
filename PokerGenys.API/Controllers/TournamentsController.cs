using Microsoft.AspNetCore.Mvc;
using PokerGenys.Domain.Models;
using PokerGenys.Domain.Models.Tournaments;
using PokerGenys.Services;
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

        // URL de tu servidor Node.js
        private const string NODE_SERVER_URL = "https://pokersocketserver.onrender.com/api/webhook/emit";

        public TournamentsController(ITournamentService service, IHttpClientFactory httpClientFactory)
        {
            _service = service;
            _httpClientFactory = httpClientFactory;
        }

        // --- WEBHOOK HELPER (Fire & Forget seguro) ---
        private async Task NotifyNodeServer(Guid tournamentId, string eventName, object payload)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                // Timeout corto para no bloquear el hilo de .NET si Node está lento
                client.Timeout = TimeSpan.FromSeconds(2);

                var body = new { tournamentId, @event = eventName, data = payload };
                var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

                // No usamos 'await' bloqueante estricto para la respuesta, solo para el envío
                _ = client.PostAsync(NODE_SERVER_URL, content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Webhook Error] {ex.Message}");
            }
        }

        // ============================================================
        // 1. CONTROL DE JUEGO (START / PAUSE / STATE)
        // ============================================================

        [HttpPost("{id}/start")]
        public async Task<IActionResult> StartTournament(Guid id)
        {
            var t = await _service.StartTournamentAsync(id);
            if (t == null) return NotFound();

            // OPTIMIZACIÓN: Enviamos timeLeft aquí también para arranque instantáneo en frontend
            var timeLeft = t.ClockState?.SecondsRemaining ?? 0;

            await NotifyNodeServer(id, "tournament-control", new
            {
                type = "start",
                data = new
                {
                    level = t.CurrentLevel,
                    timeLeft = timeLeft // <--- CRÍTICO para sincronización inmediata
                }
            });
            return Ok(t);
        }

        [HttpPost("{id}/pause")]
        public async Task<IActionResult> PauseTournament(Guid id)
        {
            // 1. El servicio calcula el tiempo restante exacto y lo guarda en BD
            var t = await _service.PauseTournamentAsync(id);
            if (t == null) return NotFound();

            // 2. Obtenemos el tiempo congelado
            // CORRECCIÓN: Acceder a la propiedad SecondsRemaining, no al objeto ClockState nulo
            var frozenTime = t.ClockState?.SecondsRemaining ?? 0;

            // 3. Emitir evento al Socket Server
            await NotifyNodeServer(id, "tournament-control", new
            {
                type = "pause",
                data = new
                {
                    level = t.CurrentLevel,
                    timeLeft = frozenTime
                }
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
        // 3. GESTIÓN DE JUGADORES (REGISTRATIONS)
        // ============================================================

        [HttpGet("{id}/registrations")]
        public async Task<IActionResult> GetRegistrations(Guid id) => Ok(await _service.GetRegistrationsAsync(id));

        public class RegisterRequest
        {
            public string PlayerName { get; set; } = "";
            public string PaymentMethod { get; set; } = "Cash";
            public string? Bank { get; set; }
            public string? Reference { get; set; }
        }

        [HttpPost("{id}/register")]
        public async Task<IActionResult> RegisterPlayer(Guid id, [FromBody] RegisterRequest req)
        {
            var result = await _service.RegisterPlayerAsync(id, req.PlayerName, req.PaymentMethod, req.Bank, req.Reference);
            if (result == null) return BadRequest("No se pudo registrar (Verifique estado del torneo)");

            await NotifyNodeServer(id, "player-action", new { action = "add", payload = result.Registration });

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

        [HttpDelete("{id}/registrations/{regId}")]
        public async Task<IActionResult> RemoveRegistration(Guid id, Guid regId)
        {
            var result = await _service.RemoveRegistrationAsync(id, regId);
            if (!result.Success) return NotFound();

            await NotifyNodeServer(id, "player-action", new { action = "remove", payload = new { registrationId = regId } });

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

        // ============================================================
        // 4. ASIENTOS Y MOVIMIENTOS
        // ============================================================

        public class SeatRequest { public string TableId { get; set; } = ""; public string SeatId { get; set; } = ""; }

        [HttpPost("{id}/registrations/{regId}/seat")]
        public async Task<IActionResult> AssignSeat(Guid id, Guid regId, [FromBody] SeatRequest req)
        {
            var reg = await _service.AssignSeatAsync(id, regId, req.TableId, req.SeatId);
            if (reg == null) return NotFound();

            // Notificar cambio de asiento para actualizar el Dashboard en tiempo real
            await NotifyNodeServer(id, "player-action", new { action = "move", payload = reg });

            return Ok(reg);
        }

        [HttpGet("{id}/tables")]
        public async Task<IActionResult> GetTables(Guid id)
        {
            var t = await _service.GetByIdAsync(id);
            if (t == null) return NotFound();
            return Ok(t.Tables ?? new List<TournamentTable>());
        }

        // ============================================================
        // 5. TRANSACCIONES DE JUEGO (REBUY, ADDON)
        // ============================================================

        public class GamePaymentRequest
        {
            public string PaymentMethod { get; set; } = "Cash";
            public string? Bank { get; set; }
            public string? Reference { get; set; }
        }

        [HttpPost("{id}/registrations/{regId}/rebuy")]
        public async Task<IActionResult> RebuyPlayer(Guid id, Guid regId, [FromBody] GamePaymentRequest req)
        {
            var result = await _service.RebuyPlayerAsync(id, regId, req.PaymentMethod, req.Bank, req.Reference);
            if (result == null) return BadRequest("Rebuy no permitido");

            await NotifyNodeServer(id, "player-action", new { action = "rebuy", payload = result.Registration });

            if (!string.IsNullOrEmpty(result.SystemMessage))
            {
                await NotifyNodeServer(id, "tournament-instruction", new { type = "INFO", message = result.SystemMessage });
            }
            return Ok(result);
        }

        [HttpPost("{id}/registrations/{regId}/addon")]
        public async Task<IActionResult> AddOnPlayer(Guid id, Guid regId, [FromBody] GamePaymentRequest req)
        {
            var result = await _service.AddOnPlayerAsync(id, regId, req.PaymentMethod, req.Bank, req.Reference);
            if (result == null) return BadRequest("Add-on no disponible");

            await NotifyNodeServer(id, "player-action", new { action = "addon", payload = result.Registration });
            return Ok(result);
        }

        // ============================================================
        // 6. VENTAS Y CAJA (Sales)
        // ============================================================

        public class ServiceSaleRequest
        {
            public Guid? PlayerId { get; set; }
            public decimal Amount { get; set; }
            public string Description { get; set; } = "Venta";
            public Dictionary<string, object> Items { get; set; } = new();
            public string PaymentMethod { get; set; } = "Cash";
            public string? Bank { get; set; }
            public string? Reference { get; set; }
        }

        [HttpPost("{id}/sales")]
        public async Task<IActionResult> RecordSale(Guid id, [FromBody] ServiceSaleRequest req)
        {
            var tx = await _service.RecordServiceSaleAsync(
                id, req.PlayerId, req.Amount, req.Description, req.Items,
                req.PaymentMethod, req.Bank, req.Reference
            );

            if (tx == null) return NotFound();
            return Ok(tx);
        }

        [HttpPost("{id}/transactions")]
        public async Task<IActionResult> RecordGenericTransaction(Guid id, [FromBody] TournamentTransaction tx)
        {
            if (tx.TournamentId != Guid.Empty && tx.TournamentId != id) return BadRequest("Tournament ID mismatch");
            var result = await _service.RecordTransactionAsync(id, tx);
            return result == null ? NotFound() : Ok(result);
        }
    }
}