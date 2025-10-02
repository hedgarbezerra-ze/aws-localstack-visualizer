using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using AwsLocalStackVisualizer.Models;

namespace AwsLocalStackVisualizer.Services.AWS;

public class SecretsManagerService : ISecretsManagerService
{
    private readonly AmazonSecretsManagerClient _secretsManagerClient;
    private readonly ILogger<SecretsManagerService> _logger;
    private readonly INotificationService _notificationService;

    public SecretsManagerService(AmazonSecretsManagerClient secretsManagerClient, ILogger<SecretsManagerService> logger, INotificationService notificationService)
    {
        _secretsManagerClient = secretsManagerClient;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<OperationResult<IReadOnlyList<SecretInfo>>> GetSecretsAsync()
    {
        try
        {
            var response = await _secretsManagerClient.ListSecretsAsync(new ListSecretsRequest());
            var secrets = new List<SecretInfo>();

            foreach (var secret in response.SecretList)
            {
                secrets.Add(new SecretInfo(
                    secret.Name,
                    secret.ARN,
                    secret.CreatedDate ?? DateTime.MinValue,
                    secret.LastChangedDate,
                    secret.Description,
                    secret.Tags?.ToDictionary(t => t.Key, t => t.Value) ?? new Dictionary<string, string>()
                ));
            }

            return new OperationResult<IReadOnlyList<SecretInfo>>(true, secrets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar secrets");
            _notificationService.ShowError($"Erro ao carregar secrets: {ex.Message}");
            return new OperationResult<IReadOnlyList<SecretInfo>>(false, Array.Empty<SecretInfo>(), ex.Message, ex);
        }
    }

    public async Task<OperationResult<SecretDetails>> GetSecretDetailsAsync(string secretName)
    {
        try
        {
            var secretsResult = await GetSecretsAsync();
            if (!secretsResult.IsSuccess || secretsResult.Data == null)
                return new OperationResult<SecretDetails>(false, null, "Falha ao obter lista de secrets");

            var secretInfo = secretsResult.Data.FirstOrDefault(s => s.Name == secretName);
            if (secretInfo == null)
                return new OperationResult<SecretDetails>(false, null, $"Secret '{secretName}' não encontrado");

            var versionsResponse = await _secretsManagerClient.ListSecretVersionIdsAsync(new ListSecretVersionIdsRequest
            {
                SecretId = secretName
            });

            var versions = versionsResponse.Versions.Select(v => new SecretVersionInfo(
                v.VersionId,
                v.CreatedDate ?? DateTime.MinValue,
                v.VersionStages?.Contains("AWSCURRENT") ?? false,
                v.VersionStages?.ToList() ?? new List<string>()
            )).ToList();

            var details = new SecretDetails(secretInfo, versions);
            return new OperationResult<SecretDetails>(true, details);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter detalhes do secret {SecretName}", secretName);
            _notificationService.ShowError($"Erro ao carregar detalhes do secret: {ex.Message}");
            return new OperationResult<SecretDetails>(false, null, ex.Message, ex);
        }
    }

    public async Task<OperationResult<SecretValue>> GetSecretValueAsync(string secretName, string? versionId = null)
    {
        try
        {
            var request = new GetSecretValueRequest
            {
                SecretId = secretName
            };
            
            if (!string.IsNullOrEmpty(versionId))
                request.VersionId = versionId;

            var response = await _secretsManagerClient.GetSecretValueAsync(request);
            
            var secretValue = new SecretValue(
                response.Name,
                response.SecretString,
                response.SecretBinary?.ToArray(),
                response.VersionId,
                response.CreatedDate ?? DateTime.MinValue
            );

            return new OperationResult<SecretValue>(true, secretValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter valor do secret {SecretName}", secretName);
            _notificationService.ShowError($"Erro ao obter valor do secret: {ex.Message}");
            return new OperationResult<SecretValue>(false, null, ex.Message, ex);
        }
    }

    public async Task<OperationResult<string>> CreateSecretAsync(string name, string secretValue, string? description = null)
    {
        try
        {
            var request = new CreateSecretRequest
            {
                Name = name,
                SecretString = secretValue,
                Description = description
            };

            var response = await _secretsManagerClient.CreateSecretAsync(request);
            _notificationService.ShowSuccess($"Secret '{name}' criado com sucesso");
            return new OperationResult<string>(true, response.ARN);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar secret {SecretName}", name);
            _notificationService.ShowError($"Erro ao criar secret: {ex.Message}");
            return new OperationResult<string>(false, null, ex.Message, ex);
        }
    }

    public async Task<OperationResult<bool>> UpdateSecretAsync(string name, string secretValue)
    {
        try
        {
            var request = new UpdateSecretRequest
            {
                SecretId = name,
                SecretString = secretValue
            };

            await _secretsManagerClient.UpdateSecretAsync(request);
            _notificationService.ShowSuccess($"Secret '{name}' atualizado com sucesso");
            return new OperationResult<bool>(true, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar secret {SecretName}", name);
            _notificationService.ShowError($"Erro ao atualizar secret: {ex.Message}");
            return new OperationResult<bool>(false, false, ex.Message, ex);
        }
    }

    public async Task<OperationResult<bool>> DeleteSecretAsync(string name, bool forceDelete = false)
    {
        try
        {
            var request = new DeleteSecretRequest
            {
                SecretId = name,
                ForceDeleteWithoutRecovery = forceDelete
            };

            await _secretsManagerClient.DeleteSecretAsync(request);
            var message = forceDelete 
                ? $"Secret '{name}' excluído permanentemente" 
                : $"Secret '{name}' marcado para exclusão";
            _notificationService.ShowSuccess(message);
            return new OperationResult<bool>(true, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao excluir secret {SecretName}", name);
            _notificationService.ShowError($"Erro ao excluir secret: {ex.Message}");
            return new OperationResult<bool>(false, false, ex.Message, ex);
        }
    }
}
