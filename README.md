# 📖 About mongomigrations.core

![dependabot](https://api.dependabot.com/badges/status?host=github&repo=fintermobilityas/mongomigrations.core) ![License](https://img.shields.io/github/license/fintermobilityas/mongomigrations.core.svg)

[![NuGet](https://img.shields.io/nuget/v/mongomigrations.core.svg)](https://www.nuget.org/packages/mongomigrations.core) [![downloads](https://img.shields.io/nuget/dt/mongomigrations.core)](https://www.nuget.org/packages/mongomigrations.core) ![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/mongomigrations.core) ![Size](https://img.shields.io/github/repo-size/fintermobilityas/mongomigrations.core.svg) 

| Build server | Platforms | Build status |
|--------------|----------|--------------|
| Github Actions | windows-latest, ubuntu-latest | Branch: develop ![mongomigrations.core](https://github.com/fintermobilityas/mongomigrations.core/workflows/mongomigrations.core/badge.svg?branch=develop) |
| Github Actions | windows-latest, ubuntu-latest | Branch: master ![mongomigrations.core](https://github.com/fintermobilityas/mongomigrations.core/workflows/mongomigrations.core/badge.svg?branch=master) |

## 🚀 Getting Started Guide

Typed migrations for MongoDB. 

### Migration runner

The migration runner executes migrations. The default collection name, is `DatabaseVersion`. Upon application startup, e.g. in ASP.NET Core you should invoke `UpdateToLatest` before any controller actions or hangfire jobs are executed. This method is safe to invoke in a distributed environment, e.g. a IIS web farm. 

```csharp
var migrationRunner = new MigrationRunner("connectionString");
migrationRunner.MigrationLocator.LookForMigrationsInAssembly(typeof(Migration1).Assembly);
retryMigration:
try
{
    migrationRunner.UpdateToLatest(Environment.MachineName);
}
catch (ConcurrentMigrationException)
{
    Thread.Sleep(5000);

    goto retryMigration;
}
```

### Migrate a single collection

```csharp
class Migration1 : Migration
{
    public Migration1() : base(1)
    {
        Description = "Drop a collection";
    }

    public override void Update()
    {
        Database.DropCollection("Collection");
    }
}
```

### Migrate all documents in a collection

```csharp
class Migration2 : CollectionMigration
{

    public Migration2() : base(2, "Collection")
    {
        Description = "Ensure all documents has a Reference property";
        // Execute batch writes when 1000 documents has been migrated.
        BatchSize = 1000;
    }

    public override IEnumerable<IWriteModel> MigrateDocument(MigrationDocument document)
    {
        if (!document.BsonDocument.TryGetElement("Reference", out _))
        {
            return document.Skip();
        }
      
        return document.Update(x => x.Set("Reference", BsonNull.Value));
    }

    public override void OnBeforeMigration()
    {
      // Is invoked before any migrations are migrated.
    }

    public override void OnAfterSuccessfulMigration()
    {
        // Is executed after all documents has been migrated.
    }
}
```
