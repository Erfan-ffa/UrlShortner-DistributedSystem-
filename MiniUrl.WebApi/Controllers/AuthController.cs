using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniUrl.DataAccess.Contracts;
using MiniUrl.Entities;
using MiniUrl.Models;
using MiniUrl.Services.Helpers;
using MiniUrl.Services.Identity.Contracts;
using MiniUrl.Services.Notification;
using MiniUrl.Utils.Cache;

namespace MiniUrl.Controllers;

[AllowAnonymous]
[Route("api/[controller]")]
public class AuthController : BaseController
{
    private readonly IJwtService _jwtService;
    private readonly IUserRepository _userRepository;
    private readonly IRedisCache _redisCache;

    public AuthController(IJwtService jwtService, IUserRepository userRepository, IRedisCache redisCache)
    {
        _jwtService = jwtService;
        _userRepository = userRepository;
        _redisCache = redisCache;
    }

    [HttpPost("/send-otp")]
    public async Task<IActionResult> SendOtp(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        if (HasValidRequestInput(request.PhoneNumber, request.UserName, request.Password))
            return FailedResult("Invalid member.");
        
        var userExist = await _userRepository.DoesUserExistByUserNameAsync(request.UserName, cancellationToken);
        if(userExist)
            return FailedResult("User with this username already exists");
        
        var notificationService = new NotificationService(new SmsService());
        var otp = notificationService.SendOtp(request.PhoneNumber);
        
        await CacheOtpAsync(otp, request.PhoneNumber);
        await CacheUserInfoAsync(request);
        
        return SuccessfulResult(new
        {
            PhoneNumber = request.PhoneNumber, 
            Otp = otp
        });
    }

    private async Task CacheOtpAsync(string otp, string phoneNumber)
    {
        var cacheKey = CacheKeys.OtpKey(phoneNumber);
        var hasCached = await _redisCache.WriteObject(cacheKey, otp, 60);
        if (hasCached == false)
            throw new Exception("Something wrong happened. please try again.");
    }

    private async Task CacheUserInfoAsync(RegisterUserRequest request)
    {
        var hasCached = await _redisCache.WriteObject(CacheKeys.UserInfoKey(request.PhoneNumber), new User
        {
            UserName = request.UserName,
            PasswordHash = HashGenerator.GetSha256Hash(request.Password),
            PhoneNumber = request.PhoneNumber,
            CreationTime = DateTime.Now
        });
        
        if(hasCached == false) 
            throw new Exception("Something wrong happened. please try again.");
    }

    [HttpPost("/register-otp")]
    [ProducesResponseType(typeof(ApiResponse<string>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> VerifyOtp(VerifyOtpRequest request, CancellationToken cancellationToken)
    {
        if (HasValidRequestInput(request.PhoneNumber, request.Otp))
            return FailedResult("Invalid request input.");

        var cacheKey = CacheKeys.OtpKey(request.PhoneNumber);
        var sentOtp = await _redisCache.ReadObject<string>(cacheKey);
        
        if(request.Otp.Equals(sentOtp) == false)
            return FailedResult("Invalid otp.");

        var userInfoCacheKey = CacheKeys.UserInfoKey(request.PhoneNumber);
        var userInfo = await _redisCache.ReadObject<User>(userInfoCacheKey);
        var user = await _userRepository.CreateUserAsync(userInfo, cancellationToken);

        var accessToken = _jwtService.GenerateToken(user);

       await _redisCache.KeyDelete(userInfoCacheKey);
       await _redisCache.KeyDelete(CacheKeys.OtpKey(request.PhoneNumber));
       
        return SuccessfulResult(accessToken);
    }

    private bool HasValidRequestInput(params string[] inputs)
        => inputs.Any(string.IsNullOrEmpty);

    [HttpPost("/login")]
    [ProducesResponseType(typeof(ApiResponse<string>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> Login([FromBody] LoginUserDto userDto, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetUserByUsernameAsync(userDto.UserName, cancellationToken);
        if (user is null)
            return FailedResult("Could not find user with this username.");

        var sentPasswordHash = HashGenerator.GetSha256Hash(userDto.Password);
        if (sentPasswordHash.Equals(user.PasswordHash) == false)
            return FailedResult("username or password is incorrect.");
        
        var token = _jwtService.GenerateToken(user);

        return SuccessfulResult(token);
    }
}