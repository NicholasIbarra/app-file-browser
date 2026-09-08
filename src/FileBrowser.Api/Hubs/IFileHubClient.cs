namespace FileBrowser.Api.Hubs;

public interface IFileHubClient
{
    Task FileUploadCompleted(string path);

    Task FileRenameCompleted(string oldPath, string newPath);   
}
