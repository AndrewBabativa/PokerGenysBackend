using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PokerGenys.Domain.Enums;
using System;
using System.Collections.Generic;

namespace PokerGenys.Domain.Models.Core
{
    public class Dealer
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Nickname { get; set; }
        public string DocumentId { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }

        [BsonRepresentation(BsonType.String)]
        public DealerStatus Status { get; set; } = DealerStatus.Active;

        public DateTime HireDate { get; set; } = DateTime.UtcNow;
        public decimal HourlyRate { get; set; } = 0;
        public string? PhotoUrl { get; set; }

        public Dictionary<string, object>? Metadata { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [BsonIgnore]
        public string FullName => $"{FirstName} {LastName}";
    }
}