namespace AwsLocalStackVisualizer.Models.Common;

public record OperationResult<T>(bool IsSuccess, T? Data = default, string? ErrorMessage = null, Exception? Exception = null);
