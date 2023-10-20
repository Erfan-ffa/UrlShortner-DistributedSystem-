using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniUrl.DataAccess.Contracts;
using MiniUrl.Entities;
using MiniUrl.Models;
using MiniUrl.Services.Helpers;
using MiniUrl.Services.Identity.Contracts;
using MiniUrl.Services.Notification;
using MiniUrl.Utils;
using MiniUrl.Utils.Cache;

namespace MiniUrl.Controllers;

[AllowAnonymous]
[Route("api/[controller]")]
public class UserController : BaseController
{
    private readonly IJwtService _jwtService;
    private readonly IUserRepository _userRepository;
    private readonly IRedisCache _redisCache;

    public UserController(IJwtService jwtService, IUserRepository userRepository, IRedisCache redisCache)
    {
        _jwtService = jwtService;
        _userRepository = userRepository;
        _redisCache = redisCache;
    }

    [HttpPost("/send-otp")]
    public async Task<IActionResult> SendOtp(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        if (HasValidRequestInput(request))
            return FailedResult("Invalid member.");
        
        var userExist = await _userRepository.DoesUserExistByUserNameAsync(request.UserName, cancellationToken);
        if(userExist)
            return FailedResult("User with this username already exists");
        
        var notificationService = new NotificationService(new SmsService());
        var otp = notificationService.SendOtp(request.PhoneNumber);

        var cacheKey = CacheKeys.OtpKey(request.PhoneNumber);
        await _redisCache.WriteObject(cacheKey, otp, 60);
        
        await _redisCache.WriteObject(CacheKeys.UserInfoKey(request.PhoneNumber), new User
        {
            UserName = request.UserName,
            PasswordHash = HashGenerator.GetSha256Hash(request.Password),
            PhoneNumber = request.PhoneNumber,
            CreationTime = DateTime.Now
        });
        
        return SuccessfulResult(new {PhoneNumber = request.PhoneNumber, Otp = otp});
    }
    
    [HttpPost("/register-otp")]
    public async Task<IActionResult> VerifyOtp(VerifyOtpRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.PhoneNumber) || string.IsNullOrEmpty(request.Otp))
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

    private bool HasValidRequestInput(RegisterUserRequest user)
        => string.IsNullOrEmpty(user.UserName) || string.IsNullOrEmpty(user.Password) || string.IsNullOrEmpty(user.PhoneNumber);

    [HttpPost("/login")]
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