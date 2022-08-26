//
//  IPluginDescriptor.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Remora.Results;

namespace Remora.Plugins.Abstractions;

/// <summary>
/// Represents the public API for a plugin.
/// </summary>
[PublicAPI]
public interface IPluginDescriptor
{
    /// <summary>
    /// Gets the name of the plugin. This name should be unique.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of the plugin.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the version of the plugin.
    /// </summary>
    Version Version { get; }

    /// <summary>
    /// Configures services provided by the plugin in the application's service collection.
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    /// <returns>A result that may or may not have succeeded.</returns>
    Result ConfigureServices(IServiceCollection serviceCollection);

    /// <summary>
    /// Called when the application host is ready to start the plugin.
    /// </summary>
    /// <param name="ct">Indicates that the start process has been aborted.</param>
    /// <returns>A result that may or may not have succeeded.</returns>
    Task<Result> StartAsync(CancellationToken ct = default);

    /// <summary>
    /// Called when the application host is ready to stop the plugin.
    /// </summary>
    /// <param name="ct">Indicates tha the shutdown process should no longer be graceful.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopAsync(CancellationToken ct = default);
}
