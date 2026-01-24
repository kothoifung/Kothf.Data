// Licensed to Kothf under one or more agreements.
// Kothf licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Kothf.Data;

/// <summary>
/// The settings required to establish a database connection
/// </summary>
/// <param name="ConnectionString">The connection string</param>
/// <param name="ProviderInvariantName">The invariant name of the data provider</param>
public record ConnectionStringSettings(string ConnectionString, string ProviderInvariantName);
