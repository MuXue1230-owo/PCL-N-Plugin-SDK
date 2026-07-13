using PCL.N.Plugin;
using PCL.N.Plugin.Sdk;

namespace HelloPlugin;

public sealed class HelloPlugin : IPclNPlugin
{
    public ValueTask InitializeAsync(IPluginContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IPluginCommandService commands = context.Services.Require<IPluginCommandService>();
        context.Lifetime.Track(commands.Register(new PluginCommandDescriptor(
            "dev.muxue.hello.say-hello",
            "Say hello",
            _ =>
            {
                context.Logger.Info("Hello from the sample plugin.");
                return Task.CompletedTask;
            })));

        IPluginSettingsPageCapability pages = context.Capabilities.Require<IPluginSettingsPageCapability>();
        IPluginRegistration registration = pages.Register(new PluginSettingsPageDescriptor(
            "example.hello.settings",
            "Hello Plugin",
            "lucide/puzzle",
            "Hello from IPclNPlugin",
            "This page was registered by the example plugin.",
            [new PluginSettingsHintDescriptor("The SDK is experimental.", PluginSettingsHintKind.Warning)]));
        context.Lifetime.Track(registration);
        return ValueTask.CompletedTask;
    }

    public ValueTask ShutdownAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
