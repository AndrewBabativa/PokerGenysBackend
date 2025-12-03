using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace PokerGenys.Domain.Models
{
    public class Jackpot
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid DayId { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;

        public decimal BaseAmount { get; set; }

        public List<JackpotPrize> Prizes { get; set; } = new List<JackpotPrize>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // Clase auxiliar para los premios dentro del Jackpot
    public class JackpotPrize
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public decimal Percentage { get; set; }
        public decimal? CalculatedValue { get; set; }

        public Guid? WinnerPlayerId { get; set; }
        public DateTime? ClaimedAt { get; set; }
    }
}