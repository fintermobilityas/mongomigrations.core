﻿using System;

namespace MongoMigrations
{
    public class ConcurrentMigrationException : MigrationException
    {
        public MigrationVersion Version { get; }

        public ConcurrentMigrationException(MigrationVersion version, Exception innerException) : base($"Migration is already in progress. Version: {version}", innerException)
        {
            Version = version;
        }
    }
}