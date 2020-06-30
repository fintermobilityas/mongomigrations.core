using System;
using JetBrains.Annotations;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using MongoMigrations.Core.Extensions;
using MongoMigrations.Core.WriteModels;

namespace MongoMigrations.Core.Documents
{
    public sealed class MigrationUpdateDocument : IWriteModel
    {
        public WriteModel<BsonDocument> Model { get; }
        [UsedImplicitly] public string JsonDocument => BsonDocument?.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.Strict });
        [UsedImplicitly] public BsonDocument BsonDocument => Model?.ToWriteModelBsonDocument();

        public MigrationUpdateDocument([NotNull] FilterDefinition<BsonDocument> filterDefinition, [NotNull] UpdateDefinition<BsonDocument> updateDefinition)
        {
            if (filterDefinition == null) throw new ArgumentNullException(nameof(filterDefinition));
            if (updateDefinition == null) throw new ArgumentNullException(nameof(updateDefinition));
            Model = new UpdateOneModel<BsonDocument>(filterDefinition, updateDefinition);
        }
    }
}