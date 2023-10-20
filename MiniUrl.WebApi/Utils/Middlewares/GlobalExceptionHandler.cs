using System.Net;
using System.Text.Json;
using MiniUrl.Models;

namespace MiniUrl.Utils.Middlewares;

public static class GlobalExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }   
}

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

        public GlobalExceptionHandlerMiddleware(RequestDelegate next,
            IWebHostEnvironment env,
            ILogger<GlobalExceptionHandlerMiddleware> logger)
        {
            _next = next;
            _env = env;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            string message = null;
            var httpStatusCode = HttpStatusCode.InternalServerError;

            try
            {
                await _next(context);
            }
            catch (UnauthorizedAccessException exception)
            {
                httpStatusCode = HttpStatusCode.Unauthorized;
                message = exception.Message;
                await WriteToResponseAsync();
            }
            catch (Exception exception)
            {
                if (_env.IsDevelopment())
                {
                    var dic = new Dictionary<string, string>
                    {
                        ["Exception"] = exception.Message,
                        ["StackTrace"] = exception.StackTrace,
                    };
                    message = JsonSerializer.Serialize(dic);
                }
                await WriteToResponseAsync();
            }

            async Task WriteToResponseAsync()
            {
                if (context.Response.HasStarted)
                    throw new InvalidOperationException("The response has already started, the http status code middleware will not be executed.");

                var result = new ApiResponse<object>{ Message = message, HttpStatusCode = httpStatusCode };
                var jsonResult = JsonSerializer.Serialize(result);

                context.Response.StatusCode = (int)httpStatusCode;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(jsonResult);
            }
        }
}