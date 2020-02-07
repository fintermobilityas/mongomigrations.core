using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MongoDB.Driver;
using MongoMigrations.Extensions;
using MongoMigrations.Tests.Fixtures;
using Moq;
using Xunit;

namespace MongoMigrations.Tests
{
    public class MigrationRunnerTests : IClassFixture<DatabaseFixture>
    {
        readonly DatabaseFixture _databaseFixture;

        public MigrationRunnerTests(DatabaseFixture databaseFixture)
        {
            _databaseFixture = databaseFixture;
        }

        [Fact]
        public void TestAddIndexes()
        {
            using var fixture = new MigrationRunnerFixture(_databaseFixture);

            fixture.MigrationRunner.DatabaseStatus.AddIndexes();
            fixture.MigrationRunner.DatabaseStatus.AddIndexes();

            var index = fixture.MigrationRunner.DatabaseStatus.Collection.Indexes
                .List().ToList().Select(x => x["name"].AsString).SingleOrDefault(x => x == "CompletedOn_1");

            Assert.NotNull(index);
        }

        [Fact]
        public void TestGetMigrations_Empty()
        {
            using var fixture = new MigrationRunnerFixture(_databaseFixture);

            Assert.Empty(fixture.MigrationRunner.DatabaseStatus.GetMigrations());
        }

        [Fact]
        public void TestGetMigrations_IsSortedByOldestVersion()
        {
            using var fixture = new MigrationRunnerFixture(_databaseFixture);

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
            using var fixture = new MigrationRunnerFixture(_databaseFixture);

            var migration1Mock = new Mock<IMigration>();
            migration1Mock.SetupGet(x => x.Version).Returns(new MigrationVersion(1));

            var migration2Mock = new Mock<IMigration>();
            migration2Mock.SetupGet(x => x.Version).Returns(new MigrationVersion(2));

            fixture.Collection.InsertOne(new AppliedMigration(migration1Mock.Object));
            fixture.Collection.InsertOne(new AppliedMigration(migration2Mock.Object));

            Assert.Equal(2, fixture.MigrationRunner.DatabaseStatus.GetLastAppliedMigration().Version.Version);
        }

        [Fact]
        public void TestUpdateToLatest()
        {
            var migration1Mock = new Mock<IMigration>();
            migration1Mock.SetupGet(x => x.Version).Returns(new MigrationVersion(1));
            migration1Mock.SetupGet(x => x.Database).Returns(_databaseFixture.Database);

            var migration2Mock = new Mock<IMigration>();
            migration2Mock.SetupGet(x => x.Version).Returns(new MigrationVersion(2));
            migration2Mock.SetupGet(x => x.Database).Returns(_databaseFixture.Database);

            var migrationLocator = new MigrationLocator(typeof(MigrationLocatorTests).Assembly, new List<IMigration>
            {
                migration1Mock.Object,
                migration2Mock.Object
            });

            using var fixture = new MigrationRunnerFixture(_databaseFixture, migrationLocator);
            fixture.Collection.InsertOne(new AppliedMigration(migration1Mock.Object) { CompletedOn = DateTime.Now });

            var migrations = fixture.MigrationRunner.DatabaseStatus.GetMigrations();
            Assert.Single(migrations);

            fixture.MigrationRunner.UpdateToLatest();

            Assert.True(fixture.MigrationRunner.IsDatabaseUpToDate());

            migrations = fixture.MigrationRunner.DatabaseStatus.GetMigrations();
            Assert.Equal(2, migrations.Count);

            Assert.Equal(1, migrations[0].Version.Version);
            Assert.NotNull(migrations[0].CompletedOn);
            Assert.Equal(2, migrations[1].Version.Version);
            Assert.NotNull(migrations[1].CompletedOn);
        }

        [Fact]
        public void TestUpdateToLatest_No_Prior_Migrations()
        {
            var migration1Mock = new Mock<IMigration>();
            migration1Mock.SetupGet(x => x.Version).Returns(new MigrationVersion(1));
            migration1Mock.SetupGet(x => x.Database).Returns(_databaseFixture.Database);

            var migrationLocator = new MigrationLocator(typeof(MigrationLocatorTests).Assembly, new List<IMigration> { migration1Mock.Object });

            using var fixture = new MigrationRunnerFixture(_databaseFixture, migrationLocator);

            var migrations = fixture.MigrationRunner.DatabaseStatus.GetMigrations();
            Assert.Empty(migrations);

            fixture.MigrationRunner.UpdateToLatest();

            Assert.True(fixture.MigrationRunner.IsDatabaseUpToDate());

            migrations = fixture.MigrationRunner.DatabaseStatus.GetMigrations();
            Assert.Single(migrations);
            Assert.Equal(1, migrations[0].Version.Version);
            Assert.NotNull(migrations[0].CompletedOn);
        }

        [Fact]
        public void TestUpdateLatest_Is_Thread_Safe()
        {
            var random = new Random();

            var mocks = Enumerable.Range(0, 1000).Select(version =>
            {
                var mock = new Mock<IMigration>();
                mock.SetupGet(x => x.Version).Returns(new MigrationVersion(version));
                mock.SetupGet(x => x.Database).Returns(_databaseFixture.Database);
                mock.Setup(x => x.Update()).Callback(() => Thread.Sleep(random.Next(1, 5)));
                return mock;
            }).ToList();

            var migrationLocator = new MigrationLocator(typeof(MigrationLocatorTests).Assembly, mocks.Select(x => x.Object).ToList());

            using var fixture = new MigrationRunnerFixture(_databaseFixture, migrationLocator);

            var concurrentMigrationExceptionsThrown = new ConcurrentQueue<ConcurrentMigrationException>();
            long migrationCompleted = 0;
            long migrationAttempts = 0;

            void TryMigrate(string serverName)
            {
                while (Interlocked.Read(ref migrationCompleted) == 0)
                {
                    try
                    {
                        Interlocked.Increment(ref migrationAttempts);
                        fixture.MigrationRunner.UpdateToLatest(serverName);
                        Interlocked.Exchange(ref migrationCompleted, 1);
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

            while (Interlocked.Read(ref migrationCompleted) != 1)
            {
                Thread.Sleep(50);
            }

            Assert.True(fixture.MigrationRunner.IsDatabaseUpToDate());
            Assert.True(Interlocked.Read(ref migrationAttempts) >= mocks.Count);

            var migrations = fixture.MigrationRunner.DatabaseStatus.GetMigrations();
            Assert.Equal(mocks.Count, migrations.Count);
            Assert.NotEmpty(concurrentMigrationExceptionsThrown);

            var serverNames = migrations.Select(x => x.ServerName).Distinct().ToList();
            Assert.Equal(4, serverNames.Count);
            Assert.StartsWith("thread", serverNames[0]);

            foreach (var migration in migrations)
            {
                Assert.NotNull(migration.CompletedOn);
            }

            var migrationsSortedByStartedOn = migrations.OrderBy(x => x.StartedOn).ThenBy(x => x.CompletedOn).Select(x => x.Version.Version).ToList();
            Assert.True(migrationsSortedByStartedOn.IsMonotonicallyIncreasing());
        }

        [Fact]
        public void TestUpdateLatest_Is_Thread_Safe_Long_Running()
        {
            var mocks = Enumerable.Range(0, 2).Select(version =>
            {
                var mock = new Mock<IMigration>();
                mock.SetupGet(x => x.Version).Returns(new MigrationVersion(version));
                mock.SetupGet(x => x.Database).Returns(_databaseFixture.Database);
                mock.Setup(x => x.Update()).Callback(() => Thread.Sleep(5000));
                return mock;
            }).ToList();

            var migrationLocator = new MigrationLocator(typeof(MigrationLocatorTests).Assembly, mocks.Select(x => x.Object).ToList());

            using var fixture = new MigrationRunnerFixture(_databaseFixture, migrationLocator);

            var concurrentMigrationExceptionsThrown = new ConcurrentQueue<ConcurrentMigrationException>();
            long migrationCompleted = 0;
            long migrationAttempts = 0;

            void TryMigrate(string serverName)
            {
                while (Interlocked.Read(ref migrationCompleted) == 0)
                {
                    try
                    {
                        Interlocked.Increment(ref migrationAttempts);
                        fixture.MigrationRunner.UpdateToLatest(serverName);
                        Interlocked.Exchange(ref migrationCompleted, 1);
                    }
                    catch (ConcurrentMigrationException e)
                    {
                        concurrentMigrationExceptionsThrown.Enqueue(e);
                    }
                }
            }

            new Thread(() => TryMigrate("thread1")) { IsBackground = true }.Start();
            new Thread(() => TryMigrate("thread2")) { IsBackground = true }.Start();

            while (Interlocked.Read(ref migrationCompleted) != 1)
            {
                Thread.Sleep(50);
            }

            Assert.True(fixture.MigrationRunner.IsDatabaseUpToDate());
            Assert.True(Interlocked.Read(ref migrationAttempts) >= mocks.Count);

            var migrations = fixture.MigrationRunner.DatabaseStatus.GetMigrations();
            Assert.Equal(mocks.Count, migrations.Count);
            Assert.NotEmpty(concurrentMigrationExceptionsThrown);

            var serverNames = migrations.Select(x => x.ServerName).Distinct().ToList();
            Assert.True(serverNames.Count >= 1);
            Assert.StartsWith("thread", serverNames[0]);

            foreach (var migration in migrations)
            {
                Assert.NotNull(migration.CompletedOn);
            }

            var migrationsSortedByStartedOn = migrations.OrderBy(x => x.StartedOn).ThenBy(x => x.CompletedOn).Select(x => x.Version.Version).ToList();
            Assert.True(migrationsSortedByStartedOn.IsMonotonicallyIncreasing());
        }
    }
}
