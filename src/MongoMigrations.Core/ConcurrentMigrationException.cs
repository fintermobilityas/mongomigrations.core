using System;

namespace MongoMigrations.Core;

public class ConcurrentMigrationException(MigrationVersion version, Exception innerException)
    : MigrationException($"Migration is already in progress. Version: {version}", innerException)
{
    public MigrationVersion Version { get; } = version;
}