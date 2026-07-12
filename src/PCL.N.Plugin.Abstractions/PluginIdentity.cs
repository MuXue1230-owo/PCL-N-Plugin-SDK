namespace PCL.N.Plugin;

public readonly record struct PluginId
{
    public PluginId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value ?? string.Empty;
}

public readonly record struct PluginApiVersion(int Major, int Minor) : IComparable<PluginApiVersion>
{
    public int CompareTo(PluginApiVersion other)
    {
        int majorComparison = Major.CompareTo(other.Major);
        return majorComparison != 0 ? majorComparison : Minor.CompareTo(other.Minor);
    }

    public override string ToString() => $"{Major}.{Minor}";
}

public sealed record PluginDescriptor(
    PluginId Id,
    string Name,
    Version Version);
