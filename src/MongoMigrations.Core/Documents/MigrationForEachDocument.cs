﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoMigrations.Core.WriteModels;

namespace MongoMigrations.Core.Documents;

[DebuggerDisplay("Name: {Name}. Index: {Index}")]
public sealed class MigrationForEachDocument(
    [NotNull] FilterDefinition<BsonDocument> parentFilterDefinition,
    [NotNull] string name,
    IEnumerable<BsonDocument> bsonDocuments,
    int index,
    [NotNull] BsonDocument bsonDocument)
{
    readonly FilterDefinition<BsonDocument> _parentFilterDefinition = parentFilterDefinition ?? throw new ArgumentNullException(nameof(parentFilterDefinition));

    [UsedImplicitly] public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
    [UsedImplicitly] public IEnumerable<BsonDocument> BsonDocuments { get; } = bsonDocuments ?? throw new ArgumentNullException(nameof(bsonDocuments));
    [UsedImplicitly] public int Index { get; } = index < 0 ? throw new ArgumentOutOfRangeException(nameof(index)) : index;
    [UsedImplicitly] public BsonDocument BsonDocument { get; } = bsonDocument ?? throw new ArgumentNullException(nameof(bsonDocument));

    [UsedImplicitly] public FilterDefinition<BsonDocument> ByDocumentIdFilter()
    {
        if (!BsonDocument.TryGetElement("_id", out _))
        {
            throw new Exception("A default _id property does not exist in current document.");
        }
        return Builders<BsonDocument>.Filter.Eq($"{Name}._id", this["_id"]);
    }
    [UsedImplicitly]
    public BsonValue this[string name] => BsonDocument[name];
    [UsedImplicitly] public string Field(string field) => $"{Name}.{Index}.{field}";

    [UsedImplicitly]
    public IWriteModel Update([NotNull] FilterDefinition<BsonDocument> filterDefinition, [NotNull] UpdateDefinition<BsonDocument> updateDefinition)
    {
        if (filterDefinition == null) throw new ArgumentNullException(nameof(filterDefinition));
        if (updateDefinition == null) throw new ArgumentNullException(nameof(updateDefinition));
        return new WriteModel(new UpdateOneModel<BsonDocument>(filterDefinition, updateDefinition));
    }

    [UsedImplicitly]
    public IWriteModel Update([NotNull] FilterDefinition<BsonDocument> filterDefinition, [NotNull] Func<UpdateDefinitionBuilder<BsonDocument>, UpdateDefinition<BsonDocument>> builder)
    {
        if (filterDefinition == null) throw new ArgumentNullException(nameof(filterDefinition));
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        return new WriteModel(new UpdateOneModel<BsonDocument>(filterDefinition, builder(Builders<BsonDocument>.Update)));
    }

    [UsedImplicitly]
    public IWriteModel Update([NotNull] UpdateDefinition<BsonDocument> updateDefinition)
    {
        if (updateDefinition == null) throw new ArgumentNullException(nameof(updateDefinition));
        return Update(ByDocumentIdFilter(), updateDefinition);
    }

    [UsedImplicitly]
    public IWriteModel Update([NotNull] Func<UpdateDefinitionBuilder<BsonDocument>, UpdateDefinition<BsonDocument>> builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        return Update(builder(Builders<BsonDocument>.Update));
    }

    [UsedImplicitly]
    public IWriteModel Delete([NotNull] FilterDefinition<BsonDocument> filterDefinition)
    {
        if (filterDefinition == null) throw new ArgumentNullException(nameof(filterDefinition));
        return new WriteModel(new UpdateOneModel<BsonDocument>(_parentFilterDefinition, Builders<BsonDocument>.Update.PullFilter(Name, filterDefinition)));
    }

    [UsedImplicitly]
    public IWriteModel Skip()
    {
        return new DoNotApplyWriteModel();
    }
}