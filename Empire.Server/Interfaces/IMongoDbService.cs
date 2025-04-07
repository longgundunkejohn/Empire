using MongoDB.Driver;

namespace Empire.Server.Interfaces
{
    public interface IMongoDbService
    {
        MongoDB.Driver.IMongoDatabase GameDatabase { get; }
        MongoDB.Driver.IMongoDatabase CardDatabase { get; }
    }
}