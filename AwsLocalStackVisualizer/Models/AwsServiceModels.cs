namespace AwsLocalStackVisualizer.Models;

public record ServiceStatus(string Name, bool IsEnabled, bool IsHealthy, int ResourceCount);

public record DashboardData(IReadOnlyList<ServiceStatus> Services, DateTime LastUpdated);

public record S3BucketInfo(string Name, DateTime CreationDate, long ObjectCount, long TotalSize);

public record S3ObjectInfo(string Key, long Size, DateTime LastModified, string ETag, string StorageClass);

public record S3BucketDetails(S3BucketInfo BucketInfo, IReadOnlyList<S3ObjectInfo> Objects);

public record SqsQueueInfo(string Name, string Url, int ApproximateNumberOfMessages, 
    int ApproximateNumberOfMessagesNotVisible, DateTime CreatedTimestamp);

public record SqsMessageInfo(string MessageId, string Body, string ReceiptHandle, 
    DateTime SentTimestamp, int ReceiveCount, Dictionary<string, string> Attributes);

public record SqsQueueDetails(SqsQueueInfo QueueInfo, IReadOnlyList<SqsMessageInfo> Messages);

public record SnsTopicInfo(string Name, string Arn, int SubscriptionsCount, DateTime CreatedDate);

public record SnsSubscriptionInfo(string SubscriptionArn, string Protocol, string Endpoint, 
    bool ConfirmationWasAuthenticated, string Owner);

public record SnsTopicDetails(SnsTopicInfo TopicInfo, IReadOnlyList<SnsSubscriptionInfo> Subscriptions);

public record SecretInfo(string Name, string Arn, DateTime CreatedDate, DateTime? LastChangedDate, 
    string? Description, Dictionary<string, string> Tags);

public record SecretValue(string Name, string? SecretString, byte[]? SecretBinary, 
    string VersionId, DateTime CreatedDate);

public record SecretDetails(SecretInfo SecretInfo, IReadOnlyList<SecretVersionInfo> Versions);

public record SecretVersionInfo(string VersionId, DateTime CreatedDate, bool IsCurrent, 
    IReadOnlyList<string> VersionStages);

public record AwsServiceError(string Service, string Operation, string Message, Exception? Exception = null);

public record OperationResult<T>(bool IsSuccess, T? Data = default, string? ErrorMessage = null, Exception? Exception = null);


