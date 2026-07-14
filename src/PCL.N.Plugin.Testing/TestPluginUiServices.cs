namespace PCL.N.Plugin.Testing;

public sealed class TestPluginUiSurfaceCapability : IPluginUiSurfaceCapability
{
    private readonly List<PluginUiSlotContributionDescriptor> _contributions = [];
    public string Id => "pcl.ui.surface";
    public PluginApiVersion Version => new(0, 1);
    public IReadOnlyList<PluginUiSlotContributionDescriptor> Contributions => _contributions;

    public IPluginRegistration Contribute(PluginUiSlotContributionDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        if (_contributions.Any(item => string.Equals(item.ContributionId, descriptor.ContributionId, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"UI contribution already registered: {descriptor.ContributionId}");
        _contributions.Add(descriptor);
        return new TestDelegateRegistration(descriptor.ContributionId, () => _contributions.Remove(descriptor));
    }
}

public sealed class TestPluginUiPatchService(TestPluginLifetime? lifetime = null) : IPluginUiPatchService
{
    private readonly List<PluginUiPatchDescriptor> _patches = [];
    public PluginServiceId Id => PluginServiceIds.UiPatch;
    public PluginApiVersion Version => new(0, 1);
    public IReadOnlyList<PluginUiPatchDescriptor> ListPatches(string? pluginId = null) => _patches.ToArray();

    public IPluginRegistration Register(PluginUiPatchDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        if (_patches.Any(item => string.Equals(item.OperationId, descriptor.OperationId, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"UI patch already registered: {descriptor.OperationId}");
        _patches.Add(descriptor);
        TestDelegateRegistration registration = new(descriptor.OperationId, () => _patches.Remove(descriptor));
        lifetime?.Track(registration);
        return registration;
    }

    public PluginUiPatchPlan Plan()
    {
        string[] ordered = _patches
            .OrderBy(static patch => patch.Priority)
            .ThenBy(static patch => patch.OperationId, StringComparer.Ordinal)
            .Select(static patch => patch.OperationId)
            .ToArray();
        List<PluginUiConflict> conflicts = [];
        foreach (IGrouping<string, PluginUiPatchDescriptor> group in _patches.GroupBy(static patch => patch.Target, StringComparer.OrdinalIgnoreCase))
        {
            PluginUiPatchDescriptor[] replacements = group.Where(static patch => patch.Kind == PluginUiPatchKind.Replace).ToArray();
            for (int left = 0; left < replacements.Length; left++)
            for (int right = left + 1; right < replacements.Length; right++)
                conflicts.Add(new PluginUiConflict(
                    PluginUiConflictKind.ReplaceExclusive,
                    PluginUiConflictSeverity.Error,
                    replacements[left].OperationId,
                    replacements[right].OperationId,
                    group.Key,
                    "Multiple replace operations target the same surface."));
        }
        return new PluginUiPatchPlan(ordered, conflicts, conflicts.Any(static conflict => conflict.Severity == PluginUiConflictSeverity.Error));
    }
}
