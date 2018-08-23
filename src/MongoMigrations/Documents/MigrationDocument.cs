using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using MongoMigrations.Extensions;
using MongoMigrations.WriteModels;

namespace MongoMigrations.Documents
{
    [DebuggerDisplay("Write models: {" + nameof(WriteModelsCount) + "}")]
    public sealed class MigrationDocument : IEnumerable<IWriteModel>
    {
        readonly List<IWriteModel> _writeModels;

        [UsedImplicitly] public BsonDocument BsonDocument { get; }
        [UsedImplicitly] public BsonValue this[string name] => BsonDocument[name];
        [UsedImplicitly] public List<string> WriteModelsJsonDebug => WriteModelsBsonDebug.Select(x => x?.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.Strict })).ToList();
        [UsedImplicitly] public List<BsonDocument> WriteModelsBsonDebug => this.Select(x => x.Model?.ToWriteModelBsonDocument()).ToList();
        [UsedImplicitly] public int WriteModelsCount => this.Count();
        [UsedImplicitly] public FilterDefinition<BsonDocument> ByDocumentIdFilter()
        {
            if (!BsonDocument.TryGetElement("_id", out _))
            {
                throw new Exception("A default _id property does not exist in current document.");
            }
            return Builders<BsonDocument>.Filter.Eq("_id", this["_id"]);
        }

        public MigrationDocument([NotNull] BsonDocument document)
        {
            _writeModels = new List<IWriteModel>();
            BsonDocument = document ?? throw new ArgumentNullException(nameof(document));
        }

        [UsedImplicitly]
        public List<IWriteModel> ForEach([NotNull] string name, [NotNull] Func<MigrationForEachDocument, IWriteModel> enumeratorFunc)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (enumeratorFunc == null) throw new ArgumentNullException(nameof(enumeratorFunc));

            _writeModels.AddRange(new MigrationForEachDocuments(name, this[name].AsBsonArray, enumeratorFunc));

            return _writeModels;
        }

        [UsedImplicitly]
        public IEnumerable<IWriteModel> Update([NotNull] FilterDefinition<BsonDocument> filterDefinition, [NotNull] BsonDocument document)
        {
            if (filterDefinition == null) throw new ArgumentNullException(nameof(filterDefinition));
            if (document == null) throw new ArgumentNullException(nameof(document));
            _writeModels.Add(new MigrationUpdateDocument(filterDefinition, document));
            return _writeModels;
        }

        [UsedImplicitly]
        public IEnumerable<IWriteModel> Update([NotNull] FilterDefinition<BsonDocument> filterDefinition, [NotNull] UpdateDefinition<BsonDocument> updateDefinition)
        {
            if (filterDefinition == null) throw new ArgumentNullException(nameof(filterDefinition));
            if (updateDefinition == null) throw new ArgumentNullException(nameof(updateDefinition));
            _writeModels.Add(new MigrationUpdateDocument(filterDefinition, updateDefinition));
            return _writeModels;
        }

        [UsedImplicitly]
        public IEnumerable<IWriteModel> Update([NotNull] FilterDefinition<BsonDocument> filterDefinition, [NotNull] Func<UpdateDefinitionBuilder<BsonDocument>, UpdateDefinition<BsonDocument>> builder)
        {
            if (filterDefinition == null) throw new ArgumentNullException(nameof(filterDefinition));
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            return Update(filterDefinition, builder(Builders<BsonDocument>.Update));
        }

        [UsedImplicitly]
        public IEnumerable<IWriteModel> Delete([NotNull] FilterDefinition<BsonDocument> filterDefinition)
        {
            if (filterDefinition == null) throw new ArgumentNullException(nameof(filterDefinition));
            _writeModels.Add(new MigrationDeleteDocument(filterDefinition));
            return _writeModels;
        }

        [UsedImplicitly]
        public IEnumerable<IWriteModel> Update([NotNull] BsonDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            return Update(ByDocumentIdFilter(), document);
        }

        [UsedImplicitly]
        public IEnumerable<IWriteModel> Update([NotNull] UpdateDefinition<BsonDocument> updateDefinition)
        {
            if (updateDefinition == null) throw new ArgumentNullException(nameof(updateDefinition));
            return Update(ByDocumentIdFilter(), updateDefinition);
        }

        [UsedImplicitly]
        public IEnumerable<IWriteModel> Update([NotNull] Func<UpdateDefinitionBuilder<BsonDocument>, UpdateDefinition<BsonDocument>> builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            return Update(builder(Builders<BsonDocument>.Update));
        }

        [UsedImplicitly]
        public IEnumerable<IWriteModel> Delete()
        {
            return Delete(ByDocumentIdFilter());
        }

        [UsedImplicitly]
        public IEnumerable<IWriteModel> Skip()
        {
            return new List<IWriteModel> { new DoNotApplyWriteModel() };
        }

        public IEnumerator<IWriteModel> GetEnumerator()
        {   
            return _writeModels.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}