namespace AwsLocalStackVisualizer.Configuration;

public record LocalStackConfiguration
{
    public string ServiceUrl { get; init; } = "http://localhost:4566";
    public string Region { get; init; } = "us-west-2";
    public string? AccessKey { get; init; } = null;
    public string? SecretKey { get; init; } = null;
    public bool UseAnonymousCredentials { get; init; } = true;
}


