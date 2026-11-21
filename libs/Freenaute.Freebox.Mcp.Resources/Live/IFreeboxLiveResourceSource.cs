namespace Freenaute.Freebox.Mcp.Resources.Live;

public interface IFreeboxLiveResourceSource
{
    ValueTask<FreeboxLiveSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);
}
