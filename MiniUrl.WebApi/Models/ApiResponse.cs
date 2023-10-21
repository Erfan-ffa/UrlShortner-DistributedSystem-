using System.Net;

namespace MiniUrl.Models;

public sealed class ApiResponse<T>
{
    public T Result { get; set; }
    
    public  string Message { get; set; }

    public string Error { get; set; }
    
    public  HttpStatusCode HttpStatusCode { get; set; }
}