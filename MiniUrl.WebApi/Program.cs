using Microsoft.Extensions.Options;
using MiniUrl.Configuration;
using MiniUrl.DataAccess.Contracts;
using MiniUrl.DataAccess.MongoDatabase;
using MiniUrl.DataAccess.RedisDatabase;
using MiniUrl.DataAccess.Repositories;
using MiniUrl.Models;
using MiniUrl.Services.CurrentUser;
using MiniUrl.Services.Identity.Contracts;
using MiniUrl.Services.Identity.Models;
using MiniUrl.Services.Identity.Services;
using MiniUrl.Services.Jobs;
using MiniUrl.Services.Messaging;
using MiniUrl.Services.Notification;
using MiniUrl.Services.ShorterService;
using MiniUrl.Utils.Middlewares;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHttpContextAccessor();

// builder.Services.Configure<ProducerConfig>(builder.Configuration.GetSection(nameof(ProducerConfig)));
// builder.Services.Configure<ConsumerConfig>(builder.Configuration.GetSection(nameof(ConsumerConfig)));

builder.Services.Configure<CounterRange>(builder.Configuration.GetSection(nameof(CounterRange)));
builder.Services.Configure<RedisSetting>(builder.Configuration.GetSection(nameof(RedisSetting)));
builder.Services.Configure<RabbitMqSetting>(builder.Configuration.GetSection(nameof(RabbitMqSetting)));
builder.Services.Configure<MongoSetting>(builder.Configuration.GetSection(nameof(MongoSetting)));
builder.Services.Configure<JwtSetting>(builder.Configuration.GetSection(nameof(JwtSetting)));

builder.Services.ConfigureRedis(builder.Configuration);
builder.Services.AddScoped<RateLimiterFilter>();
builder.Services.AddScoped<IMongoTransactionHandler, MongoTransactionHandler>();
builder.Services.AddSingleton<IRedisCache, RedisCache>();

builder.Services.AddScoped<INotificationServiceStrategy, SmsService>();
builder.Services.AddScoped<IUrlShorter, UrlShorter>();

builder.Services.AddHangfireConfig(builder.Configuration);
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddHangfireJobs(builder.Services.BuildServiceProvider());
builder.Services.AddScoped<IUrlMappingRepository, UrlMappingRepository>();

builder.Services.AddRabbitMqConfig(builder.Configuration, builder.Services.BuildServiceProvider());

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

builder.Services.AddScoped<IJwtService, JwtService>();

builder.Services.AddSwaggerGen();
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, SwaggerConfiguration>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseGlobalExceptionHandler();

app.AddHangfireDashboard();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();