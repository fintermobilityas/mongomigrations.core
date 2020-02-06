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

            if (!migrations.Any())
            {
                return MigrationVersion.Default();
            }

            return migrations.Max(m => m.Version);
        }

        public IEnumerable<Migration> GetMigrationsAfter(AppliedMigration currentVersion)
        {
            var migrations = GetAllMigrations();

            if (currentVersion != null)
            {
                migrations = migrations.Where(m => m.Version > currentVersion.Version);
            }

            return migrations.OrderBy(m => m.Version);
        }
    }
}