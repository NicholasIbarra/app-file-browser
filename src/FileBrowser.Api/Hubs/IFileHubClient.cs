namespace FileBrowser.Api.Hubs;

public interface IFileHubClient
{
    Task FileUploadCompleted(string path);
}
