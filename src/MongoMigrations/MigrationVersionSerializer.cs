using System;
using MongoDB.Bson.Serialization;

namespace MongoMigrations
{
    public class MigrationVersionSerializer : IBsonSerializer<MigrationVersion>
    {
        public Type ValueType => typeof(MigrationVersion);

	    public object Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
	    {
            var versionString = context.Reader.ReadString();
            return new MigrationVersion(versionString);
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, MigrationVersion value)
        {
            var versionString = $"{value.Major}.{value.Minor}.{value.Revision}";
            context.Writer.WriteString(versionString);
        }

        MigrationVersion IBsonSerializer<MigrationVersion>.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            return (MigrationVersion) Deserialize(context, args);
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
	    {
            Serialize(context, args, (MigrationVersion) value);
        }
    }
}