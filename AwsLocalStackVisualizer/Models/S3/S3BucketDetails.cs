namespace AwsLocalStackVisualizer.Models.S3;

public record S3BucketDetails(S3BucketInfo BucketInfo, IReadOnlyList<S3ObjectInfo> Objects);
