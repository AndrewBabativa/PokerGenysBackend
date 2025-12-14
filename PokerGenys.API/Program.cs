using Microsoft.AspNetCore.Http.Json;
using Microsoft.OpenApi.Models;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using PokerGenys.Infrastructure.Data;
using PokerGenys.Infrastructure.Repositories;
using PokerGenys.Services;
using PokerGenys.Shared; // Asegúrate de tener aquí tus Workers/Settings
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// =============================================================================
// 0. CONFIGURACIÓN DE APPSETTINGS (SOLUCIÓN ERROR INOTIFY)
// =============================================================================
builder.Configuration.Sources.Clear();
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables();

// ==========================================
// 1. CONFIGURACIÓN DE MONGO DB
// ==========================================
var mongoSettings = builder.Configuration
    .GetSection("MongoSettings")
    .Get<MongoSettings>();

if (mongoSettings == null)
    throw new Exception("MongoSettings no está configurado en appsettings.json");

// Registrar GuidSerializer para manejar GUIDs como Standard en Mongo
BsonSerializer.RegisterSerializer(new GuidSerializer(MongoDB.Bson.GuidRepresentation.Standard));

// Inyectar el Contexto como Singleton (Recomendado para MongoDriver)
builder.Services.AddSingleton(new MongoContext(mongoSettings));

// ==========================================
// 2. INYECCIÓN DE DEPENDENCIAS (REPOSITORIOS)
// ==========================================
// Nota: Asegúrate de que las clases Repository coincidan con estos nombres en Infrastructure

builder.Services.AddScoped<IWorkingDayRepository, WorkingDayRepository>();
builder.Services.AddScoped<ITournamentRepository, TournamentRepository>();
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
// Si tu repositorio se llama CashTableRepository pero implementa ITableRepository:
builder.Services.AddScoped<ICashTableRepository, CashTableRepository>();
builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();
builder.Services.AddScoped<IDealerRepository, DealerRepository>();
builder.Services.AddScoped<IWaitlistRepository, WaitlistRepository>();

// ==========================================
// 3. INYECCIÓN DE DEPENDENCIAS (SERVICIOS)
// ==========================================
// Estos son los servicios que refactorizamos

builder.Services.AddScoped<IWorkingDayService, WorkingDayService>();
builder.Services.AddScoped<ITournamentService, TournamentService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<ICashTableService, CashTableService>();
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<IDealerService, DealerService>();
builder.Services.AddScoped<IWaitlistService, WaitlistService>();
builder.Services.AddScoped<IReportService, ReportService>();

// ==========================================
// 4. SERVICIOS EN SEGUNDO PLANO (WORKERS)
// ==========================================
builder.Services.AddHttpClient("NodeServer", client =>
{
    client.BaseAddress = new Uri("https://pokersocketserver.onrender.com");
});

builder.Services.AddSingleton<SocketNotificationService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<SocketNotificationService>());
builder.Services.AddHostedService<TournamentClockWorker>();

// ==========================================
// 5. CONFIGURACIÓN CORS
// ==========================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ==========================================
// 6. CONTROLLERS & JSON OPTIONS
// ==========================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Convierte Enums a Texto (ej: "Open") en lugar de números (0)
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        // Evitar ciclos en JSON si alguna vez agregas referencias circulares (opcional pero recomendado)
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// ==========================================
// 7. SWAGGER GENERATOR
// ==========================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PokerGenys API",
        Version = "v1",
        Description = "API Financiera y Operativa para Poker Club"
    });

    // Solución para nombres de esquemas repetidos en Swagger
    c.CustomSchemaIds(type => type.FullName);
});

// ==========================================
// 8. BUILD & PIPELINE
// ==========================================
var app = builder.Build();

// Swagger siempre activo para pruebas fáciles
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PokerGenys API v1");
    c.RoutePrefix = string.Empty; // Swagger en la raíz (localhost:port/)
});

app.UseHttpsRedirection();

app.UseCors("AllowAll"); // Antes de Auth y Controllers

app.UseAuthorization();

app.MapControllers();

app.Run();