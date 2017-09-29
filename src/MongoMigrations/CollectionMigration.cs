using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MongoDB.Bson;
using MongoDB.Driver;

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

            var skip = 0;
            while (true)
            {
                var documents = GetDocuments(skip);
                if (documents.Any())
                {
                    UpdateDocuments(documents);
                    skip += documents.Count;
                }
                else
                {
                    break;
                }
            }
        }

        [UsedImplicitly]
        public virtual void UpdateDocuments(IEnumerable<BsonDocument> documents)
        {
            foreach (var document in documents)
                try
                {
                    UpdateDocument(document);
                }
                catch (MongoException exception)
                {
                    OnErrorUpdatingDocument(document, exception);
                }
        }

        protected virtual void OnErrorUpdatingDocument(BsonDocument document, Exception exception)
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

        [UsedImplicitly]
        public abstract void UpdateDocument(BsonDocument document);

        protected virtual List<BsonDocument> GetDocuments(int skip = 0)
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