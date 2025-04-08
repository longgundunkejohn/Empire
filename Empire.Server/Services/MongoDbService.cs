using Empire.Server.Interfaces;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace Empire.Server.Services
{
    public class MongoDbService : IMongoDbService
    {
        public IMongoDatabase GameDatabase { get; }
        public IMongoDatabase CardDatabase { get; }
        public IMongoDatabase DeckDatabase { get; } // New database for decks

        public MongoDbService(IConfiguration config)
        {
            var localConn = config.GetSection("MongoDB:ConnectionString").Value;
            var localDbName = config.GetSection("MongoDB:DatabaseName").Value;

            var atlasConn = config.GetSection("CardDB:ConnectionString").Value;
            var atlasDbName = config.GetSection("CardDB:DatabaseName").Value;

            var deckDbName = "gamedeck"; // Or "playerdeck", choose your name

            var localClient = new MongoClient(localConn);
            var atlasClient = new MongoClient(atlasConn);

            GameDatabase = localClient.GetDatabase(localDbName);
            CardDatabase = atlasClient.GetDatabase(atlasDbName);
            DeckDatabase = localClient.GetDatabase(deckDbName); // Initialize DeckDatabase
        }
    }
}