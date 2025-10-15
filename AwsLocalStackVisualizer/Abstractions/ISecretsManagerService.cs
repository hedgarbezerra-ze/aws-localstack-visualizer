using AwsLocalStackVisualizer.Models.Common;
using AwsLocalStackVisualizer.Models.SecretsManager;

namespace AwsLocalStackVisualizer.Abstractions;

public interface ISecretsManagerService
{
    Task<OperationResult<IReadOnlyList<SecretInfo>>> GetSecretsAsync();
    IAsyncEnumerable<SecretInfo> GetSecretsStreamAsync(CancellationToken cancellationToken = default);

    Task<OperationResult<SecretDetails>> GetSecretDetailsAsync(string secretName);

    Task<OperationResult<SecretValue>> GetSecretValueAsync(string secretName, string? versionId = null);

    Task<OperationResult<string>> CreateSecretAsync(string name, string secretValue, string? description = null);

    Task<OperationResult<bool>> UpdateSecretAsync(string name, string secretValue);

    Task<OperationResult<bool>> DeleteSecretAsync(string name, bool forceDelete = false);
}
