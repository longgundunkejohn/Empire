using MongoDB.Driver;

namespace Empire.Server.Interfaces
{

    public interface IMongoDbService
    {
        IMongoDatabase GetDatabase();
    }

}
