using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoMigrations.Documents;
using MongoMigrations.Extensions;
using MongoMigrations.WriteModels;
using MoreLinq;

namespace MongoMigrations
{
    public interface ICollectionMigration : ISupportFilter, ISupportOnBeforeMigration, ISupportOnAfterSuccessfullMigration, ISupportBatchSize, ISupportProjection
    {
        [UsedImplicitly]
        IMongoCollection<BsonDocument> Collection { get; }
        [UsedImplicitly]
        string CollectionName { get; }
    }

    [DebuggerDisplay("Collection name: {CollectionName}. Batch size: {BatchSize}.")]
    [UsedImplicitly]
    public abstract class CollectionMigration : Migration, ICollectionMigration
    {
        // ReSharper disable once PublicConstructorInAbstractClass
        public CollectionMigration(int major, string collectionName) : base(major)
        {
            if (string.IsNullOrWhiteSpace(collectionName)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(collectionName));
            CollectionName = collectionName;
        }

        public IMongoCollection<BsonDocument> Collection { get; [UsedImplicitly] set; }
        public string CollectionName { get; }
        public int BatchSize { get; set; } = 1000;
        public FilterDefinition<BsonDocument> Filter { get; set; } = FilterDefinition<BsonDocument>.Empty;
        public ProjectionDefinition<BsonDocument> Project { get; set; } 

        [UsedImplicitly]
        [NotNull] public abstract IEnumerable<IWriteModel> UpdateDocument(MigrationDocument document);

        /// <summary>
        ///     Invoked before `Update`
        /// </summary>
        public virtual void OnBeforeMigration()
        {
            
        }

        /// <summary>
        ///     Invoked after `Update`
        /// </summary>
        public virtual void OnAfterSuccessfulMigration()
        {
            
        }

        public override void Update()
        {
            Collection = Database.GetCollection<BsonDocument>(CollectionName);

            var buffer = new List<IWriteModel>();
            var skip = 0;
            var deletedDocuments = 0;

            void Flush()
            {
                foreach (var batch in buffer.Batch(BatchSize))
                {
                    var writeModels = batch.Select(x => x.Model).ToList();
                    Collection.BulkWrite(writeModels);
                    deletedDocuments += writeModels.Count(x => x is DeleteOneModel<BsonDocument>);
                }

                buffer.Clear();
            }

            while (true)
            {
                var documents = GetDocuments(skip - deletedDocuments);
                if (documents.Any())
                {
                    try
                    {
                        var writeModels = UpdateDocuments(documents).ToList();
                        buffer.AddRange(writeModels);

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
                    writeModels.AddRange(UpdateDocument(new MigrationDocument(document)).Where(writeModel =>
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

            if (Project != null)
            {
                cursor.Project(Project);
            }

            return cursor.ToList();
        }

    }
}