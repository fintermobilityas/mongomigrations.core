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
        public void TestGetMigrations_Empty()
        {
            using var fixture = new MigrationRunnerFixture(_databaseFixture);

            Assert.Empty(fixture.MigrationRunner.DatabaseStatus.GetMigrations());
        }

        [Fact]
        public void TestGetMigrations_IsSortedByNewestVersion()
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

            Assert.Equal(2, migrations[0].Version.Version);
            Assert.Equal(1, migrations[1].Version.Version);
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
    }
}
