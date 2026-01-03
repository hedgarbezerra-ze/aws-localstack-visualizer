using AwsLocalStackVisualizer.Configuration;
using AwsLocalStackVisualizer.Abstractions;
using AwsLocalStackVisualizer.Models.Common;
using AwsLocalStackVisualizer.Models.Dashboard;
using AwsLocalStackVisualizer.Models.S3;
using AwsLocalStackVisualizer.Models.SQS;
using AwsLocalStackVisualizer.Models.SNS;
using AwsLocalStackVisualizer.Models.SecretsManager;
using Microsoft.Extensions.Options;

namespace AwsLocalStackVisualizer.Services;


public class LocalStackService : ILocalStackService
{
    private readonly AwsConfiguration _config;
    private readonly ILogger<LocalStackService> _logger;
    private readonly IS3Service _s3Service;
    private readonly ISqsService _sqsService;
    private readonly ISnsService _snsService;
    private readonly ISecretsManagerService _secretsManagerService;
    private readonly ICacheService _cacheService;

    private readonly SemaphoreSlim _s3Semaphore = new(1, 1);
    private readonly SemaphoreSlim _sqsSemaphore = new(1, 1);
    private readonly SemaphoreSlim _snsSemaphore = new(1, 1);
    private readonly SemaphoreSlim _secretsSemaphore = new(1, 1);

    private async Task InvalidateS3CacheAsync()
    {
        await _cacheService.RemoveAsync("s3_buckets");
    }

    private async Task InvalidateSqsCacheAsync()
    {
        await _cacheService.RemoveAsync("sqs_queues");
    }

    private async Task InvalidateSnsCacheAsync()
    {
        await _cacheService.RemoveAsync("sns_topics");
    }

    private async Task InvalidateSecretsCacheAsync()
    {
        await _cacheService.RemoveAsync("secrets_manager");
    }

    public LocalStackService(
        IOptions<AwsConfiguration> config,
        ILogger<LocalStackService> logger,
        IS3Service s3Service,
        ISqsService sqsService,
        ISnsService snsService,
        ISecretsManagerService secretsManagerService,
        ICacheService cacheService)
    {
        _config = config.Value;
        _logger = logger;
        _s3Service = s3Service;
        _sqsService = sqsService;
        _snsService = snsService;
        _secretsManagerService = secretsManagerService;
        _cacheService = cacheService;
    }

    public async Task<OperationResult<bool>> TestConnectionAsync()
    {
        return await _s3Service.TestConnectionAsync();
    }

    public async Task<DashboardServiceData> GetDashboardDataAsync()
    {
        var s3Data = await GetS3ServiceDataAsync();
        var sqsData = await GetSqsServiceDataAsync();
        var snsData = await GetSnsServiceDataAsync();
        var secretsData = await GetSecretsServiceDataAsync();

        return new DashboardServiceData(s3Data, sqsData, snsData, secretsData, DateTime.UtcNow);
    }

    public async IAsyncEnumerable<S3BucketInfo> GetS3BucketsStreamAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var bucket in _s3Service.GetBucketsStreamAsync(cancellationToken))
        {
            yield return bucket;
        }
    }

    public async IAsyncEnumerable<SqsQueueInfo> GetSqsQueuesStreamAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var queue in _sqsService.GetQueuesStreamAsync(cancellationToken))
        {
            yield return queue;
        }
    }

    public async IAsyncEnumerable<SnsTopicInfo> GetSnsTopicsStreamAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var topic in _snsService.GetTopicsStreamAsync(cancellationToken))
        {
            yield return topic;
        }
    }

    public async IAsyncEnumerable<SecretInfo> GetSecretsStreamAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var secret in _secretsManagerService.GetSecretsStreamAsync(cancellationToken))
        {
            yield return secret;
        }
    }

    private async Task<S3ServiceData> GetS3ServiceDataAsync()
    {
        try
        {
            var bucketsResult = await _s3Service.GetBucketsAsync();
            if (!bucketsResult.IsSuccess)
                return new S3ServiceData(false, 0, 0, 0);

            var buckets = bucketsResult.Data ?? new List<S3BucketInfo>();
            var totalObjects = buckets.Sum(b => b.ObjectCount);
            var totalSize = buckets.Sum(b => b.TotalSize);

            return new S3ServiceData(true, buckets.Count, totalObjects, totalSize);
        }
        catch
        {
            return new S3ServiceData(false, 0, 0, 0);
        }
    }

    private async Task<SqsServiceData> GetSqsServiceDataAsync()
    {
        try
        {
            var queuesResult = await _sqsService.GetQueuesAsync();
            if (!queuesResult.IsSuccess)
                return new SqsServiceData(false, 0, 0, 0);

            var queues = queuesResult.Data ?? new List<SqsQueueInfo>();
            var totalMessages = queues.Sum(q => q.ApproximateNumberOfMessages);
            var visibleMessages = queues.Sum(q => q.ApproximateNumberOfMessages - q.ApproximateNumberOfMessagesNotVisible);

            return new SqsServiceData(true, queues.Count, totalMessages, visibleMessages);
        }
        catch
        {
            return new SqsServiceData(false, 0, 0, 0);
        }
    }

    private async Task<SnsServiceData> GetSnsServiceDataAsync()
    {
        try
        {
            var topicsResult = await _snsService.GetTopicsAsync();
            if (!topicsResult.IsSuccess)
                return new SnsServiceData(false, 0, 0);

            var topics = topicsResult.Data ?? new List<SnsTopicInfo>();
            var totalSubscriptions = topics.Sum(t => t.SubscriptionsCount);

            return new SnsServiceData(true, topics.Count, totalSubscriptions);
        }
        catch
        {
            return new SnsServiceData(false, 0, 0);
        }
    }

    private async Task<SecretsServiceData> GetSecretsServiceDataAsync()
    {
        try
        {
            var secretsResult = await _secretsManagerService.GetSecretsAsync();
            if (!secretsResult.IsSuccess)
                return new SecretsServiceData(false, 0);

            var secrets = secretsResult.Data ?? new List<SecretInfo>();
            return new SecretsServiceData(true, secrets.Count);
        }
        catch
        {
            return new SecretsServiceData(false, 0);
        }
    }

    public async Task<OperationResult<IReadOnlyList<S3BucketInfo>>> GetS3BucketsAsync()
    {
        const string cacheKey = "s3_buckets";

        var cachedResult = await _cacheService.GetAsync<OperationResult<IReadOnlyList<S3BucketInfo>>>(cacheKey);
        if (cachedResult != null)
            return cachedResult;

        await _s3Semaphore.WaitAsync();
        try
        {
            var result = await _s3Service.GetBucketsAsync();
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(2));
            return result;
        }
        finally
        {
            _s3Semaphore.Release();
        }
    }

    public Task<OperationResult<S3BucketDetails>> GetS3BucketDetailsAsync(string bucketName) =>
        _s3Service.GetBucketDetailsAsync(bucketName);

    public Task<OperationResult<string>> GetS3ObjectContentAsync(string bucketName, string objectKey) =>
        _s3Service.GetObjectContentAsync(bucketName, objectKey);

    public async Task<OperationResult<IReadOnlyList<SqsQueueInfo>>> GetSqsQueuesAsync()
    {
        const string cacheKey = "sqs_queues";

        await _sqsSemaphore.WaitAsync();
        try
        {
            var result = await _sqsService.GetQueuesAsync();
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(2));
            return result;
        }
        finally
        {
            _sqsSemaphore.Release();
        }
    }

    public Task<OperationResult<SqsQueueDetails>> GetSqsQueueDetailsAsync(string queueName) =>
        _sqsService.GetQueueDetailsAsync(queueName);

    public async Task<OperationResult<IReadOnlyList<SnsTopicInfo>>> GetSnsTopicsAsync()
    {
        const string cacheKey = "sns_topics";

        await _snsSemaphore.WaitAsync();
        try
        {
            var result = await _snsService.GetTopicsAsync();
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(2));
            return result;
        }
        finally
        {
            _snsSemaphore.Release();
        }
    }

    public Task<OperationResult<SnsTopicDetails>> GetSnsTopicDetailsAsync(string topicArn) =>
        _snsService.GetTopicDetailsAsync(topicArn);

    public Task<OperationResult<IReadOnlyList<SnsSubscriptionInfo>>> GetSnsSubscriptionsAsync(string topicArn) =>
        _snsService.GetSubscriptionsAsync(topicArn);

    public Task<OperationResult<IReadOnlyList<SnsMessageInfo>>> GetSnsTopicMessagesAsync(string topicArn) =>
        _snsService.GetTopicMessagesAsync(topicArn);

    public async Task<OperationResult<IReadOnlyList<SecretInfo>>> GetSecretsAsync()
    {
        const string cacheKey = "secrets_manager";

        var cachedResult = await _cacheService.GetAsync<OperationResult<IReadOnlyList<SecretInfo>>>(cacheKey);
        if (cachedResult != null)
            return cachedResult;

        await _secretsSemaphore.WaitAsync();
        try
        {
            var result = await _secretsManagerService.GetSecretsAsync();
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(2));
            return result;
        }
        finally
        {
            _secretsSemaphore.Release();
        }
    }

    public Task<OperationResult<SecretDetails>> GetSecretDetailsAsync(string secretName) =>
        _secretsManagerService.GetSecretDetailsAsync(secretName);

    public Task<OperationResult<SecretValue>> GetSecretValueAsync(string secretName, string? versionId = null) =>
        _secretsManagerService.GetSecretValueAsync(secretName, versionId);

    public async Task<OperationResult<string>> CreateSecretAsync(string name, string secretValue, string? description = null)
    {
        var result = await _secretsManagerService.CreateSecretAsync(name, secretValue, description);
        if (result.IsSuccess)
        {
            await InvalidateSecretsCacheAsync();
        }
        return result;
    }

    public async Task<OperationResult<bool>> UpdateSecretAsync(string name, string secretValue)
    {
        var result = await _secretsManagerService.UpdateSecretAsync(name, secretValue);
        if (result.IsSuccess)
        {
            await InvalidateSecretsCacheAsync();
        }
        return result;
    }

    public async Task<OperationResult<bool>> DeleteSecretAsync(string name, bool forceDelete = false)
    {
        var result = await _secretsManagerService.DeleteSecretAsync(name, forceDelete);
        if (result.IsSuccess)
        {
            await InvalidateSecretsCacheAsync();
        }
        return result;
    }

    public async Task<OperationResult<bool>> CreateS3BucketAsync(string bucketName)
    {
        var result = await _s3Service.CreateBucketAsync(bucketName);
        if (result.IsSuccess)
        {
            await InvalidateS3CacheAsync();
        }
        return result;
    }

    public async Task<OperationResult<bool>> UploadS3ObjectAsync(string bucketName, string objectKey, string content, string? contentType = null)
    {
        var result = await _s3Service.UploadObjectAsync(bucketName, objectKey, content, contentType);
        if (result.IsSuccess)
        {
            await InvalidateS3CacheAsync();
        }
        return result;
    }

    public async Task<OperationResult<bool>> UploadFileAsync(string bucketName, string objectKey, Stream fileStream, string contentType)
    {
        var result = await _s3Service.UploadFileAsync(bucketName, objectKey, fileStream, contentType);
        if (result.IsSuccess)
        {
            await InvalidateS3CacheAsync();
        }
        return result;
    }

    public Task<OperationResult<byte[]>> DownloadObjectAsync(string bucketName, string objectKey) =>
        _s3Service.DownloadObjectAsync(bucketName, objectKey);

    public async Task<OperationResult<bool>> DeleteS3BucketAsync(string bucketName, bool force = false)
    {
        var result = await _s3Service.DeleteBucketAsync(bucketName, force);
        if (result.IsSuccess)
        {
            await InvalidateS3CacheAsync();
        }
        return result;
    }

    public async Task<OperationResult<bool>> DeleteS3ObjectAsync(string bucketName, string objectKey)
    {
        var result = await _s3Service.DeleteObjectAsync(bucketName, objectKey);
        if (result.IsSuccess)
        {
            await InvalidateS3CacheAsync();
        }
        return result;
    }

    public async Task<OperationResult<string>> CreateSqsQueueAsync(string queueName, Dictionary<string, string>? attributes = null)
    {
        var result = await _sqsService.CreateQueueAsync(queueName, attributes);
        if (result.IsSuccess)
        {
            await InvalidateSqsCacheAsync();
        }
        return result;
    }

    public Task<OperationResult<string>> SendSqsMessageAsync(string queueUrl, string messageBody, Dictionary<string, string>? messageAttributes = null) =>
        _sqsService.SendMessageAsync(queueUrl, messageBody, messageAttributes);

    public async Task<OperationResult<bool>> DeleteSqsQueueAsync(string queueUrl)
    {
        var result = await _sqsService.DeleteQueueAsync(queueUrl);
        if (result.IsSuccess)
        {
            await InvalidateSqsCacheAsync();
        }
        return result;
    }

    public Task<OperationResult<bool>> PurgeSqsQueueAsync(string queueUrl) =>
        _sqsService.PurgeQueueAsync(queueUrl);

    public async Task<OperationResult<string>> CreateSnsTopicAsync(string topicName, Dictionary<string, string>? attributes = null)
    {
        var result = await _snsService.CreateTopicAsync(topicName, attributes);
        if (result.IsSuccess)
        {
            await InvalidateSnsCacheAsync();
        }
        return result;
    }

    public Task<OperationResult<string>> PublishSnsMessageAsync(string topicArn, string message, string? subject = null, Dictionary<string, string>? messageAttributes = null) =>
        _snsService.PublishMessageAsync(topicArn, message, subject, messageAttributes);

    public async Task<OperationResult<string>> SubscribeSnsAsync(string topicArn, string protocol, string endpoint, Dictionary<string, string>? attributes = null)
    {
        var result = await _snsService.SubscribeAsync(topicArn, protocol, endpoint, attributes);
        if (result.IsSuccess)
        {
            await InvalidateSnsCacheAsync();
        }
        return result;
    }

    public async Task<OperationResult<bool>> DeleteSnsTopicAsync(string topicArn)
    {
        var result = await _snsService.DeleteTopicAsync(topicArn);
        if (result.IsSuccess)
        {
            await InvalidateSnsCacheAsync();
        }
        return result;
    }

    public async Task<OperationResult<bool>> UnsubscribeSnsAsync(string subscriptionArn)
    {
        var result = await _snsService.UnsubscribeAsync(subscriptionArn);
        if (result.IsSuccess)
        {
            await InvalidateSnsCacheAsync();
        }
        return result;
    }
}
