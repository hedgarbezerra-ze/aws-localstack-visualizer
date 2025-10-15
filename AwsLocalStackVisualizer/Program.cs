using Amazon.S3;
using Amazon.SQS;
using Amazon.SimpleNotificationService;
using Amazon.SecretsManager;
using AwsLocalStackVisualizer.Abstractions;
using AwsLocalStackVisualizer.Components;
using AwsLocalStackVisualizer.Configuration;
using AwsLocalStackVisualizer.Services;
using AwsLocalStackVisualizer.Services.AWS;
using AwsLocalStackVisualizer.Handlers;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.Configure<AwsConfiguration>(
    builder.Configuration.GetSection("AWS"));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var awsConfig = builder.Configuration.GetSection("AWS").Get<AwsConfiguration>() ?? new AwsConfiguration();

using var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog());
var logger = loggerFactory.CreateLogger("AWS.Configuration");

var credentials = AwsCredentialsFactory.CreateCredentials(awsConfig, logger);

builder.Services.AddSingleton(credentials);

builder.Services.AddSingleton<AmazonS3Client>(provider =>
{
    var s3Client = AwsClientFactory.CreateS3Client(credentials, awsConfig);
    logger.LogInformation("S3 Client configurado para {Environment}", awsConfig.UseLocalStack ? "LocalStack" : "AWS Real");
    return s3Client;
});

builder.Services.AddSingleton<AmazonSQSClient>(provider =>
{
    var sqsClient = AwsClientFactory.CreateSqsClient(credentials, awsConfig);
    logger.LogInformation("SQS Client configurado para {Environment}", awsConfig.UseLocalStack ? "LocalStack" : "AWS Real");
    return sqsClient;
});

builder.Services.AddSingleton<AmazonSimpleNotificationServiceClient>(provider =>
{
    var snsClient = AwsClientFactory.CreateSnsClient(credentials, awsConfig);
    logger.LogInformation("SNS Client configurado para {Environment}", awsConfig.UseLocalStack ? "LocalStack" : "AWS Real");
    return snsClient;
});

builder.Services.AddSingleton<AmazonSecretsManagerClient>(provider =>
{
    var secretsClient = AwsClientFactory.CreateSecretsManagerClient(credentials, awsConfig);
    logger.LogInformation("SecretsManager Client configurado para {Environment}", awsConfig.UseLocalStack ? "LocalStack" : "AWS Real");
    return secretsClient;
});

builder.Services.AddScoped<IS3Service, S3Service>();
builder.Services.AddScoped<ISqsService, SqsService>();
builder.Services.AddScoped<ISnsService, SnsService>();
builder.Services.AddScoped<ISecretsManagerService, SecretsManagerService>();

builder.Services.AddScoped<ILocalStackService, LocalStackService>();

builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IReadOnlyService, ReadOnlyService>();
builder.Services.AddSingleton<ICacheService, CacheService>();

builder.Services.AddHealthChecks();

builder.WebHost.UseStaticWebAssets();

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
app.UseStaticFiles();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapHealthChecks("/health");

app.Run();