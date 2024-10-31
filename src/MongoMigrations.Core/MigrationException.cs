using System;

namespace MongoMigrations.Core;

public class MigrationException(string message, Exception innerException) : Exception(message, innerException);