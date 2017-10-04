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
    [DebuggerDisplay("Write models: {WriteModelsCount}")]
    public sealed class MigrationDocument : IEnumerable<IWriteModel>
    {
        readonly List<IWriteModel> _writeModels;
        MigrationUpdateDocument _migrationUpdateDocument;

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
        public void ForEach([NotNull] string name, [NotNull] Func<MigrationForEachDocument, IWriteModel> enumeratorFunc)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (enumeratorFunc == null) throw new ArgumentNullException(nameof(enumeratorFunc));

            _writeModels.AddRange(new MigrationForEachDocuments(name, this[name].AsBsonArray, enumeratorFunc));
        }

        [UsedImplicitly]
        public void UpdateCombine([NotNull] FilterDefinition<BsonDocument> filterDefinition, [NotNull] Func<UpdateDefinitionBuilder<BsonDocument>, UpdateDefinition<BsonDocument>> builder)
        {
            if (_migrationUpdateDocument != null)
            {
                throw new Exception($"A {nameof(MigrationUpdateDocument)} can only be instantiated once.");
            }

            _migrationUpdateDocument = new MigrationUpdateDocument(filterDefinition, builder(Builders<BsonDocument>.Update));
        }

        [UsedImplicitly]
        public void UpdateCombine([NotNull] Func<UpdateDefinitionBuilder<BsonDocument>, UpdateDefinition<BsonDocument>> builder)
        {
            if (_migrationUpdateDocument == null)
            {
                _migrationUpdateDocument = new MigrationUpdateDocument(ByDocumentIdFilter(), builder(Builders<BsonDocument>.Update));
                return;
            }

            _migrationUpdateDocument = _migrationUpdateDocument.Combine(builder);
        }

        [UsedImplicitly]
        public IEnumerable<IWriteModel> Update([NotNull] FilterDefinition<BsonDocument> filterDefinition, [NotNull] BsonDocument document)
        {
            if (filterDefinition == null) throw new ArgumentNullException(nameof(filterDefinition));
            if (document == null) throw new ArgumentNullException(nameof(document));
            return new UpdateOneModel<BsonDocument>(filterDefinition, document).AsEnumerable();
        }

        [UsedImplicitly]
        public IEnumerable<IWriteModel> Update([NotNull] FilterDefinition<BsonDocument> filterDefinition, [NotNull] UpdateDefinition<BsonDocument> updateDefinition)
        {
            if (filterDefinition == null) throw new ArgumentNullException(nameof(filterDefinition));
            if (updateDefinition == null) throw new ArgumentNullException(nameof(updateDefinition));
            return new UpdateOneModel<BsonDocument>(filterDefinition, updateDefinition).AsEnumerable();
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
            return new DeleteOneModel<BsonDocument>(filterDefinition).AsEnumerable();
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
            var enumerator = _writeModels.ToList();

            if (_migrationUpdateDocument != null)
            {
                enumerator.Add(_migrationUpdateDocument);
            }

            return enumerator.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}