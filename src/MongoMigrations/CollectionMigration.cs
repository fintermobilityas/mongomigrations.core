using System.Linq;

namespace MongoMigrations
{
	using System;
	using System.Collections.Generic;
	using MongoDB.Bson;
	using MongoDB.Driver;

	public abstract class CollectionMigration : Migration
	{
		protected string CollectionName;
	    protected int BatchSize = 1000;

		public CollectionMigration(MigrationVersion version, string collectionName) : base(version)
		{
			CollectionName = collectionName;
		}

		public virtual IMongoQuery Filter()
		{
			return null;
		}

		public override void Update()
		{
		    var skip = 0;
		    while (true)
		    {
                var collection = GetCollection();
                var documents = GetDocuments(collection, skip);
		        if (documents.Any())
		        {
		            UpdateDocuments(collection, documents);
		            skip += documents.Count;
		        }
		        else
		        {
		            break;
		        }
		    }
		}

		public virtual void UpdateDocuments(MongoCollection<BsonDocument> collection, List<BsonDocument> documents)
		{
			foreach (var document in documents)
			{
			    try
			    {
			        UpdateDocument(collection, document);
			    }
				catch (MongoException exception)
				{
					OnErrorUpdatingDocument(document, exception);
				}
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

		public abstract void UpdateDocument(MongoCollection<BsonDocument> collection, BsonDocument document);

		protected virtual MongoCollection<BsonDocument> GetCollection()
		{
			return Database.GetCollection(CollectionName);
		}

		protected virtual List<BsonDocument> GetDocuments(MongoCollection<BsonDocument> collection, int skip = 0)
		{
			var query = Filter();
            var cursor = query != null
                       ? collection.Find(query)
                       : collection.FindAll();
		    cursor.SetSkip(skip);
		    cursor.SetLimit(BatchSize);
		    return cursor.ToList();
		}
	}
}