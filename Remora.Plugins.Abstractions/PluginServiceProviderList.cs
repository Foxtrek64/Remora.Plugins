//
//  PluginServiceProviderList.cs
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
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Remora.Plugins.Abstractions;

/// <summary>
/// Represents the Service Providers tracked by the plugin service.
/// </summary>
[PublicAPI]
public class PluginServiceProviderList
{
    private readonly Dictionary<string, ServiceProvider> _serviceProviders;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginServiceProviderList"/> class.
    /// </summary>
    /// <param name="applicationProvider">
    /// The application's service provider.
    /// This is for fallback when all of the plugin providers do not contain a particular service.
    /// </param>
    public PluginServiceProviderList(IServiceProvider applicationProvider)
    {
        _serviceProviders = new Dictionary<string, ServiceProvider>
        {
            { "Default", (ServiceProvider)applicationProvider }
        };
        Current = this;
    }

    /// <summary>
    /// Gets the current instance of the <see cref="PluginServiceProviderList"/>
    /// or <see langword="null"/> if not created yet by the plugin service.
    /// </summary>
    public static PluginServiceProviderList? Current { get; private set; }

    /// <summary>
    /// Creates a new <see cref="IServiceProvider"/> that is owned by this instance for plugins to use.
    /// </summary>
    /// <param name="pluginName">
    /// The name of the plugin for which the services are from.
    /// </param>
    /// <param name="services">
    /// The services that plugins are requesting to have for the built <see cref="IServiceProvider"/>.
    /// </param>
    internal void CreateProvider(string pluginName, IServiceCollection services)
    {
        _serviceProviders.Add(pluginName, services.BuildServiceProvider());
    }

    /// <summary>
    /// Disposes and removes the <see cref="ServiceProvider"/> created from a specific plugin.
    /// </summary>
    /// <param name="pluginName">
    /// The name of the plugin for which the <see cref="ServiceProvider"/> needs to be disposed on.
    /// </param>
    internal void DisposeProvider(string pluginName)
    {
        _ = _serviceProviders.TryGetValue(pluginName, out var provider);
        _ = _serviceProviders.Remove(pluginName);
        provider?.Dispose();
    }

    /// <summary>
    /// Gets a service from the list of service providers.
    /// </summary>
    /// <typeparam name="TService">
    /// The type of the service to look for.
    /// </typeparam>
    /// <returns>
    /// The requested service (if found), else null.
    /// </returns>
    public TService? GetService<TService>()
    {
        TService? result = default;
        foreach (var provider in _serviceProviders.Values)
        {
            if (result != null)
            {
                break;
            }

            result = provider.GetService<TService>();
        }

        // if still null, then not found.
        return result;
    }
}
