namespace AwsLocalStackVisualizer.Models.Common;

public record ServiceStatus(string Name, bool IsEnabled, bool IsHealthy, int ResourceCount);
