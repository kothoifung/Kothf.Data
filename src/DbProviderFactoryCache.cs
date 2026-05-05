// Licensed to Kothf under one or more agreements.
// Kothf licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using System.Collections.Concurrent;

namespace Kothf.Data;

/// <summary>
/// Provides a thread-safe cache for DbProviderFactory objects
/// </summary>
/// <remarks>Intended to optimize repeated access to DbProviderFactories.</remarks>
internal static class DbProviderFactoryCache
{
    private static readonly ConcurrentDictionary<string, DbProviderFactory> _factoryCache = new();

    /// <summary>
    /// Retrieves the DbProviderFactory instance for the specified data provider
    /// </summary>
    /// <param name="providerInvariantName">The invariant name of the data provider</param>
    /// <returns>The DbProviderFactory instance</returns>
    public static DbProviderFactory GetFactory(string providerInvariantName)
    {
        // First check the cache to avoid unnecessary calls to DbProviderFactories.GetFactory
        // Because the DbProviderFactory instance was existed in most cases.
        if (_factoryCache.TryGetValue(providerInvariantName, out var factory))
        {
            return factory;
        }

        return _factoryCache.GetOrAdd(providerInvariantName, static name => DbProviderFactories.GetFactory(name));
    }
}
