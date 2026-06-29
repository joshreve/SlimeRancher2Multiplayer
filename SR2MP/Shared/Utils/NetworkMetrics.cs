using System.Net;

namespace SR2MP.Shared.Utils;

internal static class NetworkMetrics
{
    // Queue sizes
    public static int ClientQueueSize => MainThreadDispatcher.Instance ? MainThreadDispatcher.Instance.ClientQueueCount : 0;
    public static int ServerQueueSize => MainThreadDispatcher.Instance ? MainThreadDispatcher.Instance.ServerQueueCount : 0;

    // Processing delay (in seconds)
    public static float AccProcessingDelay;
    public static int ProcessingDelayCount;

    // Reliability metrics
    public static int ReliableSent;
    public static int ResendAttempts;
    public static int FailedPackets;

    // Drop metrics
    public static int CorruptedDropped;
    public static int DuplicateIgnored;
    public static int OutOfOrderDropped;

    // Throughput metrics (accumulated over 1 second window)
    public static int BytesReceived;
    public static int PacketsReceived;
    public static int BytesSent;
    public static int PacketsSent;

    // Historical throughput rates (updated every 1s)
    public static float RxKbps;
    public static float RxPacketsPerSec;
    public static float TxKbps;
    public static float TxPacketsPerSec;

    private static float statsTimer;
    private static float reportTimer;

    public static void Update(float deltaTime)
    {
        statsTimer += deltaTime;
        if (statsTimer >= 1f)
        {
            RxKbps = (BytesReceived * 8f) / (1024f * statsTimer);
            RxPacketsPerSec = PacketsReceived / statsTimer;
            TxKbps = (BytesSent * 8f) / (1024f * statsTimer);
            TxPacketsPerSec = PacketsSent / statsTimer;

            BytesReceived = 0;
            PacketsReceived = 0;
            BytesSent = 0;
            PacketsSent = 0;
            statsTimer = 0f;
        }

        reportTimer += deltaTime;
        if (reportTimer >= 5f)
        {
            reportTimer = 0f;
            if (Main.Client.IsConnected || Main.Server.IsRunning)
            {
                float avgDelayMs = ProcessingDelayCount > 0 ? (AccProcessingDelay / ProcessingDelayCount) * 1000f : 0f;
                AccProcessingDelay = 0f;
                ProcessingDelayCount = 0;

                SrLogger.LogMessage(
                    $"[METRICS] FPS: {GlobalVariables.LocalFPS:F1} | " +
                    $"Queue (C/S): {ClientQueueSize}/{ServerQueueSize} | " +
                    $"Main-Thread Delay: {avgDelayMs:F1}ms | " +
                    $"RTT Pkts (Sent/Resent/Failed): {ReliableSent}/{ResendAttempts}/{FailedPackets} | " +
                    $"Dropped (CRC/Dup/Reorder): {CorruptedDropped}/{DuplicateIgnored}/{OutOfOrderDropped} | " +
                    $"Rx: {RxPacketsPerSec:F1} p/s ({RxKbps:F1} Kbps) | " +
                    $"Tx: {TxPacketsPerSec:F1} p/s ({TxKbps:F1} Kbps)"
                );
            }
        }
    }
}
