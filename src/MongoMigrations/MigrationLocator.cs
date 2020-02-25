using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace MongoMigrations
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
    public interface IMigrationLocator
    {
        void LookForMigrationsInAssemblyOfType<T>();
        void LookForMigrationsInAssembly([NotNull] Assembly assembly);
        IEnumerable<IMigration> GetAllMigrations();
        MigrationVersion GetLatestVersion();
        Task<MigrationVersion> GetLatestVersionAsync(CancellationToken cancellationToken = default);
        IEnumerable<IMigration> GetMigrationsAfter([NotNull] AppliedMigration appliedMigration);
    }

    public sealed class MigrationLocator : IMigrationLocator
    {
        readonly List<Assembly> _assemblies;
        readonly Dictionary<string, List<IMigration>> _migrationsDictionary;
        readonly object _syncRoot;

        public MigrationLocator()
        {
            _assemblies = new List<Assembly>();
            _migrationsDictionary = new Dictionary<string, List<IMigration>>();
            _syncRoot = new object();
        }

        internal MigrationLocator([NotNull] Assembly assembly, [NotNull] List<IMigration> migrations) : this()
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            _assemblies.Add(assembly);
            _migrationsDictionary[assembly.FullName] = migrations ?? throw new ArgumentNullException(nameof(migrations));
        }

        [UsedImplicitly]
        public void LookForMigrationsInAssemblyOfType<T>()
        {
            var assembly = typeof(T).Assembly;
            LookForMigrationsInAssembly(assembly);
        }

        public void LookForMigrationsInAssembly(Assembly assembly)
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

        public IEnumerable<IMigration> GetAllMigrations()
        {
            static List<IMigration> FindMigrations(Assembly assembly)
            {
                if (assembly == null) throw new ArgumentNullException(nameof(assembly));

                try
                {
                    return assembly.GetTypes()
                        .Where(t => typeof(Migration).IsAssignableFrom(t) && !t.IsAbstract)
                        .Select(Activator.CreateInstance)
                        .OfType<Migration>()
                        .OrderBy(x => x.Version)
                        .Cast<IMigration>()
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

            return !migrations.Any() ? MigrationVersion.Default : migrations.Max(m => m.Version);
        }

        public Task<MigrationVersion> GetLatestVersionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GetLatestVersion());
        }

        public IEnumerable<IMigration> GetMigrationsAfter(AppliedMigration appliedMigration)
        {
            var migrations = GetAllMigrations();

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (appliedMigration != null)
            {
                migrations = migrations.Where(x => x.Version > appliedMigration.Version);
            }

            return migrations.OrderBy(m => m.Version);
        }
    }
}