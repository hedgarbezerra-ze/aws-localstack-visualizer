using Amazon.S3;
using Amazon.S3.Model;
using AwsLocalStackVisualizer.Models;

namespace AwsLocalStackVisualizer.Services.AWS;

public class S3Service : IS3Service
{
    private readonly AmazonS3Client _s3Client;
    private readonly ILogger<S3Service> _logger;
    private readonly INotificationService _notificationService;

    public S3Service(AmazonS3Client s3Client, ILogger<S3Service> logger, INotificationService notificationService)
    {
        _s3Client = s3Client;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<OperationResult<bool>> TestConnectionAsync()
    {
        try
        {
            await _s3Client.ListBucketsAsync();
            return new OperationResult<bool>(true, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao conectar com S3");
            _notificationService.ShowError($"Falha na conexão S3: {ex.Message}", "Erro de Conexão");
            return new OperationResult<bool>(false, false, ex.Message, ex);
        }
    }

    public async Task<OperationResult<IReadOnlyList<S3BucketInfo>>> GetBucketsAsync()
    {
        try
        {
            var response = await _s3Client.ListBucketsAsync();
            var buckets = new List<S3BucketInfo>();

            foreach (var bucket in response.Buckets)
            {
                var objectCount = await GetBucketObjectCountAsync(bucket.BucketName);
                var totalSize = await GetBucketTotalSizeAsync(bucket.BucketName);
                
                buckets.Add(new S3BucketInfo(
                    bucket.BucketName,
                    bucket.CreationDate ?? DateTime.MinValue,
                    objectCount,
                    totalSize
                ));
            }

            return new OperationResult<IReadOnlyList<S3BucketInfo>>(true, buckets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar buckets S3");
            _notificationService.ShowError($"Erro ao carregar buckets S3: {ex.Message}");
            return new OperationResult<IReadOnlyList<S3BucketInfo>>(false, Array.Empty<S3BucketInfo>(), ex.Message, ex);
        }
    }

    public async Task<OperationResult<S3BucketDetails>> GetBucketDetailsAsync(string bucketName)
    {
        try
        {
            var bucketsResult = await GetBucketsAsync();
            if (!bucketsResult.IsSuccess || bucketsResult.Data == null)
                return new OperationResult<S3BucketDetails>(false, null, "Falha ao obter lista de buckets");
                
            var bucketInfo = bucketsResult.Data.FirstOrDefault(b => b.Name == bucketName);
            
            if (bucketInfo == null)
            {
                var defaultBucket = new S3BucketInfo(bucketName, DateTime.MinValue, 0, 0);
                return new OperationResult<S3BucketDetails>(true, new S3BucketDetails(defaultBucket, Array.Empty<S3ObjectInfo>()));
            }

            var response = await _s3Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = bucketName
            });

            var objects = response.S3Objects.Select(obj => new S3ObjectInfo(
                obj.Key,
                obj.Size ?? 0,
                obj.LastModified ?? DateTime.MinValue,
                obj.ETag,
                obj.StorageClass?.Value ?? "STANDARD"
            )).ToList();

            return new OperationResult<S3BucketDetails>(true, new S3BucketDetails(bucketInfo, objects));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter detalhes do bucket {BucketName}", bucketName);
            _notificationService.ShowError($"Erro ao carregar detalhes do bucket: {ex.Message}");
            var defaultBucket = new S3BucketInfo(bucketName, DateTime.MinValue, 0, 0);
            return new OperationResult<S3BucketDetails>(false, new S3BucketDetails(defaultBucket, Array.Empty<S3ObjectInfo>()), ex.Message, ex);
        }
    }

    public async Task<OperationResult<string>> GetObjectContentAsync(string bucketName, string objectKey)
    {
        try
        {
            var response = await _s3Client.GetObjectAsync(bucketName, objectKey);
            using var reader = new StreamReader(response.ResponseStream);
            var content = await reader.ReadToEndAsync();
            return new OperationResult<string>(true, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter conteúdo do objeto {ObjectKey} no bucket {BucketName}", objectKey, bucketName);
            _notificationService.ShowError($"Erro ao carregar objeto: {ex.Message}");
            return new OperationResult<string>(false, null, ex.Message, ex);
        }
    }

    private async Task<long> GetBucketObjectCountAsync(string bucketName)
    {
        try
        {
            var response = await _s3Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = bucketName
            });
            return response.S3Objects.Count;
        }
        catch
        {
            return 0;
        }
    }

    private async Task<long> GetBucketTotalSizeAsync(string bucketName)
    {
        try
        {
            var response = await _s3Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = bucketName
            });
            return response.S3Objects.Sum(obj => obj.Size ?? 0L);
        }
        catch
        {
            return 0;
        }
    }

    public async Task<OperationResult<bool>> CreateBucketAsync(string bucketName)
    {
        try
        {
            await _s3Client.PutBucketAsync(new PutBucketRequest
            {
                BucketName = bucketName
            });
            
            _logger.LogInformation("Bucket {BucketName} criado com sucesso", bucketName);
            _notificationService.ShowSuccess($"Bucket '{bucketName}' criado com sucesso!");
            return new OperationResult<bool>(true, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar bucket {BucketName}", bucketName);
            _notificationService.ShowError($"Erro ao criar bucket: {ex.Message}");
            return new OperationResult<bool>(false, false, ex.Message, ex);
        }
    }

    public async Task<OperationResult<bool>> UploadObjectAsync(string bucketName, string objectKey, string content, string? contentType = null)
    {
        try
        {
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            
            await _s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey,
                InputStream = stream,
                ContentType = contentType ?? "text/plain"
            });
            
            _logger.LogInformation("Objeto {ObjectKey} enviado para bucket {BucketName}", objectKey, bucketName);
            _notificationService.ShowSuccess($"Objeto '{objectKey}' enviado com sucesso!");
            return new OperationResult<bool>(true, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar objeto {ObjectKey} para bucket {BucketName}", objectKey, bucketName);
            _notificationService.ShowError($"Erro ao enviar objeto: {ex.Message}");
            return new OperationResult<bool>(false, false, ex.Message, ex);
        }
    }

    public async Task<OperationResult<bool>> DeleteBucketAsync(string bucketName, bool force = false)
    {
        try
        {
            if (force)
            {
                var objects = await _s3Client.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = bucketName
                });

                if (objects.S3Objects.Any())
                {
                    var deleteRequest = new DeleteObjectsRequest
                    {
                        BucketName = bucketName,
                        Objects = objects.S3Objects.Select(obj => new KeyVersion { Key = obj.Key }).ToList()
                    };
                    await _s3Client.DeleteObjectsAsync(deleteRequest);
                }
            }

            await _s3Client.DeleteBucketAsync(bucketName);
            
            _logger.LogInformation("Bucket {BucketName} excluído com sucesso", bucketName);
            _notificationService.ShowSuccess($"Bucket '{bucketName}' excluído com sucesso!");
            return new OperationResult<bool>(true, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao excluir bucket {BucketName}", bucketName);
            _notificationService.ShowError($"Erro ao excluir bucket: {ex.Message}");
            return new OperationResult<bool>(false, false, ex.Message, ex);
        }
    }

    public async Task<OperationResult<bool>> DeleteObjectAsync(string bucketName, string objectKey)
    {
        try
        {
            await _s3Client.DeleteObjectAsync(bucketName, objectKey);
            
            _logger.LogInformation("Objeto {ObjectKey} excluído do bucket {BucketName}", objectKey, bucketName);
            _notificationService.ShowSuccess($"Objeto '{objectKey}' excluído com sucesso!");
            return new OperationResult<bool>(true, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao excluir objeto {ObjectKey} do bucket {BucketName}", objectKey, bucketName);
            _notificationService.ShowError($"Erro ao excluir objeto: {ex.Message}");
            return new OperationResult<bool>(false, false, ex.Message, ex);
        }
    }
}
