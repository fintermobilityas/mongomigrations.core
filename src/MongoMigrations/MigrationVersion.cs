﻿using System;
using JetBrains.Annotations;

namespace MongoMigrations
{
    public struct MigrationVersion : IComparable<MigrationVersion>
    {
        public int Major { get; }
        public int Minor { get; }
        public int Revision { get; }

        public static MigrationVersion Default => new MigrationVersion(0);

        public MigrationVersion(string version)
        {
            var versionParts = version?.Split('.') ?? Array.Empty<string>();

            if (versionParts.Length != 3)
                throw new ArgumentException($"Versions must have format: major.minor.revision, this doesn\'t match: {version}", nameof(version));

            var majorString = versionParts[0];
            if (!int.TryParse(majorString, out var major))
                throw new ArgumentException($"Invalid major version value: {majorString}", nameof(version));
            Major = major;

            var minorString = versionParts[1];
            if (!int.TryParse(minorString, out var minor))
                throw new ArgumentException($"Invalid major version value: {minorString}", nameof(version));
            Minor = minor;

            var revisionString = versionParts[2];
            if (!int.TryParse(revisionString, out var revision))
                throw new ArgumentException($"Invalid major version value: {revisionString}", nameof(version));
            Revision = revision;
        }

        public MigrationVersion(int major, int minor = 0, int revision = 0)
        {
            if (major < 0) throw new ArgumentOutOfRangeException(nameof(major));
            if (revision < 0) throw new ArgumentOutOfRangeException(nameof(revision));
            if (minor < 0) throw new ArgumentOutOfRangeException(nameof(minor));

            Major = major;
            Minor = minor;
            Revision = revision;
        }

        public static bool operator ==(MigrationVersion a, MigrationVersion b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(MigrationVersion a, MigrationVersion b)
        {
            return !(a == b);
        }

        public static bool operator >(MigrationVersion a, MigrationVersion b)
        {
            return a.Major > b.Major
                   || a.Major == b.Major && a.Minor > b.Minor
                   || a.Major == b.Major && a.Minor == b.Minor && a.Revision > b.Revision;
        }

        public static bool operator <(MigrationVersion a, MigrationVersion b)
        {
            return a != b && !(a > b);
        }

        public static bool operator <=(MigrationVersion a, MigrationVersion b)
        {
            return a == b || a < b;
        }

        public static bool operator >=(MigrationVersion a, MigrationVersion b)
        {
            return a == b || a > b;
        }

        [UsedImplicitly]
        public bool Equals(MigrationVersion other)
        {
            return other.Major == Major && other.Minor == Minor && other.Revision == Revision;
        }

        public int CompareTo(MigrationVersion other)
        {
            if (Equals(other))
                return 0;
            return this > other ? 1 : -1;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is MigrationVersion version && Equals(version);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var result = Major;
                result = (result * 397) ^ Minor;
                result = (result * 397) ^ Revision;
                return result;
            }
        }

        public override string ToString()
        {
            return $"{Major}.{Minor}.{Revision}";
        }

        public static implicit operator string(MigrationVersion version)
        {
            return version.ToString();
        }
    }
}