using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace MiniUrl.Models;

public sealed class ApiResponse<T>
{
    public T Result { get; set; }
    
    public  string Message { get; set; }

    public string Error { get; set; }
    
    public  HttpStatusCode HttpStatusCode { get; set; }

    public static ApiResponse<T> Successful(T result, string message = default)
    {
        return new ApiResponse<T>()
        {
            Result = result,
            Message = message,
            HttpStatusCode = HttpStatusCode.OK
        };
    }
}