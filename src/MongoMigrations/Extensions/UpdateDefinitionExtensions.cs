using System;
using JetBrains.Annotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace MongoMigrations.Extensions
{
    public static class UpdateDefinitionExtensions
    {
        [UsedImplicitly]
        public static UpdateDefinition<TDocument> SetIf<TDocument>(this UpdateDefinition<TDocument> builder, string field, BsonValue value, Func<bool> ifFunc)
        {
            return ifFunc() ? builder.Set(field, value) : builder;
        }

        [UsedImplicitly]
        public static UpdateDefinition<TDocument> SetIf<TDocument>(this UpdateDefinition<TDocument> builder, string field, BsonValue value, bool truthy)
        {
            return builder.SetIf(field, value, () => truthy);
        }

        [UsedImplicitly]
        public static UpdateDefinition<TDocument> SetIf<TDocument>(this UpdateDefinitionBuilder<TDocument> builder, string field, BsonValue value, Func<bool> ifFunc)
        {
            return ifFunc() ? builder.Set(field, value) : NoopUpdateDefinition<TDocument>.Instance;
        }

        [UsedImplicitly]
        public static UpdateDefinition<TDocument> SetIf<TDocument>(this UpdateDefinitionBuilder<TDocument> builder, string field, BsonValue value, bool truthy)
        {
            return builder.SetIf(field, value, () => truthy);
        }

        [UsedImplicitly]
        public static UpdateDefinition<TDocument> UnsetIf<TDocument>(this UpdateDefinition<TDocument> builder, string field, Func<bool> ifFunc)
        {
            return ifFunc() ? builder.Unset(field) : builder;
        }

        [UsedImplicitly]
        public static UpdateDefinition<TDocument> UnsetIf<TDocument>(this UpdateDefinition<TDocument> builder, string field, bool truthy)
        {
            return builder.UnsetIf(field, () => truthy);
        }

        [UsedImplicitly]
        public static UpdateDefinition<TDocument> UnsetIf<TDocument>(this UpdateDefinitionBuilder<TDocument> builder, string field, Func<bool> ifFunc)
        {
            return ifFunc() ? builder.Unset(field) : NoopUpdateDefinition<TDocument>.Instance;
        }

        [UsedImplicitly]
        public static UpdateDefinition<TDocument> UnsetIf<TDocument>(this UpdateDefinitionBuilder<TDocument> builder, string field, bool truthy)
        {
            return builder.UnsetIf(field, () => truthy);
        }

        public sealed class NoopUpdateDefinition<TDocument> : UpdateDefinition<TDocument>
        {
            NoopUpdateDefinition()
            {
            }

            public static NoopUpdateDefinition<TDocument> Instance { get; } = new NoopUpdateDefinition<TDocument>();

            public override BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
            {
                return new BsonDocument();
            }
        }
    }
}
