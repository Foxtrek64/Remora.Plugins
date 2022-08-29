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

using LuzFaltex.Extensions.DependencyInjection;

namespace Remora.Plugins.Abstractions;

/// <summary>
/// A special <see cref="MutableServiceProvider"/> that is for plugins.
/// </summary>
internal class PluginServiceProvider : MutableServiceProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginServiceProvider"/> class.
    /// </summary>
    private PluginServiceProvider()
    {
    }

    /// <summary>
    /// Gets the current instance of the <see cref="PluginServiceProvider"/>.
    /// </summary>
    public static PluginServiceProvider Default { get; } = new();
}
