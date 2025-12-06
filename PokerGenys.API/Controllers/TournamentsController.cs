using Microsoft.AspNetCore.Mvc;
using PokerGenys.Domain.Models;
using PokerGenys.Domain.Models.Tournaments; // Aquí están tus modelos (ServiceSaleRequest, etc.)
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
        private const string NODE_SERVER_URL = "https://pokersocketserver.onrender.com/api/webhook/emit";

        public TournamentsController(ITournamentService service, IHttpClientFactory httpClientFactory)
        {
            _service = service;
            _httpClientFactory = httpClientFactory;
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

            await NotifyNodeServerSafe(id, "tournament-control", new
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

            await NotifyNodeServerSafe(id, "tournament-control", new
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

        // Usa RegisterRequest definido abajo (necesario para leer el JSON del frontend)
        [HttpPost("{id}/register")]
        public async Task<IActionResult> RegisterPlayer(Guid id, [FromBody] RegisterRequest req)
        {
            var result = await _service.RegisterPlayerAsync(id, req.PlayerName, req.PaymentMethod, req.Bank, req.Reference);
            if (result == null) return BadRequest("No se pudo registrar.");
            await NotifyWithStats(id, "add", new { payload = result.Registration });
            return Ok(result);
        }

        // ... imports

        [HttpDelete("{id}/registrations/{regId}")]
        public async Task<IActionResult> RemoveRegistration(Guid id, Guid regId)
        {
            var result = await _service.RemoveRegistrationAsync(id, regId);
            if (!result.Success) return NotFound();

            await NotifyWithStats(id, "remove", new { registrationId = regId });

            if (!string.IsNullOrEmpty(result.InstructionType))
            {
                object extraData = null;

                // CASO 1: Inicio de Mesa Final
                if (result.InstructionType == "FINAL_TABLE_START")
                {
                    var registrations = await _service.GetRegistrationsAsync(id);
                    extraData = new { players = registrations };
                }
                // CASO 2: Ganador (Cualquiera de los dos eventos)
                else if (result.InstructionType == "TOURNAMENT_WINNER" || result.InstructionType == "FINAL_TABLE_FINISHED")
                {
                    string winnerName = "Campeón";

                    // A. Intentar leer del mensaje
                    if (!string.IsNullOrEmpty(result.Message))
                    {
                        winnerName = result.Message
                            .Replace("¡Tenemos un Campeón:", "")
                            .Replace("¡Mesa Final Terminada!", "")
                            .Replace("¡Mesa Final Terminda!", "") // Fix typo
                            .Replace("Ganador:", "")
                            .Trim(new char[] { ' ', '!', '.' });
                    }

                    // B. Si falló, buscar en base de datos al último vivo o último eliminado
                    if (string.IsNullOrWhiteSpace(winnerName) || winnerName.Length < 2)
                    {
                        var regs = await _service.GetRegistrationsAsync(id);
                        var active = regs.FirstOrDefault(r => r.Status == RegistrationStatus.Active)
                                     ?? regs.OrderByDescending(r => r.EliminatedAt).FirstOrDefault();

                        if (active != null) winnerName = active.PlayerName;
                    }

                    extraData = new { winnerName };
                    result.InstructionType = "TOURNAMENT_WINNER";
                }

                await NotifyNodeServerSafe(id, "tournament-instruction", new
                {
                    type = result.InstructionType,
                    message = result.Message,
                    data = extraData
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