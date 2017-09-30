using System;
using JetBrains.Annotations;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoMigrations.WriteModels
{
    public interface IWriteModel
    {
        WriteModel<BsonDocument> Model { get; }
    }

    public class WriteModel : IWriteModel
    {
        public WriteModel<BsonDocument> Model { get; }

        public WriteModel([NotNull] WriteModel<BsonDocument> model)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
        }
    }
}