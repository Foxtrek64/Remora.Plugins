//
//  PluginLoadContext.cs
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
    private PluginLoadContext(string name, string pluginPath)
        : base(name, true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    /// <inheritdoc/>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var isLoadedToDefaultContext = new Func<string, bool>(static name
            => Default.Assemblies.Any(assembly =>
                assembly.FullName is not null && assembly.FullName.Equals(name, StringComparison.Ordinal)));
        var getFromDefaultContext = new Func<string, Assembly?>(static name
            => Default.Assemblies.FirstOrDefault(assembly =>
                assembly.FullName is not null && assembly.FullName.Equals(name, StringComparison.Ordinal)));
        if (isLoadedToDefaultContext(assemblyName.FullName))
        {
            // return the assembly from the default context instead of reloading it (is same assembly and version).
            return getFromDefaultContext(assemblyName.FullName);
        }

        var libraryPath = _resolver.ResolveAssemblyToPath(assemblyName);
        var assemblyPath = $"{AppContext.BaseDirectory}{assemblyName.Name}.dll";
        var (asmBytes, pdbBytes) = OpenAssemblyFiles(
            assemblyPath,
            assemblyPath.Replace(
                ".dll",
                ".pdb",
                StringComparison.OrdinalIgnoreCase));
        using MemoryStream ms1 = asmBytes is not null ? new(asmBytes) : null!;

        // optional debug symbols as a pdb file (useful for debugging plugins with a debugger).
        // normally people embed their pdb's however some people might want to instead have them separate
        // as portable pdbs. As such we must support that option as well.
        using MemoryStream ms2 = Debugger.IsAttached && pdbBytes is not null ? new(pdbBytes) : null!;
        return (libraryPath is not null, !File.Exists(assemblyPath)) switch
        {
            (false, true) => null,
            (false, false) => LoadFromStream(ms1, ms2),

            // Preferably to only lock assemblies that do not exist under the Application's directory.
            _ => LoadFromAssemblyPath(libraryPath!),
        };
    }

    /// <inheritdoc/>
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return (libraryPath is not null, !File.Exists($"{AppContext.BaseDirectory}{unmanagedDllName}.dll")) switch
        {
            (false, true) => IntPtr.Zero,
            (false, false) => LoadUnmanagedDllFromPath($"{AppContext.BaseDirectory}{unmanagedDllName}.dll"),
            _ => LoadUnmanagedDllFromPath(libraryPath!),
        };
    }

    /// <summary>
    /// Loads a plugin from a specified path.
    /// </summary>
    /// <param name="assemblyPath">
    /// The path to the plugin to load.
    /// </param>
    /// <param name="contexts">The list of contexts.</param>
    /// <returns>
    /// The assembly instance of the loaded plugin.
    /// </returns>
    internal static RemoraPluginAttribute LoadPlugin(string? assemblyPath, ref List<PluginLoadContext> contexts)
    {
        ArgumentNullException.ThrowIfNull(assemblyPath);
        var context = new PluginLoadContext(Path.GetFileNameWithoutExtension(assemblyPath), assemblyPath);
        var (asmBytes, pdbBytes) = OpenAssemblyFiles(
            assemblyPath,
            assemblyPath.Replace(
                ".dll",
                ".pdb",
                StringComparison.OrdinalIgnoreCase));
        Assembly assembly;
        using MemoryStream ms1 = new(asmBytes!);

        // optional debug symbols as a pdb file (useful for debugging plugins with a debugger).
        // normally people embed their pdb's however some people might want to instead have them separate
        // as portable pdbs. As such we must support that option as well.
        using MemoryStream ms2 = Debugger.IsAttached && pdbBytes is not null ? new(pdbBytes) : null!;
        try
        {
            assembly = context.LoadFromStream(ms1, ms2);
        }
        catch
        {
            // On error clean up the AssemblyLoadContext and then return.
            context.Unload();
            return null!;
        }

        var pluginAttribute = assembly.GetCustomAttribute<RemoraPluginAttribute>();
        if (pluginAttribute is null)
        {
            // Unload ALC as the main assembly is not a plugin.
            context.Unload();
            return null!;
        }

        contexts.Add(context);
        return pluginAttribute;
    }

    /// <summary>
    /// Unloads a plugin using the plugin's file name (without extension).
    /// </summary>
    /// <param name="pluginName">
    /// The file name of the plugin (without extension).
    /// </param>
    /// <param name="contexts">The list of contexts.</param>
    internal static void UnloadPlugin(string pluginName, ref List<PluginLoadContext> contexts)
    {
        PluginLoadContext? unloaded = null;
        foreach (var context in contexts.Where(context => context.Name?.Equals(pluginName) == true))
        {
            unloaded = context;
            context.Unload();
            break;
        }

        contexts.Remove(unloaded!);
    }
#else
/// <summary>
/// Contains helpers to load plugins.
/// </summary>
internal static class PluginLoadContext
{
    /// <summary>
    /// Loads a plugin from a specified path.
    /// </summary>
    /// <param name="assemblyPath">
    /// The path to the plugin to load.
    /// </param>
    /// <param name="domains">The list of domains.</param>
    /// <returns>
    /// The assembly instance of the loaded plugin.
    /// </returns>
    internal static RemoraPluginAttribute LoadPlugin(string? assemblyPath, ref List<AppDomain> domains)
    {
        if (assemblyPath == null)
        {
            throw new ArgumentNullException(assemblyPath);
        }

        var domain = AppDomain.CreateDomain(Path.GetFileNameWithoutExtension(assemblyPath));
        var (asmBytes, pdbBytes) = PluginLoadContext.OpenAssemblyFiles(
            assemblyPath,
            assemblyPath.Replace(
                ".dll",
                ".pdb",
                StringComparison.OrdinalIgnoreCase));
        Assembly assembly;
        try
        {
            assembly = Debugger.IsAttached && pdbBytes is not null
                ? domain.Load(asmBytes, pdbBytes)
                : domain.Load(asmBytes);
        }
        catch
        {
            // On Error clean up the AppDomain and then return.
            AppDomain.Unload(domain);
            return null!;
        }

        var pluginAttribute = assembly.GetCustomAttribute<RemoraPluginAttribute>();
        if (pluginAttribute is null)
        {
            // Unload AppDomain as the main assembly is not a plugin.
            AppDomain.Unload(domain);
            return null!;
        }

        domains.Add(domain);
        return pluginAttribute;
    }

    /// <summary>
    /// Unloads a plugin using the plugin's file name (without extension).
    /// </summary>
    /// <param name="pluginName">
    /// The file name of the plugin (without extension).
    /// </param>
    /// <param name="domains">The list of domains.</param>
    internal static void UnloadPlugin(string pluginName, ref List<AppDomain> domains)
    {
        AppDomain? unloaded = null;
        foreach (var domain in domains.Where(domain => domain.FriendlyName.Equals(pluginName)))
        {
            unloaded = domain;
            AppDomain.Unload(domain);
            break;
        }

        domains.Remove(unloaded!);
    }
#endif

    /// <summary>
    /// Opens the assembly files for loading without locking them.
    /// </summary>
    /// <param name="dllFile">The dll file to load.</param>
    /// <param name="pdbFile">The pdb file to the dll to load.</param>
    /// <returns>A tuple with the dll and pdb file contents.</returns>
    private static (byte[]? AsmBytes, byte[]? PdbBytes) OpenAssemblyFiles(string dllFile, string pdbFile)
    {
        var asmBytes = File.Exists(dllFile) ? File.ReadAllBytes(dllFile) : null;

        // We need to handle the case where the pdb does not exist and where the
        // symbols might actually be embedded inside the dll instead or simply does
        // not exist yet.
        var pdbBytes = Debugger.IsAttached && File.Exists(pdbFile)
            ? File.ReadAllBytes(pdbFile) : null;
        return (asmBytes, pdbBytes);
    }
}
