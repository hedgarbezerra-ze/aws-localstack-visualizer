using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;

namespace AwsLocalStackVisualizer.Extensions
{
    public static class LocalStackExtensions
    {
        private static readonly ILoggerFactory LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
            builder.AddConsole());
        
        private static readonly ILogger Logger = LoggerFactory.CreateLogger("LocalStack.Extensions");
        
        public static async Task EnsureBucketExistsAsync(this IAmazonS3 s3Client, string bucketName)
        {
            try
            {
                // Verifica se o bucket já existe
                var response = await s3Client.ListBucketsAsync();
                var bucketExists = response.Buckets.Any(b => b.BucketName == bucketName);
                
                if (!bucketExists)
                {
                    // Cria o bucket se não existir
                    await s3Client.PutBucketAsync(new PutBucketRequest
                    {
                        BucketName = bucketName
                    });
                    Logger.LogInformation("Bucket {BucketName} criado com sucesso", bucketName);
                }
                else
                {
                    Logger.LogDebug("Bucket {BucketName} já existe", bucketName);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Erro ao verificar/criar bucket {BucketName}", bucketName);
            }
        }
        
        public static bool IsDevelopment()
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            return env == "Development";
        }
    }
}
