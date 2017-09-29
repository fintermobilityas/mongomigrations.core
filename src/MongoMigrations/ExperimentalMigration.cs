using System;
using JetBrains.Annotations;

namespace MongoMigrations
{
    [UsedImplicitly]
    [AttributeUsage(AttributeTargets.Class)]
    public class ExperimentalAttribute : Attribute
    {
    }
}