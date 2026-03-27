using Amazon.S3;
using Amazon.SQS;
using Amazon.SimpleNotificationService;
using Amazon.SecretsManager;

namespace AwsLocalStackVisualizer.Configuration;

public interface IAppAwsClients : IDisposable
{
    AmazonS3Client S3 { get; }

    AmazonSQSClient SQS { get; }

    AmazonSimpleNotificationServiceClient SNS { get; }

    AmazonSecretsManagerClient SecretsManager { get; }
}
