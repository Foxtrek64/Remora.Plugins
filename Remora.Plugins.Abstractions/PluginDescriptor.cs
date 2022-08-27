//
//  PluginDescriptor.cs
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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Remora.Plugins.Abstractions;

/// <summary>
/// Acts as a base class for plugin descriptors.
/// </summary>
[PublicAPI]
public abstract class PluginDescriptor : IPluginDescriptor
{
    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract string Description { get; }

    /// <inheritdoc />
    public abstract IServiceCollection Services { get; }

    /// <inheritdoc />
    public virtual Version Version => Assembly.GetAssembly(GetType())?.GetName().Version ?? new Version(1, 0, 0);

    /// <inheritdoc/>
    public abstract Task<StartResult> StartAsync(CancellationToken ct = default);

    /// <inheritdoc/>
    public abstract Task StopAsync(CancellationToken ct = default);

    /// <inheritdoc/>
    public sealed override string ToString()
    {
        return Name;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        DisposePlugin(disposing: true);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        await DisposePluginAsync().ConfigureAwait(false);
        DisposePlugin(disposing: false);
    }

    /// <summary>
    /// Called when the plugin is being disposed.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> if the method was called from the <see cref="Dispose()"/> method; otherwise, <see langword="false"/>.
    /// </param>
    protected abstract void DisposePlugin(bool disposing);

    /// <summary>
    /// Called when the plugin is being disposed asynchronously.
    /// </summary>
    /// <returns>A ValueTask indicating the result of the operation.</returns>
    protected virtual ValueTask DisposePluginAsync()
    {
        return default;
    }
}
