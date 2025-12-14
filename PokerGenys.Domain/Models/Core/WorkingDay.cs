using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PokerGenys.Domain.Enums;

namespace PokerGenys.Domain.Models.Core
{
    public class WorkingDay
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTime StartAt { get; set; } = DateTime.UtcNow;
        public DateTime? EndAt { get; set; }

        [BsonRepresentation(BsonType.String)]
        public WorkingDayStatus Status { get; set; } = WorkingDayStatus.Open;

        // --- ARQUEO DE CAJA FÍSICA ---
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal InitialCapita { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal FinalCapitaDeclared { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal SystemExpectedCash { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal CashVariance { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal OperationalExpenses { get; set; } = 0;

        // --- SNAPSHOTS FINANCIEROS (Histórico) ---

        // Cash Games
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal CashGameBuyIns { get; set; } = 0;
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal CashGameCashOuts { get; set; } = 0;
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal CashGameRake { get; set; } = 0;

        // Torneos
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TournamentCollected { get; set; } = 0;
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TournamentPayouts { get; set; } = 0;
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TournamentNetProfit { get; set; } = 0;

        public string? Notes { get; set; }
        public string? AuditLog { get; set; }
    }
}