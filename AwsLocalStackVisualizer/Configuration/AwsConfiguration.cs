namespace AwsLocalStackVisualizer.Configuration;

public enum AwsCredentialType
{
    Anonymous,
    Session,
    Basic
}

public record AwsConfiguration
{
    public bool UseLocalStack { get; init; } = true;
    public string? ServiceUrl { get; init; } = null;
    public AwsCredentials Credentials { get; init; } = new();
}

public record AwsCredentials
{
    public AwsCredentialType Type { get; init; } = AwsCredentialType.Anonymous;
    public string? AccessKey { get; init; } = null;
    public string? SecretKey { get; init; } = null;
    public string? SessionToken { get; init; } = null;
    public string? ProfileName { get; init; } = null;
}