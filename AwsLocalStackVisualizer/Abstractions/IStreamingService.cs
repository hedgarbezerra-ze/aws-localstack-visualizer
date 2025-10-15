using AwsLocalStackVisualizer.Models.S3;
using AwsLocalStackVisualizer.Models.SQS;
using AwsLocalStackVisualizer.Models.SNS;
using AwsLocalStackVisualizer.Models.SecretsManager;

namespace AwsLocalStackVisualizer.Abstractions;

public interface IStreamingService
{
    IAsyncEnumerable<S3BucketInfo> GetS3BucketsStreamAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<SqsQueueInfo> GetSqsQueuesStreamAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<SnsTopicInfo> GetSnsTopicsStreamAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<SecretInfo> GetSecretsStreamAsync(CancellationToken cancellationToken = default);
}

public record ServiceUpdate<T>(
    string ServiceName,
    T? Data,
    bool IsComplete,
    string? ErrorMessage = null
);
