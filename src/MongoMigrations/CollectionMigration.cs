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
        [UsedImplicitly] int DocumentCount { get; }
        [UsedImplicitly] int DocumentsDeletedCount {get;}
        [UsedImplicitly]
        IMongoCollection<BsonDocument> Collection { get; }
        [UsedImplicitly]
        string CollectionName { get; }
    }

    [DebuggerDisplay("Version: {" + nameof(Version) + "}. Collection name: {" + nameof(CollectionName) + "}. Batch size: {"+ nameof(BatchSize) + "}.")]
    [UsedImplicitly]
    public abstract class CollectionMigration : Migration, ICollectionMigration
    {
        // ReSharper disable once PublicConstructorInAbstractClass
        public CollectionMigration(int major, string collectionName) : base(major)
        {
            if (string.IsNullOrWhiteSpace(collectionName)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(collectionName));
            CollectionName = collectionName;
        }

        public int DocumentCount { get; private set; }
        public int DocumentsDeletedCount { get; private set; }
        public IMongoCollection<BsonDocument> Collection { get; internal set; }
        public string CollectionName { get; }
        public int BatchSize { get; set; } = 1000;
        public FilterDefinition<BsonDocument> Filter { get; set; } = FilterDefinition<BsonDocument>.Empty;
        public ProjectionDefinition<BsonDocument> Project { get; set; } 

        [UsedImplicitly]
        [NotNull] public abstract IEnumerable<IWriteModel> MigrateDocument(MigrationDocument document);

        /// <summary>
        ///     Invoked before `Update`
        /// </summary>
        public abstract void OnBeforeMigration();

        /// <summary>
        ///     Invoked after a successfull `Update`
        /// </summary>
        public abstract void OnAfterSuccessfulMigration();

        public override void Update()
        {
            var buffer = new List<IWriteModel>();
            var skip = 0;

            void Flush()
            {
                foreach (var batch in buffer.Batch(BatchSize))
                {
                    var writeModels = batch.Select(x => x.Model).ToList();
                    Collection.BulkWrite(writeModels);
                    DocumentsDeletedCount += writeModels.Count(x => x is DeleteOneModel<BsonDocument>);
                }

                buffer.Clear();
            }

            while (true)
            {
                var documents = GetDocuments(skip - DocumentsDeletedCount);
                if (documents.Any())
                {
                    DocumentCount += documents.Count;

                    try
                    {
                        buffer.AddRange(MigrateDocuments(documents));

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

        IEnumerable<IWriteModel> MigrateDocuments(IEnumerable<BsonDocument> documents)
        {
            var writeModels = new List<IWriteModel>();
            foreach (var document in documents)
            {
                try
                {
                    var doNotApplyWriteModelCount = 0;

                    var migrationDocumentWriteModels = MigrateDocument(new MigrationDocument(document)).Where(writeModel =>
                    {
                        if (writeModel == null)
                        {
                            throw new ArgumentNullException(nameof(writeModel),
                                $"Illegal return value. Cannot return a {nameof(IWriteModel)} of type null from method {nameof(MigrateDocument)}.");
                        }

                        var doNotApplyWriteModel = writeModel is DoNotApplyWriteModel;
                        if (doNotApplyWriteModel)
                        {
                            doNotApplyWriteModelCount += 1;
                        }

                        return !doNotApplyWriteModel;
                    }).ToList();

                    var migrateDocumentDeleteWriteModels = migrationDocumentWriteModels.Where(x => x.Model is DeleteOneModel<BsonDocument>).ToList();
                    if (migrateDocumentDeleteWriteModels.Count > 1)
                    {
                        throw new Exception($"Multiple delete write models is not allowed. Delete count: {migrateDocumentDeleteWriteModels.Count}. Write models count: {migrationDocumentWriteModels.Count}");
                    }
      
                    if (doNotApplyWriteModelCount == 1 && migrationDocumentWriteModels.Count > 1)
                    {
                        throw new Exception($"Multiple write models is not allowed when skipping a document. Write model count: {migrationDocumentWriteModels.Count}.");
                    }

                    writeModels.AddRange(migrationDocumentWriteModels);

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
                    Message = "Failed to migrate document",
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
                    Message = "Failed to migrate documents",
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

            var filterDefinition = Filter ?? FilterDefinition<BsonDocument>.Empty;
            var cursor = Collection.Find(filterDefinition);

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