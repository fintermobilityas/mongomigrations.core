using MongoDB.Driver;

namespace MongoMigrations
{
    using System.Linq;
    using MongoDB.Bson;

    public static class BsonDocumentExtensions
    {
        /// <summary>
        /// 	Rename all instances of a name in a bson document to the new name.
        /// </summary>
        public static void ChangeName(this BsonDocument bsonDocument, string originalName, string newName)
        {
            var elements = bsonDocument.Elements
                .Where(e => e.Name == originalName)
                .ToList();
            foreach (var element in elements)
            {
                bsonDocument.RemoveElement(element);
                bsonDocument.Add(new BsonElement(newName, element.Value));
            }
        }

        public static object TryGetDocumentId(this BsonDocument bsonDocument)
        {
            BsonValue id;
            bsonDocument.TryGetValue("_id", out id);
            return id ?? "Cannot find id";
        }

        public static void Save(this IMongoCollection<BsonDocument> collection, BsonDocument bsonDocument, string id = "_id")
        {
            var documentId = bsonDocument.GetValue(id);
            collection.ReplaceOne(Builders<BsonDocument>.Filter.Eq(x => x[id], documentId), bsonDocument);
        }
    }
}