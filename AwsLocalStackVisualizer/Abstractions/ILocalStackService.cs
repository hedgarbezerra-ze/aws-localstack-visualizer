using AwsLocalStackVisualizer.Models.Common;
using AwsLocalStackVisualizer.Models.Dashboard;
using AwsLocalStackVisualizer.Models.S3;
using AwsLocalStackVisualizer.Models.SQS;
using AwsLocalStackVisualizer.Models.SNS;
using AwsLocalStackVisualizer.Models.SecretsManager;

namespace AwsLocalStackVisualizer.Abstractions;

public interface ILocalStackService
{
    Task<DashboardServiceData> GetDashboardDataAsync();
    IAsyncEnumerable<S3BucketInfo> GetS3BucketsStreamAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<SqsQueueInfo> GetSqsQueuesStreamAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<SnsTopicInfo> GetSnsTopicsStreamAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<SecretInfo> GetSecretsStreamAsync(CancellationToken cancellationToken = default);
    Task<OperationResult<bool>> TestConnectionAsync();

    Task<OperationResult<IReadOnlyList<S3BucketInfo>>> GetS3BucketsAsync();
    Task<OperationResult<S3BucketDetails>> GetS3BucketDetailsAsync(string bucketName);
    Task<OperationResult<string>> GetS3ObjectContentAsync(string bucketName, string objectKey);
    Task<OperationResult<bool>> CreateS3BucketAsync(string bucketName);
    Task<OperationResult<bool>> UploadS3ObjectAsync(string bucketName, string objectKey, string content, string? contentType = null);
    Task<OperationResult<bool>> UploadFileAsync(string bucketName, string objectKey, Stream fileStream, string contentType);
    Task<OperationResult<byte[]>> DownloadObjectAsync(string bucketName, string objectKey);
    Task<OperationResult<bool>> DeleteS3BucketAsync(string bucketName, bool force = false);
    Task<OperationResult<bool>> DeleteS3ObjectAsync(string bucketName, string objectKey);

    Task<OperationResult<IReadOnlyList<SqsQueueInfo>>> GetSqsQueuesAsync();
    Task<OperationResult<SqsQueueDetails>> GetSqsQueueDetailsAsync(string queueName);
    Task<OperationResult<string>> CreateSqsQueueAsync(string queueName, Dictionary<string, string>? attributes = null);
    Task<OperationResult<string>> SendSqsMessageAsync(string queueUrl, string messageBody, Dictionary<string, string>? messageAttributes = null);
    Task<OperationResult<bool>> DeleteSqsQueueAsync(string queueUrl);
    Task<OperationResult<bool>> PurgeSqsQueueAsync(string queueUrl);

    Task<OperationResult<IReadOnlyList<SnsTopicInfo>>> GetSnsTopicsAsync();
    Task<OperationResult<SnsTopicDetails>> GetSnsTopicDetailsAsync(string topicArn);
    Task<OperationResult<IReadOnlyList<SnsSubscriptionInfo>>> GetSnsSubscriptionsAsync(string topicArn);
    Task<OperationResult<IReadOnlyList<SnsMessageInfo>>> GetSnsTopicMessagesAsync(string topicArn);
    Task<OperationResult<string>> CreateSnsTopicAsync(string topicName, Dictionary<string, string>? attributes = null);
    Task<OperationResult<string>> PublishSnsMessageAsync(string topicArn, string message, string? subject = null, Dictionary<string, string>? messageAttributes = null);
    Task<OperationResult<string>> SubscribeSnsAsync(string topicArn, string protocol, string endpoint, Dictionary<string, string>? attributes = null);
    Task<OperationResult<bool>> DeleteSnsTopicAsync(string topicArn);
    Task<OperationResult<bool>> UnsubscribeSnsAsync(string subscriptionArn);

    Task<OperationResult<IReadOnlyList<SecretInfo>>> GetSecretsAsync();
    Task<OperationResult<SecretDetails>> GetSecretDetailsAsync(string secretName);
    Task<OperationResult<SecretValue>> GetSecretValueAsync(string secretName, string? versionId = null);
    Task<OperationResult<string>> CreateSecretAsync(string name, string secretValue, string? description = null);
    Task<OperationResult<bool>> UpdateSecretAsync(string name, string secretValue);
    Task<OperationResult<bool>> DeleteSecretAsync(string name, bool forceDelete = false);
}
