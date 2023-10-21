using System.Net;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniUrl.DataAccess.Contracts;
using MiniUrl.Events;
using MiniUrl.Models;
using MiniUrl.Services.CurrentUser;
using MiniUrl.Services.ShorterService;
using MiniUrl.Utils;
using MiniUrl.Utils.Middlewares;

namespace MiniUrl.Controllers;

public class UrlMappingController : BaseController
{
    private readonly IUrlMappingRepository _urlMappingRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPublishEndpoint _eventHandler;
    private readonly HttpContext _httpContext;
    private readonly IUrlShorter _urlShorter;

    public UrlMappingController(IUrlShorter urlShorter,
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

    [HttpPost("/create-short-url")]
    [TypeFilter(typeof(RateLimiterFilter))]
    [ProducesResponseType(typeof(ApiResponse<string>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> CreateShortUrl([FromBody] string longUrl, CancellationToken cancellationToken)
    {
        if(string.IsNullOrEmpty(longUrl))
            return FailedResult("Invalid url."); 
        
        var ownerId = _currentUserService.GetUserId();
        if (ownerId.Equals(default))
            return FailedResult("An error occured please try again.");

        var shortText = await _urlShorter.GenerateUniqueText();
        var shortUrl = $"{_httpContext.Request.Host}/{shortText}";

        var shortUrlCreatedEvent = new ShortUrlCreated
        {
            LongUrl = longUrl,
            ShortUrl = shortText,
            OwnerId = ownerId,
            CreationDateTime = DateTime.Now
        };

        await _eventHandler.Publish(shortUrlCreatedEvent, cancellationToken);
        
        return SuccessfulResult(shortUrl);
    }

    [HttpGet("{shortUrl}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<string>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> Redirect(string shortUrl, CancellationToken cancellationToken)
    {
        if(string.IsNullOrEmpty(shortUrl))
            return FailedResult("Invalid url."); 
        
        var redirectUrl = await _urlMappingRepository.GetRedirectUrlByShortUrlAsync(shortUrl, cancellationToken);

        _httpContext.AddQueryParamsToUrl(ref redirectUrl);

        return RedirectPermanent(redirectUrl);
    }

    [HttpGet("/url-views")]
    public async Task<IActionResult> GetUrlViews(string shortUrl, CancellationToken cancellationToken)
    {
        var urlViews = await _urlMappingRepository.GetUrlViewsAsync(shortUrl, cancellationToken);
        if (urlViews is null)
            return FailedResult("Invalid url.");

        return SuccessfulResult(urlViews);
    }

    [HttpGet("/my-urls")]
    public async Task<IActionResult> GetMyUrls(CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetUserId();
        var result = await _urlMappingRepository.GetUrlMappingsByUserIdAsync(currentUserId, cancellationToken);

        return SuccessfulResult(result);
    }
}
