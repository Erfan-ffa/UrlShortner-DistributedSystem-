namespace MiniUrl.Services.Notification;

public class NotificationService
{
    private readonly INotificationServiceStrategy _notificationService;

    public NotificationService(INotificationServiceStrategy notificationService)
    {
        _notificationService = notificationService;
    }

    public string SendOtp(string reciever)
    {
        var sentOtp = _notificationService.SendOtp(reciever);

        return sentOtp;
    }
}