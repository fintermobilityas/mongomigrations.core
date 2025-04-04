using System;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoMigrations.Core.WriteModels;

internal class DoNotApplyWriteModel : IWriteModel
{
    public WriteModel<BsonDocument> Model => throw new NotSupportedException($"{GetType().FullName} is not writable model.");
}