using System;

namespace MongoMigrations
{
    public class MigrationException : Exception
    {
        public MigrationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}