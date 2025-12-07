// Services/TournamentClockWorker.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PokerGenys.Domain.Models; // Asegúrate de tener los using correctos
using PokerGenys.Domain.Models.Tournaments;
using PokerGenys.Infrastructure.Repositories;

public class TournamentClockWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SocketNotificationService _notifier;

    public TournamentClockWorker(IServiceProvider serviceProvider, SocketNotificationService notifier)
    {
        _serviceProvider = serviceProvider;
        _notifier = notifier;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);

            using (var scope = _serviceProvider.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<ITournamentRepository>();

                // 1. Usar la consulta optimizada (sin traer jugadores)
                var activeTournaments = await repo.GetRunningTournamentsAsync();

                foreach (var t in activeTournaments)
                {
                    if (!t.ClockState.LastUpdatedAt.HasValue) continue;

                    var now = DateTime.UtcNow;
                    var elapsed = (now - t.ClockState.LastUpdatedAt.Value).TotalSeconds;

                    t.ClockState.LastUpdatedAt = now;
                    t.ClockState.SecondsRemaining -= elapsed;

                    bool stateChanged = false;

                    // Lógica de cambio de nivel...
                    if (t.ClockState.SecondsRemaining <= 0)
                    {
                        var nextLevel = t.Levels.FirstOrDefault(l => l.LevelNumber == t.CurrentLevel + 1);
                        if (nextLevel != null)
                        {
                            t.CurrentLevel++;
                            t.ClockState.SecondsRemaining = nextLevel.DurationSeconds;
                            stateChanged = true;

                            await _notifier.QueueNotificationAsync(t.Id, "timer-sync", new
                            {
                                timeLeft = t.ClockState.SecondsRemaining,
                                currentLevel = t.CurrentLevel
                            });
                        }
                        else
                        {
                            t.Status = TournamentStatus.Paused;
                            t.ClockState.IsPaused = true;
                            t.ClockState.SecondsRemaining = 0;
                            stateChanged = true;
                        }
                    }

                    // --- CAMBIO CLAVE AQUÍ ---
                    // Guardamos CADA SEGUNDO (o cuando cambia estado) usando el método ligero.
                    // Al ser un $set parcial, es muy rápido y seguro.
                    await repo.UpdateClockStateAsync(
                        t.Id,
                        t.ClockState,
                        t.CurrentLevel,
                        t.Status
                    );
                }
            }
        }
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
}