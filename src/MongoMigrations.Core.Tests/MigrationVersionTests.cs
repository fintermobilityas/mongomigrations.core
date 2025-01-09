using Xunit;

namespace MongoMigrations.Core.Tests;

public class MigrationVersionTests
{
    [Fact]
    public void TestDefault()
    {
        Assert.Equal(new MigrationVersion(0), MigrationVersion.Default);
        Assert.NotEqual(new object(), MigrationVersion.Default);
        Assert.Equal(0, MigrationVersion.Default.Version);
        Assert.Equal(0, new MigrationVersion(0).Version);
    }

    [Fact]
    public void Test_Operator_Equals()
    {
        // ReSharper disable once EqualExpressionComparison
        Assert.True(new MigrationVersion(0) == new MigrationVersion(0));
        Assert.False(new MigrationVersion(0) == new MigrationVersion(1));
    }

    [Fact]
    public void Test_Operator_NotEquals()
    {
        Assert.True(new MigrationVersion(0) != new MigrationVersion(1));
        // ReSharper disable once EqualExpressionComparison
        Assert.False(new MigrationVersion(0) != new MigrationVersion(0));
    }

    [Fact]
    public void Test_Operator_GreaterThan()
    {
        Assert.True(new MigrationVersion(1) > new MigrationVersion(0));
        Assert.False(new MigrationVersion(0) > new MigrationVersion(1));
    }

    [Fact]
    public void Test_Operator_LessThan()
    {
        Assert.True(new MigrationVersion(0) < new MigrationVersion(1));
        Assert.False(new MigrationVersion(1) < new MigrationVersion(0));
    }

    [Fact]
    public void Test_Operator_LessThanEqualto()
    {
        Assert.True(new MigrationVersion(0) <= new MigrationVersion(1));
        // ReSharper disable once EqualExpressionComparison
        Assert.True(new MigrationVersion(1) <= new MigrationVersion(1));
        Assert.False(new MigrationVersion(1) <= new MigrationVersion(0));
    }

    [Fact]
    public void Test_Operator_GreaterThanOrEqualTo()
    {
        Assert.True(new MigrationVersion(1) >= new MigrationVersion(0));
        // ReSharper disable once EqualExpressionComparison
        Assert.True(new MigrationVersion(1) >= new MigrationVersion(1));
        Assert.False(new MigrationVersion(0) >= new MigrationVersion(1));
    }

    [Fact]
    public void TestEquals()
    {
        Assert.Equal(new MigrationVersion(0), new MigrationVersion(0));
        Assert.NotEqual(new MigrationVersion(0), new MigrationVersion(1));
    }

    [Fact]
    public void TestToString()
    {
        Assert.Equal("1", new MigrationVersion(1).ToString());
        Assert.Equal("1", new MigrationVersion(1));
    }

    [Fact]
    public void TestCompareTo()
    {
        Assert.Equal(0, new MigrationVersion(0).CompareTo(new MigrationVersion(0)));
        Assert.Equal(1, new MigrationVersion(1).CompareTo(new MigrationVersion(0)));
        Assert.Equal(-1, new MigrationVersion(0).CompareTo(new MigrationVersion(1)));
    }

    [Fact]
    public void TestGetHashCode()
    {
        Assert.Equal(123, new MigrationVersion(123).GetHashCode());
    }
}