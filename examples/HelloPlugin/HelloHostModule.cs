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

        IPluginLocalizedSettingsPageCapability pages = context.Capabilities.Require<IPluginLocalizedSettingsPageCapability>();
        IPluginRegistration registration = pages.Register(new PluginLocalizedSettingsPageDescriptor(
            "example.hello.settings",
            new PclLocalizedString("settings.title", "你好插件"),
            "lucide/puzzle",
            new PclLocalizedString("settings.heading", "来自 IPclNPlugin 的问候"),
            new PclLocalizedString("settings.description", "此页面由示例插件注册。"),
            [new PluginLocalizedSettingsHintDescriptor(
                new PclLocalizedString("settings.experimental", "SDK 仍处于实验阶段。"),
                PluginSettingsHintKind.Warning)]));
        context.Lifetime.Track(registration);
        return ValueTask.CompletedTask;
    }

    public ValueTask ShutdownAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
