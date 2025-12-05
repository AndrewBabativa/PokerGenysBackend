using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PokerGenys.Domain.Models.Tournaments
{
    [BsonIgnoreExtraElements]
    public class TournamentRegistration
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid TournamentId { get; set; }
        public Guid WorkingDayId { get; set; } // Redundancia útil para reportes rápidos por día

        // Datos del Jugador
        public string PlayerId { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty; // Snapshot del nombre
        public string? MemberId { get; set; }

        // Ubicación
        public string? SeatId { get; set; }
        public string? TableId { get; set; }

        // Estado del Juego
        public int Chips { get; set; }

        [BsonRepresentation(BsonType.String)]
        public RegistrationType RegistrationType { get; set; } = RegistrationType.Standard;

        [BsonRepresentation(BsonType.String)]
        public RegistrationStatus Status { get; set; } = RegistrationStatus.Active;

        // --- FINANZAS ---
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal PaidAmount { get; set; } // Cuánto pagó realmente

        [BsonRepresentation(BsonType.String)]
        public PaymentMethod PaymentMethod { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? PayoutAmount { get; set; } // Premio ganado (Salida de dinero)

        // Tiempos
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public DateTime? EliminatedAt { get; set; }

        public Dictionary<string, object>? Metadata { get; set; }
    }
}