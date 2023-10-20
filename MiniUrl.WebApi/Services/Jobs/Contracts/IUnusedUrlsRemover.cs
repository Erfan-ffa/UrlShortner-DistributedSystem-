namespace MiniUrl.Services.Jobs.Contracts;

public interface IUnusedUrlsRemover
{
    Task RemoveUnusedUrls();
}