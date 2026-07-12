using PCL.Plugin.Sdk;

namespace HelloPlugin;

public sealed class HelloHostModule : IPclHostModule
{
    public HostModuleId Id => new("example.hello");

    public void Configure(IPclHostBuilder builder)
    {
        IHostSettingsPageRegistry pages = builder.RequireCapability<IHostSettingsPageRegistry>();
        pages.Add(new HostSettingsPage(
            "example.hello.settings",
            "Hello Plugin",
            "lucide/puzzle",
            "Hello from a HostModule",
            "This page was registered by the example plugin.",
            [new HostSettingsHint("The SDK is experimental.", HostSettingsHintKind.Warning)]));
    }
}
