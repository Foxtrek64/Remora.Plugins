using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Remora.Plugins.Services;

namespace Remora.Plugins.Extensions
{
    /// <summary>
    /// Contains extensions for registering plugins with the <see cref="IServiceCollection"/>
    /// </summary>
    [PublicAPI]
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPluginsFromAssembly
        (
            this IServiceCollection serviceCollection,
            bool scanEntryAssemblyDirectory = true,
            params string[] pluginSearchPaths
        )
            => AddPluginsFromAssembly(serviceCollection, new PluginServiceOptions(pluginSearchPaths, scanEntryAssemblyDirectory));

        public static IServiceCollection AddPluginsFromAssembly(this IServiceCollection serviceCollection, PluginServiceOptions options)
        {
            
        }
    }
}
