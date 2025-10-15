namespace AwsLocalStackVisualizer.Models.SNS;

public record SnsTopicInfo(string Name, string Arn, int SubscriptionsCount, DateTime CreatedDate);
