using AwsLocalStackVisualizer.Configuration;
using Microsoft.Extensions.Options;

namespace AwsLocalStackVisualizer.Services;

public interface IReadOnlyService
{
    bool IsReadOnly { get; }
}

public class ReadOnlyService : IReadOnlyService
{
    private readonly AwsConfiguration _awsConfiguration;

    public ReadOnlyService(IOptions<AwsConfiguration> awsConfiguration)
    {
        _awsConfiguration = awsConfiguration.Value;
    }

    public bool IsReadOnly => !_awsConfiguration.UseLocalStack;
}
