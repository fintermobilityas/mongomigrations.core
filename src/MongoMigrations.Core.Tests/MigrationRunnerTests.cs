using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoMigrations.Core.Extensions;
using MongoMigrations.Core.Tests.Fixtures;
using Moq;
using Xunit;

namespace MongoMigrations.Core.Tests;

public class MigrationRunnerTests(DatabaseFixture databaseFixture) : IClassFixture<DatabaseFixture>
{
    [Fact]
    public void TestAddIndexes()
    {
        using var fixture = new MigrationRunnerFixture(databaseFixture);

        fixture.MigrationRunner.DatabaseStatus.AddIndexes();
        fixture.MigrationRunner.DatabaseStatus.AddIndexes();

        var index = fixture.MigrationRunner.DatabaseStatus.Collection.Indexes
            .List().ToList().Select(x => x["name"].AsString).SingleOrDefault(x => x == "CompletedOn_1");

        Assert.NotNull(index);
    }

    [Fact]
    public void TestGetMigrations_Empty()
    {
        using var fixture = new MigrationRunnerFixture(databaseFixture);

        Assert.Empty(fixture.MigrationRunner.DatabaseStatus.GetMigrations());
    }

    [Fact]
    public void TestGetMigrations_IsSortedByOldestVersion()
    {
        using var fixture = new MigrationRunnerFixture(databaseFixture);

        var migration1Mock = new Mock<IMigration>();
        migration1Mock.SetupGet(x => x.Version).Returns(new MigrationVersion(1));

        var migration2Mock = new Mock<IMigration>();
        migration2Mock.SetupGet(x => x.Version).Returns(new MigrationVersion(2));

        fixture.Collection.InsertOne(new AppliedMigration(migration1Mock.Object));
        fixture.Collection.InsertOne(new AppliedMigration(migration2Mock.Object));

        var migrations = fixture.MigrationRunner.DatabaseStatus.GetMigrations();
        Assert.Equal(2, migrations.Count);

        Assert.Equal(1, migrations[0].Version.Version);
        Assert.Equal(2, migrations[1].Version.Version);
    }

    [Fact]
    public void TestGetLastAppliedMigration()
    {
        using var fixture = new MigrationRunnerFixture(databaseFixture);

        var migration1Mock = new Mock<IMigration>();
        migration1Mock.SetupGet(x => x.Version).Returns(new MigrationVersion(1));

        var migration2Mock = new Mock<IMigration>();
        migration2Mock.SetupGet(x => x.Version).Returns(new MigrationVersion(2));

        fixture.Collection.InsertOne(new AppliedMigration(migration1Mock.Object) { CompletedOn = DateTime.MinValue });
        fixture.Collection.InsertOne(new AppliedMigration(migration2Mock.Object) { CompletedOn = DateTime.MinValue.AddSeconds(1) });

        var lastAppliedMigration = fixture.MigrationRunner.DatabaseStatus.GetLastAppliedMigration();
        Assert.NotNull(lastAppliedMigration);
        Assert.Equal(2, lastAppliedMigration.Version.Version);
    }

    [Fact]
    public async Task TestGetLastAppliedMigrationAsync()
    {
        using var fixture = new MigrationRunnerFixture(databaseFixture);

        var migration1Mock = new Mock<IMigration>();
        migration1Mock.SetupGet(x => x.Version).Returns(new MigrationVersion(1));

        var migration2Mock = new Mock<IMigration>();
        migration2Mock.SetupGet(x => x.Version).Returns(new MigrationVersion(2));

        await fixture.Collection.InsertOneAsync(new AppliedMigration(migration1Mock.Object) { CompletedOn = DateTime.MinValue });
        await fixture.Collection.InsertOneAsync(new AppliedMigration(migration2Mock.Object) { CompletedOn = DateTime.MinValue });

        var lastAppliedMigration = await fixture.MigrationRunner.DatabaseStatus.GetLastAppliedMigrationAsync();
        Assert.NotNull(lastAppliedMigration);

        Assert.Equal(2, lastAppliedMigration.Version.Version);
    }

    [Fact]
    public void TestUpdateToLatest()
    {
        var migration1Mock = new Mock<IMigration>();
        migration1Mock.SetupGet(x => x.Version).Returns(new MigrationVersion(1));
        migration1Mock.SetupGet(x => x.Database).Returns(databaseFixture.Database);

        var migration2Mock = new Mock<IMigration>();
        migration2Mock.SetupGet(x => x.Version).Returns(new MigrationVersion(2));
        migration2Mock.SetupGet(x => x.Database).Returns(databaseFixture.Database);

        var migrationLocator = new MigrationLocator(typeof(MigrationLocatorTests).Assembly, new List<IMigration>
        {
            migration1Mock.Object,
            migration2Mock.Object
        });

        using var fixture = new MigrationRunnerFixture(databaseFixture, migrationLocator);
        fixture.Collection.InsertOne(new AppliedMigration(migration1Mock.Object) { CompletedOn = DateTime.Now });

        var migrations = fixture.MigrationRunner.DatabaseStatus.GetMigrations();
        Assert.Single(migrations);

        fixture.MigrationRunner.UpdateToLatest();

        Assert.True(fixture.MigrationRunner.IsDatabaseUpToDate(default));

        migrations = fixture.MigrationRunner.DatabaseStatus.GetMigrations();
        Assert.Equal(2, migrations.Count);

        Assert.Equal(1, migrations[0].Version.Version);
        Assert.NotNull(migrations[0].CompletedOn);
        Assert.Equal(2, migrations[1].Version.Version);
        Assert.NotNull(migrations[1].CompletedOn);
    }

    [Fact]
    public void TestUpdateToLatest_IsNotLatestVersion()
    {
        var migration1Mock = new Mock<IMigration>();
        migration1Mock.SetupGet(x => x.Version).Returns(new MigrationVersion(1));
        migration1Mock.SetupGet(x => x.Database).Returns(databaseFixture.Database);

        var migration2Mock = new Mock<IMigration>();
        migration2Mock.SetupGet(x => x.Version).Returns(new MigrationVersion(2));
        migration2Mock.SetupGet(x => x.Database).Returns(databaseFixture.Database);

        var migrationLocator = new MigrationLocator(typeof(MigrationLocatorTests).Assembly, new List<IMigration>
        {
            migration1Mock.Object,
            migration2Mock.Object
        });

        using var fixture = new MigrationRunnerFixture(databaseFixture, migrationLocator);
        fixture.Collection.InsertOne(new AppliedMigration(migration1Mock.Object) { CompletedOn = DateTime.Now });

        var migrations = fixture.MigrationRunner.DatabaseStatus.GetMigrations();
        Assert.Single(migrations);

        Assert.False(fixture.MigrationRunner.IsDatabaseUpToDate(default));

        migrations = fixture.MigrationRunner.DatabaseStatus.GetMigrations();
        Assert.Single(migrations);

        Assert.Equal(1, migrations[0].Version.Version);
        Assert.NotNull(migrations[0].CompletedOn);
    }
        
    [Fact]
    public void TestUpdateToLatest_IsNotLatestVersion_MigrationFailed()
    {
        var migration1Mock = new Mock<IMigration>();
        migration1Mock.SetupGet(x => x.Version).Returns(new MigrationVersion(1));
        migration1Mock.SetupGet(x => x.Database).Returns(databaseFixture.Database);

        var migration2Mock = new Mock<IMigration>();
        migration2Mock.SetupGet(x => x.Version).Returns(new MigrationVersion(2));
        migration2Mock.SetupGet(x => x.Database).Returns(databaseFixture.Database);

        var migrationLocator = new MigrationLocator(typeof(MigrationLocatorTests).Assembly, new List<IMigration>
        {
            migration1Mock.Object,
            migration2Mock.Object
        });

        using var fixture = new MigrationRunnerFixture(databaseFixture, migrationLocator);
        fixture.Collection.InsertOne(new AppliedMigration(migration1Mock.Object) { CompletedOn = DateTime.Now });
        fixture.Collection.InsertOne(new AppliedMigration(migration2Mock.Object) { FailedOn = DateTime.Now });

        var migrations = fixture.MigrationRunner.DatabaseStatus.GetMigrations();
        Assert.Equal(2, migrations.Count);

        Assert.False(fixture.MigrationRunner.IsDatabaseUpToDate(default));

        migrations = fixture.MigrationRunner.DatabaseStatus.GetMigrations();
        Assert.Equal(2, migrations.Count);

        Assert.Equal(1, migrations[0].Version.Version);
        Assert.NotNull(migrations[0].CompletedOn);
        Assert.Null(migrations[0].FailedOn);
            
        Assert.Equal(2, migrations[1].Version.Version);
        Assert.Null(migrations[1].CompletedOn);
        Assert.NotNull(migrations[1].FailedOn);
    }

    [Fact]
    public void TestUpdateToLatest_Exception_Is_Thrown()
    {
        var migration1Mock = new Mock<IMigration>();
        migration1Mock.SetupGet(x => x.Version).Returns(new MigrationVersion(1));
        migration1Mock.SetupGet(x => x.Database).Returns(databaseFixture.Database);
        migration1Mock.Setup(x => x.Update()).Throws(new Exception("YOLO"));

        var migrationLocator = new MigrationLocator(typeof(MigrationLocatorTests).Assembly, new List<IMigration>
        {
            migration1Mock.Object
        });

        using var fixture = new MigrationRunnerFixture(databaseFixture, migrationLocator);

        var ex = Assert.Throws<MigrationException>(() => fixture.MigrationRunner.UpdateToLatest());
        Assert.StartsWith("{ Message = Migration failed to be applied: YOLO", ex.Message);

        Assert.False(fixture.MigrationRunner.IsDatabaseUpToDate(default));

        var migrations = fixture.MigrationRunner.DatabaseStatus.GetMigrations();
        Assert.Single(migrations);

        Assert.Equal(1, migrations[0].Version.Version);
        Assert.Null(migrations[0].CompletedOn);
        Assert.Equal("YOLO", migrations[0].ExceptionMessage);
        Assert.NotNull(migrations[0].FailedOn);
    }

    [Fact]
    public void TestUpdateToLatest_No_Prior_Migrations()
    {
        var migration1Mock = new Mock<IMigration>();
        migration1Mock.SetupGet(x => x.Version).Returns(new MigrationVersion(1));
        migration1Mock.SetupGet(x => x.Database).Returns(databaseFixture.Database);

        var migrationLocator = new MigrationLocator(typeof(MigrationLocatorTests).Assembly, new List<IMigration> { migration1Mock.Object });

        using var fixture = new MigrationRunnerFixture(databaseFixture, migrationLocator);

        var migrations = fixture.MigrationRunner.DatabaseStatus.GetMigrations();
        Assert.Empty(migrations);

        fixture.MigrationRunner.UpdateToLatest();

        Assert.True(fixture.MigrationRunner.IsDatabaseUpToDate(default));

        migrations = fixture.MigrationRunner.DatabaseStatus.GetMigrations();
        Assert.Single(migrations);
        Assert.Equal(1, migrations[0].Version.Version);
        Assert.NotNull(migrations[0].CompletedOn);
    }

    [Fact]
    public void TestUpdateLatest_Is_Thread_Safe()
    {
        var random = new Random();
            
        var mocks = Enumerable.Range(0, 2).Select(version =>
        {
            var mock = new Mock<IMigration>();
            mock.SetupGet(x => x.Version).Returns(new MigrationVersion(version));
            mock.SetupGet(x => x.Database).Returns(databaseFixture.Database);
            mock.Setup(x => x.Update()).Callback(() => Thread.Sleep(random.Next(100, 300)));
            return mock;
        }).ToList();

        var migrationLocator = new MigrationLocator(typeof(MigrationLocatorTests).Assembly, mocks.Select(x => x.Object).ToList());

        using var fixture = new MigrationRunnerFixture(databaseFixture, migrationLocator);

        var concurrentMigrationExceptionsThrown = new ConcurrentQueue<ConcurrentMigrationException>();
        long migrationCompleted = 0;
        long migrationAttempts = 0;
        long threadsExited = 0;

        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(30));

        void TryMigrate(string serverName)
        {
            while (!cts.Token.IsCancellationRequested)
            {
                cts.Token.ThrowIfCancellationRequested();

                try
                {
                    Interlocked.Increment(ref migrationAttempts);
                    fixture.MigrationRunner.UpdateToLatest(serverName);
                    var migrationsCompleted = Interlocked.Increment(ref migrationCompleted);
                    if (migrationsCompleted >= mocks.Count)
                    {
                        Interlocked.Increment(ref threadsExited);
                        break;
                    }
                }
                catch (ConcurrentMigrationException e)
                {
                    concurrentMigrationExceptionsThrown.Enqueue(e);
                }
            }
        }
            
        new Thread(() => TryMigrate("thread1")) { IsBackground = true }.Start();
        new Thread(() => TryMigrate("thread2")) { IsBackground = true }.Start();
        new Thread(() => TryMigrate("thread3")) { IsBackground = true }.Start();
        new Thread(() => TryMigrate("thread4")) { IsBackground = true }.Start();

        while (Interlocked.Read(ref threadsExited) != 4)
        {
            Thread.Sleep(50);
        }

        Assert.True(fixture.MigrationRunner.IsDatabaseUpToDate(default));
        Assert.True(Interlocked.Read(ref migrationAttempts) >= mocks.Count);

        var appliedMigrations = fixture.MigrationRunner.DatabaseStatus.GetMigrations();
        Assert.Equal(mocks.Count, appliedMigrations.Count);
        Assert.NotEmpty(concurrentMigrationExceptionsThrown);

        var serverNames = appliedMigrations.Select(x => x.ServerName).Distinct().ToList();
        Assert.StartsWith("thread", serverNames[0]);

        foreach (var migration in appliedMigrations)
        {
            Assert.NotNull(migration.CompletedOn);
        }

        var appliedMigrationsVersions = appliedMigrations.Select(x => x.Version.Version).ToList();
        Assert.True(appliedMigrationsVersions.IsMonotonicallyIncreasing());
    }
}