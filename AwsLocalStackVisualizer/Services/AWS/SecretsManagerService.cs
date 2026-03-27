using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using AwsLocalStackVisualizer.Abstractions;
using AwsLocalStackVisualizer.Configuration;
using AwsLocalStackVisualizer.Models.Common;
using AwsLocalStackVisualizer.Models.SecretsManager;
using System.Runtime.CompilerServices;

namespace AwsLocalStackVisualizer.Services.AWS;

public class SecretsManagerService : ISecretsManagerService
{
    private readonly IAppAwsClients _awsClients;
    private readonly ILogger<SecretsManagerService> _logger;
    private readonly INotificationService _notificationService;

    public SecretsManagerService(IAppAwsClients awsClients, ILogger<SecretsManagerService> logger, INotificationService notificationService)
    {
        _awsClients = awsClients ?? throw new ArgumentNullException(nameof(awsClients));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    }

    public async Task<OperationResult<IReadOnlyList<SecretInfo>>> GetSecretsAsync()
    {
        try
        {
            var response = await _awsClients.SecretsManager.ListSecretsAsync(new ListSecretsRequest());
            
            if (response is not { SecretList: { } secretsList })
            {
                _logger.LogWarning("Resposta do Secrets Manager ListSecrets é nula ou não contém secrets");
                return new OperationResult<IReadOnlyList<SecretInfo>>(true, []);
            }

            var secrets = new List<SecretInfo>();

            foreach (var secret in secretsList)
            {
                if (string.IsNullOrWhiteSpace(secret.Name))
                {
                    _logger.LogWarning("Secret com nome inválido encontrado, ignorando");
                    continue;
                }

                try
                {
                    var tags = new Dictionary<string, string>();
                    if (secret.Tags != null)
                    {
                        foreach (var tag in secret.Tags)
                        {
                            if (!string.IsNullOrWhiteSpace(tag.Key))
                            {
                                tags[tag.Key] = tag.Value ?? string.Empty;
                            }
                        }
                    }

                    secrets.Add(new SecretInfo(
                        secret.Name,
                        secret.ARN ?? string.Empty,
                        secret.CreatedDate ?? DateTime.MinValue,
                        secret.LastChangedDate,
                        secret.Description ?? string.Empty,
                        tags
                    ));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erro ao processar secret {SecretName}, ignorando", secret.Name);
                }
            }

            return new OperationResult<IReadOnlyList<SecretInfo>>(true, secrets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar secrets");
            return new OperationResult<IReadOnlyList<SecretInfo>>(false, [], ex.Message, ex);
        }
    }

    public async IAsyncEnumerable<SecretInfo> GetSecretsStreamAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        List<Task<SecretInfo?>> validSecrets;
        try
        {
            var response = await _awsClients.SecretsManager.ListSecretsAsync(new ListSecretsRequest());
            
            if (response is not { SecretList: { } secretsList })
            {
                _logger.LogWarning("Resposta do Secrets Manager ListSecrets é nula ou não contém secrets");
                yield break;
            }

            validSecrets = secretsList
                .Where(secret => !string.IsNullOrWhiteSpace(secret.Name))
                .Select(secret => ProcessSecretAsync(secret))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar secrets");
            yield break;
        }

        await foreach (var secretInfo in ProcessSecretsSafelyAsync(validSecrets, cancellationToken))
        {
            yield return secretInfo;
        }
    }

    private async IAsyncEnumerable<SecretInfo> ProcessSecretsSafelyAsync(List<Task<SecretInfo?>> secretTasks, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var secretTask in secretTasks)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            var secretInfo = await GetSecretInfoSafelyAsync(secretTask);
            if (secretInfo is not null)
            {
                yield return secretInfo;
            }
        }
    }

    private Task<SecretInfo?> ProcessSecretAsync(dynamic secret)
    {
        try
        {
            var tags = new Dictionary<string, string>();
            if (secret.Tags != null)
            {
                foreach (var tag in secret.Tags)
                {
                    if (!string.IsNullOrWhiteSpace(tag.Key))
                    {
                        tags[tag.Key] = tag.Value ?? string.Empty;
                    }
                }
            }

            return Task.FromResult<SecretInfo?>(new SecretInfo(
                secret.Name,
                secret.ARN ?? string.Empty,
                secret.CreatedDate ?? DateTime.MinValue,
                secret.LastChangedDate,
                secret.Description ?? string.Empty,
                tags
            ));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao processar secret {SecretName}, ignorando", (string)secret.Name);
            return Task.FromResult<SecretInfo?>(null);
        }
    }

    private async Task<SecretInfo?> GetSecretInfoSafelyAsync(Task<SecretInfo?> secretTask)
    {
        try
        {
            return await secretTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao processar secret");
            return null;
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

            var versionsResponse = await _awsClients.SecretsManager.ListSecretVersionIdsAsync(new ListSecretVersionIdsRequest
            {
                SecretId = secretName
            });

            if (versionsResponse is not { Versions: { } versionsList })
                return new OperationResult<SecretDetails>(true, new SecretDetails(secretInfo, []));

            var versions = versionsList
                .Where(v => v is not null and { VersionId: not null and not "" })
                .Select(v => new SecretVersionInfo(
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

            var response = await _awsClients.SecretsManager.GetSecretValueAsync(request);
            
            if (response is null)
            {
                _logger.LogWarning("Resposta do Secrets Manager GetSecretValue é nula para secret {SecretName}", secretName);
                return new OperationResult<SecretValue>(false, null, "Resposta inválida do Secrets Manager");
            }
            
            var secretValue = new SecretValue(
                response.Name ?? secretName,
                response.SecretString ?? string.Empty,
                response.SecretBinary?.ToArray(),
                response.VersionId ?? string.Empty,
                response.CreatedDate ?? DateTime.MinValue
            );

            return new OperationResult<SecretValue>(true, secretValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter valor do secret {SecretName}", secretName);
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

            var response = await _awsClients.SecretsManager.CreateSecretAsync(request);
            _notificationService.ShowSuccess($"Secret '{name}' criado com sucesso");
            return new OperationResult<string>(true, response.ARN);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar secret {SecretName}", name);
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

            await _awsClients.SecretsManager.UpdateSecretAsync(request);
            _notificationService.ShowSuccess($"Secret '{name}' atualizado com sucesso");
            return new OperationResult<bool>(true, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar secret {SecretName}", name);
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

            await _awsClients.SecretsManager.DeleteSecretAsync(request);
            var message = forceDelete 
                ? $"Secret '{name}' excluído permanentemente" 
                : $"Secret '{name}' marcado para exclusão";
            _notificationService.ShowSuccess(message);
            return new OperationResult<bool>(true, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao excluir secret {SecretName}", name);
            return new OperationResult<bool>(false, false, ex.Message, ex);
        }
    }
}
