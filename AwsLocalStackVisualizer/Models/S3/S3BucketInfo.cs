namespace AwsLocalStackVisualizer.Models.S3;

public record S3BucketInfo(string Name, DateTime CreationDate, long ObjectCount, long TotalSize);
