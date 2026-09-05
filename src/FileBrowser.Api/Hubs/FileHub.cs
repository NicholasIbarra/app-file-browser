using Microsoft.AspNetCore.SignalR;

namespace FileBrowser.Api.Hubs;

public sealed class FileHub : Hub<IFileHubClient>
{
}
