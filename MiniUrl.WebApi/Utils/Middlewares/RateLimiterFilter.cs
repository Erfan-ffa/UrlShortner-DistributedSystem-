using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using MiniUrl.DataAccess.Contracts;
using MiniUrl.Services.CurrentUser;
using MiniUrl.Utils.Exceptions;

namespace MiniUrl.Utils.Middlewares;

public class RateLimiterFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserRepository _userRepository;

    public RateLimiterFilter(ICurrentUserService currentUserService, IUserRepository userRepository)
    {
        _currentUserService = currentUserService;
        _userRepository = userRepository;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var userId = _currentUserService.GetUserId();
        if (userId.Equals(default))
            throw new UnauthorizedAccessException("You unauthorized please login or register first.");

        var userAllowedUrlCreationCount =  await _userRepository.GetUserAllowedUrlCreationCountAsync(userId);
        if (userAllowedUrlCreationCount <= 0)
            throw new TooManyRequestsException("You've reached the limitation for creating new url.");
        
        context.HttpContext.Response.Headers.Add(new KeyValuePair<string, StringValues>
            ("remained-urls-count-to-create", userAllowedUrlCreationCount.ToString()));

        await next.Invoke();
    }
}