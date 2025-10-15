namespace AwsLocalStackVisualizer.Models.SecretsManager;

public record SecretDetails(SecretInfo SecretInfo, IReadOnlyList<SecretVersionInfo> Versions);
