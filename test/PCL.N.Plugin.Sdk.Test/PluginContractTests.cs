using HelloPlugin;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.N.Plugin;
using PCL.N.Plugin.Sdk;
using PCL.N.Plugin.Testing;

namespace PCL.N.Plugin.Sdk.Test;

[TestClass]
public sealed class PluginContractTests
{
    [TestMethod]
    public async Task ExamplePlugin_RegistersAndReleasesSettingsPage()
    {
        InMemoryPluginSettingsPageCapability pages = new();
        await using TestPluginContext context = CreateContext();
        context.TestCapabilities.Add<IPluginLocalizedSettingsPageCapability>(pages);

        await new HelloPlugin.HelloPlugin().InitializeAsync(context, CancellationToken.None);

        Assert.AreEqual("example.hello.settings", pages.Pages.Single().Id);
        Assert.AreEqual(PluginSettingsHintKind.Warning, pages.Pages.Single().Hints.Single().Kind);
        TestPluginCommandService commands = (TestPluginCommandService)context.Services.Require<IPluginCommandService>();
        Assert.AreEqual("dev.muxue.hello.say-hello", commands.Commands.Single().Id);
        await commands.InvokeAsync("dev.muxue.hello.say-hello");
        Assert.IsTrue(context.Logger.Entries.Any(static entry => entry.Contains("Hello from", StringComparison.Ordinal)));
        await context.DisposeAsync();
        Assert.AreEqual(0, pages.Pages.Count);
        Assert.AreEqual(0, commands.Commands.Count);
    }

    [TestMethod]
    public async Task TestUiServices_TrackContributionsAndDetectExclusiveReplace()
    {
        TestPluginUiSurfaceCapability contributions = new();
        await contributions.Contribute(new PluginUiSlotContributionDescriptor(
            "pcl.page.launch", "primary-actions.after", "sample.panel")).DisposeAsync();
        Assert.AreEqual(0, contributions.Contributions.Count);

        TestPluginUiPatchService patches = new();
        patches.Register(new PluginUiPatchDescriptor("replace-a", "pcl.page.launch", PluginUiPatchKind.Replace));
        patches.Register(new PluginUiPatchDescriptor("replace-b", "pcl.page.launch", PluginUiPatchKind.Replace));
        PluginUiPatchPlan plan = patches.Plan();
        Assert.IsTrue(plan.HasBlockingConflicts);
        Assert.AreEqual(PluginUiConflictKind.ReplaceExclusive, plan.Conflicts.Single().Kind);
    }

    [TestMethod]
    public void RequireCapability_ReportsUnsupportedHostFeature()
    {
        TestPluginCapabilityProvider capabilities = new();

        Assert.ThrowsExactly<NotSupportedException>(
            () => capabilities.Require<IPluginLocalizedSettingsPageCapability>());
    }

    [TestMethod]
    public void SettingsCapability_RejectsDuplicateIdsCaseInsensitively()
    {
        InMemoryPluginSettingsPageCapability pages = new();
        pages.Register(CreatePage("example.page"));

        Assert.ThrowsExactly<InvalidOperationException>(() => pages.Register(CreatePage("EXAMPLE.PAGE")));
    }

    [TestMethod]
    public void HostModuleContract_IsNotPartOfPublicPluginAbi()
    {
        Type[] publicTypes = typeof(IPclNPlugin).Assembly.GetExportedTypes();

        Assert.IsFalse(publicTypes.Any(type => type.Name is "IPclHostModule" or "IPclHostBuilder"));
    }

    [TestMethod]
    public void PluginContext_ExposesServicesLoggerDispatcherAndDirectories()
    {
        using TemporaryPluginRoot temporary = new();
        TestPluginContext context = new(
            new PluginDescriptor(new PluginId("example.hello"), "Hello Plugin", PluginVersion.Parse("0.1.0")),
            new PluginApiVersion(0, 1),
            PluginDirectorySet.CreateUnder(temporary.Path));

        Assert.IsNotNull(context.Services);
        Assert.IsNotNull(context.Logger);
        Assert.IsNotNull(context.Dispatcher);
        Assert.IsTrue(Directory.Exists(context.Directories.Data));
        context.Logger.Info("hello");
        Assert.AreEqual(1, context.Logger.Entries.Count);
        context.Dispatcher.Post(() => context.Logger.Info("dispatched"));
        Assert.AreEqual(2, context.Logger.Entries.Count);
    }

    [TestMethod]
    public void StableServiceIds_AndVersionRanges_ArePublicContracts()
    {
        Assert.AreEqual("pcl.logging", PluginServiceIds.Logging.Value);
        Assert.AreEqual("pcl.dispatcher", PluginServiceIds.Dispatcher.Value);
        Assert.AreEqual("pcl.notifications", PluginServiceIds.Notifications.Value);
        Assert.AreEqual("pcl.settings", PluginServiceIds.Settings.Value);
        Assert.AreEqual("pcl.commands", PluginServiceIds.Commands.Value);
        Assert.AreEqual("pcl.tasks", PluginServiceIds.Tasks.Value);
        Assert.AreEqual("pcl.instances.read", PluginServiceIds.InstancesRead.Value);
        Assert.AreEqual("pcl.ui", PluginServiceIds.Ui.Value);
        Assert.AreEqual("pcl.ui.patch", PluginServiceIds.UiPatch.Value);
        Assert.AreEqual("pcl.ui.components", PclUiServiceIds.Components.Value);
        Assert.AreEqual("pcl.market", PluginServiceIds.Market.Value);
        Assert.AreEqual("pcl.secure-storage", PluginServiceIds.SecureStorage.Value);
        Assert.AreEqual("pcl.uri-launcher", PluginServiceIds.UriLauncher.Value);
        Assert.IsFalse(UnconfiguredPluginMarketClient.Instance.IsRemoteConfigured);

        PluginApiVersion v = new(0, 1);
        Assert.IsTrue(PluginServiceVersionRanges.Matches("*", v));
        Assert.IsTrue(PluginServiceVersionRanges.Matches(">=0.1 <1.0", v));
        Assert.IsFalse(PluginServiceVersionRanges.Matches(">=1.0", v));
        Assert.IsTrue(PluginApiVersionRange.Parse(">=0.1 <1.0").Contains(v));
        Assert.IsTrue(typeof(IPluginNotificationService).IsAssignableTo(typeof(IPluginService)));
        Assert.IsTrue(typeof(IPluginSettingsStore).IsAssignableTo(typeof(IPluginService)));
        Assert.IsTrue(typeof(IPluginCommandService).IsAssignableTo(typeof(IPluginService)));
        Assert.IsTrue(typeof(IPluginTaskService).IsAssignableTo(typeof(IPluginService)));
        Assert.IsTrue(typeof(IPluginInstanceReadService).IsAssignableTo(typeof(IPluginService)));
        Assert.IsTrue(typeof(IPluginSecureStorage).IsAssignableTo(typeof(IPluginService)));
        Assert.IsTrue(typeof(IPluginUriLauncher).IsAssignableTo(typeof(IPluginService)));
        Assert.IsTrue(typeof(IPluginUiSurfaceRegistry).IsAssignableTo(typeof(IPluginService)));
        Assert.IsTrue(typeof(IPluginUiSurfaceCapability).IsAssignableTo(typeof(IPluginCapability)));
        Assert.IsTrue(typeof(IPluginUiPatchService).IsAssignableTo(typeof(IPluginService)));
        Assert.IsTrue(typeof(PclUiService).IsAssignableTo(typeof(IPluginService)));
        Assert.IsNotNull(typeof(IPluginMarketClient).GetMethod(nameof(IPluginMarketClient.ListPluginsAsync)));
        Assert.IsNotNull(typeof(PluginMarketPluginSummary).GetProperty(nameof(PluginMarketPluginSummary.PriceCents)));
        Assert.IsNotNull(typeof(PluginMarketPluginSummary).GetProperty(nameof(PluginMarketPluginSummary.RequiresPurchase)));
        Assert.IsNotNull(typeof(PluginMarketPluginDetail).GetProperty(nameof(PluginMarketPluginDetail.Category)));
        Assert.IsTrue(Enum.IsDefined(PluginMarketAccessFailure.PurchaseRequired));
        Assert.IsNotNull(typeof(IPluginMarketClient).GetMethod(nameof(IPluginMarketClient.GetDownloadAsync)));
        Assert.IsNotNull(typeof(IPluginMarketClient).GetMethod(nameof(IPluginMarketClient.VerifyPackageAsync)));
    }

    private static TestPluginContext CreateContext() =>
        new(
            new PluginDescriptor(new PluginId("example.hello"), "Hello Plugin", PluginVersion.Parse("0.1.0")),
            new PluginApiVersion(0, 1));

    private sealed class TemporaryPluginRoot : IDisposable
    {
        public TemporaryPluginRoot()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pcl-n-sdk-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                    Directory.Delete(Path, recursive: true);
            }
            catch
            {
                // best-effort
            }
        }
    }

    [TestMethod]
    public void LegacySettingsContracts_AreMarkedObsolete()
    {
#pragma warning disable CS0618
        Assert.IsNotNull(typeof(PluginSettingsPageDescriptor).GetCustomAttributes(typeof(ObsoleteAttribute), false).SingleOrDefault());
        Assert.IsNotNull(typeof(IPluginSettingsPageCapability).GetCustomAttributes(typeof(ObsoleteAttribute), false).SingleOrDefault());
        Assert.IsNotNull(typeof(PclUiString).GetCustomAttributes(typeof(ObsoleteAttribute), false).SingleOrDefault());
#pragma warning restore CS0618
    }

    private static PluginLocalizedSettingsPageDescriptor CreatePage(string id) =>
        new(
            id,
            new PclLocalizedString("page.title", "页面"),
            "lucide/puzzle",
            new PclLocalizedString("page.heading", "标题"),
            new PclLocalizedString("page.description", "描述"),
            []);
}
