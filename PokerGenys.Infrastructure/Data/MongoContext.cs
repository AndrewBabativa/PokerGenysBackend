// Infrastructure/Data/MongoContext.cs
using MongoDB.Driver;
using PokerGenys.Domain.Models.CashGame;
using PokerGenys.Domain.Models.Core;
using PokerGenys.Domain.Models.Tournaments;
using PokerGenys.Shared;


namespace PokerGenys.Infrastructure.Data
{
    public class MongoContext
    {
        private readonly IMongoDatabase _database;

        public MongoContext(MongoSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            _database = client.GetDatabase(settings.DatabaseName);
        }

        public IMongoCollection<Tournament> Tournaments => _database.GetCollection<Tournament>("Tournaments");
        // Agrega estas propiedades a tu clase MongoContext existente
        public IMongoCollection<WorkingDay> WorkingDays => _database.GetCollection<WorkingDay>("WorkingDays");
        public IMongoCollection<CashTable> Tables => _database.GetCollection<CashTable>("CashTable");
        public IMongoCollection<CashSession> Sessions => _database.GetCollection<CashSession>("CashSession");
        public IMongoCollection<Dealer> Dealers => _database.GetCollection<Dealer>("Dealers");
        public IMongoCollection<DealerShift> DealerShifts => _database.GetCollection<DealerShift>("DealerShifts");
        public IMongoCollection<WaitlistItem> Waitlist => _database.GetCollection<WaitlistItem>("Waitlist");
        public IMongoCollection<Player> Players => _database.GetCollection<Player>("Players");
    }
}
