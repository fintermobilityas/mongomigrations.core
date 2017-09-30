using System;
using System.Diagnostics;
using System.Globalization;
using JetBrains.Annotations;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using MongoMigrations.WriteModels;
using MongoMigrations.Extensions;

namespace MongoMigrations.Documents
{
    public sealed class MigrationUpdateDocument : IWriteModel
    {
        readonly FilterDefinition<BsonDocument> _filterDefinition;
        readonly UpdateDefinition<BsonDocument> _updateDefinition;

        public WriteModel<BsonDocument> Model { get; }
        [UsedImplicitly] public string JsonDocument => BsonDocument?.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.Strict });
        [UsedImplicitly] public BsonDocument BsonDocument => Model?.ToWriteModelBsonDocument();

        public MigrationUpdateDocument([NotNull] FilterDefinition<BsonDocument> filterDefinition, [NotNull] UpdateDefinition<BsonDocument> updateDefinition)
        {
            _updateDefinition = updateDefinition ?? throw new ArgumentNullException(nameof(updateDefinition));
            _filterDefinition = filterDefinition ?? throw new ArgumentNullException(nameof(filterDefinition));
            Model = new UpdateOneModel<BsonDocument>(_filterDefinition, _updateDefinition);
        }

        internal MigrationUpdateDocument Combine([NotNull] Func<UpdateDefinitionBuilder<BsonDocument>, UpdateDefinition<BsonDocument>> builder)
        {
           var updateDefinition = Builders<BsonDocument>.Update.Combine(_updateDefinition, builder(Builders<BsonDocument>.Update));
           return new MigrationUpdateDocument(_filterDefinition, updateDefinition);
        }
    }
}