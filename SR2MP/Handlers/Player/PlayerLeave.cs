using System.Net;
using SR2MP.Components.UI;
using SR2MP.Handlers.Internal;
using SR2MP.Packets;
using SR2MP.Packets.Player;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Player;

internal abstract class BasePlayerLeaveHandler : BasePacketHandler<PlayerLeavePacket>
{
    protected void RemovePlayerData(string playerId)
    {
        PlayerManager.RemovePlayer(playerId);

        if (PlayerObjects.TryGetValue(playerId, out var playerObj))
        {
            if (playerObj)
            {
                Object.Destroy(playerObj);
                if (!IsServerSide) SrLogger.LogPacketSize($"Destroyed player object for {playerId}");
                else SrLogger.LogMessage($"Destroyed player object for {playerId}");
            }
            PlayerObjects.Remove(playerId);
        }
    }
}

[PacketHandler((byte)PacketType.BroadcastPlayerLeave, HandlerType.Client)]
internal sealed class ClientPlayerLeaveHandler : BasePlayerLeaveHandler
{
    protected override bool Handle(PlayerLeavePacket packet, IPEndPoint? _)
    {
        if (PlayerManager.GetPlayer(packet.PlayerId) == null)
        {
            SrLogger.LogMessage($"Player {packet.PlayerId} doesn't exist (already left?)");
            return false;
        }

        RemovePlayerData(packet.PlayerId);
        return true;
    }
}

[PacketHandler((byte)PacketType.PlayerLeave, HandlerType.Server)]
internal sealed class ServerPlayerLeaveHandler : BasePlayerLeaveHandler
{
    protected override bool Handle(PlayerLeavePacket packet, IPEndPoint? clientEp)
    {
        var playerId = packet.PlayerId;

        if (PlayerManager.GetPlayer(playerId) == null)
        {
            SrLogger.LogMessage($"Player {playerId} doesn't exist (already left?)");
            return false;
        }

        var clientInfo = $"{clientEp!.Address}:{clientEp.Port}";
        SrLogger.LogMessage($"Player leave request received (PlayerId: {playerId})",
            $"Player leave request from {clientInfo} (PlayerId: {playerId})");

        if (Main.Server.ClientManager.RemoveClient(clientInfo))
        {
            SrLogger.LogMessage($"Player {playerId} leave request handled via ClientManager removal");
        }
        else
        {
            SrLogger.LogWarning($"Player leave request from unknown client (PlayerId: {playerId})",
                $"Player leave request from unknown client: {clientInfo} (PlayerId: {playerId})");
        }

        return false;
    }
}