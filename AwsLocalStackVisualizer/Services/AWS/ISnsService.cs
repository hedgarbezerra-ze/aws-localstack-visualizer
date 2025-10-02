using AwsLocalStackVisualizer.Models;

namespace AwsLocalStackVisualizer.Services.AWS;

public interface ISnsService
{
    // Read operations
    Task<OperationResult<IReadOnlyList<SnsTopicInfo>>> GetTopicsAsync();
    Task<OperationResult<SnsTopicDetails>> GetTopicDetailsAsync(string topicArn);
    
    // Create operations
    Task<OperationResult<string>> CreateTopicAsync(string topicName, Dictionary<string, string>? attributes = null);
    Task<OperationResult<string>> PublishMessageAsync(string topicArn, string message, string? subject = null, Dictionary<string, string>? messageAttributes = null);
    Task<OperationResult<string>> SubscribeAsync(string topicArn, string protocol, string endpoint, Dictionary<string, string>? attributes = null);
    Task<OperationResult<bool>> DeleteTopicAsync(string topicArn);
    Task<OperationResult<bool>> UnsubscribeAsync(string subscriptionArn);
}
