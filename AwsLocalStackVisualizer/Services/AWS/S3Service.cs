using Amazon.S3;
using Amazon.S3.Model;
using AwsLocalStackVisualizer.Abstractions;
using AwsLocalStackVisualizer.Configuration;
using AwsLocalStackVisualizer.Models.Common;
using AwsLocalStackVisualizer.Models.S3;
using Microsoft.Extensions.Options;

namespace AwsLocalStackVisualizer.Services.AWS;

public class S3Service : IS3Service
{
    private readonly AmazonS3Client _s3Client;
    private readonly ILogger<S3Service> _logger;
    private readonly INotificationService _notificationService;
    private readonly AwsConfiguration _configuration;

    public S3Service(AmazonS3Client s3Client, ILogger<S3Service> logger, INotificationService notificationService, IOptions<AwsConfiguration> configuration)
    {
        _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
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
            return new OperationResult<bool>(false, false, ex.Message, ex);
        }
    }

    public async Task<OperationResult<IReadOnlyList<S3BucketInfo>>> GetBucketsAsync()
    {
        try
        {
            var response = await _s3Client.ListBucketsAsync();
            
            if (response is not { Buckets: { } bucketsList })
            {
                _logger.LogWarning("Resposta do S3 ListBuckets é nula ou não contém buckets");
                return new OperationResult<IReadOnlyList<S3BucketInfo>>(true, []);
            }

            var buckets = new List<S3BucketInfo>();
            var bucketTasks = new List<Task<S3BucketInfo>>();
            
            foreach (var bucket in bucketsList)
            {
                if (string.IsNullOrWhiteSpace(bucket.BucketName))
                {
                    _logger.LogWarning("Bucket com nome inválido encontrado, ignorando");
                    continue;
                }

                bucketTasks.Add(GetBucketInfoAsync(bucket.BucketName, bucket.CreationDate ?? DateTime.MinValue));
            }

            var bucketResults = await Task.WhenAll(bucketTasks);
            buckets.AddRange(bucketResults);

            return new OperationResult<IReadOnlyList<S3BucketInfo>>(true, buckets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar buckets S3");
            return new OperationResult<IReadOnlyList<S3BucketInfo>>(false, [], ex.Message, ex);
        }
    }

    public async IAsyncEnumerable<S3BucketInfo> GetBucketsStreamAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetBucketsResponseAsync();
        if (response is null)
            yield break;

        var validBuckets = response
            .Where(bucket => !string.IsNullOrWhiteSpace(bucket.BucketName))
            .Select(bucket => GetBucketInfoAsync(bucket.BucketName, bucket.CreationDate ?? DateTime.MinValue))
            .ToList();

        foreach (var bucketTask in validBuckets)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            var bucketInfo = await GetBucketInfoSafelyAsync(bucketTask);
            if (bucketInfo is not null)
            {
                yield return bucketInfo;
            }
        }
    }

    private async Task<List<Amazon.S3.Model.S3Bucket>?> GetBucketsResponseAsync()
    {
        try
        {
            var response = await _s3Client.ListBucketsAsync();
            return response is { Buckets: { } bucketsList } ? bucketsList : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar buckets S3");
            return null;
        }
    }

    private async Task<S3BucketInfo?> GetBucketInfoSafelyAsync(Task<S3BucketInfo> bucketTask)
    {
        try
        {
            return await bucketTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao obter informações do bucket");
            return null;
        }
    }

    public async Task<OperationResult<S3BucketDetails>> GetBucketDetailsAsync(string bucketName)
    {
        try
        {
            var bucketsResult = await GetBucketsAsync();
            if (!bucketsResult.IsSuccess || bucketsResult.Data is null)
                return new OperationResult<S3BucketDetails>(false, null, "Falha ao obter lista de buckets");
                
            var bucketInfo = bucketsResult.Data.FirstOrDefault(b => b.Name == bucketName);
            
            if (bucketInfo is null)
            {
                var defaultBucket = new S3BucketInfo(bucketName, DateTime.MinValue, 0, 0);
                return new OperationResult<S3BucketDetails>(true, new S3BucketDetails(defaultBucket, []));
            }

            var response = await _s3Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = bucketName
            });

            var objects = new List<S3ObjectInfo>();
            
            if (response is { S3Objects: { } objectsList })
            {
                foreach (var obj in objectsList)
                {
                    if (string.IsNullOrWhiteSpace(obj.Key))
                    {
                        _logger.LogWarning("Objeto com chave inválida encontrado no bucket {BucketName}, ignorando", bucketName);
                        continue;
                    }

                    objects.Add(new S3ObjectInfo(
                        obj.Key,
                        obj.Size ?? 0,
                        obj.LastModified ?? DateTime.MinValue,
                        obj.ETag ?? string.Empty,
                        obj.StorageClass?.Value ?? "STANDARD"
                    ));
                }
            }

            return new OperationResult<S3BucketDetails>(true, new S3BucketDetails(bucketInfo, objects));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter detalhes do bucket {BucketName}", bucketName);
            var defaultBucket = new S3BucketInfo(bucketName, DateTime.MinValue, 0, 0);
            return new OperationResult<S3BucketDetails>(false, new S3BucketDetails(defaultBucket, []), ex.Message, ex);
        }
    }

    public async Task<OperationResult<string>> GetObjectContentAsync(string bucketName, string objectKey)
    {
        try
        {
            var response = await _s3Client.GetObjectAsync(bucketName, objectKey);
            
            if (response is not { ResponseStream: { } })
            {
                _logger.LogWarning("Resposta do S3 GetObject é nula ou não contém stream para objeto {ObjectKey} no bucket {BucketName}", objectKey, bucketName);
                return new OperationResult<string>(false, null, "Resposta inválida do S3");
            }

            using var reader = new StreamReader(response.ResponseStream);
            var content = await reader.ReadToEndAsync();
            return new OperationResult<string>(true, content ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter conteúdo do objeto {ObjectKey} no bucket {BucketName}", objectKey, bucketName);
            return new OperationResult<string>(false, null, ex.Message, ex);
        }
    }

    private async Task<S3BucketInfo> GetBucketInfoAsync(string bucketName, DateTime creationDate)
    {
        try
        {
            var response = await _s3Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = bucketName
            });

            var objectCount = response is { S3Objects: { Count: var count } } ? count : 0;
            var totalSize = response is { S3Objects: { } objects } 
                ? objects.Where(obj => obj is not null).Sum(obj => obj.Size ?? 0L) 
                : 0L;

            return new S3BucketInfo(bucketName, creationDate, objectCount, totalSize);
        }
        catch
        {
            return new S3BucketInfo(bucketName, creationDate, 0, 0);
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
            return response is { S3Objects: { Count: var count } } ? count : 0;
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
            return response is { S3Objects: { } objects } 
                ? objects.Where(obj => obj is not null).Sum(obj => obj.Size ?? 0L) 
                : 0L;
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
            var bucktRequest = new PutBucketRequest
            {
                BucketName = bucketName,
                UseClientRegion = true,
                PutBucketConfiguration = new PutBucketConfiguration
                {
                    LocationConstraint = BucketLocationConstraint.FindValue(_configuration.Region)
                }
            };
            
            var response = await _s3Client.PutBucketAsync(bucktRequest);
            
            if (response is null)
            {
                _logger.LogWarning("Resposta do S3 PutBucket é nula para bucket {BucketName}", bucketName);
                return new OperationResult<bool>(false, false, "Resposta inválida do S3");
            }
            
            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK && 
                response.HttpStatusCode != System.Net.HttpStatusCode.Created)
            {
                _logger.LogWarning("Status HTTP inesperado ao criar bucket {BucketName}: {StatusCode}", bucketName, response.HttpStatusCode);
                return new OperationResult<bool>(false, false, $"Status HTTP inesperado: {response.HttpStatusCode}");
            }
            
            _logger.LogInformation("Bucket {BucketName} criado com sucesso", bucketName);
            _notificationService.ShowSuccess($"Bucket '{bucketName}' criado com sucesso!");
            return new OperationResult<bool>(true, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar bucket {BucketName}", bucketName);
            return new OperationResult<bool>(false, false, ex.Message, ex);
        }
    }

    
    public async Task<OperationResult<bool>> UploadObjectAsync(string bucketName, string objectKey, string content, string? contentType = null)
    {
        try
        {
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            
            var request = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey,
                InputStream = stream,
                ContentType = contentType ?? "text/plain"
            };
            
            await _s3Client.PutObjectAsync(request);
            
            _logger.LogInformation("Objeto {ObjectKey} enviado para bucket {BucketName}", objectKey, bucketName);
            _notificationService.ShowSuccess($"Objeto '{objectKey}' enviado com sucesso!");
            return new OperationResult<bool>(true, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar objeto {ObjectKey} para bucket {BucketName}", objectKey, bucketName);
            return new OperationResult<bool>(false, false, ex.Message, ex);
        }
    }

    public async Task<OperationResult<bool>> UploadFileAsync(string bucketName, string objectKey, Stream fileStream, string contentType)
    {
        try
        {
            var request = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey,
                InputStream = fileStream,
                ContentType = contentType
            };
            
            await _s3Client.PutObjectAsync(request);
            
            _logger.LogInformation("Arquivo {ObjectKey} enviado para bucket {BucketName} ({Size} bytes)", objectKey, bucketName, fileStream.Length);
            _notificationService.ShowSuccess($"Arquivo '{objectKey}' enviado com sucesso!");
            return new OperationResult<bool>(true, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar arquivo {ObjectKey} para bucket {BucketName}", objectKey, bucketName);
            return new OperationResult<bool>(false, false, ex.Message, ex);
        }
    }

    public async Task<OperationResult<byte[]>> DownloadObjectAsync(string bucketName, string objectKey)
    {
        try
        {
            var request = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey
            };

            var response = await _s3Client.GetObjectAsync(request);
            
            if (response is not { ResponseStream: { } })
            {
                _logger.LogWarning("Resposta do S3 GetObject é nula ou não contém stream para objeto {ObjectKey} no bucket {BucketName}", objectKey, bucketName);
                return new OperationResult<byte[]>(false, null, "Resposta inválida do S3");
            }

            using var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream);
            var data = memoryStream.ToArray();
            
            _logger.LogInformation("Objeto {ObjectKey} baixado do bucket {BucketName} ({Size} bytes)", objectKey, bucketName, data.Length);
            return new OperationResult<byte[]>(true, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao baixar objeto {ObjectKey} do bucket {BucketName}", objectKey, bucketName);
            return new OperationResult<byte[]>(false, null, ex.Message, ex);
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

                if (objects is { S3Objects: { Count: > 0 } })
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
            return new OperationResult<bool>(false, false, ex.Message, ex);
        }
    }
}
