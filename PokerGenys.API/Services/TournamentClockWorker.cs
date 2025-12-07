// Services/TournamentClockWorker.cs
using PokerGenys.Domain.Models;
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
            // Ejecutar cada 1 segundo
            await Task.Delay(1000, stoppingToken);

            using (var scope = _serviceProvider.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<ITournamentRepository>();

                // Obtener SOLO torneos corriendo (Optimización de consulta necesaria en Repo)
                var activeTournaments = (await repo.GetAllAsync())
                                        .Where(t => t.Status == TournamentStatus.Running && !t.ClockState.IsPaused)
                                        .ToList();

                foreach (var t in activeTournaments)
                {
                    if (!t.ClockState.LastUpdatedAt.HasValue) continue;

                    var now = DateTime.UtcNow;
                    var elapsed = (now - t.ClockState.LastUpdatedAt.Value).TotalSeconds;

                    // Actualizar referencia temporal
                    t.ClockState.LastUpdatedAt = now;
                    t.ClockState.SecondsRemaining -= elapsed;

                    bool stateChanged = false;

                    // Lógica de cambio de nivel
                    if (t.ClockState.SecondsRemaining <= 0)
                    {
                        var nextLevel = t.Levels.FirstOrDefault(l => l.LevelNumber == t.CurrentLevel + 1);
                        if (nextLevel != null)
                        {
                            t.CurrentLevel++;
                            t.ClockState.SecondsRemaining = nextLevel.DurationSeconds;
                            stateChanged = true;

                            // Notificar cambio de nivel
                            await _notifier.QueueNotificationAsync(t.Id, "timer-sync", new
                            {
                                timeLeft = t.ClockState.SecondsRemaining,
                                currentLevel = t.CurrentLevel
                            });
                        }
                        else
                        {
                            // Fin del torneo
                            t.Status = TournamentStatus.Paused;
                            t.ClockState.IsPaused = true;
                            t.ClockState.SecondsRemaining = 0;
                            stateChanged = true;
                        }
                    }

                    // Guardamos cambios (solo si cambió el nivel o pasaron X segundos para persistencia)
                    // NOTA: Para alto rendimiento, no guardes en BD cada segundo. Hazlo cada 10s o al cambiar nivel.
                    if (stateChanged)
                    {
                        await repo.UpdateAsync(t);
                    }
                }
            }
        }
    }
}