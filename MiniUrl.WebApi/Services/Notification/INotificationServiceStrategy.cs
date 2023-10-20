namespace MiniUrl.Services.Notification;

public interface INotificationServiceStrategy
{
    string SendOtp(string reciever);
}