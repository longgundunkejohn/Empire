using Empire.Server.Interfaces;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace Empire.Server.Services
{
    public class MongoDbService : IMongoDbService
    {
        public IMongoDatabase GameDatabase { get; }
        public IMongoDatabase CardDatabase { get; }

        public MongoDbService(IConfiguration config)
        {
            var localConn = config.GetSection("MongoDB:ConnectionString").Value;
            var localDbName = config.GetSection("MongoDB:DatabaseName").Value;

            var atlasConn = config.GetSection("CardDB:ConnectionString").Value;
            var atlasDbName = config.GetSection("CardDB:DatabaseName").Value;

            var localClient = new MongoClient(localConn);
            var atlasClient = new MongoClient(atlasConn);

            GameDatabase = localClient.GetDatabase(localDbName);
            CardDatabase = atlasClient.GetDatabase(atlasDbName);
        }
    }
}
