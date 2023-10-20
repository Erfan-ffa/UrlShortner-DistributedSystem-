namespace MiniUrl.Services.Notification;

public class SmsService : INotificationServiceStrategy
{
    public string SendOtp(string reciever)
    {
        return Guid.NewGuid().ToString();
    }
}