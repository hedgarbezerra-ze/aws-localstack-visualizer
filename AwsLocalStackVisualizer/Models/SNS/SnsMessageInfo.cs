namespace AwsLocalStackVisualizer.Models.SNS;

public record SnsMessageInfo(
    string MessageId,
    string TopicArn,
    string Subject,
    string Message,
    DateTime Timestamp,
    Dictionary<string, string> MessageAttributes);

