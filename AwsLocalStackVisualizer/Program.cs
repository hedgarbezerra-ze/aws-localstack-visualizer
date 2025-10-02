using Amazon;
using Amazon.S3;
using Amazon.SQS;
using Amazon.SimpleNotificationService;
using Amazon.SecretsManager;
using Amazon.Runtime;
using AwsLocalStackVisualizer.Components;
using AwsLocalStackVisualizer.Configuration;
using AwsLocalStackVisualizer.Services;
using AwsLocalStackVisualizer.Services.AWS;
using AwsLocalStackVisualizer.Extensions;
using AwsLocalStackVisualizer.Handlers;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.Configure<LocalStackConfiguration>(
    builder.Configuration.GetSection("LocalStack"));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var localStackConfig = builder.Configuration.GetSection("LocalStack").Get<LocalStackConfiguration>() ?? new LocalStackConfiguration();

using var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog());
var logger = loggerFactory.CreateLogger("LocalStack.Configuration");

AWSCredentials credentials;
if (localStackConfig.UseAnonymousCredentials)
{
    credentials = new AnonymousAWSCredentials();
    logger.LogInformation("Configurando LocalStack com credenciais anônimas");
}
else
{
    // Usar credenciais "test" conforme documentação do LocalStack
    credentials = new BasicAWSCredentials("test", "test");
    logger.LogInformation("Configurando LocalStack com credenciais básicas (test/test)");
}

builder.Services.AddSingleton(credentials);
builder.Services.AddSingleton<AmazonS3Client>(provider =>
{
    // Configuração S3 seguindo documentação oficial do LocalStack
    // https://docs.localstack.cloud/aws/integrations/aws-sdks/net/#s3-specific-endpoint
    var config = new AmazonS3Config
    {
        ServiceURL = localStackConfig.ServiceUrl,
        ForcePathStyle = true,
        UseHttp = localStackConfig.ServiceUrl.StartsWith("http://"),
        AuthenticationRegion = localStackConfig.Region
    };
    var s3Client = new AmazonS3Client(credentials, config);
    
    // Garantir que buckets essenciais existam (apenas em desenvolvimento)
    if (LocalStackExtensions.IsDevelopment())
    {
        Task.Run(async () =>
        {
            var s3Logger = loggerFactory.CreateLogger("LocalStack.S3.Initialization");
            s3Logger.LogInformation("Inicializando buckets essenciais para desenvolvimento");
            
            await s3Client.EnsureBucketExistsAsync("sample-bucket");
            await s3Client.EnsureBucketExistsAsync("logs-bucket");
            await s3Client.EnsureBucketExistsAsync("uploads-bucket");
            
            s3Logger.LogInformation("Inicialização de buckets concluída");
        });
    }
    
    return s3Client;
});

builder.Services.AddSingleton<AmazonSQSClient>(provider =>
{
    var config = new AmazonSQSConfig
    {
        ServiceURL = localStackConfig.ServiceUrl,
        UseHttp = localStackConfig.ServiceUrl.StartsWith("http://"),
        AuthenticationRegion = localStackConfig.Region
    };
    return new AmazonSQSClient(credentials, config);
});

builder.Services.AddSingleton<AmazonSimpleNotificationServiceClient>(provider =>
{
    var config = new AmazonSimpleNotificationServiceConfig
    {
        ServiceURL = localStackConfig.ServiceUrl,
        UseHttp = localStackConfig.ServiceUrl.StartsWith("http://"),
        AuthenticationRegion = localStackConfig.Region
    };
    return new AmazonSimpleNotificationServiceClient(credentials, config);
});

builder.Services.AddSingleton<AmazonSecretsManagerClient>(provider =>
{
    var config = new AmazonSecretsManagerConfig
    {
        ServiceURL = localStackConfig.ServiceUrl,
        UseHttp = localStackConfig.ServiceUrl.StartsWith("http://"),
        AuthenticationRegion = localStackConfig.Region
    };
    return new AmazonSecretsManagerClient(credentials, config);
});

builder.Services.AddScoped<IS3Service, S3Service>();
builder.Services.AddScoped<ISqsService, SqsService>();
builder.Services.AddScoped<ISnsService, SnsService>();
builder.Services.AddScoped<ISecretsManagerService, SecretsManagerService>();

builder.Services.AddScoped<ILocalStackService, LocalStackService>();

builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configurar Serilog request logging
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.FirstOrDefault());
        diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress?.ToString());
    };
});

app.UseExceptionHandler();

if (!app.Environment.IsDevelopment())
{
     app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapHealthChecks("/health");

app.Run();
