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
        context.TestCapabilities.Add<IPluginSettingsPageCapability>(pages);

        await new HelloPlugin.HelloPlugin().InitializeAsync(context, CancellationToken.None);

        Assert.AreEqual("example.hello.settings", pages.Pages.Single().Id);
        Assert.AreEqual(PluginSettingsHintKind.Warning, pages.Pages.Single().Hints.Single().Kind);
        await context.DisposeAsync();
        Assert.AreEqual(0, pages.Pages.Count);
    }

    [TestMethod]
    public void RequireCapability_ReportsUnsupportedHostFeature()
    {
        TestPluginCapabilityProvider capabilities = new();

        Assert.ThrowsExactly<NotSupportedException>(
            () => capabilities.Require<IPluginSettingsPageCapability>());
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

    private static TestPluginContext CreateContext() =>
        new(
            new PluginDescriptor(new PluginId("example.hello"), "Hello Plugin", PluginVersion.Parse("0.1.0")),
            new PluginApiVersion(0, 1));

    private static PluginSettingsPageDescriptor CreatePage(string id) =>
        new(id, "Page", "lucide/puzzle", "Heading", "Description", []);
}
