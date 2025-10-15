namespace AwsLocalStackVisualizer.Models.SecretsManager;

public record SecretInfo(string Name, string Arn, DateTime CreatedDate, DateTime? LastChangedDate, 
    string? Description, Dictionary<string, string> Tags);
