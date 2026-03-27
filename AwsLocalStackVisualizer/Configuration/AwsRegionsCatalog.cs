using Amazon;

namespace AwsLocalStackVisualizer.Configuration;

public static class AwsRegionsCatalog
{
    public const string DefaultRegion = "us-east-1";

    public static IReadOnlyList<string> All { get; } = RegionEndpoint.EnumerableAllRegions
        .Select(static r => r.SystemName)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(static s => s, StringComparer.Ordinal)
        .ToList();
}
