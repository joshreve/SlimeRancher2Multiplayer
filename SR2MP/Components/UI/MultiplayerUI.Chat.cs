using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppInterop.Runtime.Attributes;
using SR2MP.Packets;
using SR2MP.Server.Managers;
using SR2MP.Server.Models;
using UnityEngine;

namespace SR2MP.Components.UI;

internal sealed partial class MultiplayerUI
{
    private bool chatHidden = true;
    private readonly List<ChatMessage> chatMessages = new();
    private readonly Queue<Action> pendingMessageRegistrations = new();
    private readonly HashSet<string> processedMessageIds = new();

    private string chatInput = string.Empty;
    private bool isChatFocused;

    private bool shouldUnfocusChat;
    private bool internalChatToggle;
    private bool shouldFocusChat;
    private bool disabledInput;

    private sealed class ChatMessage
    {
        public string message;
        public string playerName;
        public long time;
        public int lines;
        public string messageId;
        public bool isSystemMessage;
        public byte systemMessageType;
    }

    public void RegisterChatMessage(string message, string playerName, string messageId)
        => RegisterMessageInternal(message, playerName, messageId, false, 0);

    public void RegisterSystemMessage(string message, string messageId, byte type)
        => RegisterMessageInternal(message, "SYSTEM", messageId, true, type);

    private void RegisterMessageInternal(string message, string displayName, string messageId, bool isSystem, byte systemType)
    {
        if (processedMessageIds.Contains(messageId)) return;

        pendingMessageRegistrations.Enqueue(() =>
        {
            var trimmedMessage = message.Trim();
            var dateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var formattedMessage = $"[{DateTimeOffset.FromUnixTimeSeconds(dateTime).ToLocalTime():HH:mm:ss}] {displayName}: {trimmedMessage}";
            var (lines, _) = CalculateMessageHeight(formattedMessage);

            chatMessages.Add(new ChatMessage
            {
                message = trimmedMessage,
                playerName = displayName,
                lines = lines,
                time = dateTime,
                messageId = messageId,
                isSystemMessage = isSystem,
                systemMessageType = systemType
            });

            processedMessageIds.Add(messageId);

            if (processedMessageIds.Count <= 1000) return;

            // Snapshot to avoid mutating the set while iterating
            var toRemove = processedMessageIds.Take(500).ToList();
            foreach (var id in toRemove)
                processedMessageIds.Remove(id);
        });
    }

    private void ProcessPendingMessages()
    {
        while (pendingMessageRegistrations.TryDequeue(out var registration))
            registration?.Invoke();
    }

    private int CalculateTotalLinesInUse()
    {
        var total = 0;
        foreach (var message in chatMessages)
            total += message.lines;
        return total;
    }

    private void TrimOldMessages()
    {
        var totalLines = CalculateTotalLinesInUse();

        while (totalLines > MaxChatLines && chatMessages.Count > 0)
        {
            totalLines -= chatMessages[0].lines;
            chatMessages.RemoveAt(0);
        }
    }

    private static (int lines, float height) CalculateMessageHeight(string text)
    {
        var style = GUI.skin.label;
        const float maxWidth = ChatWidth - (HorizontalSpacing * 2);
        var height = style.CalcHeight(new GUIContent(text), maxWidth);
        var lineCount = Mathf.CeilToInt(height / style.lineHeight);
        return (lineCount, height);
    }

    [HideFromIl2Cpp]
    private void RenderChatMessage(ChatMessage message)
    {
        var timeString = DateTimeOffset.FromUnixTimeSeconds(message.time).ToLocalTime().ToString("HH:mm:ss");

        string formattedMessage;
        if (message.isSystemMessage)
        {
            var systemColor = message.systemMessageType switch
            {
                SystemMessageConnect => ColorSystemConnect,
                SystemMessageDisconnect => ColorSystemDisconnect,
                SystemMessageClose => ColorSystemClose,
                _ => ColorSystemNormal
            };
            formattedMessage = $"<color={systemColor}>[{timeString}] SYSTEM: {message.message}</color>";
        }
        else
        {
            formattedMessage = $"[{timeString}] {message.playerName}: {message.message}";
        }

        GUI.Label(CalculateChatMessageRect(formattedMessage), formattedMessage);
    }

    private Rect CalculateChatMessageRect(string text)
    {
        const float maxWidth = ChatWidth - (HorizontalSpacing * 2);
        var (_, height) = CalculateMessageHeight(text);

        var rect = new Rect(
            6 + HorizontalSpacing,
            previousLayoutChatRect.y + previousLayoutChatRect.height,
            maxWidth,
            height
        );

        previousLayoutChatRect = rect;
        return rect;
    }

    private string SystemMessageId() => $"SYSTEM_{Guid.NewGuid()}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

    [HideFromIl2Cpp]
    private System.Collections.IEnumerator SpawnInventoryFountain(PlayerData playerData)
    {
        var localPlayer = SceneContext.Instance?.player;
        if (!localPlayer)
        {
            SrLogger.LogWarning("Cannot spawn inventory fountain: local player is null!");
            yield break;
        }

        var sceneGroup = SystemContext.Instance.SceneLoader._currentSceneGroup;
        var startPos = localPlayer.transform.position + localPlayer.transform.forward * 2f + Vector3.up * 1f;

        var itemsToSpawn = new List<int>();
        foreach (var slot in playerData.Inventory.Values)
        {
            if (slot.Identifiable != -1 && slot.Count > 0)
            {
                for (int i = 0; i < slot.Count; i++)
                {
                    itemsToSpawn.Add(slot.Identifiable);
                }
            }
        }

        if (itemsToSpawn.Count == 0)
        {
            RegisterSystemMessage($"Player {playerData.PlayerName} had an empty inventory.", SystemMessageId(), SystemMessageNormal);
            yield break;
        }

        RegisterSystemMessage($"Spawning {itemsToSpawn.Count} items...", SystemMessageId(), SystemMessageConnect);

        int spawnedCount = 0;
        foreach (var typeId in itemsToSpawn)
        {
            if (GlobalVariables.ActorManager.ActorTypes.TryGetValue(typeId, out var type) && type && type.prefab)
            {
                var prefab = type.prefab;
                var spread = UnityEngine.Random.insideUnitSphere * 0.2f;
                spread.y = Math.Abs(spread.y);
                var spawnPos = startPos + spread;

                var rotation = UnityEngine.Random.rotation;

                var spawnedObj = InstantiationHelpers.InstantiateActor(
                    prefab,
                    sceneGroup,
                    spawnPos,
                    rotation,
                    false,
                    SlimeAppearance.AppearanceSaveSet.NONE,
                    SlimeAppearance.AppearanceSaveSet.NONE,
                    new Il2CppSystem.Nullable<Il2CppMonomiPark.SlimeRancher.Player.AmmoSlot.AmmoMetadata>(),
                    false,
                    false
                );

                if (spawnedObj)
                {
                    var rb = spawnedObj.GetComponent<Rigidbody>();
                    if (rb)
                    {
                        var forceDir = localPlayer.transform.forward + UnityEngine.Random.insideUnitSphere * 0.3f;
                        forceDir.y = Math.Abs(forceDir.y) + 0.5f;
                        rb.velocity = forceDir.normalized * UnityEngine.Random.Range(3f, 6f);
                    }
                }
            }

            spawnedCount++;
            if (spawnedCount % 2 == 0)
            {
                yield return null;
            }
        }

        RegisterSystemMessage("All items successfully recovered!", SystemMessageId(), SystemMessageConnect);
    }

    private void SendChatMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        message = message.Trim();

        if (message.StartsWith("/recover"))
        {
            ClearChatInput();
            if (!Main.Server.IsRunning)
            {
                RegisterSystemMessage("Only the host can run the /recover command.", SystemMessageId(), SystemMessageDisconnect);
                return;
            }

            var parts = message.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                var discPlayers = PlayerDataManager.Instance.GetDisconnectedPlayers();
                if (discPlayers.Count == 0)
                {
                    RegisterSystemMessage("No disconnected players found in the database.", SystemMessageId(), SystemMessageDisconnect);
                }
                else
                {
                    RegisterSystemMessage("Disconnected players available for recovery:", SystemMessageId(), SystemMessageConnect);
                    foreach (var p in discPlayers)
                    {
                        var timeStr = p.LastConnected != DateTime.MinValue ? p.LastConnected.ToLocalTime().ToString("g") : "Unknown";
                        RegisterSystemMessage($"- {p.PlayerName} ({p.PlayerId}) - Last connected: {timeStr}", SystemMessageId(), SystemMessageNormal);
                    }
                    RegisterSystemMessage("Run '/recover <NameOrID>' to retrieve their inventory.", SystemMessageId(), SystemMessageNormal);
                }
            }
            else
            {
                var target = parts[1].Trim();
                var discPlayers = PlayerDataManager.Instance.GetDisconnectedPlayers();
                var matches = discPlayers.Where(p =>
                    p.PlayerId.Equals(target, StringComparison.OrdinalIgnoreCase) ||
                    p.PlayerName.Equals(target, StringComparison.OrdinalIgnoreCase)
                ).ToList();

                if (matches.Count == 0)
                {
                    RegisterSystemMessage($"No disconnected player found matching '{target}'.", SystemMessageId(), SystemMessageDisconnect);
                }
                else if (matches.Count > 1)
                {
                    RegisterSystemMessage($"Multiple matches found for '{target}':", SystemMessageId(), SystemMessageDisconnect);
                    foreach (var p in matches)
                    {
                        var timeStr = p.LastConnected != DateTime.MinValue ? p.LastConnected.ToLocalTime().ToString("g") : "Unknown";
                        RegisterSystemMessage($"- {p.PlayerName} ({p.PlayerId}) - Last connected: {timeStr}", SystemMessageId(), SystemMessageNormal);
                    }
                    RegisterSystemMessage("Please recover using the specific Player ID.", SystemMessageId(), SystemMessageNormal);
                }
                else
                {
                    var targetPlayer = matches[0];
                    RegisterSystemMessage($"Recovering inventory for {targetPlayer.PlayerName} ({targetPlayer.PlayerId}). Spawning items...", SystemMessageId(), SystemMessageConnect);

                    MelonLoader.MelonCoroutines.Start(SpawnInventoryFountain(targetPlayer));
                    PlayerDataManager.Instance.ClearInventory(targetPlayer.PlayerId);
                }
            }
            return;
        }

        var messageId = $"{Main.Username}_{message.GetHashCode()}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        RegisterChatMessage(message, Main.Username, messageId);

        Main.SendToAllOrServer(new ChatMessagePacket
        {
            Message = message,
            Username = Main.Username,
            MessageID = messageId,
            MessageType = 0
        });
    }

    private void ClearChatInput() => chatInput = string.Empty;

    public void ClearChatMessages()
    {
        chatMessages.Clear();
        processedMessageIds.Clear();
    }

    public void ClearAndWelcome()
    {
        ClearChatMessages();
        RegisterSystemMessage(
            "Welcome to Ranching Together!",
            $"SYSTEM_WELCOME_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            SystemMessageNormal
        );
    }

    private void FocusChat() => SetChatFocusPending(true);

    private void UnfocusChat() => SetChatFocusPending(false);

    private void SetChatFocusPending(bool focus)
    {
        shouldFocusChat = focus;
        shouldUnfocusChat = !focus;
    }

    private void ProcessFocusRequests()
    {
        if (shouldFocusChat && Event.current.type == EventType.Repaint)
        {
            shouldFocusChat = false;

            if (!disabledInput)
            {
                isChatFocused = true;
                activeInputId = "chat_input";
                DisableInput();
                disabledInput = true;
            }
        }
        else if (shouldUnfocusChat)
        {
            shouldUnfocusChat = false;

            if (disabledInput)
            {
                isChatFocused = false;
                activeInputId = string.Empty;
                EnableInput();
                disabledInput = false;
            }
        }
    }

    private void DrawChat()
    {
        if (state == MenuState.DisconnectedMainMenu || chatHidden) return;

        var chatY = Screen.height / 2f;

        GUI.Box(new Rect(6, chatY, ChatWidth, ChatHeight), "Chat (F5 to toggle)");

        ProcessPendingMessages();
        TrimOldMessages();

        previousLayoutChatRect = new Rect(6, chatY + ChatHeaderHeight, ChatWidth, 0);

        foreach (var message in chatMessages)
            RenderChatMessage(message);

        chatInput = DrawSafeTextInput(
            "chat_input",
            new Rect(
                6 + HorizontalSpacing,
                chatY + ChatHeight - InputHeight - 5,
                ChatWidth - (HorizontalSpacing * 2),
                InputHeight
            ),
            chatInput,
            MaxChatMessageLength,
            numbersOnly: false,
            isChat: true
        );

        ProcessFocusRequests();
    }
}