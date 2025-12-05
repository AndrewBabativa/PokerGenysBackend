using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace PokerGenys.Domain.Models.Tournaments
{
    [BsonIgnoreExtraElements]
    public class TournamentTransaction
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid TournamentId { get; set; }
        public Guid WorkingDayId { get; set; }
        public Guid? PlayerId { get; set; }

        [BsonRepresentation(BsonType.String)]
        public TransactionType Type { get; set; }

        // --- DINERO ---
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Amount { get; set; }

        // --- DETALLES DE PAGO (OPTIMIZADO) ---

        [BsonRepresentation(BsonType.String)]
        public PaymentMethod PaymentMethod { get; set; } // Ej: Transfer

        [BsonRepresentation(BsonType.String)]
        public PaymentProvider? Bank { get; set; } // Ej: Nequi

        public string? PaymentReference { get; set; }

        // --- AUDITORÍA ---
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Description { get; set; } = string.Empty;

        // Metadatos flexibles (Ej: "Mesa 1", "Mesero Juan")
        public Dictionary<string, object>? Metadata { get; set; }
    }
}