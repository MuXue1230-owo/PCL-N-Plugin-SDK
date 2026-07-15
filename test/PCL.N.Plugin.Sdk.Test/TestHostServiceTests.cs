using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.N.Plugin.Testing;

namespace PCL.N.Plugin.Sdk.Test;

[TestClass]
public sealed class TestHostServiceTests
{
    [TestMethod]
    public async Task Context_ProvidesStableServices_AndReleasesRegistrations()
    {
        TestPluginContext context = new(
            new PluginDescriptor(new PluginId("dev.muxue.test"), "Test", PluginVersion.Parse("1.0.0")),
            new PluginApiVersion(0, 2));
        int invoked = 0;
        context.Commands.Register(new PluginCommandDescriptor(
            "dev.muxue.test.run",
            "Run",
            _ =>
            {
                invoked++;
                return Task.CompletedTask;
            }));
        await context.Commands.InvokeAsync("dev.muxue.test.run");
        await context.Settings.SetAsync(new PluginSettingKey<int>("count"), 42);
        await context.SecureStorage.WriteAsync(new PluginSecretKey("token"), "secret"u8.ToArray());
        await context.UriLauncher.OpenAsync(new Uri("https://pcl.example/plugin"));
        context.Notifications.ShowInformation("ready");
        context.UiPatches.Register(new PluginUiPatchDescriptor(
            "inject",
            "pcl.page.launch",
            PluginUiPatchKind.Inject,
            slot: "primary-actions.after"));

        Assert.AreEqual(1, invoked);
        Assert.AreEqual(42, await context.Settings.GetAsync(new PluginSettingKey<int>("count"), 0));
        CollectionAssert.AreEqual("secret"u8.ToArray(), (await context.SecureStorage.ReadAsync(new PluginSecretKey("token"))).Value);
        Assert.AreEqual("https://pcl.example/plugin", context.UriLauncher.OpenedUris.Single().AbsoluteUri);
        Assert.AreEqual(1, context.Notifications.Messages.Count);
        Assert.AreEqual(1, context.UiPatches.ListPatches().Count);
        Assert.IsTrue(context.Services.Supports(PluginServiceIds.Exports, PluginApiVersionRange.Parse(">=0.1 <1.0")));

        await context.DisposeAsync();
        Assert.AreEqual(0, context.Commands.Commands.Count);
        Assert.AreEqual(0, context.UiPatches.ListPatches().Count);
    }
}
