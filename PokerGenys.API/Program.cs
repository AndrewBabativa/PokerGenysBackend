using Microsoft.OpenApi.Models;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using PokerGenys.Infrastructure.Data;
using PokerGenys.Infrastructure.Repositories;
using PokerGenys.Services;
using PokerGenys.Shared;

var builder = WebApplication.CreateBuilder(args);

// --------------------------
// CONFIGURACIÓN DE MONGO
// --------------------------
var mongoSettings = builder.Configuration
    .GetSection("MongoSettings")
    .Get<MongoSettings>();

if (mongoSettings == null)
    throw new Exception("MongoSettings no está configurado en appsettings.json");

// Registrar GuidSerializer con GuidRepresentation.Standard
BsonSerializer.RegisterSerializer(new GuidSerializer(MongoDB.Bson.GuidRepresentation.Standard));

// --------------------------
// INYECCIÓN DE DEPENDENCIAS
// --------------------------
builder.Services.AddSingleton(new MongoContext(mongoSettings));
builder.Services.AddScoped<ITournamentRepository, TournamentRepository>();
builder.Services.AddScoped<ITournamentService, TournamentService>();

// --------------------------
// CORS
// --------------------------
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
            "http://localhost:5173",
            "http://192.168.80.22:5173",
            "http://localhost:4000",
            "ws://localhost:4000",
            "https://pokergenys.netlify.app",
            "https://pokergenys.netlify.app:4000",
            "ws://pokergenys.netlify.app:4000"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// --------------------------
// CONTROLLERS & SWAGGER
// --------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PokerGenys API",
        Version = "v1"
    });
});

// --------------------------
// BUILD APP
// --------------------------
var app = builder.Build();

// --------------------------
// MIDDLEWARES
// --------------------------
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PokerGenys API v1");
    c.RoutePrefix = string.Empty; // Swagger como página por defecto
});

app.UseHttpsRedirection();
app.UseCors();
app.MapControllers();

app.Run();
