namespace AwsLocalStackVisualizer.Models.SQS;

public record SqsQueueInfo(string Name, string Url, int ApproximateNumberOfMessages, 
    int ApproximateNumberOfMessagesNotVisible, DateTime CreatedTimestamp);
