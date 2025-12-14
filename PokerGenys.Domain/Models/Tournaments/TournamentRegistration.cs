using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PokerGenys.Domain.Enums;
using System;
using System.Collections.Generic;

namespace PokerGenys.Domain.Models.Tournaments
{
    [BsonIgnoreExtraElements]
    public class TournamentRegistration
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TournamentId { get; set; }
        public Guid WorkingDayId { get; set; }

        public Guid PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public string? MemberId { get; set; }

        public string? SeatId { get; set; }
        public string? TableId { get; set; }

        public int Chips { get; set; }

        [BsonRepresentation(BsonType.String)]
        public RegistrationType RegistrationType { get; set; } = RegistrationType.Standard;

        [BsonRepresentation(BsonType.String)]
        public RegistrationStatus Status { get; set; } = RegistrationStatus.Active;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal PaidAmount { get; set; }

        [BsonRepresentation(BsonType.String)]
        public PaymentMethod PaymentMethod { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? PayoutAmount { get; set; }

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public DateTime? EliminatedAt { get; set; }

        public Dictionary<string, object>? Metadata { get; set; }
    }
}