using System;
using JetBrains.Annotations;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using MongoMigrations.Extensions;
using MongoMigrations.WriteModels;

namespace MongoMigrations.Documents
{
    public sealed class MigrationDeleteDocument : IWriteModel
    {
        public WriteModel<BsonDocument> Model { get; }
        [UsedImplicitly] public string JsonDocument => BsonDocument?.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.Strict });
        [UsedImplicitly] public BsonDocument BsonDocument => Model?.ToWriteModelBsonDocument();

        public MigrationDeleteDocument([NotNull] FilterDefinition<BsonDocument> filterDefinition)
        {
            if (filterDefinition == null) throw new ArgumentNullException(nameof(filterDefinition));
            Model = new DeleteOneModel<BsonDocument>(filterDefinition);
        }
    }
}