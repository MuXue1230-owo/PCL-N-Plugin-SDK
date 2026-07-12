using HelloPlugin;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.Plugin.Sdk;

namespace PCL.Plugin.Sdk.Test;

[TestClass]
public sealed class HostModuleTests
{
    [TestMethod]
    public void ExampleModule_RegistersSettingsPageThroughCapability()
    {
        HostSettingsPageRegistry pages = new();
        PclHostBuilder builder = new PclHostBuilder(new Version(0, 1))
            .AddCapability<IHostSettingsPageRegistry>(pages);

        new HelloHostModule().Configure(builder);

        Assert.AreEqual("example.hello.settings", pages.Pages.Single().Id);
        Assert.AreEqual(HostSettingsHintKind.Warning, pages.Pages.Single().Hints.Single().Kind);
    }

    [TestMethod]
    public void RequireCapability_ReportsUnsupportedHostFeature()
    {
        PclHostBuilder builder = new(new Version(0, 1));

        Assert.ThrowsExactly<NotSupportedException>(
            () => builder.RequireCapability<IHostSettingsPageRegistry>());
    }

    [TestMethod]
    public void SettingsRegistry_RejectsDuplicateIdsCaseInsensitively()
    {
        HostSettingsPageRegistry pages = new();
        pages.Add(CreatePage("example.page"));

        Assert.ThrowsExactly<InvalidOperationException>(() => pages.Add(CreatePage("EXAMPLE.PAGE")));
    }

    private static HostSettingsPage CreatePage(string id) =>
        new(id, "Page", "lucide/puzzle", "Heading", "Description", []);
}
