using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoMigrations.Extensions;
using MoreLinq;

namespace MongoMigrations
{
    public interface ICollectionMigration : ISupportFilter, ISupportOnBeforeMigration, ISupportOnAfterSuccessfullMigration, ISupportBatchSize
    {
        [UsedImplicitly]
        IMongoCollection<BsonDocument> Collection { get; }
        [UsedImplicitly]
        string CollectionName { get; }
    }

    [UsedImplicitly]
    public abstract class CollectionMigration : Migration, ICollectionMigration
    {
        protected CollectionMigration(MigrationVersion version, string collectionName) : base(version)
        {
            if (string.IsNullOrWhiteSpace(collectionName)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(collectionName));
            CollectionName = collectionName;
            Version = version;
        }

        public IMongoCollection<BsonDocument> Collection { get; [UsedImplicitly] set; }
        public string CollectionName { get; }
        public int BatchSize { get; set; } = 1000;
        public FilterDefinition<BsonDocument> Filter { get; set; } = FilterDefinition<BsonDocument>.Empty;

        [UsedImplicitly]
        [NotNull] public abstract IEnumerable<IWriteModel> UpdateDocument(MigrationRootDocument rootDocument);

        /// <summary>
        ///     Invoked before `Update`
        /// </summary>
        public virtual void OnBeforeMigration()
        {
        }

        /// <summary>
        ///     Invoke after `Update`
        /// </summary>
        public virtual void OnAfterSuccessfulMigration()
        {
        }

        public override void Update()
        {
            Collection = Database.GetCollection<BsonDocument>(CollectionName);

            var buffer = new List<IWriteModel>();
            var skip = 0;

            void Flush()
            {
                foreach (var batch in buffer.Batch(BatchSize))
                {
                    Collection.BulkWrite(batch.Select(x => x.Model));
                }

                buffer.Clear();
            }

            while (true)
            {
                var documents = GetDocuments(skip);
                if (documents.Any())
                {
                    try
                    {
                        buffer.AddRange(UpdateDocuments(documents));

                        if (buffer.Count >= BatchSize)
                        {
                            Flush();
                        }
                    }
                    catch (Exception exception)
                    {
                        ThrowErrorUpdatingDocuments(exception);
                    }

                    skip += documents.Count;
                    continue;
                }

                try
                {
                    Flush();
                }
                catch (Exception exception)
                {
                    ThrowErrorUpdatingDocuments(exception);
                }

                break;
            }
        }

        [UsedImplicitly]
        IEnumerable<IWriteModel> UpdateDocuments(IEnumerable<BsonDocument> documents)
        {
            var writeModels = new List<IWriteModel>();
            foreach (var document in documents)
            {
                try
                {
                    writeModels.AddRange(UpdateDocument(new MigrationRootDocument(document)).Where(writeModel =>
                    {
                        if (writeModel == null)
                        {
                            throw new ArgumentNullException(nameof(writeModel), $"Illegal return value. Cannot return a {nameof(IWriteModel)} of type null from method {nameof(UpdateDocument)}.");
                        }

                        return !(writeModel is DoNotApplyWriteModel);
                    }));
                }
                catch (MongoException exception)
                {
                    ThrowErrorUpdatingDocument(document, exception);
                }
            }
            return writeModels;
        }

        void ThrowErrorUpdatingDocument(BsonDocument document, Exception exception)
        {
            var message =
                new
                {
                    Message = "Failed to update document",
                    CollectionName,
                    Id = document.TryGetDocumentId(),
                    MigrationVersion = Version,
                    MigrationDescription = Description
                };

            throw new MigrationException(message.ToString(), exception);
        }

        void ThrowErrorUpdatingDocuments(Exception exception)
        {
            var message =
                new
                {
                    Message = "Failed to update documents",
                    CollectionName,
                    MigrationVersion = Version,
                    MigrationDescription = Description
                };

            throw new MigrationException(message.ToString(), exception);
        }

        List<BsonDocument> GetDocuments(int skip = 0)
        {
            if (BatchSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(BatchSize), "Must be greater than zero.");

            var cursor = Filter != null ? Collection.Find(Filter) : Collection.Find(FilterDefinition<BsonDocument>.Empty);

            cursor
                .Skip(skip)
                .Limit(BatchSize);

            return cursor.ToList();
        }
    }
}