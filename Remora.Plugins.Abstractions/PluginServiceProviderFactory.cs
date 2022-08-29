//
//  PluginServiceProviderFactory.cs
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
using JetBrains.Annotations;
using LuzFaltex.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Remora.Plugins.Abstractions;

/// <summary>
/// A service provider factory for plugins. Use in place of the default service provider factory.
/// </summary>
[PublicAPI]
public class PluginServiceProviderFactory : MutableServiceProviderFactory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginServiceProviderFactory"/> class
    /// with default options.
    /// </summary>
    public PluginServiceProviderFactory()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginServiceProviderFactory"/> class
    /// with the specified <paramref name="options"/>.
    /// </summary>
    /// <param name="options">The options to use for this instance.</param>
    public PluginServiceProviderFactory(ServiceProviderOptions options)
        : base(options)
    {
    }

    /// <inheritdoc cref="MutableServiceProviderFactory.CreateServiceProvider" />
    public new IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder)
    {
        PluginServiceProvider.Default.AddServices("Default", containerBuilder);

        // Returns the default plugin service provider.
        return PluginServiceProvider.Default;
    }
}
