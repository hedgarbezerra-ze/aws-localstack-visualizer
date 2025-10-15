namespace AwsLocalStackVisualizer.Models.SNS;

public record SnsTopicDetails(SnsTopicInfo TopicInfo, IReadOnlyList<SnsSubscriptionInfo> Subscriptions);
