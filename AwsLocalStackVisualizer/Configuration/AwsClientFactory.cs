using Amazon.S3;
using Amazon.SQS;
using Amazon.SimpleNotificationService;
using Amazon.SecretsManager;
using Amazon.Runtime;

namespace AwsLocalStackVisualizer.Configuration;

public static class AwsClientFactory
{
    public static AmazonS3Client CreateS3Client(AWSCredentials credentials, AwsConfiguration config, string region)
    {
        if (config.UseLocalStack)
        {
            var s3Config = new AmazonS3Config
            {
                ServiceURL = config.ServiceUrl,
                ForcePathStyle = true,
                UseHttp = config.ServiceUrl?.StartsWith("http://") ?? false,
                AuthenticationRegion = region
            };
            return new AmazonS3Client(credentials, s3Config);
        }

        var awsS3Config = new AmazonS3Config
        {
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region)
        };
        return new AmazonS3Client(credentials, awsS3Config);
    }

    public static AmazonSQSClient CreateSqsClient(AWSCredentials credentials, AwsConfiguration config, string region)
    {
        if (config.UseLocalStack)
        {
            var sqsConfig = new AmazonSQSConfig
            {
                ServiceURL = config.ServiceUrl,
                UseHttp = config.ServiceUrl?.StartsWith("http://") ?? false,
                AuthenticationRegion = region
            };
            return new AmazonSQSClient(credentials, sqsConfig);
        }

        var awsSqsConfig = new AmazonSQSConfig
        {
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region)
        };
        return new AmazonSQSClient(credentials, awsSqsConfig);
    }

    public static AmazonSimpleNotificationServiceClient CreateSnsClient(AWSCredentials credentials, AwsConfiguration config, string region)
    {
        if (config.UseLocalStack)
        {
            var snsConfig = new AmazonSimpleNotificationServiceConfig
            {
                ServiceURL = config.ServiceUrl,
                UseHttp = config.ServiceUrl?.StartsWith("http://") ?? false,
                AuthenticationRegion = region
            };
            return new AmazonSimpleNotificationServiceClient(credentials, snsConfig);
        }

        var awsSnsConfig = new AmazonSimpleNotificationServiceConfig
        {
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region)
        };
        return new AmazonSimpleNotificationServiceClient(credentials, awsSnsConfig);
    }

    public static AmazonSecretsManagerClient CreateSecretsManagerClient(AWSCredentials credentials, AwsConfiguration config, string region)
    {
        if (config.UseLocalStack)
        {
            var secretsConfig = new AmazonSecretsManagerConfig
            {
                ServiceURL = config.ServiceUrl,
                UseHttp = config.ServiceUrl?.StartsWith("http://") ?? false,
                AuthenticationRegion = region
            };
            return new AmazonSecretsManagerClient(credentials, secretsConfig);
        }

        var awsSecretsConfig = new AmazonSecretsManagerConfig
        {
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region)
        };
        return new AmazonSecretsManagerClient(credentials, awsSecretsConfig);
    }
}
