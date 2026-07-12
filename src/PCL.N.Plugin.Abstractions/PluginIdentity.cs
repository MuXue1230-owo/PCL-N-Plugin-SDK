namespace PCL.N.Plugin;

public readonly record struct PluginId
{
    public PluginId(string value)
    {
        if (!TryValidate(value, out string? error))
            throw new ArgumentException(error, nameof(value));
        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value ?? string.Empty;

    public static bool TryParse(string? value, out PluginId id)
    {
        if (!TryValidate(value, out _))
        {
            id = default;
            return false;
        }

        id = new PluginId(value!);
        return true;
    }

    private static bool TryValidate(string? value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            error = "Plugin ID cannot be empty.";
            return false;
        }
        if (value.StartsWith('.') || value.EndsWith('.') || value.StartsWith('-') || value.EndsWith('-'))
        {
            error = "Plugin ID cannot start or end with a separator.";
            return false;
        }

        bool previousSeparator = false;
        foreach (char character in value)
        {
            bool separator = character is '.' or '-';
            if (!(character is >= 'a' and <= 'z' || character is >= '0' and <= '9' || separator) ||
                (separator && previousSeparator))
            {
                error = "Plugin ID must match ^[a-z0-9]+([.-][a-z0-9]+)*$.";
                return false;
            }
            previousSeparator = separator;
        }

        error = null;
        return true;
    }
}

public readonly record struct PluginVersion : IComparable<PluginVersion>
{
    private readonly int[]? _core;
    private readonly string[]? _preRelease;

    private PluginVersion(string value, int[] core, string[] preRelease)
    {
        Value = value;
        _core = core;
        _preRelease = preRelease;
    }

    public string Value { get; }

    public static PluginVersion Parse(string value) =>
        TryParse(value, out PluginVersion version)
            ? version
            : throw new FormatException($"Invalid SemVer 2.0 version: {value}");

    public static bool TryParse(string? value, out PluginVersion version)
    {
        version = default;
        if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
            return false;

        string[] buildParts = value.Split('+');
        if (buildParts.Length > 2 || (buildParts.Length == 2 && !ValidateIdentifiers(buildParts[1], allowLeadingZero: true)))
            return false;
        string[] preParts = buildParts[0].Split('-');
        if (preParts.Length > 2)
            return false;

        string[] coreParts = preParts[0].Split('.');
        if (coreParts.Length != 3)
            return false;
        int[] core = new int[3];
        for (int index = 0; index < coreParts.Length; index++)
        {
            string part = coreParts[index];
            if (part.Length == 0 || (part.Length > 1 && part[0] == '0') ||
                !int.TryParse(part, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out core[index]))
            {
                return false;
            }
        }

        string[] preRelease = [];
        if (preParts.Length == 2)
        {
            if (!ValidateIdentifiers(preParts[1], allowLeadingZero: false))
                return false;
            preRelease = preParts[1].Split('.');
        }

        version = new PluginVersion(value, core, preRelease);
        return true;
    }

    public int CompareTo(PluginVersion other)
    {
        for (int index = 0; index < 3; index++)
        {
            int comparison = CoreAt(index).CompareTo(other.CoreAt(index));
            if (comparison != 0)
                return comparison;
        }

        string[] left = _preRelease ?? [];
        string[] right = other._preRelease ?? [];
        if (left.Length == 0 || right.Length == 0)
            return left.Length == right.Length ? 0 : left.Length == 0 ? 1 : -1;

        for (int index = 0; index < Math.Min(left.Length, right.Length); index++)
        {
            bool leftNumeric = left[index].All(char.IsDigit);
            bool rightNumeric = right[index].All(char.IsDigit);
            int comparison = leftNumeric && rightNumeric
                ? CompareNumericIdentifier(left[index], right[index])
                : leftNumeric ? -1
                : rightNumeric ? 1
                : string.CompareOrdinal(left[index], right[index]);
            if (comparison != 0)
                return comparison;
        }
        return left.Length.CompareTo(right.Length);
    }

    public override string ToString() => Value ?? string.Empty;

    public bool Equals(PluginVersion other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);

    public static bool operator <(PluginVersion left, PluginVersion right) => left.CompareTo(right) < 0;
    public static bool operator >(PluginVersion left, PluginVersion right) => left.CompareTo(right) > 0;
    public static bool operator <=(PluginVersion left, PluginVersion right) => left.CompareTo(right) <= 0;
    public static bool operator >=(PluginVersion left, PluginVersion right) => left.CompareTo(right) >= 0;

    private int CoreAt(int index) => _core is { Length: 3 } ? _core[index] : 0;

    private static int CompareNumericIdentifier(string left, string right)
    {
        int lengthComparison = left.Length.CompareTo(right.Length);
        return lengthComparison != 0 ? lengthComparison : string.CompareOrdinal(left, right);
    }

    private static bool ValidateIdentifiers(string value, bool allowLeadingZero)
    {
        foreach (string identifier in value.Split('.'))
        {
            if (identifier.Length == 0 ||
                identifier.Any(character => !(character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '-')) ||
                (!allowLeadingZero && identifier.Length > 1 && identifier[0] == '0' && identifier.All(char.IsDigit)))
            {
                return false;
            }
        }
        return true;
    }
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
    PluginVersion Version);
