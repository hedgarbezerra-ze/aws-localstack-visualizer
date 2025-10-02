namespace AwsLocalStackVisualizer.Services;

public interface INotificationService
{
    event Action<NotificationMessage>? OnNotification;
    void ShowError(string message, string? title = null);
    void ShowWarning(string message, string? title = null);
    void ShowSuccess(string message, string? title = null);
    void ShowInfo(string message, string? title = null);
}

public class NotificationService : INotificationService
{
    public event Action<NotificationMessage>? OnNotification;

    public void ShowError(string message, string? title = null) =>
        OnNotification?.Invoke(new NotificationMessage(NotificationType.Error, message, title ?? "Erro"));

    public void ShowWarning(string message, string? title = null) =>
        OnNotification?.Invoke(new NotificationMessage(NotificationType.Warning, message, title ?? "Aviso"));

    public void ShowSuccess(string message, string? title = null) =>
        OnNotification?.Invoke(new NotificationMessage(NotificationType.Success, message, title ?? "Sucesso"));

    public void ShowInfo(string message, string? title = null) =>
        OnNotification?.Invoke(new NotificationMessage(NotificationType.Info, message, title ?? "Informação"));
}

public record NotificationMessage(NotificationType Type, string Message, string Title);

public enum NotificationType
{
    Error,
    Warning,
    Success,
    Info
}
