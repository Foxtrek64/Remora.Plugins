//
//  PluginLoader.cs
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Remora.Plugins.Abstractions.Attributes;
#if NET6_0_OR_GREATER
using System.Runtime.Loader;
#endif

namespace Remora.Plugins.Abstractions;

/// <summary>
/// Class to abstract the loading of plugins.
/// </summary>
internal class PluginLoader
{
#if NET6_0_OR_GREATER
    /// <summary>
    /// AssemblyLoadContest for loading plugins.
    /// </summary>
    internal class PluginLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginLoadContext"/> class.
        /// </summary>
        /// <param name="name">
        /// The name for the plugin load context (plugin file name without extension in this case).
        /// </param>
        /// <param name="pluginPath">The path for the plugins.</param>
        public PluginLoadContext(string name, string pluginPath)
            : base(name, true)
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        /// <inheritdoc/>
        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var isLoadedToDefaultContext = new Func<string, bool>(static name
                => Default.Assemblies.Any(assembly => assembly.FullName is not null && assembly.FullName.Equals(name, StringComparison.Ordinal)));
            var getFromDefaultContext = new Func<string, Assembly?>(static name
                => Default.Assemblies.FirstOrDefault(assembly => assembly.FullName is not null && assembly.FullName.Equals(name, StringComparison.Ordinal)));
            if (isLoadedToDefaultContext(assemblyName.FullName))
            {
                // return the assembly from the default context instead of reloading it (is same assembly and version).
                return getFromDefaultContext(assemblyName.FullName);
            }

            var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            return (assemblyPath is not null, !File.Exists($"{AppContext.BaseDirectory}{assemblyName.Name}.dll")) switch
            {
                (false, true) => null,
                (false, false) => this.LoadFromAssemblyPath($"{AppContext.BaseDirectory}{assemblyName.Name}.dll"),
                _ => this.LoadFromAssemblyPath(assemblyPath!),
            };
        }

        /// <inheritdoc/>
        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            return (libraryPath is not null, !File.Exists($"{AppContext.BaseDirectory}{unmanagedDllName}.dll")) switch
            {
                (false, true) => IntPtr.Zero,
                (false, false) => this.LoadUnmanagedDllFromPath($"{AppContext.BaseDirectory}{unmanagedDllName}.dll"),
                _ => this.LoadUnmanagedDllFromPath(libraryPath!),
            };
        }
    }

    private readonly List<PluginLoadContext> _contexts = new List<PluginLoadContext>();
#else
    private readonly List<AppDomain> _domains = new List<AppDomain>();
#endif

    /// <summary>
    /// Loads a plugin from a specified path.
    /// </summary>
    /// <param name="assemblyPath">
    /// The path to the plugin to load.
    /// </param>
    /// <returns>
    /// The assembly instance of the loaded plugin.
    /// </returns>
    public (RemoraPluginAttribute PluginAttribute, Assembly PluginAssembly) LoadPlugin(string? assemblyPath)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(assemblyPath);
        var context = new PluginLoadContext(Path.GetFileNameWithoutExtension(assemblyPath), assemblyPath);
#else
        if (assemblyPath == null)
        {
            throw new ArgumentNullException(assemblyPath);
        }

        var domain = AppDomain.CreateDomain(Path.GetFileNameWithoutExtension(assemblyPath));
#endif
        var (asmBytes, pdbBytes) = OpenAssemblyFiles(assemblyPath, assemblyPath.Replace(".dll", ".pdb", StringComparison.OrdinalIgnoreCase));
        Assembly assembly;
#if NET6_0_OR_GREATER
        using MemoryStream ms1 = new(asmBytes!);

        // optional debug symbols as a pdb file (useful for debugging plugins with a debugger).
        // normally people embed their pdb's however some people might want to instead have them separate
        // as portable pdbs. As such we must support that option as well.
        using MemoryStream? ms2 = Debugger.IsAttached && pdbBytes is not null ? new(pdbBytes) : null;
#endif
        try
        {
#if NET6_0_OR_GREATER
            assembly = context.LoadFromStream(ms1, ms2);
#else
            assembly = Debugger.IsAttached && pdbBytes is not null
                ? domain.Load(asmBytes, pdbBytes)
                : domain.Load(asmBytes);
#endif
        }
        catch
        {
            // On Error clean up the AppDomain/AssemblyLoadContext and then return.
#if NET6_0_OR_GREATER
            context.Unload();
#else
            AppDomain.Unload(domain);
#endif
            return (null!, null!);
        }

        var pluginAttribute = assembly.GetCustomAttribute<RemoraPluginAttribute>();
        if (pluginAttribute is null)
        {
#if NET6_0_OR_GREATER
            // Unload ALC as the main assembly is not a plugin.
            context.Unload();
#else
            // Unload AppDomain as the main assembly is not a plugin.
            AppDomain.Unload(domain);
#endif
        }
        else
        {
#if NET6_0_OR_GREATER
            _contexts.Add(context);
#else
            _domains.Add(domain);
#endif
        }
        return (pluginAttribute!, assembly);
    }

    /// <summary>
    /// Unloads a plugin using the plugin's file name (without extension).
    /// </summary>
    /// <param name="pluginName">
    /// The file name of the plugin (without extension).
    /// </param>
    public void UnloadPlugin(string pluginName)
    {
#if NET6_0_OR_GREATER
        PluginLoadContext? unloaded = null;
        foreach (var context in _contexts)
        {
            if (context.Name?.Equals(pluginName) == true)
            {
                unloaded = context;
                context.Unload();
                break;
            }
        }

        _contexts.Remove(unloaded!);
#else
        AppDomain? unloaded = null;
        foreach (var domain in _domains)
        {
            if (domain.FriendlyName.Equals(pluginName))
            {
                unloaded = domain;
                AppDomain.Unload(domain);
                break;
            }
        }

        _domains.Remove(unloaded!);
#endif
    }

    // This avoids locking the file (the old code used to lock the files).
    // With this new code, it is now possible to make Remora.Plugins auto reload plugins
    // when it's file changes (using an FileSystemWatcher).
    private static (byte[]? AsmBytes, byte[]? PdbBytes) OpenAssemblyFiles(string dllFile, string pdbFile)
    {
        if (File.Exists(dllFile))
        {
            var asmBytes = File.ReadAllBytes(dllFile);

            // We need to handle the case where the pdb does not exist and where the
            // symbols might actually be embedded inside the dll instead or simply does
            // not exist yet.
            var pdbBytes = Debugger.IsAttached && File.Exists(pdbFile)
                ? File.ReadAllBytes(pdbFile) : null;
            return (asmBytes, pdbBytes);
        }

        return (null, null);
    }
}
