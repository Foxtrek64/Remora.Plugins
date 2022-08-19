//
//  PluginDescriptor.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
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
using Remora.Results;

namespace Remora.Plugins.Abstractions;

/// <summary>
/// Acts as a base class for plugin descriptors.
/// </summary>
[PublicAPI]
public abstract class PluginDescriptor : IPluginDescriptor
{
    private bool _isDisposed = false;

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract string Description { get; }

    /// <inheritdoc />
    public virtual Version Version => Assembly.GetAssembly(GetType())?.GetName().Version ?? new Version(1, 0, 0);

    /// <inheritdoc />
    public virtual ValueTask<Result> StartupAsync(CancellationToken ct = default)
    {
        return new(Result.FromSuccess());
    }

    /// <inheritdoc />
    public virtual ValueTask<Result> ShutdownAsync(bool shutdown = false, CancellationToken ct = default)
    {
        return new(Result.FromSuccess());
    }

    /// <inheritdoc/>
    public sealed override string ToString()
    {
        return this.Name;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (this._isDisposed)
        {
            return;
        }

        Dispose(disposing: true);
        this._isDisposed = true;
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (this._isDisposed)
        {
            return;
        }

        // Perform async cleanup.
        await DisposeAsyncCore();

        // Dispose of unmanaged resources.
        Dispose(disposing: false);

        this._isDisposed = true;

        // Suppress finalization
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc cref="Dispose()"/>
    /// <param name="disposing">A value indicating whether to dispose managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
    }

    /// <inheritdoc cref="DisposeAsync()"/>
    protected virtual ValueTask DisposeAsyncCore()
    {
        return default;
    }
}
