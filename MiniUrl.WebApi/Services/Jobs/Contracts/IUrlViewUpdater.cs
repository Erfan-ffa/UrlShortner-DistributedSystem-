﻿using Hangfire;
using MiniUrl.Models;

namespace MiniUrl.Services.Jobs.Contracts;

[AutomaticRetry(Attempts = 3, DelaysInSeconds = new []{ 60 , 120 , 120 })]
public interface IUrlViewUpdater
{
    Task UpdateViewsAsync(UrlMappingShit urlMappingShit, string shortUrl, long lastViewsCount, CancellationToken cancellationToken);
}