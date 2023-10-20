using System.Collections.Specialized;
using System.Web;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniUrl.DataAccess.Contracts;
using MiniUrl.Events;
using MiniUrl.Services.CurrentUser;
using MiniUrl.Services.ShorterService;
using MiniUrl.Utils;
using MiniUrl.Utils.Middlewares;

namespace MiniUrl.Controllers;
[AllowAnonymous]
public class UrlController : BaseController
{
    // private readonly ITopicProducer<ShortUrlCreated2> _producer;
    private readonly IUrlMappingRepository _urlMappingRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPublishEndpoint _eventHandler;
    private readonly HttpContext _httpContext;
    private readonly IUrlShorter _urlShorter;

    public UrlController(IUrlShorter urlShorter,
        ICurrentUserService currentUserService,
        IUrlMappingRepository urlMappingRepository,
        IHttpContextAccessor contextAccessor,
        IPublishEndpoint eventHandler)
    {
        _urlShorter = urlShorter;
        _currentUserService = currentUserService;
        _urlMappingRepository = urlMappingRepository;
        _eventHandler = eventHandler;
        _httpContext = contextAccessor.HttpContext;
    }

    [HttpPost("api/[controller]/short-url")]
    // [TypeFilter(typeof(RateLimiterFilter))]
    public async Task<IActionResult> ShortenUrl([FromBody] string longUrl, CancellationToken cancellationToken)
    {
        var ownerId = _currentUserService.GetUserId();
        if (ownerId.Equals(default))
            return FailedResult("An error occured please try again.");

        var shortText = await _urlShorter.GenrateUniqueText();
        var shortUrl = $"{_httpContext.Request.Host}/{shortText}";

        var shortUrlCreatedEvent = new ShortUrlCreated
        {
            LongUrl = longUrl,
            ShortUrl = shortText,
            OwnerId = ownerId,
            CreationDateTime = DateTime.Now
        };

        // await _producer.Produce(shortUrlCreatedEvent, cancellationToken);
        await _eventHandler.Publish(shortUrlCreatedEvent, cancellationToken);
        

        return SuccessfulResult(shortUrl);
    }

    [HttpGet("{shortUrl}")]
    [AllowAnonymous]
    public async Task<IActionResult> Redirect(string shortUrl, CancellationToken cancellationToken)
    {
        var redirectUrl = await _urlMappingRepository.GetRedirectUrlByShortUrlAsync(shortUrl, cancellationToken);

        _httpContext.AddQueryParamsToUrl(ref redirectUrl);

        return RedirectPermanent(redirectUrl);
    }
    
    [HttpGet("user-urls")]
    public async Task<IActionResult> Test2(CancellationToken cancellationToken)
    {
        return Ok(await _urlMappingRepository.GetUserUrls(_currentUserService.GetUserId(), cancellationToken));
    } 
    
    [HttpGet("url-views")]  
    public async Task<IActionResult> Test2(Guid guid , CancellationToken cancellationToken)
    {
        return Ok(await _urlMappingRepository.GetUrlViewsByMappingIdAsync(guid, cancellationToken));
    } 
    
    [HttpGet("job")]  
    public async Task<IActionResult> Job(CancellationToken cancellationToken)
    {
        var x = await _urlMappingRepository.RemoveOldUnusedUrlsAsync(cancellationToken);
        return Ok(x);
    }
}
