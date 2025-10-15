using AwsLocalStackVisualizer.Abstractions;

namespace AwsLocalStackVisualizer.Services;

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

