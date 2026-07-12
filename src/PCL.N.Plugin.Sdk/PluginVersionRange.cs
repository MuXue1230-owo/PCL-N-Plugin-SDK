namespace PCL.N.Plugin.Sdk;

public sealed class PluginVersionRange
{
    private readonly IReadOnlyList<Constraint> _constraints;

    private PluginVersionRange(string value, IReadOnlyList<Constraint> constraints)
    {
        Value = value;
        _constraints = constraints;
    }

    public string Value { get; }

    public static PluginVersionRange Parse(string value) =>
        TryParse(value, out PluginVersionRange? range) && range is not null
            ? range
            : throw new FormatException($"Invalid plugin version range: {value}");

    public static bool TryParse(string? value, out PluginVersionRange? range)
    {
        range = null;
        if (string.IsNullOrWhiteSpace(value))
            return false;
        if (value == "*")
        {
            range = new PluginVersionRange(value, []);
            return true;
        }

        List<Constraint> constraints = [];
        foreach (string token in value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string operation = token.StartsWith(">=", StringComparison.Ordinal) ? ">=" :
                token.StartsWith("<=", StringComparison.Ordinal) ? "<=" :
                token.StartsWith('>') ? ">" :
                token.StartsWith('<') ? "<" :
                token.StartsWith('=') ? "=" : "=";
            string versionText = operation == "=" && !token.StartsWith('=') ? token : token[operation.Length..];
            if (!PluginVersion.TryParse(versionText, out PluginVersion version))
                return false;
            constraints.Add(new Constraint(operation, version));
        }

        range = new PluginVersionRange(value, constraints);
        return true;
    }

    public bool Contains(PluginVersion version)
    {
        foreach (Constraint constraint in _constraints)
        {
            int comparison = version.CompareTo(constraint.Version);
            if (constraint.Operation switch
                {
                    ">=" => comparison >= 0,
                    ">" => comparison > 0,
                    "<=" => comparison <= 0,
                    "<" => comparison < 0,
                    _ => comparison == 0
                } is false)
            {
                return false;
            }
        }
        return true;
    }

    public override string ToString() => Value;

    private sealed record Constraint(string Operation, PluginVersion Version);
}
