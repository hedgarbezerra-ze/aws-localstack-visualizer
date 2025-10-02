using AwsLocalStackVisualizer.Models;

namespace AwsLocalStackVisualizer.Services.AWS;

public interface ISecretsManagerService
{
    Task<OperationResult<IReadOnlyList<SecretInfo>>> GetSecretsAsync();
    Task<OperationResult<SecretDetails>> GetSecretDetailsAsync(string secretName);
    Task<OperationResult<SecretValue>> GetSecretValueAsync(string secretName, string? versionId = null);
    Task<OperationResult<string>> CreateSecretAsync(string name, string secretValue, string? description = null);
    Task<OperationResult<bool>> UpdateSecretAsync(string name, string secretValue);
    Task<OperationResult<bool>> DeleteSecretAsync(string name, bool forceDelete = false);
}
