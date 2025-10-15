namespace AwsLocalStackVisualizer.Abstractions;

public interface INotificationService
{
    event Action<NotificationMessage>? OnNotification;

    void ShowError(string message, string? title = null);

    void ShowWarning(string message, string? title = null);

    void ShowSuccess(string message, string? title = null);

    void ShowInfo(string message, string? title = null);
}

public record NotificationMessage(NotificationType Type, string Message, string Title);

public enum NotificationType
{
    Error,
    Warning,
    Success,
    Info
}
