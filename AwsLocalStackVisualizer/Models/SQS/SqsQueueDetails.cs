namespace AwsLocalStackVisualizer.Models.SQS;

public record SqsQueueDetails(SqsQueueInfo QueueInfo, IReadOnlyList<SqsMessageInfo> Messages);
