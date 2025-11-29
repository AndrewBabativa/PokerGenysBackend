// Infrastructure/Data/MongoContext.cs
using MongoDB.Driver;
using PokerGenys.Domain.Models;
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
    }
}
