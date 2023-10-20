namespace MiniUrl.Utils.Exceptions;

public class TooManyRequestsException : Exception
{
    public TooManyRequestsException(string message) : base(message)
    {
        
    }
}