using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;

namespace MongoMigrations
{
    public sealed class MigrationLocator
    {
        readonly List<Assembly> _assemblies = new List<Assembly>();

        [UsedImplicitly] public List<MigrationFilter> MigrationFilters = new List<MigrationFilter>();

        [UsedImplicitly]
        public void LookForMigrationsInAssemblyOfType<T>()
        {
            var assembly = typeof(T).Assembly;
            LookForMigrationsInAssembly(assembly);
        }

        public void LookForMigrationsInAssembly(Assembly assembly)
        {
            if (_assemblies.Contains(assembly))
            {
                return;
            }

            _assemblies.Add(assembly);
        }

        public IEnumerable<Migration> GetAllMigrations()
        {
            return _assemblies
                .SelectMany(GetMigrationsFromAssembly)
                .OrderBy(m => m.Version);
        }

        IEnumerable<Migration> GetMigrationsFromAssembly(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes()
                    .Where(t => typeof(Migration).IsAssignableFrom(t) && !t.IsAbstract)
                    .Select(Activator.CreateInstance)
                    .OfType<Migration>()
                    .Where(m => !MigrationFilters.Any(f => f.Exclude(m)));
            }
            catch (Exception exception)
            {
                throw new MigrationException($"Cannot load migrations from assembly: {assembly.FullName}", exception);
            }
        }

        public MigrationVersion LatestVersion()
        {
            var migrations = GetAllMigrations().ToList();

            return !migrations.Any() ? MigrationVersion.Default() : migrations.Max(m => m.Version);
        }

        public IEnumerable<Migration> GetMigrationsAfter([NotNull] AppliedMigration version)
        {
            if (version == null) throw new ArgumentNullException(nameof(version));

            return GetAllMigrations().Where(m => m.Version > version.Version).OrderBy(m => m.Version);
        }
    }
}