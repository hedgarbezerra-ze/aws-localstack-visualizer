using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
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
        _logger.LogError(exception, "Ocorreu uma exceção não tratada: {Message}", exception.Message);

        var problemDetails = CreateProblemDetails(httpContext, exception);
        
        httpContext.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        
        return true;
    }

    private static ProblemDetails CreateProblemDetails(HttpContext context, Exception exception)
    {
        return exception switch
        {
            ArgumentNullException nullEx => new ProblemDetails
            {
                Status = (int)HttpStatusCode.BadRequest,
                Title = "Parâmetro obrigatório não informado",
                Detail = $"O parâmetro '{nullEx.ParamName}' é obrigatório",
                Instance = context.Request.Path,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            },
            
            ArgumentException argEx => new ProblemDetails
            {
                Status = (int)HttpStatusCode.BadRequest,
                Title = "Parâmetro inválido",
                Detail = argEx.Message,
                Instance = context.Request.Path,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            },
            
            UnauthorizedAccessException => new ProblemDetails
            {
                Status = (int)HttpStatusCode.Unauthorized,
                Title = "Acesso negado",
                Detail = "Você não tem permissão para acessar este recurso",
                Instance = context.Request.Path,
                Type = "https://tools.ietf.org/html/rfc7235#section-3.1"
            },
            
            TimeoutException => new ProblemDetails
            {
                Status = (int)HttpStatusCode.RequestTimeout,
                Title = "Timeout na operação",
                Detail = "A operação excedeu o tempo limite permitido",
                Instance = context.Request.Path,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.7"
            },
            
            HttpRequestException httpEx => new ProblemDetails
            {
                Status = (int)HttpStatusCode.BadGateway,
                Title = "Erro de comunicação",
                Detail = $"Falha na comunicação com o serviço externo: {httpEx.Message}",
                Instance = context.Request.Path,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.3"
            },
            
            Amazon.Runtime.AmazonServiceException awsEx when awsEx.Message.Contains("Authentication Token") => new ProblemDetails
            {
                Status = (int)HttpStatusCode.Unauthorized,
                Title = "Erro de autenticação AWS",
                Detail = "Falha na autenticação com LocalStack. Verifique as credenciais AWS configuradas.",
                Instance = context.Request.Path,
                Type = "https://tools.ietf.org/html/rfc7235#section-3.1"
            },
            
            Amazon.Runtime.AmazonServiceException awsEx => new ProblemDetails
            {
                Status = (int)HttpStatusCode.BadGateway,
                Title = "Erro do serviço AWS",
                Detail = $"Erro no serviço AWS: {awsEx.Message}",
                Instance = context.Request.Path,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.3"
            },
            
            TaskCanceledException => new ProblemDetails
            {
                Status = (int)HttpStatusCode.RequestTimeout,
                Title = "Operação cancelada",
                Detail = "A operação foi cancelada devido ao timeout",
                Instance = context.Request.Path,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.7"
            },
            
            _ => new ProblemDetails
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Title = "Erro interno do servidor",
                Detail = "Ocorreu um erro inesperado. Tente novamente mais tarde",
                Instance = context.Request.Path,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            }
        };
    }
}
