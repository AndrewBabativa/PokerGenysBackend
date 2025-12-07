using Microsoft.AspNetCore.Http.Json;
using Microsoft.OpenApi.Models;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using PokerGenys.Infrastructure.Data;
using PokerGenys.Infrastructure.Repositories;
using PokerGenys.Services;
using PokerGenys.Shared;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. CONFIGURACIÓN DE MONGO DB
// ==========================================
var mongoSettings = builder.Configuration
    .GetSection("MongoSettings")
    .Get<MongoSettings>();

if (mongoSettings == null)
    throw new Exception("MongoSettings no está configurado en appsettings.json");

// Registrar GuidSerializer para manejar GUIDs correctamente en Mongo
BsonSerializer.RegisterSerializer(new GuidSerializer(MongoDB.Bson.GuidRepresentation.Standard));

// ==========================================
// 2. INYECCIÓN DE DEPENDENCIAS (DI)
// ==========================================
builder.Services.AddHttpClient("NodeServer", client =>
{
    client.BaseAddress = new Uri("https://pokersocketserver.onrender.com");
});

// Program.cs
builder.Services.AddScoped<IWorkingDayRepository, WorkingDayRepository>();
builder.Services.AddSingleton<SocketNotificationService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<SocketNotificationService>());
builder.Services.AddHostedService<TournamentClockWorker>();
// Contexto de Base de Datos (Singleton es seguro para MongoClient)
builder.Services.AddSingleton(new MongoContext(mongoSettings));
builder.Services.AddHttpClient();
// --- TORNEOS (Lógica existente) ---
builder.Services.AddScoped<ITournamentRepository, TournamentRepository>();
builder.Services.AddScoped<ITournamentService, TournamentService>();

// --- MESAS CASH (Nuevos módulos separados) ---

// Working Days
builder.Services.AddScoped<IWorkingDayRepository, WorkingDayRepository>();
builder.Services.AddScoped<IWorkingDayService, WorkingDayService>();

// Tables
builder.Services.AddScoped<ITableRepository, TableRepository>();
builder.Services.AddScoped<ITableService, TableService>();

// Sessions
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<ISessionService, SessionService>();

// Dealers & Shifts
builder.Services.AddScoped<IDealerRepository, DealerRepository>();
builder.Services.AddScoped<IDealerService, DealerService>();

// Waitlist
builder.Services.AddScoped<IWaitlistRepository, WaitlistRepository>();
builder.Services.AddScoped<IWaitlistService, WaitlistService>();

// Players
builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();
builder.Services.AddScoped<IPlayerService, PlayerService>();


// ==========================================
// 3. CONFIGURACIÓN CORS
// ==========================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true) // <--- ¡LA MAGIA! Acepta cualquier origen que pida entrar
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Necesario para SignalR/Sockets y Auth
    });
});

// ==========================================
// 4. CONTROLLERS & JSON OPTIONS
// ==========================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // IMPORTANTE: Esto hace que los Enums se vean como texto en Swagger/React
        // Ejemplo: Verás "Open" en vez de 0
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// ==========================================
// 5. SWAGGER GENERATOR
// ==========================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PokerGenys API",
        Version = "v1",
        Description = "API para gestión de Torneos y Mesas Cash en Tiempo Real"
    });

    // Opcional: Esto ayuda si tienes nombres de clases repetidos en diferentes namespaces
    c.CustomSchemaIds(type => type.ToString());
});

// ==========================================
// 6. BUILD APP
// ==========================================
var app = builder.Build();

// ==========================================
// 7. MIDDLEWARES PIPELINE
// ==========================================

// Swagger siempre habilitado para que puedas probar (o ponlo dentro de if (app.Environment.IsDevelopment()))
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PokerGenys API v1");
    // Esto hace que Swagger sea la página de inicio (localhost:port/)
    c.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();

app.UseCors("AllowAll");// CORS debe ir antes de MapControllers

app.UseAuthorization(); // Si tienes auth en el futuro

app.MapControllers();

app.Run();