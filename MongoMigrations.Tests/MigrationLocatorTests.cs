using Xunit;

namespace MongoMigrations.Tests
{
    public class MigrationLocatorTests : IClassFixture<BaseFixture>
    {
        readonly BaseFixture _baseFixture;
        readonly MigrationLocator _migrationLocator;

        public MigrationLocatorTests(BaseFixture baseFixture)
        {
            _baseFixture = baseFixture;
            _migrationLocator = new MigrationLocator();
        }

        [Fact]
        public void TestLookForMigrationsInAssemblyOfType()
        {
            _migrationLocator.LookForMigrationsInAssembly(typeof(MigrationLocatorTests).Assembly);

            var migrations = _migrationLocator.GetAllMigrations();
            Assert.Single(migrations);
        }
    }

    class TestMigration01 : Migration
    {
        public TestMigration01() : base(0)
        {
        }

        public override void Update()
        {
            throw new System.NotImplementedException();
        }
    }
}
