using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace MongoMigrations.Tests
{
    public class MigrationLocatorTests : IClassFixture<BaseFixture>
    {
        readonly MigrationLocator _migrationLocator;

        public MigrationLocatorTests(BaseFixture baseFixture)
        {
            _migrationLocator = new MigrationLocator();
        }

        [Fact]
        public void TestGetAllMigrations()
        {
            AddMigrations();

            var migrations = _migrationLocator.GetAllMigrations();
            Assert.Single(migrations);
        }

        [Fact]
        public void TestGetLatestVersion()
        {
            AddMigrations();

            var latestVersion = _migrationLocator.GetLatestVersion();
            Assert.Equal(new MigrationVersion(1), latestVersion);
            Assert.Equal(1, latestVersion.Major);
        }

        [Fact]
        public void TestGetLatestVersion_No_Prior_Migrations()
        {
            Assert.Empty(_migrationLocator.GetAllMigrations());

            var latestVersion = _migrationLocator.GetLatestVersion();
            Assert.Equal(MigrationVersion.Default(), latestVersion);
        }

        void AddMigrations()
        {
            _migrationLocator.LookForMigrationsInAssembly(typeof(MigrationLocatorTests).Assembly);
        }
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    class TestMigration01 : Migration
    {
        public TestMigration01() : base(1)
        {
        }

        public override void Update()
        {
            throw new System.NotImplementedException();
        }
    }
}
