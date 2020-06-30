using System;

namespace MongoMigrations.Core
{
    public class MigrationException : Exception
    {
        public MigrationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}