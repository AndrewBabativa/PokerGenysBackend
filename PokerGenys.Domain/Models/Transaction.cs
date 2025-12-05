using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PokerGenys.Domain.Models.CashGame;

namespace PokerGenys.Domain.Models
{
    public class Transaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SessionId { get; set; }

        [BsonRepresentation(BsonType.String)]
        public TransactionType Type { get; set; }

        [BsonRepresentation(BsonType.String)]
        public TransactionCategory? Category { get; set; }

        public decimal Amount { get; set; }

        [BsonRepresentation(BsonType.String)]
        public PaymentStatus? PaymentStatus { get; set; }

        [BsonRepresentation(BsonType.String)]
        public PaymentMethod? PaymentMethod { get; set; }

        [BsonRepresentation(BsonType.String)]
        public BankMethod? BankMethod { get; set; }

        public string? TransferProof { get; set; }
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}