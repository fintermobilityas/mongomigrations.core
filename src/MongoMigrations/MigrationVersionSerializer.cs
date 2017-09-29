using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoMigrations
{
    public class MigrationVersionSerializer : SerializerBase<MigrationVersion>
    {
        public override MigrationVersion Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var versionString = context.Reader.ReadString();
            return new MigrationVersion(versionString);
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, MigrationVersion value)
        {
            var versionString = $"{value.Major}.{value.Minor}.{value.Revision}";
            context.Writer.WriteString(versionString);
        }
    }
}