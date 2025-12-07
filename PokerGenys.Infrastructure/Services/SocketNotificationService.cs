// Infrastructure/Services/SocketNotificationService.cs
using System.Threading.Channels;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Hosting;
using System.Net.Http;

public class SocketNotificationService : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Channel<SocketMessage> _channel;

    public SocketNotificationService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        // Cola ilimitada para evitar bloqueos, un solo consumidor
        _channel = Channel.CreateUnbounded<SocketMessage>();
    }

    // Método público que usará tu Controller/Service
    public async ValueTask QueueNotificationAsync(Guid tournamentId, string eventName, object payload)
    {
        await _channel.Writer.WriteAsync(new SocketMessage(tournamentId, eventName, payload));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var client = _httpClientFactory.CreateClient("NodeServer"); // Asegúrate de configurarlo en Program.cs

        await foreach (var msg in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                var body = new { tournamentId = msg.TournamentId, @event = msg.EventName, data = msg.Payload };
                var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
                // Fire and forget real, pero secuencial para no saturar puertos
                await client.PostAsync("/api/webhook/emit", content, stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Socket Error] {ex.Message}");
            }
        }
    }

    private record SocketMessage(Guid TournamentId, string EventName, object Payload);
}