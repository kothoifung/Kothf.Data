// Licensed to Kothf under one or more agreements.
// Kothf licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using System.Runtime.CompilerServices;

#if NET9_0_OR_GREATER
using System.Threading;
#endif

namespace Kothf.Data;

/// <summary>
/// Provides a thread-safe cache for DbProviderFactory objects
/// </summary>
/// <remarks>Intended to optimize repeated access to DbProviderFactories.</remarks>
internal static class DbProviderFactoryCache
{
    private static readonly Dictionary<string, DbProviderFactory> _factoryCache = [];
#if NET9_0_OR_GREATER
    private static readonly Lock _lock = new();
#else
    private static readonly object _syncRoot = new();
#endif

    /// <summary>
    /// Retrieves the DbProviderFactory instance for the specified data provider
    /// </summary>
    /// <param name="providerInvariantName">The invariant name of the data provider</param>
    /// <returns>The DbProviderFactory instance</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DbProviderFactory GetFactory(string providerInvariantName)
    {
        if (!_factoryCache.TryGetValue(providerInvariantName, out var factory))
        {
#if NET9_0_OR_GREATER
            using (_lock.EnterScope())
            {
                if (!_factoryCache.TryGetValue(providerInvariantName, out factory))
                {
                    factory = DbProviderFactories.GetFactory(providerInvariantName);
                    _factoryCache[providerInvariantName] = factory;
                }
            }
#else
            lock (_syncRoot)
            {
                if (!_factoryCache.TryGetValue(providerInvariantName, out factory))
                {
                    factory = DbProviderFactories.GetFactory(providerInvariantName);
                    _factoryCache[providerInvariantName] = factory;
                }
            }
#endif
        }

        return factory;
    }
}
