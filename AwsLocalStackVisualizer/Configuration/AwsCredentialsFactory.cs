using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Microsoft.Extensions.Logging;

namespace AwsLocalStackVisualizer.Configuration;

public static class AwsCredentialsFactory
{
    public static AWSCredentials CreateCredentials(AwsConfiguration config, ILogger logger)
    {
        var credentials = config.Credentials;
        
        logger.LogInformation("Criando credenciais - Tipo: {Type}, AccessKey: {AccessKey}, SecretKey: {SecretKey}", 
            credentials.Type, credentials.AccessKey, credentials.SecretKey);
        
        return credentials.Type switch
        {
            AwsCredentialType.Anonymous => CreateAnonymousCredentials(logger),
            AwsCredentialType.Session => CreateSessionCredentials(credentials, logger),
            AwsCredentialType.Basic => CreateBasicCredentials(credentials, logger),
            _ => CreateAnonymousCredentials(logger)
        };
    }
    
    private static AWSCredentials CreateAnonymousCredentials(ILogger logger)
    {
        logger.LogInformation("Configurando com credenciais anônimas");
        return new AnonymousAWSCredentials();
    }
    
    private static AWSCredentials CreateSessionCredentials(AwsCredentials credentials, ILogger logger)
    {
        if (string.IsNullOrEmpty(credentials.AccessKey) || string.IsNullOrEmpty(credentials.SecretKey))
        {
            logger.LogWarning("Credenciais de sessão incompletas, usando fallback para Anonymous");
            return CreateAnonymousCredentials(logger);
        }
        
        if (string.IsNullOrEmpty(credentials.SessionToken))
        {
            logger.LogWarning("SessionToken não fornecido para credenciais de sessão, usando Basic");
            return new BasicAWSCredentials(credentials.AccessKey, credentials.SecretKey);
        }
        
        logger.LogInformation("Configurando com credenciais de sessão (AccessKey: {AccessKey})", credentials.AccessKey);
        return new SessionAWSCredentials(credentials.AccessKey, credentials.SecretKey, credentials.SessionToken);
    }
    
    private static AWSCredentials CreateBasicCredentials(AwsCredentials credentials, ILogger logger)
    {
        if (string.IsNullOrEmpty(credentials.AccessKey) || string.IsNullOrEmpty(credentials.SecretKey))
        {
            logger.LogWarning("Credenciais básicas incompletas, usando fallback para Anonymous");
            return CreateAnonymousCredentials(logger);
        }
        
        logger.LogInformation("Configurando com credenciais básicas (AccessKey: {AccessKey})", credentials.AccessKey);
        return new BasicAWSCredentials(credentials.AccessKey, credentials.SecretKey);
    }
}
