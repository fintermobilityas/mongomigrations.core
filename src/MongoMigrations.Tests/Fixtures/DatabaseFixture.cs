using System;
using MongoDB.Driver;

namespace MongoMigrations.Tests.Fixtures
{
    public sealed class DatabaseFixture : IDisposable
    {
        public string MongoDbConnectionString = "mongodb://localhost:27017/?connect=replicaset";
        public IMongoDatabase Database { get; }
        public IMongoClient MongoClient { get; }

        public DatabaseFixture()
        {
            MongoClient = new MongoClient(MongoDbConnectionString);
            Database = MongoClient.GetDatabase("MongoMigrationsUnitTests");
        }
        
        public void Dispose()
        {

        }
    }
}
