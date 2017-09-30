using System;
using JetBrains.Annotations;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoMigrations
{
    public interface IWriteModel
    {
        WriteModel<BsonDocument> Model { get; }
    }

    internal class WriteModel : IWriteModel
    {
        public WriteModel<BsonDocument> Model { get; }

        public WriteModel([NotNull] WriteModel<BsonDocument> model)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
        }
    }

    internal class DoNotApplyWriteModel : IWriteModel
    {
        public WriteModel<BsonDocument> Model => throw new NotSupportedException($"{GetType().FullName} is not writable model.");
    }

    internal sealed class BreakWriteModel : DoNotApplyWriteModel
    {

    }
}