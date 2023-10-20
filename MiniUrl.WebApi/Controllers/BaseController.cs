using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniUrl.Models;

namespace MiniUrl.Controllers;

[Authorize]
[ApiController]
public class BaseController : ControllerBase
{
    protected IActionResult SuccessfulResult<T>(T result, string message = null)
    {
        return Ok(new ApiResponse<T>
        {
            Result = result,
            Message = message,
            HttpStatusCode = HttpStatusCode.OK
        });
    }
    
    protected IActionResult FailedResult(string message, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest)
    {
        return new BadRequestObjectResult(new ApiResponse<object>
        {
            Message = message,
            HttpStatusCode = httpStatusCode
        });
    }
}