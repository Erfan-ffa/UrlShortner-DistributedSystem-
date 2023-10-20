namespace MiniUrl.Services.CurrentUser;

public class CurrentUserService : ICurrentUserService
{
    private readonly HttpContext _httpContext;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContext = httpContextAccessor.HttpContext;
    }
    
    public Guid GetUserId()
    {
        Guid.TryParse(_httpContext?.User?.Claims?
                .FirstOrDefault(c => c.Type == "sid")?.Value, out var result);
        
        return result;
    }
}