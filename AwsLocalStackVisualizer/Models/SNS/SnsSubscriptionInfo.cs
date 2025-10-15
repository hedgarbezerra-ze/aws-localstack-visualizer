namespace AwsLocalStackVisualizer.Models.SNS;

public record SnsSubscriptionInfo(string SubscriptionArn, string Protocol, string Endpoint, 
    bool ConfirmationWasAuthenticated, string Owner);
