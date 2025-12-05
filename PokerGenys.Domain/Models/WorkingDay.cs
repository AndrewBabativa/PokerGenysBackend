using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PokerGenys.Domain.Models.CashGame;

namespace PokerGenys.Domain.Models
{
    public class WorkingDay
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTime StartAt { get; set; }
        public DateTime? EndAt { get; set; }

        [BsonRepresentation(BsonType.String)]
        public WorkingDayStatus Status { get; set; } = WorkingDayStatus.Open;

        // --- CAMPOS CALCULADOS PARA REPORTE INSTANTÁNEO ---
        // Estos se actualizan cada vez que ocurre una transacción
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TotalCashIn { get; set; } = 0; // Entradas (Buyins, Rebuys)

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TotalCashOut { get; set; } = 0; // Salidas (Premios, Cashouts)

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TotalRake { get; set; } = 0; // Ganancia neta casa

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }
    }
}