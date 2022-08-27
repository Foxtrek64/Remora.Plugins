//
//  PluginServiceProvider.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Remora.Plugins.Abstractions;

/// <summary>
/// A dummy service provider for which allows resolve services from other service providers because
/// it internally tracks them all inside of it.
/// </summary>
internal class PluginServiceProvider : IServiceProvider, IDisposable, IAsyncDisposable
{
    private readonly Dictionary<string, ServiceProvider> _serviceProviders;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginServiceProvider"/> class.
    /// </summary>
    private PluginServiceProvider()
    {
        _serviceProviders = new Dictionary<string, ServiceProvider>();
    }

    /// <summary>
    /// Gets the current instance of the <see cref="PluginServiceProvider"/>.
    /// </summary>
    public static PluginServiceProvider Default { get; } = new();

    /// <summary>
    /// Creates a new named <see cref="IServiceProvider"/> that is owned by this instance.
    /// </summary>
    /// <param name="name">
    /// The name of the service provider.
    /// </param>
    /// <param name="services">
    /// The services that plugins are requesting to have for the built <see cref="IServiceProvider"/>.
    /// </param>
    public void CreateServiceProvider(string name, IServiceCollection services)
    {
        AddServiceProvider(name, services.BuildServiceProvider());
    }

    /// <summary>
    /// Disposes and removes the <see cref="ServiceProvider"/> created using a specific name.
    /// </summary>
    /// <param name="name">
    /// The name of the service provider that needs to be disposed.
    /// </param>
    public void DisposeServiceProvider(string name)
    {
        _ = _serviceProviders.TryGetValue(name, out var provider);
        _ = _serviceProviders.Remove(name);
        provider?.Dispose();
    }

    /// <summary>
    /// Gets a service from the list of service providers.
    /// </summary>
    /// <param name="serviceType">
    /// The type of the service to look for.
    /// </param>
    /// <returns>
    /// The requested service (if found), else null.
    /// </returns>
    public object? GetService(Type serviceType)
    {
        object? result = default;
        foreach (var provider in _serviceProviders.Values.TakeWhile(_ => result == null))
        {
            result = provider.GetService(serviceType);
        }

        // if still null, then not found.
        return result;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var provider in _serviceProviders.Values)
        {
            provider.Dispose();
        }

        _serviceProviders.Clear();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        foreach (var provider in _serviceProviders.Values)
        {
            await provider.DisposeAsync();
        }

        _serviceProviders.Clear();
    }

    /// <summary>
    /// Adds a service provider with a specified name to the list of providers managed by this instance.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="serviceProvider">The service provider.</param>
    internal void AddServiceProvider(string name, IServiceProvider serviceProvider)
    {
        _serviceProviders.Add(name, (ServiceProvider)serviceProvider);
    }
}
