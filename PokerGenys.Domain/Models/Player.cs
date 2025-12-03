using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace PokerGenys.Domain.Models
{
    public class Player
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}