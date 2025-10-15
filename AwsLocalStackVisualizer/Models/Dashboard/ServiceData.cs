namespace AwsLocalStackVisualizer.Models.Dashboard;

public record S3ServiceData(bool IsHealthy, int BucketCount, long ObjectCount, long TotalSize) : ServiceData(IsHealthy, BucketCount);

public record SqsServiceData(bool IsHealthy, int QueueCount, int TotalMessages, int VisibleMessages) : ServiceData(IsHealthy, QueueCount);

public record SnsServiceData(bool IsHealthy, int TopicCount, int SubscriptionCount) : ServiceData(IsHealthy, TopicCount);

public record SecretsServiceData(bool IsHealthy, int SecretCount) : ServiceData(IsHealthy, SecretCount);
