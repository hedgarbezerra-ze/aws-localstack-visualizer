using AwsLocalStackVisualizer.Configuration;
using AwsLocalStackVisualizer.Models;
using AwsLocalStackVisualizer.Services.AWS;
using Microsoft.Extensions.Options;

namespace AwsLocalStackVisualizer.Services;

public interface ILocalStackService
{
    Task<DashboardData> GetDashboardDataAsync();
    Task<OperationResult<bool>> TestConnectionAsync();
    
    // S3 Operations
    Task<OperationResult<IReadOnlyList<S3BucketInfo>>> GetS3BucketsAsync();
    Task<OperationResult<S3BucketDetails>> GetS3BucketDetailsAsync(string bucketName);
    Task<OperationResult<string>> GetS3ObjectContentAsync(string bucketName, string objectKey);
    Task<OperationResult<bool>> CreateS3BucketAsync(string bucketName);
    Task<OperationResult<bool>> UploadS3ObjectAsync(string bucketName, string objectKey, string content, string? contentType = null);
    Task<OperationResult<bool>> DeleteS3BucketAsync(string bucketName, bool force = false);
    Task<OperationResult<bool>> DeleteS3ObjectAsync(string bucketName, string objectKey);
    
    // SQS Operations  
    Task<OperationResult<IReadOnlyList<SqsQueueInfo>>> GetSqsQueuesAsync();
    Task<OperationResult<SqsQueueDetails>> GetSqsQueueDetailsAsync(string queueName);
    Task<OperationResult<string>> CreateSqsQueueAsync(string queueName, Dictionary<string, string>? attributes = null);
    Task<OperationResult<string>> SendSqsMessageAsync(string queueUrl, string messageBody, Dictionary<string, string>? messageAttributes = null);
    Task<OperationResult<bool>> DeleteSqsQueueAsync(string queueUrl);
    Task<OperationResult<bool>> PurgeSqsQueueAsync(string queueUrl);
    
    // SNS Operations
    Task<OperationResult<IReadOnlyList<SnsTopicInfo>>> GetSnsTopicsAsync();
    Task<OperationResult<SnsTopicDetails>> GetSnsTopicDetailsAsync(string topicArn);
    Task<OperationResult<string>> CreateSnsTopicAsync(string topicName, Dictionary<string, string>? attributes = null);
    Task<OperationResult<string>> PublishSnsMessageAsync(string topicArn, string message, string? subject = null, Dictionary<string, string>? messageAttributes = null);
    Task<OperationResult<string>> SubscribeSnsAsync(string topicArn, string protocol, string endpoint, Dictionary<string, string>? attributes = null);
    Task<OperationResult<bool>> DeleteSnsTopicAsync(string topicArn);
    Task<OperationResult<bool>> UnsubscribeSnsAsync(string subscriptionArn);
    
    // Secrets Manager Operations
    Task<OperationResult<IReadOnlyList<SecretInfo>>> GetSecretsAsync();
    Task<OperationResult<SecretDetails>> GetSecretDetailsAsync(string secretName);
    Task<OperationResult<SecretValue>> GetSecretValueAsync(string secretName, string? versionId = null);
    Task<OperationResult<string>> CreateSecretAsync(string name, string secretValue, string? description = null);
    Task<OperationResult<bool>> UpdateSecretAsync(string name, string secretValue);
    Task<OperationResult<bool>> DeleteSecretAsync(string name, bool forceDelete = false);
}

public class LocalStackService : ILocalStackService
{
    private readonly LocalStackConfiguration _config;
    private readonly ILogger<LocalStackService> _logger;
    private readonly IS3Service _s3Service;
    private readonly ISqsService _sqsService;
    private readonly ISnsService _snsService;
    private readonly ISecretsManagerService _secretsManagerService;

    public LocalStackService(
        IOptions<LocalStackConfiguration> config, 
        ILogger<LocalStackService> logger,
        IS3Service s3Service,
        ISqsService sqsService,
        ISnsService snsService,
        ISecretsManagerService secretsManagerService)
    {
        _config = config.Value;
        _logger = logger;
        _s3Service = s3Service;
        _sqsService = sqsService;
        _snsService = snsService;
        _secretsManagerService = secretsManagerService;
    }

    public async Task<OperationResult<bool>> TestConnectionAsync()
    {
        return await _s3Service.TestConnectionAsync();
    }

    public async Task<DashboardData> GetDashboardDataAsync()
    {
        var services = new List<ServiceStatus>();
        var s3Status = await GetS3StatusAsync();
        services.Add(s3Status);
        
        var sqsStatus = await GetSqsStatusAsync();
        services.Add(sqsStatus);
        
        var snsStatus = await GetSnsStatusAsync();
        services.Add(snsStatus);
        
        var secretsStatus = await GetSecretsManagerStatusAsync();
        services.Add(secretsStatus);

        return new DashboardData(services, DateTime.UtcNow);
    }

    public Task<OperationResult<IReadOnlyList<S3BucketInfo>>> GetS3BucketsAsync() => 
        _s3Service.GetBucketsAsync();

    public Task<OperationResult<S3BucketDetails>> GetS3BucketDetailsAsync(string bucketName) => 
        _s3Service.GetBucketDetailsAsync(bucketName);

    public Task<OperationResult<string>> GetS3ObjectContentAsync(string bucketName, string objectKey) => 
        _s3Service.GetObjectContentAsync(bucketName, objectKey);

    public Task<OperationResult<IReadOnlyList<SqsQueueInfo>>> GetSqsQueuesAsync() => 
        _sqsService.GetQueuesAsync();

    public Task<OperationResult<SqsQueueDetails>> GetSqsQueueDetailsAsync(string queueName) => 
        _sqsService.GetQueueDetailsAsync(queueName);

    public Task<OperationResult<IReadOnlyList<SnsTopicInfo>>> GetSnsTopicsAsync() => 
        _snsService.GetTopicsAsync();

    public Task<OperationResult<SnsTopicDetails>> GetSnsTopicDetailsAsync(string topicArn) => 
        _snsService.GetTopicDetailsAsync(topicArn);

    public Task<OperationResult<IReadOnlyList<SecretInfo>>> GetSecretsAsync() => 
        _secretsManagerService.GetSecretsAsync();

    public Task<OperationResult<SecretDetails>> GetSecretDetailsAsync(string secretName) => 
        _secretsManagerService.GetSecretDetailsAsync(secretName);

    public Task<OperationResult<SecretValue>> GetSecretValueAsync(string secretName, string? versionId = null) => 
        _secretsManagerService.GetSecretValueAsync(secretName, versionId);

    public Task<OperationResult<string>> CreateSecretAsync(string name, string secretValue, string? description = null) => 
        _secretsManagerService.CreateSecretAsync(name, secretValue, description);

    public Task<OperationResult<bool>> UpdateSecretAsync(string name, string secretValue) => 
        _secretsManagerService.UpdateSecretAsync(name, secretValue);

    public Task<OperationResult<bool>> DeleteSecretAsync(string name, bool forceDelete = false) => 
        _secretsManagerService.DeleteSecretAsync(name, forceDelete);

    // S3 Create Operations
    public Task<OperationResult<bool>> CreateS3BucketAsync(string bucketName) => 
        _s3Service.CreateBucketAsync(bucketName);

    public Task<OperationResult<bool>> UploadS3ObjectAsync(string bucketName, string objectKey, string content, string? contentType = null) => 
        _s3Service.UploadObjectAsync(bucketName, objectKey, content, contentType);

    public Task<OperationResult<bool>> DeleteS3BucketAsync(string bucketName, bool force = false) => 
        _s3Service.DeleteBucketAsync(bucketName, force);

    public Task<OperationResult<bool>> DeleteS3ObjectAsync(string bucketName, string objectKey) => 
        _s3Service.DeleteObjectAsync(bucketName, objectKey);

    // SQS Create Operations
    public Task<OperationResult<string>> CreateSqsQueueAsync(string queueName, Dictionary<string, string>? attributes = null) => 
        _sqsService.CreateQueueAsync(queueName, attributes);

    public Task<OperationResult<string>> SendSqsMessageAsync(string queueUrl, string messageBody, Dictionary<string, string>? messageAttributes = null) => 
        _sqsService.SendMessageAsync(queueUrl, messageBody, messageAttributes);

    public Task<OperationResult<bool>> DeleteSqsQueueAsync(string queueUrl) => 
        _sqsService.DeleteQueueAsync(queueUrl);

    public Task<OperationResult<bool>> PurgeSqsQueueAsync(string queueUrl) => 
        _sqsService.PurgeQueueAsync(queueUrl);

    // SNS Create Operations
    public Task<OperationResult<string>> CreateSnsTopicAsync(string topicName, Dictionary<string, string>? attributes = null) => 
        _snsService.CreateTopicAsync(topicName, attributes);

    public Task<OperationResult<string>> PublishSnsMessageAsync(string topicArn, string message, string? subject = null, Dictionary<string, string>? messageAttributes = null) => 
        _snsService.PublishMessageAsync(topicArn, message, subject, messageAttributes);

    public Task<OperationResult<string>> SubscribeSnsAsync(string topicArn, string protocol, string endpoint, Dictionary<string, string>? attributes = null) => 
        _snsService.SubscribeAsync(topicArn, protocol, endpoint, attributes);

    public Task<OperationResult<bool>> DeleteSnsTopicAsync(string topicArn) => 
        _snsService.DeleteTopicAsync(topicArn);

    public Task<OperationResult<bool>> UnsubscribeSnsAsync(string subscriptionArn) => 
        _snsService.UnsubscribeAsync(subscriptionArn);
    private async Task<ServiceStatus> GetS3StatusAsync()
    {
        try
        {
            var buckets = await _s3Service.GetBucketsAsync();
            return new ServiceStatus("S3", true, buckets.IsSuccess, buckets.Data?.Count ?? 0);
        }
        catch
        {
            return new ServiceStatus("S3", true, false, 0);
        }
    }

    private async Task<ServiceStatus> GetSqsStatusAsync()
    {
        try
        {
            var queues = await _sqsService.GetQueuesAsync();
            return new ServiceStatus("SQS", true, queues.IsSuccess, queues.Data?.Count ?? 0);
        }
        catch
        {
            return new ServiceStatus("SQS", true, false, 0);
        }
    }

    private async Task<ServiceStatus> GetSnsStatusAsync()
    {
        try
        {
            var topics = await _snsService.GetTopicsAsync();
            return new ServiceStatus("SNS", true, topics.IsSuccess, topics.Data?.Count ?? 0);
        }
        catch
        {
            return new ServiceStatus("SNS", true, false, 0);
        }
    }

    private async Task<ServiceStatus> GetSecretsManagerStatusAsync()
    {
        try
        {
            var secrets = await _secretsManagerService.GetSecretsAsync();
            return new ServiceStatus("Secrets Manager", true, secrets.IsSuccess, secrets.Data?.Count ?? 0);
        }
        catch
        {
            return new ServiceStatus("Secrets Manager", true, false, 0);
        }
    }
}