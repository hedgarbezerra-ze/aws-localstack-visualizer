using Microsoft.AspNetCore.Diagnostics;
using System.Net;

namespace AwsLocalStackVisualizer.Handlers;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, 
        Exception exception, 
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, 
            "Exceção capturada pelo GlobalExceptionHandler: {Message} | Path: {Path} | Method: {Method} | UserAgent: {UserAgent}", 
            exception.Message,
            httpContext.Request.Path,
            httpContext.Request.Method,
            httpContext.Request.Headers.UserAgent.FirstOrDefault());

        var statusCode = GetStatusCode(exception);
        var problemDetails = new
        {
            Status = statusCode,
            Title = GetTitle(exception),
            Detail = GetDetail(exception),
            Type = GetType(exception),
            Instance = httpContext.Request.Path,
            TraceId = httpContext.TraceIdentifier
        };
        
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        
        return true;
    }

    private static int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            ArgumentNullException or ArgumentException => (int)HttpStatusCode.BadRequest,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            TimeoutException or TaskCanceledException => (int)HttpStatusCode.RequestTimeout,
            HttpRequestException => (int)HttpStatusCode.BadGateway,
            Amazon.Runtime.AmazonServiceException awsEx when awsEx.Message.Contains("Authentication Token") => (int)HttpStatusCode.Unauthorized,
            Amazon.Runtime.AmazonServiceException => (int)HttpStatusCode.BadGateway,
            _ => (int)HttpStatusCode.InternalServerError
        };
    }

    private static string GetTitle(Exception exception)
    {
        return exception switch
        {
            ArgumentNullException => "Parâmetro obrigatório não informado",
            ArgumentException => "Parâmetro inválido",
            UnauthorizedAccessException => "Acesso negado",
            TimeoutException => "Timeout na operação",
            TaskCanceledException => "Operação cancelada",
            HttpRequestException => "Erro de comunicação",
            Amazon.Runtime.AmazonServiceException awsEx when awsEx.Message.Contains("Authentication Token") => "Erro de autenticação AWS",
            Amazon.Runtime.AmazonServiceException => "Erro do serviço AWS",
            _ => "Erro interno do servidor"
        };
    }

    private static string GetDetail(Exception exception)
    {
        return exception switch
        {
            ArgumentNullException nullEx => $"O parâmetro '{nullEx.ParamName}' é obrigatório",
            ArgumentException argEx => argEx.Message,
            UnauthorizedAccessException => "Você não tem permissão para acessar este recurso",
            TimeoutException => "A operação excedeu o tempo limite permitido",
            TaskCanceledException => "A operação foi cancelada devido ao timeout",
            HttpRequestException httpEx => $"Falha na comunicação com o serviço externo: {httpEx.Message}",
            Amazon.Runtime.AmazonServiceException awsEx when awsEx.Message.Contains("Authentication Token") => "Falha na autenticação com LocalStack. Verifique as credenciais AWS configuradas.",
            Amazon.Runtime.AmazonServiceException awsEx => $"Erro no serviço AWS: {awsEx.Message}",
            _ => "Ocorreu um erro inesperado. Tente novamente mais tarde"
        };
    }

    private static string GetType(Exception exception)
    {
        return exception switch
        {
            ArgumentNullException or ArgumentException => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            UnauthorizedAccessException => "https://tools.ietf.org/html/rfc7235#section-3.1",
            TimeoutException or TaskCanceledException => "https://tools.ietf.org/html/rfc7231#section-6.5.7",
            HttpRequestException => "https://tools.ietf.org/html/rfc7231#section-6.6.3",
            Amazon.Runtime.AmazonServiceException awsEx when awsEx.Message.Contains("Authentication Token") => "https://tools.ietf.org/html/rfc7235#section-3.1",
            Amazon.Runtime.AmazonServiceException => "https://tools.ietf.org/html/rfc7231#section-6.6.3",
            _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };
    }
}
