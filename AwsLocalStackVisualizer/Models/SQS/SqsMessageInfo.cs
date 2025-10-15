namespace AwsLocalStackVisualizer.Models.SQS;

public record SqsMessageInfo(string MessageId, string Body, string ReceiptHandle, 
    DateTime SentTimestamp, int ReceiveCount, Dictionary<string, string> Attributes);
