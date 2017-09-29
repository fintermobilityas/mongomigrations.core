using System;
using JetBrains.Annotations;

namespace MongoMigrations
{
    public struct MigrationVersion : IComparable<MigrationVersion>
    {
        /// <summary>
        ///     Return the default, "first" version 0.0.0
        /// </summary>
        /// <returns></returns>
        public static MigrationVersion Default()
        {
            return default;
        }

        public readonly int Major;
        public readonly int Minor;
        public readonly int Revision;

        public MigrationVersion(string version)
        {
            var versionParts = version?.Split('.') ?? new string[] {};
            if (versionParts.Length != 3)
                throw new ArgumentException("Versions must have format: major.minor.revision, this doesn't match: " + version);
            var majorString = versionParts[0];
            if (!int.TryParse(majorString, out Major))
                throw new ArgumentException("Invalid major version value: " + majorString);
            var minorString = versionParts[1];
            if (!int.TryParse(minorString, out Minor))
                throw new ArgumentException("Invalid major version value: " + minorString);
            var revisionString = versionParts[2];
            if (!int.TryParse(revisionString, out Revision))
                throw new ArgumentException("Invalid major version value: " + revisionString);
        }

        [UsedImplicitly]
        public MigrationVersion(int major, int minor = 0, int revision = 0)
        {
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

        public static implicit operator MigrationVersion(string version)
        {
            return new MigrationVersion(version);
        }

        public static implicit operator string(MigrationVersion version)
        {
            return version.ToString();
        }
    }
}