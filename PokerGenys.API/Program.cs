using Microsoft.OpenApi;
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

// Inyección de MongoContext
builder.Services.AddSingleton(new MongoContext(mongoSettings));

// --------------------------
// INYECCIÓN DE DEPENDENCIAS
// --------------------------
builder.Services.AddScoped<ITournamentRepository, TournamentRepository>();
builder.Services.AddScoped<ITournamentService, TournamentService>();

// --------------------------
// CONTROLLERS & SWAGGER
// --------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PokerGenys API", Version = "v1" });
});

// --------------------------
// CORS
// --------------------------
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173") // Puerto de React
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// --------------------------
// BUILD APP
// --------------------------
var app = builder.Build();

// Middlewares
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PokerGenys API v1"));
}

app.UseHttpsRedirection();
app.UseCors();
app.MapControllers();

app.Run();
