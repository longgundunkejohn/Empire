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
            var gameConn = config["MongoDB:ConnectionString"];
            var gameDbName = config["MongoDB:DatabaseName"];

            var deckConn = config["DeckDB:ConnectionString"];
            var deckDbName = config["DeckDB:DatabaseName"];

            var atlasConn = config["CardDB:ConnectionString"];
            var atlasDbName = config["CardDB:DatabaseName"];

            var gameClient = new MongoClient(gameConn);
            var deckClient = new MongoClient(deckConn);
            var atlasClient = new MongoClient(atlasConn);

            GameDatabase = gameClient.GetDatabase(gameDbName);
            DeckDatabase = deckClient.GetDatabase(deckDbName);
            CardDatabase = atlasClient.GetDatabase(atlasDbName);
        }
    }
}