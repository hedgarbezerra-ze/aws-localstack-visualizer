using Amazon.Runtime;
using Amazon.S3;
using Amazon.SQS;
using Amazon.SimpleNotificationService;
using Amazon.SecretsManager;
using Microsoft.Extensions.Options;

namespace AwsLocalStackVisualizer.Configuration;

public sealed class AppAwsClients : IAppAwsClients
{
    private readonly AwsRegionContext _regionContext;
    private readonly AwsConfiguration _config;
    private readonly AWSCredentials _credentials;
    private readonly object _gate = new();
    private AmazonS3Client? _s3;
    private AmazonSQSClient? _sqs;
    private AmazonSimpleNotificationServiceClient? _sns;
    private AmazonSecretsManagerClient? _secrets;
    private string? _activeRegion;

    public AppAwsClients(AwsRegionContext regionContext, IOptions<AwsConfiguration> configuration, AWSCredentials credentials)
    {
        _regionContext = regionContext;
        _config = configuration.Value;
        _credentials = credentials;
    }

    public AmazonS3Client S3
    {
        get
        {
            EnsureClients();
            return _s3!;
        }
    }

    public AmazonSQSClient SQS
    {
        get
        {
            EnsureClients();
            return _sqs!;
        }
    }

    public AmazonSimpleNotificationServiceClient SNS
    {
        get
        {
            EnsureClients();
            return _sns!;
        }
    }

    public AmazonSecretsManagerClient SecretsManager
    {
        get
        {
            EnsureClients();
            return _secrets!;
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            DisposeClientsLocked();
        }
    }

    private void EnsureClients()
    {
        var region = _regionContext.Region;
        if (string.IsNullOrWhiteSpace(region))
            region = AwsRegionsCatalog.DefaultRegion;

        lock (_gate)
        {
            if (_activeRegion == region && _s3 != null)
                return;

            DisposeClientsLocked();
            _activeRegion = region;
            _s3 = AwsClientFactory.CreateS3Client(_credentials, _config, region);
            _sqs = AwsClientFactory.CreateSqsClient(_credentials, _config, region);
            _sns = AwsClientFactory.CreateSnsClient(_credentials, _config, region);
            _secrets = AwsClientFactory.CreateSecretsManagerClient(_credentials, _config, region);
        }
    }

    private void DisposeClientsLocked()
    {
        _s3?.Dispose();
        _sqs?.Dispose();
        _sns?.Dispose();
        _secrets?.Dispose();
        _s3 = null;
        _sqs = null;
        _sns = null;
        _secrets = null;
        _activeRegion = null;
    }
}
