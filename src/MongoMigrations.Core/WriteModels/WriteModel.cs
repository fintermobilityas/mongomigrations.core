using System;
using System.Diagnostics.CodeAnalysis;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoMigrations.Core.WriteModels;

public interface IWriteModel
{
    WriteModel<BsonDocument> Model { get; }
}

public class WriteModel([NotNull] WriteModel<BsonDocument> model) : IWriteModel
{
    public WriteModel<BsonDocument> Model { get; } = model ?? throw new ArgumentNullException(nameof(model));
}