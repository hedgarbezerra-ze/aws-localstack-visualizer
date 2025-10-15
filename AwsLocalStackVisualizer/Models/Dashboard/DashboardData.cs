using AwsLocalStackVisualizer.Models.Common;

namespace AwsLocalStackVisualizer.Models.Dashboard;

public record DashboardData(IReadOnlyList<ServiceStatus> Services, DateTime LastUpdated);

public record ServiceData(bool IsHealthy, int ResourceCount);

public record DashboardServiceData(
    S3ServiceData S3Data, 
    SqsServiceData SqsData, 
    SnsServiceData SnsData, 
    SecretsServiceData SecretsData, 
    DateTime LastUpdated);
