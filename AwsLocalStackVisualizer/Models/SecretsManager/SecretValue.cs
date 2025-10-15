namespace AwsLocalStackVisualizer.Models.SecretsManager;

public record SecretValue(string Name, string? SecretString, byte[]? SecretBinary, 
    string VersionId, DateTime CreatedDate);
