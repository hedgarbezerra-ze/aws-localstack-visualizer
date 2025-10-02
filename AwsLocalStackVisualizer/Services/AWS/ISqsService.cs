using AwsLocalStackVisualizer.Models;

namespace AwsLocalStackVisualizer.Services.AWS;

public interface ISqsService
{
    // Read operations
    Task<OperationResult<IReadOnlyList<SqsQueueInfo>>> GetQueuesAsync();
    Task<OperationResult<SqsQueueDetails>> GetQueueDetailsAsync(string queueName);
    
    // Create operations
    Task<OperationResult<string>> CreateQueueAsync(string queueName, Dictionary<string, string>? attributes = null);
    Task<OperationResult<string>> SendMessageAsync(string queueUrl, string messageBody, Dictionary<string, string>? messageAttributes = null);
    Task<OperationResult<bool>> DeleteQueueAsync(string queueUrl);
    Task<OperationResult<bool>> PurgeQueueAsync(string queueUrl);
}
