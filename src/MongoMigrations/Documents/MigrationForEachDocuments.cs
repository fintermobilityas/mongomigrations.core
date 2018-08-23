using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoMigrations.WriteModels;
using MongoMigrations.Extensions;

namespace MongoMigrations.Documents
{
    [DebuggerDisplay("Name: {_name}")]
    public sealed class MigrationForEachDocuments : IEnumerable<IWriteModel>
    {
        readonly string _name;
        readonly BsonArray _bsonArray;
        readonly Func<MigrationForEachDocument, IWriteModel> _enumeratorFunc;
        readonly List<IWriteModel> _writeModels;

        [UsedImplicitly] public List<string> JsonDocuments => BsonDocuments.Select(x => x?.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.Strict })).ToList();
        [UsedImplicitly] public List<BsonDocument> BsonDocuments => this.Select(x => x.Model?.ToWriteModelBsonDocument()).ToList();

        public MigrationForEachDocuments([NotNull] string name, [NotNull] BsonArray bsonArray, [NotNull] Func<MigrationForEachDocument, IWriteModel> enumeratorFunc)
        {
            _name = name;
            _bsonArray = bsonArray;
            _enumeratorFunc = enumeratorFunc;
            _writeModels = new List<IWriteModel>();
        }

        public IEnumerator<IWriteModel> GetEnumerator()
        {
            if (_writeModels.Any())
            {
                using (var enumerator = _writeModels.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        yield return enumerator.Current;
                    }
                }

                yield break;
            }

            var index = 0;
            var subDocuments = _bsonArray.Select(x => x.AsBsonDocument).ToList();
            foreach (var document in _bsonArray.Select(x => x.AsBsonDocument))
            {
                var writeModel = _enumeratorFunc(new MigrationForEachDocument(_name, subDocuments, index++, document));
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