using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.N.Plugin.Testing;
using System.Reflection;
using System.Text.Json;

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
        context.Process.EnqueueResult(new PluginProcessResult(7, "out", "err"));
        PluginProcessResult process = await context.Process.RunAsync(new PluginProcessRequest
        {
            FileName = "tool",
            Arguments = ["--version"],
            CaptureOutput = true
        });
        await context.Clipboard.WriteTextAsync("clip");
        await context.Files.WriteAsync("state/data.bin", "abc"u8.ToArray());
        context.Accounts.AddProvider(new PluginAccountProviderInfo("offline", "Offline", null));
        context.Downloads.AddSource(new PluginDownloadSourceInfo("official", "Official", new Uri("https://example.invalid/"), "Metadata"));
        context.LaunchModifications.Register(new PluginLaunchModification(
            "demo",
            request => request with { GameArguments = request.GameArguments.Concat(["--demo"]).ToArray() }));
        using JsonDocument registryJson = JsonDocument.Parse("""{"enabled":true}""");
        context.Registry.Register(new PluginRegistryNodeDescriptor(
            "plugins.dev.muxue.test.feature",
            registryJson.RootElement.Clone()));
        context.RuntimePatches.Register(new PluginRuntimePatchDescriptor
        {
            PatchId = "sample-postfix",
            Target = new PluginRuntimePatchTarget("PCL.Application", "PCL.Sample", "Run"),
            Postfix = typeof(TestHostServiceTests).GetMethod(
                nameof(SamplePostfix),
                BindingFlags.Static | BindingFlags.NonPublic)
        });

        Assert.AreEqual(1, invoked);
        Assert.AreEqual(42, await context.Settings.GetAsync(new PluginSettingKey<int>("count"), 0));
        CollectionAssert.AreEqual("secret"u8.ToArray(), (await context.SecureStorage.ReadAsync(new PluginSecretKey("token"))).Value);
        Assert.AreEqual("https://pcl.example/plugin", context.UriLauncher.OpenedUris.Single().AbsoluteUri);
        Assert.AreEqual(1, context.Notifications.Messages.Count);
        Assert.AreEqual(1, context.UiPatches.ListPatches().Count);
        Assert.AreEqual(7, process.ExitCode);
        Assert.AreEqual("--version", context.Process.Requests.Single().Arguments.Single());
        Assert.AreEqual("clip", await context.Clipboard.ReadTextAsync());
        CollectionAssert.AreEqual("abc"u8.ToArray(), await context.Files.ReadAsync("state/data.bin"));
        Assert.AreEqual("state/data.bin", context.Files.List("state").Single());
        Assert.AreEqual("offline", context.Accounts.ListProviders().Single().Id);
        Assert.AreEqual("official", context.Downloads.ListSources().Single().Id);
        PluginLaunchRequest launch = context.LaunchModifications.ApplyAll(new PluginLaunchRequest("i", [], [], new Dictionary<string, string>()));
        Assert.AreEqual("--demo", launch.GameArguments.Single());
        Assert.IsNotNull(context.Registry.GetNode("plugins.dev.muxue.test.feature"));
        Assert.AreEqual(1, context.RuntimePatches.ListOwned().Count);
        Assert.IsTrue(context.Services.Supports(PluginServiceIds.Exports, PluginApiVersionRange.Parse(">=0.1 <1.0")));

        await context.DisposeAsync();
        Assert.AreEqual(0, context.Commands.Commands.Count);
        Assert.AreEqual(0, context.UiPatches.ListPatches().Count);
        Assert.AreEqual(0, context.LaunchModifications.Modifications.Count);
        Assert.IsNull(context.Registry.GetNode("plugins.dev.muxue.test.feature"));
        Assert.AreEqual(0, context.RuntimePatches.ListOwned().Count);
    }

    private static void SamplePostfix() { }
}
