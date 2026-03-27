using AwsLocalStackVisualizer.Models.Common;
using AwsLocalStackVisualizer.Models.SQS;

namespace AwsLocalStackVisualizer.Abstractions;

public interface ISqsService
{
    Task<OperationResult<IReadOnlyList<SqsQueueInfo>>> GetQueuesAsync();
    IAsyncEnumerable<SqsQueueInfo> GetQueuesStreamAsync(CancellationToken cancellationToken = default);

    Task<OperationResult<SqsQueueDetails>> GetQueueDetailsAsync(string queueName);

    Task<OperationResult<string>> CreateQueueAsync(string queueName, Dictionary<string, string>? attributes = null);

    Task<OperationResult<string>> SendMessageAsync(string queueUrl, string messageBody, Dictionary<string, string>? messageAttributes = null);

    Task<OperationResult<bool>> DeleteQueueAsync(string queueUrl);

    Task<OperationResult<bool>> PurgeQueueAsync(string queueUrl);

    Task<OperationResult<string>> GetQueueArnAsync(string queueUrl);

    Task<OperationResult<bool>> EnsureQueuePolicyAllowsSnsAsync(string queueUrl, string queueArn, string topicArn);
}
