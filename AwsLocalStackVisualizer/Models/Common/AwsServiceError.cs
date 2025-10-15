namespace AwsLocalStackVisualizer.Models.Common;

public record AwsServiceError(string Service, string Operation, string Message, Exception? Exception = null);
