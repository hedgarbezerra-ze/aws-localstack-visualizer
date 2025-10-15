namespace AwsLocalStackVisualizer.Models.SecretsManager;

public record SecretVersionInfo(string VersionId, DateTime CreatedDate, bool IsCurrent, 
    IReadOnlyList<string> VersionStages);
