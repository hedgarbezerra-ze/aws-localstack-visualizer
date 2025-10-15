using AwsLocalStackVisualizer.Models.Common;
using AwsLocalStackVisualizer.Models.S3;

namespace AwsLocalStackVisualizer.Abstractions;

public interface IS3Service
{
    Task<OperationResult<IReadOnlyList<S3BucketInfo>>> GetBucketsAsync();
    IAsyncEnumerable<S3BucketInfo> GetBucketsStreamAsync(CancellationToken cancellationToken = default);
    Task<OperationResult<S3BucketDetails>> GetBucketDetailsAsync(string bucketName);
    Task<OperationResult<string>> GetObjectContentAsync(string bucketName, string objectKey);
    Task<OperationResult<bool>> TestConnectionAsync();
    Task<OperationResult<bool>> CreateBucketAsync(string bucketName);
    Task<OperationResult<bool>> UploadObjectAsync(string bucketName, string objectKey, string content, string? contentType = null);
    Task<OperationResult<bool>> UploadFileAsync(string bucketName, string objectKey, Stream fileStream, string contentType);
    Task<OperationResult<byte[]>> DownloadObjectAsync(string bucketName, string objectKey);
    Task<OperationResult<bool>> DeleteBucketAsync(string bucketName, bool force = false);
    Task<OperationResult<bool>> DeleteObjectAsync(string bucketName, string objectKey);
}
