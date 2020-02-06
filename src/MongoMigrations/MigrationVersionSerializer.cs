using System;
using JetBrains.Annotations;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoMigrations
{
    public class MigrationVersionSerializer : SerializerBase<MigrationVersion>
    {
        public override MigrationVersion Deserialize([NotNull] BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            var versionString = context.Reader.ReadInt32();
            return new MigrationVersion(versionString);
        }

        public override void Serialize([NotNull] BsonSerializationContext context, BsonSerializationArgs args, MigrationVersion value)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            context.Writer.WriteInt32(value.Version);
        }
    }
}