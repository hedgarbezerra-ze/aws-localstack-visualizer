using System.Text.RegularExpressions;

namespace AwsLocalStackVisualizer.Validators;

public static class S3BucketValidator
{
    private static readonly Regex BucketNameRegex = new Regex(
        @"^[a-z0-9][a-z0-9\-]*[a-z0-9]$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    private static readonly string[] ReservedNames = {
        "xn--",
        "sthree-",
        "sthree-configurator",
        "amzn-s3-demo-bucket"
    };

    private static readonly string[] InvalidSuffixes = {
        "-s3alias",
        "--ol-s3"
    };

    public static ValidationResult ValidateBucketName(string bucketName)
    {
        if (string.IsNullOrWhiteSpace(bucketName))
        {
            return new ValidationResult(false, "Nome do bucket não pode estar vazio.");
        }

        if (bucketName.Length < 3)
        {
            return new ValidationResult(false, "Nome do bucket deve ter pelo menos 3 caracteres.");
        }

        if (bucketName.Length > 63)
        {
            return new ValidationResult(false, "Nome do bucket não pode ter mais de 63 caracteres.");
        }

        if (!BucketNameRegex.IsMatch(bucketName))
        {
            return new ValidationResult(false, "Nome do bucket deve conter apenas letras minúsculas, números e hífens. Deve começar e terminar com letra ou número.");
        }

        if (IsIpAddress(bucketName))
        {
            return new ValidationResult(false, "Nome do bucket não pode ser um endereço IP.");
        }

        if (bucketName.Contains("--"))
        {
            return new ValidationResult(false, "Nome do bucket não pode conter dois hífens consecutivos.");
        }

        foreach (var reserved in ReservedNames)
        {
            if (bucketName.StartsWith(reserved, StringComparison.OrdinalIgnoreCase))
            {
                return new ValidationResult(false, $"Nome do bucket não pode começar com '{reserved}'.");
            }
        }

        foreach (var suffix in InvalidSuffixes)
        {
            if (bucketName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return new ValidationResult(false, $"Nome do bucket não pode terminar com '{suffix}'.");
            }
        }

        if (bucketName != bucketName.ToLowerInvariant())
        {
            return new ValidationResult(false, "Nome do bucket deve conter apenas letras minúsculas.");
        }

        return new ValidationResult(true, "Nome do bucket é válido.");
    }

    private static bool IsIpAddress(string input)
    {
        var parts = input.Split('.');
        if (parts.Length != 4) return false;

        foreach (var part in parts)
        {
            if (!int.TryParse(part, out var num) || num < 0 || num > 255)
                return false;
        }

        return true;
    }

    public static string GetBucketNamingRules()
    {
        return @"Regras para nomes de buckets S3:
• 3-63 caracteres de comprimento
• Apenas letras minúsculas, números e hífens (-)
• Deve começar e terminar com letra ou número
• Não pode conter dois hífens consecutivos (--)
• Não pode ser um endereço IP (ex: 192.168.1.1)
• Não pode começar com 'xn--', 'sthree-'
• Não pode terminar com '-s3alias' ou '--ol-s3'";
    }
}

public record ValidationResult(bool IsValid, string Message);
