namespace MiniUrl.Services.ShorterService;

public interface IUrlShorter
{
    public Task<string> GenrateUniqueText();
}