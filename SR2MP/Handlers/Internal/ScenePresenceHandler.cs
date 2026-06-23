using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Internal;

/// <summary>
/// Handles ScenePresencePacket on both server and client.
/// Server: updates the authoritative ScenePresenceManager registry.
/// Client: updates the local ScenePresenceManager cache.
/// 
/// When Handle returns true on the server, BasePacketHandler automatically
/// relays the packet to all other clients via SendToAllExcept.
/// </summary>
[PacketHandler((byte)PacketType.ScenePresence)]
internal sealed class ScenePresenceHandler : BasePacketHandler<ScenePresencePacket>
{
    protected override bool Handle(ScenePresencePacket packet, IPEndPoint? sender)
    {
        if (packet.Entered)
            GlobalVariables.ScenePresenceManager.OnPlayerEnteredScene(packet.PlayerId, packet.SceneGroupId);
        else
            GlobalVariables.ScenePresenceManager.OnPlayerExitedScene(packet.PlayerId, packet.SceneGroupId);

        // Return true on server so the packet is relayed to all other clients.
        // On client, returning true has no effect (no relay).
        return true;
    }
}
