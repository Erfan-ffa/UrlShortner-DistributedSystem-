using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniUrl.DataAccess.Contracts;

namespace MiniUrl.Controllers;

[AllowAnonymous]
[Route(("api/[controller]"))]
public class CacheController : BaseController
{
    private readonly IRedisCache _redisCache;

    public CacheController(IRedisCache redisCache)
    {
        _redisCache = redisCache;
    }

    [HttpGet]
    public async Task<IActionResult> Get(string key)
    {
        return Ok(await _redisCache.KeyExists(key));
    }
    
    [HttpDelete]
    public IActionResult Delete(string key)
    {
        return Ok(_redisCache.KeyDelete(key));
    }
    
    [HttpGet("/views")]
    public async Task<IActionResult> Value(string key)
    {
        var x = await _redisCache.ReadObject<long>(key);
        return Ok(x);
    }
}