using System;
using JetBrains.Annotations;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoMigrations
{
    public class AppliedMigration
    {
        const string ManuallyMarked = "Manually marked";

        AppliedMigration()
        {
        }

        public AppliedMigration(IMigration migration)
        {
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

        public override string ToString()
        {
            return $"{Version} started on {StartedOn} completed on {CompletedOn}";
        }

        public static AppliedMigration MarkerOnly(MigrationVersion version)
        {
            return new AppliedMigration
            {
                Version = version,
                Description = ManuallyMarked,
                StartedOn = DateTime.Now,
                CompletedOn = DateTime.Now
            };
        }
    }
}