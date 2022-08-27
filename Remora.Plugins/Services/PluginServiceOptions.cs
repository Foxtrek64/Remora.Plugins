//
//  PluginServiceOptions.cs
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
using System.IO;
using Remora.Results;

namespace Remora.Plugins.Services;

/// <summary>
/// Represents various options made available to the plugin service.
/// </summary>
/// <param name="PluginSearchPaths">Additional plugin search paths to consider.</param>
/// <param name="ScanAssemblyDirectory">
/// Whether the directory of the entry assembly should be scanned for plugins.
/// </param>
/// <param name="Filter">The filter to use with the plugin service's <see cref="FileSystemWatcher"/>.</param>
/// <param name="ErrorDelegate">The delegate used to process plugin initialization or migration errors.</param>
public record PluginServiceOptions
(
    IEnumerable<string> PluginSearchPaths,
    bool ScanAssemblyDirectory = true,
    string Filter = "*.dll",
    Action<Result>? ErrorDelegate = null
)
{
    /// <summary>
    /// Gets a default instance of the <see cref="PluginServiceOptions"/> type.
    /// </summary>
    public static readonly PluginServiceOptions Default = new(Array.Empty<string>());
}
