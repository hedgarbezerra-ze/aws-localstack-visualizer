using AwsLocalStackVisualizer.Models;

namespace AwsLocalStackVisualizer.Services.AWS;

public interface IS3Service
{
    // Read operations
    Task<OperationResult<IReadOnlyList<S3BucketInfo>>> GetBucketsAsync();
    Task<OperationResult<S3BucketDetails>> GetBucketDetailsAsync(string bucketName);
    Task<OperationResult<string>> GetObjectContentAsync(string bucketName, string objectKey);
    Task<OperationResult<bool>> TestConnectionAsync();
    
    // Create operations
    Task<OperationResult<bool>> CreateBucketAsync(string bucketName);
    Task<OperationResult<bool>> UploadObjectAsync(string bucketName, string objectKey, string content, string? contentType = null);
    Task<OperationResult<bool>> DeleteBucketAsync(string bucketName, bool force = false);
    Task<OperationResult<bool>> DeleteObjectAsync(string bucketName, string objectKey);
}
