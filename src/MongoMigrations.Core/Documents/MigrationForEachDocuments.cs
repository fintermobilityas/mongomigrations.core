using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using MongoMigrations.Core.Extensions;
using MongoMigrations.Core.WriteModels;

namespace MongoMigrations.Core.Documents
{
    [DebuggerDisplay("Name: {" + nameof(_name) + "}")]
    public sealed class MigrationForEachDocuments : IEnumerable<IWriteModel>
    {
        readonly FilterDefinition<BsonDocument> _parentFilterDefinition;

        readonly string _name;
        readonly BsonArray _bsonArray;
        readonly Func<MigrationForEachDocument, IWriteModel> _enumeratorFunc;
        readonly List<IWriteModel> _writeModels;

        [UsedImplicitly] public List<string> JsonDocuments => BsonDocuments.Select(x => x?.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.Strict })).ToList();
        [UsedImplicitly] public List<BsonDocument> BsonDocuments => this.Select(x => x.Model?.ToWriteModelBsonDocument()).ToList();

        public MigrationForEachDocuments([NotNull] FilterDefinition<BsonDocument> parentFilterDefinition, [NotNull] string name, [NotNull] BsonArray bsonArray, [NotNull] Func<MigrationForEachDocument, IWriteModel> enumeratorFunc)
        {
            _parentFilterDefinition = parentFilterDefinition ?? throw new ArgumentNullException(nameof(parentFilterDefinition));
            _name = name;
            _bsonArray = bsonArray;
            _enumeratorFunc = enumeratorFunc;
            _writeModels = new List<IWriteModel>();
        }

        public IEnumerator<IWriteModel> GetEnumerator()
        {
            if (_writeModels.Any())
            {
                using var enumerator = _writeModels.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    yield return enumerator.Current;
                }

                yield break;
            }

            var index = 0;
            var subDocuments = _bsonArray.Select(x => x.AsBsonDocument).ToList();
            foreach (var document in _bsonArray.Select(x => x.AsBsonDocument))
            {
                var writeModel = _enumeratorFunc(new MigrationForEachDocument(_parentFilterDefinition, _name, subDocuments, index++, document));
                switch (writeModel)
                {
                    case DoNotApplyWriteModel _:
                        continue;
                }

                _writeModels.Add(writeModel);
                yield return writeModel;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}