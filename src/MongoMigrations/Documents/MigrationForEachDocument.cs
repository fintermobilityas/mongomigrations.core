using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoMigrations.WriteModels;

namespace MongoMigrations.Documents
{
    [DebuggerDisplay("Name: {Name}. Index: {Index}")]
    public sealed class MigrationForEachDocument
    {
        [UsedImplicitly] public FilterDefinition<BsonDocument> ByIdFilter { get; set; }
        [UsedImplicitly] public string Name { get; }
        [UsedImplicitly] public IEnumerable<BsonDocument> BsonDocuments { get; }
        [UsedImplicitly] public int Index { get; }
        [UsedImplicitly] public BsonDocument BsonDocument { get; }

        [UsedImplicitly]
        public BsonValue this[string name] => BsonDocument[name];

        public MigrationForEachDocument([NotNull] string name, IEnumerable<BsonDocument> bsonDocuments, int index, [NotNull] BsonDocument bsonDocument)
        {
            BsonDocuments = bsonDocuments ?? throw new ArgumentNullException(nameof(bsonDocuments));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Index = index < 0 ? throw new ArgumentOutOfRangeException(nameof(index)) : index;
            BsonDocument = bsonDocument ?? throw new ArgumentNullException(nameof(bsonDocument));
            ByIdFilter = Builders<BsonDocument>.Filter.ElemMatch(name, Builders<BsonDocument>.Filter.Eq("_id", bsonDocument["_id"]));
        }

        [UsedImplicitly]
        public IWriteModel Update([NotNull] FilterDefinition<BsonDocument> filterDefinition, [NotNull] UpdateDefinition<BsonDocument> updateDefinition)
        {
            if (filterDefinition == null) throw new ArgumentNullException(nameof(filterDefinition));
            if (updateDefinition == null) throw new ArgumentNullException(nameof(updateDefinition));
            return new WriteModel(new UpdateOneModel<BsonDocument>(filterDefinition, updateDefinition));
        }

        [UsedImplicitly]
        public IWriteModel Update([NotNull] FilterDefinition<BsonDocument> filterDefinition, [NotNull] Func<UpdateDefinitionBuilder<BsonDocument>, UpdateDefinition<BsonDocument>> builder)
        {
            if (filterDefinition == null) throw new ArgumentNullException(nameof(filterDefinition));
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            return new WriteModel(new UpdateOneModel<BsonDocument>(filterDefinition, builder(Builders<BsonDocument>.Update)));
        }

        [UsedImplicitly]
        public IWriteModel Delete([NotNull] FilterDefinition<BsonDocument> filterDefinition)
        {
            if (filterDefinition == null) throw new ArgumentNullException(nameof(filterDefinition));
            return new WriteModel(new DeleteOneModel<BsonDocument>(filterDefinition));
        }
        
        [UsedImplicitly]
        public IWriteModel Update([NotNull] UpdateDefinition<BsonDocument> updateDefinition)
        {
            if (updateDefinition == null) throw new ArgumentNullException(nameof(updateDefinition));
            return Update(ByIdFilter, updateDefinition);
        }

        [UsedImplicitly]
        public IWriteModel Update([NotNull] Func<UpdateDefinitionBuilder<BsonDocument>, UpdateDefinition<BsonDocument>> builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            return Update(builder(Builders<BsonDocument>.Update));
        }

        [UsedImplicitly]
        public IWriteModel Delete()
        {
            return Delete(ByIdFilter);
        }

        [UsedImplicitly]
        public IWriteModel Skip()
        {
            return new DoNotApplyWriteModel();
        }

        [UsedImplicitly]
        public IWriteModel Break()
        {
            return new BreakWriteModel();
        }
    }
}