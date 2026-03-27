namespace AwsLocalStackVisualizer.Configuration;

public sealed class AwsRegionContext
{
    public string Region { get; private set; } = AwsRegionsCatalog.DefaultRegion;

    public event Action? RegionChanged;

    public bool SetRegion(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (string.Equals(Region, value, StringComparison.Ordinal))
            return false;

        Region = value;
        RegionChanged?.Invoke();
        return true;
    }
}
