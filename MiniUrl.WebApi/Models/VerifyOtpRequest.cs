namespace MiniUrl.Models;

public class VerifyOtpRequest
{
    public string Otp { get; set; }
    
    public string PhoneNumber { get; set; }
}