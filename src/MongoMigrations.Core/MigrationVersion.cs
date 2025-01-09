using System;
using System.Globalization;

namespace MongoMigrations.Core;

public readonly struct MigrationVersion : IComparable<MigrationVersion>
{
    public int Version { get; }

    public static MigrationVersion Default => new(0);

    public MigrationVersion(int version)
    {
        if (version < 0) throw new ArgumentOutOfRangeException(nameof(version));

        Version = version;
    }

    public static bool operator ==(MigrationVersion lhs, MigrationVersion rhs)
    {
        return lhs.Equals(rhs);
    }

    public static bool operator !=(MigrationVersion lhs, MigrationVersion rhs)
    {
        return !(lhs == rhs);
    }

    public static bool operator >(MigrationVersion lhs, MigrationVersion rhs)
    {
        return lhs.Version > rhs.Version;
    }

    public static bool operator <(MigrationVersion lhs, MigrationVersion rhs)
    {
        return lhs.Version < rhs.Version;
    }

    public static bool operator <=(MigrationVersion lhs, MigrationVersion rhs)
    {
        return lhs.Version <= rhs.Version;
    }

    public static bool operator >=(MigrationVersion lhs, MigrationVersion rhs)
    {
        return lhs.Version >= rhs.Version;
    }

    public bool Equals(MigrationVersion other)
    {
        return other.Version == Version;
    }

    public int CompareTo(MigrationVersion other)
    {
        if (Equals(other))
        {
            return 0;
        }

        return Version > other.Version ? 1 : -1;
    }

    public override bool Equals(object obj)
    {
        return obj is MigrationVersion version && Equals(version);
    }

    public override int GetHashCode()
    {
        return Version;
    }

    public override string ToString()
    {
        return Version.ToString(CultureInfo.InvariantCulture);
    }

    public static implicit operator string(MigrationVersion version)
    {
        return version.ToString();
    }
}