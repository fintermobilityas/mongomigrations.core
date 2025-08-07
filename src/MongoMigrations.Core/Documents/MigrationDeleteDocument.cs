using System;
using System.Diagnostics.CodeAnalysis;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using MongoMigrations.Core.Extensions;
using MongoMigrations.Core.WriteModels;

namespace MongoMigrations.Core.Documents;

public sealed class MigrationDeleteDocument : IWriteModel
{
    public WriteModel<BsonDocument> Model { get; }
    public string JsonDocument => BsonDocument?.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.CanonicalExtendedJson });
    public BsonDocument BsonDocument => Model?.ToWriteModelBsonDocument();

    public MigrationDeleteDocument([NotNull] FilterDefinition<BsonDocument> filterDefinition)
    {
        if (filterDefinition == null) throw new ArgumentNullException(nameof(filterDefinition));
        Model = new DeleteOneModel<BsonDocument>(filterDefinition);
    }
}