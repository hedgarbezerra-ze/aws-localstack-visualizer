namespace AwsLocalStackVisualizer.Models.S3;

public record S3ObjectInfo(string Key, long Size, DateTime LastModified, string ETag, string StorageClass);
