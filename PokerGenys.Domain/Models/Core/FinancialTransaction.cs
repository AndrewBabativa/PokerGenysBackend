using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PokerGenys.Domain.Enums;
using System;
using System.Collections.Generic;

namespace PokerGenys.Domain.Models.Core
{
    [BsonIgnoreExtraElements]
    public class FinancialTransaction
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid WorkingDayId { get; set; }

        [BsonRepresentation(BsonType.String)]
        public TransactionSource Source { get; set; }

        public Guid? SourceId { get; set; } // ID de Mesa o Torneo
        public Guid? PlayerId { get; set; }

        [BsonRepresentation(BsonType.String)]
        public TransactionType Type { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Amount { get; set; }

        [BsonRepresentation(BsonType.String)]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        [BsonRepresentation(BsonType.String)]
        public PaymentProvider? Bank { get; set; }

        [BsonRepresentation(BsonType.String)]
        public PaymentStatus Status { get; set; } = PaymentStatus.Paid;

        public string? ReferenceCode { get; set; } // Comprobante

        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Guid? CreatedByUserId { get; set; }

        public Dictionary<string, object>? Metadata { get; set; }
    }
}