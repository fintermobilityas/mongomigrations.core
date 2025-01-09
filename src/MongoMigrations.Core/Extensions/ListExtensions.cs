using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace MongoMigrations.Core.Extensions;

public static class ListExtensions
{
    // Copyright: https://stackoverflow.com/a/14815511
    public static bool IsMonotonicallyIncreasing<T>([NotNull] this List<T> list) where T : IComparable
    {
        return list.Zip(list.Skip(1), (lhs, rhs) => lhs.CompareTo(rhs) <= 0).All(x => x);
    }
}