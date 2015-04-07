using System;
using MongoDB.Bson.Serialization;

namespace MongoMigrations
{
    public class MigrationVersionSerializer : IBsonSerializer
    {
        public Type ValueType => typeof(MigrationVersion);

	    public object Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
	    {
            var versionString = context.Reader.ReadString();
            return new MigrationVersion(versionString);
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
	    {
            var version = (MigrationVersion)value;
            var versionString = string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Revision);
            context.Writer.WriteString(versionString);
        }
    }
}