Remora.Plugins
==============

Remora.Plugins is a simple plugin system for .NET, providing a dynamic pluggable
framework for your applications. In short, class libraries can be written as 
standalone packages and loaded at runtime as integrated parts of your 
application, allowing loose coupling and easily swappable components. The plugin 
system is designed around Microsoft's dependency injection framework.

## Usage
Creating a plugin is as simple as creating a class library project, and 
annotating the assembly with an attribute denoting the type used as a plugin 
descriptor. The descriptor acts as the entry point of the plugin, as well as an
encapsulation point for information about it.

Generally, plugins should only reference Remora.Plugins.Abstractions, while the
main application should reference and use Remora.Plugins.

```c#
[assembly: RemoraPlugin(typeof(MyPlugin))]

public sealed class MyPlugin : PluginDescriptor
{
    /// <inheritdoc />
    public override string Name => "My Plugin";

    /// <inheritdoc />
    public override string Description => "My plugin that does a thing.";

    /// <inheritdoc/>
    public override IServiceCollection Services
        => new ServiceCollection()
            .AddScoped<MyService>();

    /// <inheritdoc />
    public override async Task<StartResult> StartAsync(CancellationToken ct = default)
    {
        var myService = serviceProvider.GetRequiredService<MyService>();
        var doThing = await myService.DoTheThingAsync();
        if (!doThing.IsSuccess)
        {
            return doThing;
        }

        // may optionally return an delegate for optional
        // migrations that can be done.
        return Result.FromSuccess();
    }

    /// <inheritdoc/>
    public override Task StopAsync(CancellationToken ct = default)
    {
        // Can optionally be used to stop some service here that a plugin may start.
        return Task.CompletedTask;
    }

    // optionally dispose resources that might need to be disposed
    // in the plugin.
    /// <inheritdoc/>
    public override void DisposePlugin(bool disposing)
    {
        // For this example do nothing.
    }
}
```

Loading plugins in your application is equally simple. The example below is
perhaps a little convoluted, but shows the flexibility of the system.

```c#
Action<Result> errorDelegate = (result) =>
{
    Console.WriteLine($"Error in plugin Initialization or Migration: {result.Error.Message}");
});
var serviceCollection = new ServiceCollection()
    .AddSingleton<PluginService>(
        serviceProvider => new PluginService(
            new PluginServiceOptions(
                Array.Empty<string>(),

                // Filter the FileSystemWatcher to only watch for plugins with
                // the file name pattern of *.Plugin.dll.
                Filter = "*.Plugin.dll",
                ErrorDelegate = errorDelegate);

var _services = serviceCollection.BuildServiceProvider();
var pluginService = _services.GetRequiredService<PluginService>();
pluginService.LoadPlugins();
```

Plugins should be designed in such a way that a registration or initialization 
failure does not corrupt the application.

## Breaking Changes

The following is the current breaking changes in Remora.Plugins (or possible breaking changes):

- The result errors: AssemblyIsNotPluginError, InvalidPluginError, and PluginConfigurationFailed was removed.
- IPluginDescriptor now implements IDisposable and IAsyncDisposable (are disposables).
- IMigratablePlugin was removed, For plugins that used to implement that interface use PluginDescriptor instead.
- "Result ConfigureServices(IServiceCollection)" is now a get only property named Services that returns IServiceCollection (plugins must new a service collection).
- "InitializeAsync(IServiceProvider, CancellationToken)" is now "StartAsync(CancellationToken)" and returns a StartResult which holds a Result and a migration delegate (readonly struct like Result is).
- "Task StopAsync(CancellationToken)" added (is run only when plugins will unload).
- PluginService:
   - Is now Disposable (to dispose of the internal FileSystemWatcher).
   - Can be constructed using DependencyInjection now.
   - LoadPluginTree() was removed.
   - Internal FileSystemWatcher added which checks for file system changes to unload/reload plugins for you.
- PluginTree, and PluginTreeNode was removed.
- PluginServiceOptions:
   - Added Optional delegate to the record for when plugin initialization errors or migration errors occur (that way applications using Remora.Plugins can log the errors if they choose to).
   - Added "Filter" to the record to provide a filter into PluginService's FileSystemWatcher.
- Possibly breaking: When another plugin references a plugin, it may lock it's file which is not desirable.
- To resolve types added by plugins using DependencyInjection that were registered into their own separate service providers, the application must replace the DefaultServiceProviderFactory with MutableServiceProviderFactory otherwise it will not resolve them and then error when trying to construct services.

## Building
The library does not require anything out of the ordinary to compile.

```bash
cd $SOLUTION_DIR
dotnet build
dotnet pack -c Release
```

## Downloading
Get it on [NuGet][1].


[1]: https://www.nuget.org/packages/Remora.Plugins/
