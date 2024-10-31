using System;
using JetBrains.Annotations;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoMigrations.Core;

public sealed class AppliedMigration
{ 
    AppliedMigration()
    {
            
    }

    public AppliedMigration([NotNull] IMigration migration)
    {
        if (migration == null) throw new ArgumentNullException(nameof(migration));
        Version = migration.Version;
        StartedOn = DateTime.Now;
        Description = migration.Description;
    }

    [BsonId]
    public MigrationVersion Version { get; [UsedImplicitly]  set; }
    [UsedImplicitly]
    public string Description{ get; set; }
    [UsedImplicitly]
    public DateTime StartedOn { get; set; }
    public DateTime? CompletedOn { get; set; }
    public DateTime? FailedOn { get; set; }
    public string ServerName { get; set; }
    public string ExceptionMessage { get; set; }
        
    public override string ToString()
    {
        return $"{Version} started on {StartedOn} completed on {CompletedOn}";
    }
}