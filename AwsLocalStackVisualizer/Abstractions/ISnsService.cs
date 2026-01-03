using AwsLocalStackVisualizer.Models.Common;
using AwsLocalStackVisualizer.Models.SNS;

namespace AwsLocalStackVisualizer.Abstractions;

public interface ISnsService
{
    Task<OperationResult<IReadOnlyList<SnsTopicInfo>>> GetTopicsAsync();
    IAsyncEnumerable<SnsTopicInfo> GetTopicsStreamAsync(CancellationToken cancellationToken = default);

    Task<OperationResult<SnsTopicDetails>> GetTopicDetailsAsync(string topicArn);

    Task<OperationResult<IReadOnlyList<SnsSubscriptionInfo>>> GetSubscriptionsAsync(string topicArn);

    Task<OperationResult<IReadOnlyList<SnsMessageInfo>>> GetTopicMessagesAsync(string topicArn);

    Task<OperationResult<string>> CreateTopicAsync(string topicName, Dictionary<string, string>? attributes = null);

    Task<OperationResult<string>> PublishMessageAsync(string topicArn, string message, string? subject = null, Dictionary<string, string>? messageAttributes = null);

    Task<OperationResult<string>> SubscribeAsync(string topicArn, string protocol, string endpoint, Dictionary<string, string>? attributes = null);

    Task<OperationResult<bool>> DeleteTopicAsync(string topicArn);

    Task<OperationResult<bool>> UnsubscribeAsync(string subscriptionArn);
}
