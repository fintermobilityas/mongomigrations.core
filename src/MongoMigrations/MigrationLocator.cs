using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;

namespace MongoMigrations
{
    public sealed class MigrationLocator
    {
        readonly List<Assembly> _assemblies;
        readonly Dictionary<string, List<Migration>> _migrationsDictionary;
        readonly object _syncRoot;

        public MigrationLocator()
        {
            _assemblies = new List<Assembly>();
            _migrationsDictionary = new Dictionary<string, List<Migration>>();
            _syncRoot = new object();
        }

        [UsedImplicitly]
        public void LookForMigrationsInAssemblyOfType<T>()
        {
            var assembly = typeof(T).Assembly;
            LookForMigrationsInAssembly(assembly);
        }

        public void LookForMigrationsInAssembly([NotNull] Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            lock (_syncRoot)
            {
                if (!_assemblies.Contains(assembly))
                {
                    _assemblies.Add(assembly);
                }
            }
        }

        public IEnumerable<Migration> GetAllMigrations()
        {
            static List<Migration> FindMigrations(Assembly assembly)
            {
                if (assembly == null) throw new ArgumentNullException(nameof(assembly));

                try
                {
                    return assembly.GetTypes()
                        .Where(t => typeof(Migration).IsAssignableFrom(t) && !t.IsAbstract)
                        .Select(Activator.CreateInstance)
                        .OfType<Migration>()
                        .OrderBy(x => x.Version)
                        .ToList();
                }
                catch (Exception exception)
                {
                    throw new MigrationException($"Cannot load migrations from assembly: {assembly.FullName}", exception);
                }
            }

            lock (_syncRoot)
            {
                foreach (var assembly in _assemblies)
                {
                    if (!_migrationsDictionary.ContainsKey(assembly.FullName))
                    {
                        _migrationsDictionary[assembly.FullName] = FindMigrations(assembly);
                    }

                    foreach (var migration in _migrationsDictionary[assembly.FullName])
                    {
                        yield return migration;
                    }
                }
            }
        }

        public MigrationVersion GetLatestVersion()
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