using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoMigrations.Extensions;

namespace MongoMigrations
{
    public sealed class MigrationForEachDocument
    {
        readonly BsonDocument _document;

        [UsedImplicitly] public FilterDefinition<BsonDocument> ByIdFilter { get; }
        [UsedImplicitly] public string Name { get; }

        public MigrationForEachDocument(string name, [NotNull] BsonDocument document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            Name = name;
            ByIdFilter = Builders<BsonDocument>.Filter.ElemMatch(name, Builders<BsonDocument>.Filter.Eq("_id", document["_id"]));
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

        public static explicit operator BsonDocument(MigrationForEachDocument document)
        {
            return document._document;
        }
    }

    public sealed class MigrationForEachDocuments 
    {
        readonly string _name;
        readonly BsonArray _bsonArray;

        public MigrationForEachDocuments(string name, [NotNull] BsonArray bsonArray)
        {
            _name = name;
            _bsonArray = bsonArray;
        }

        [UsedImplicitly]
        public IEnumerable<IWriteModel> ForEach([NotNull] Func<(IEnumerable<BsonDocument> documents, int index, BsonDocument current, MigrationForEachDocument apply), IWriteModel> enumeratorFunc)
        {
            if (enumeratorFunc == null) throw new ArgumentNullException(nameof(enumeratorFunc));

            var writeModels = new List<IWriteModel>();
            var index = 0;
            var subDocuments = _bsonArray.Select(x => x.AsBsonDocument).ToList();
            foreach (var document in subDocuments)
            {
                var writeModel = enumeratorFunc((subDocuments, index++, document, new MigrationForEachDocument(_name, document)));
                if (writeModel is BreakWriteModel)
                {
                    break;
                }
                if (writeModel is DoNotApplyWriteModel)
                {
                    continue;
                }
                writeModels.Add(writeModel);
            }
            return writeModels;
        }
    }

    public sealed class MigrationRootDocument : IEnumerable<IWriteModel>
    {
        readonly List<IWriteModel> _writeModels;

        [UsedImplicitly] public FilterDefinition<BsonDocument> ByIdFilter { get; }
        [UsedImplicitly] public BsonDocument Document { get; }

        [UsedImplicitly]
        public BsonValue this[string name] => Document[name];

        public MigrationRootDocument([NotNull] BsonDocument document)
        {
            Document = document ?? throw new ArgumentNullException(nameof(document));
            _writeModels = new List<IWriteModel>();
            ByIdFilter = Builders<BsonDocument>.Filter.Eq("_id", this["_id"]);
        }

        [UsedImplicitly]
        public void ForEach([NotNull] string name, [NotNull] Func<(IEnumerable<BsonDocument> documents, int index, BsonDocument current, MigrationForEachDocument apply), IWriteModel> enumeratorFunc)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (enumeratorFunc == null) throw new ArgumentNullException(nameof(enumeratorFunc));

            _writeModels.AddRange(new MigrationForEachDocuments(name, this[name].AsBsonArray).ForEach(enumeratorFunc));
        }

        [UsedImplicitly]
        public IEnumerable<IWriteModel> Update([NotNull] FilterDefinition<BsonDocument> filter, [NotNull] BsonDocument document)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            if (document == null) throw new ArgumentNullException(nameof(document));
            return new UpdateOneModel<BsonDocument>(filter, document).AsEnumerable();
        }

        [UsedImplicitly]
        public IEnumerable<IWriteModel> Update([NotNull] FilterDefinition<BsonDocument> filter, [NotNull] UpdateDefinition<BsonDocument> updateDefinition)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            if (updateDefinition == null) throw new ArgumentNullException(nameof(updateDefinition));
            return new UpdateOneModel<BsonDocument>(filter, updateDefinition).AsEnumerable();
        }

        [UsedImplicitly]
        public IEnumerable<IWriteModel> Update([NotNull] FilterDefinition<BsonDocument> filter, [NotNull] Func<UpdateDefinitionBuilder<BsonDocument>, UpdateDefinition<BsonDocument>> builder)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            return Update(filter, builder(Builders<BsonDocument>.Update));
        }

        [UsedImplicitly]
        public IEnumerable<IWriteModel> Delete([NotNull] FilterDefinition<BsonDocument> filter)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            return new DeleteOneModel<BsonDocument>(filter).AsEnumerable();
        }

        [UsedImplicitly]
        public IEnumerable<IWriteModel> Update([NotNull] BsonDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            return Update(ByIdFilter, document);
        }

        [UsedImplicitly]
        public IEnumerable<IWriteModel> Update([NotNull] UpdateDefinition<BsonDocument> updateDefinition)
        {
            if (updateDefinition == null) throw new ArgumentNullException(nameof(updateDefinition));
            return Update(ByIdFilter, updateDefinition);
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
            return Delete(ByIdFilter);
        }

        [UsedImplicitly]
        public IEnumerable<IWriteModel> Skip()
        {
            return new List<IWriteModel> {new DoNotApplyWriteModel()};
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