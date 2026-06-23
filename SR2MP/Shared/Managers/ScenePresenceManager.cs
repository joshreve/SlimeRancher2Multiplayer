using System.Collections.Generic;
using System.Linq;

namespace SR2MP.Shared.Managers;

/// <summary>
/// Tracks which peers have which SceneGroups (zones) loaded.
/// Used for spawn delegation (who should execute a spawn when the host doesn't have the scene)
/// and ownership tie-breaking (nearest player with the scene loaded).
/// 
/// The host maintains the authoritative registry; clients maintain a local cache
/// updated via relayed ScenePresencePackets.
/// </summary>
internal sealed class ScenePresenceManager
{
    /// <summary>
    /// PlayerId → set of SceneGroup persistent IDs currently loaded by that player.
    /// </summary>
    private readonly Dictionary<string, HashSet<int>> _playerScenes = new();

    /// <summary>
    /// Record that a player has entered (loaded) a SceneGroup.
    /// </summary>
    public void OnPlayerEnteredScene(string playerId, int sceneGroupId)
    {
        if (!_playerScenes.TryGetValue(playerId, out var scenes))
        {
            scenes = new HashSet<int>();
            _playerScenes[playerId] = scenes;
        }

        scenes.Add(sceneGroupId);
    }

    /// <summary>
    /// Record that a player has exited (unloaded) a SceneGroup.
    /// </summary>
    public void OnPlayerExitedScene(string playerId, int sceneGroupId)
    {
        if (_playerScenes.TryGetValue(playerId, out var scenes))
        {
            scenes.Remove(sceneGroupId);
        }
    }

    /// <summary>
    /// Remove all scene presence records for a player (on disconnect).
    /// </summary>
    public void OnPlayerDisconnected(string playerId)
    {
        _playerScenes.Remove(playerId);
    }

    /// <summary>
    /// Returns the set of player IDs that currently have the given SceneGroup loaded.
    /// </summary>
    public List<string> GetPlayersInSceneGroup(int sceneGroupId)
    {
        var result = new List<string>();
        foreach (var (playerId, scenes) in _playerScenes)
        {
            if (scenes.Contains(sceneGroupId))
                result.Add(playerId);
        }

        return result;
    }

    /// <summary>
    /// Check if any peer (other than the local player) has the given SceneGroup loaded.
    /// </summary>
    public bool AnyPeerHasSceneLoaded(int sceneGroupId, string excludePlayerId)
    {
        foreach (var (playerId, scenes) in _playerScenes)
        {
            if (playerId == excludePlayerId)
                continue;
            if (scenes.Contains(sceneGroupId))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Check if a specific player has the given SceneGroup loaded.
    /// </summary>
    public bool HasSceneLoaded(string playerId, int sceneGroupId)
    {
        return _playerScenes.TryGetValue(playerId, out var scenes) && scenes.Contains(sceneGroupId);
    }

    /// <summary>
    /// Returns the set of SceneGroup IDs loaded by a specific player, or empty if unknown.
    /// </summary>
    public HashSet<int> GetScenesForPlayer(string playerId)
    {
        return _playerScenes.TryGetValue(playerId, out var scenes) ? scenes : new HashSet<int>();
    }

    /// <summary>
    /// Clear all scene presence data (on session end / disconnect).
    /// </summary>
    public void Clear()
    {
        _playerScenes.Clear();
    }
}
