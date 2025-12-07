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
            try
            {
                var result = await _service.RegisterPlayerAsync(
                    id,
                    req.PlayerName,
                    req.PaymentMethod,
                    req.Bank,
                    req.Reference,
                    req.PlayerId // <--- Pasamos el ID si existe
                );

                if (result == null) return BadRequest("No se pudo registrar (verifique reglas de torneo).");

                // ... Notificación socket ...
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
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
            await _notifier.QueueNotificationAsync(id, "player-action", new { action = "move", payload = reg });
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
            await _notifier.QueueNotificationAsync(id, "rebuy", result.Registration);
            return Ok(result);
        }

        [HttpPost("{id}/registrations/{regId}/addon")]
        public async Task<IActionResult> AddOnPlayer(Guid id, Guid regId, [FromBody] GamePaymentRequest req)
        {
            var result = await _service.AddOnPlayerAsync(id, regId, req.PaymentMethod, req.Bank, req.Reference);
            if (result == null) return BadRequest("Add-on no disponible");
            await _notifier.QueueNotificationAsync(id, "player-action", new { action = "addon", payload = result.Registration });
            return Ok(result);
        }

        // Usa ServiceSaleRequest (Que YA existe en tus modelos de dominio)
        [HttpPost("{id}/sales")]
        public async Task<IActionResult> RecordSale(Guid id, [FromBody] ServiceSaleRequest req)
        {
            try
            {
                // Validación de seguridad
                if (req.Amount <= 0) return BadRequest("El monto debe ser mayor a 0");

                // CONVERSIÓN CLAVE: De <string, string> a <string, object>
                // Esto "limpia" los datos para que Mongo los acepte sin problemas
                var safeItems = req.Items?.ToDictionary(
                    k => k.Key,
                    v => (object)v.Value
                ) ?? new Dictionary<string, object>();

                var tx = await _service.RecordServiceSaleAsync(
                    id,
                    req.PlayerId,
                    req.Amount,
                    req.Description,
                    safeItems,
                    req.PaymentMethod,
                    req.Bank,
                    req.Reference
                );

                if (tx == null) return NotFound("Torneo no encontrado");

                return Ok(tx);
            }
            catch (Exception ex)
            {
                // Log para ver el error real en la consola de Render
                Console.WriteLine($"[ERROR VENTA] {ex.Message} \n {ex.StackTrace}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("{id}/transactions")]
        public async Task<IActionResult> RecordGenericTransaction(Guid id, [FromBody] TournamentTransaction tx)
        {
            if (tx.TournamentId != Guid.Empty && tx.TournamentId != id) return BadRequest("ID mismatch");
            var result = await _service.RecordTransactionAsync(id, tx);
            return result == null ? NotFound() : Ok(result);
        }

        // En TournamentsController.cs

        [HttpPost("{id}/finish")]
        public async Task<IActionResult> FinishTournament(Guid id)
        {
            // 1. Obtener torneo
            var t = await _service.GetByIdAsync(id);
            if (t == null) return NotFound();

            // 2. Aplicar lógica de cierre (Congelar estado)
            t.Status = TournamentStatus.Finished;
            t.EndTime = DateTime.UtcNow;

            // Detener reloj forzosamente
            if (t.ClockState != null)
            {
                t.ClockState.IsPaused = true;
                t.ClockState.SecondsRemaining = 0;
                t.ClockState.LastUpdatedAt = DateTime.UtcNow;
            }

            // 3. Cerrar todas las mesas activas
            if (t.Tables != null)
            {
                foreach (var table in t.Tables)
                {
                    table.Status = TournamentTableStatus.Finished;
                }
            }

            // 4. Guardar cambios
            await _service.UpdateAsync(t);

            // 5. Notificar a las pantallas (TV) para que muestren al ganador si hay
            await _notifier.QueueNotificationAsync(id, "tournament-instruction", new
            {
                type = "TOURNAMENT_FINISHED",
                message = "El torneo ha finalizado."
            });

            return Ok(t);
        }

        [HttpPost("{id}/registrations/{regId}/payout")] // <--- RUTA QUE BUSCA EL FRONTEND
        public async Task<IActionResult> RegisterPayout(Guid id, Guid regId, [FromBody] PayoutRequest req)
        {
            // Lógica para registrar el pago del premio
            // Esto es similar a una venta, pero es un egreso (Type = PrizePayout)

            // 1. Validar torneo y jugador
            var t = await _service.GetByIdAsync(id);
            if (t == null) return NotFound();

            // 2. Registrar Transacción de Egreso
            var tx = new TournamentTransaction
            {
                Id = Guid.NewGuid(),
                TournamentId = t.Id,
                WorkingDayId = t.WorkingDayId,
                PlayerId = regId,
                Type = TransactionType.PrizePayout, // Importante: Tipo Premio
                Amount = req.Amount,
                PaymentMethod = Enum.Parse<PaymentMethod>(req.Method), // O usar string seguro
                Description = req.Notes ?? "Pago de Premio",
                Timestamp = DateTime.UtcNow
            };

            t.Transactions.Add(tx);

            // 3. Actualizar Jugador (opcional, para saber cuánto ganó)
            var player = t.Registrations.FirstOrDefault(r => r.Id == regId);
            if (player != null)
            {
                player.PayoutAmount = req.Amount;
            }

            await _service.UpdateAsync(t);

            return Ok(tx);
        }
    }
}