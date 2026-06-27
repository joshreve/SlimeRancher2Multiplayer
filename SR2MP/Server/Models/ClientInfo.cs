using System.Net;

namespace SR2MP.Server.Models;

public enum ClientSyncState
{
    Connected,
    LoadingWorld,
    SyncingDeferred,
    Active
}

public sealed class ClientInfo
{
    public readonly IPEndPoint EndPoint;
    public readonly string PlayerId;
    public ClientSyncState SyncState { get; set; } = ClientSyncState.Connected;

    private DateTime lastHeartbeat;

    internal ClientInfo(IPEndPoint endPoint, string playerId = "")
    {
        EndPoint = endPoint;
        lastHeartbeat = DateTime.UtcNow;
        PlayerId = playerId;
    }

    public void UpdateHeartbeat() => lastHeartbeat = DateTime.UtcNow;

    public bool IsTimedOut()
        => (DateTime.UtcNow - lastHeartbeat).TotalSeconds > 30;

    public string GetClientInfo() => EndPoint.Address + ":" + EndPoint.Port;
}