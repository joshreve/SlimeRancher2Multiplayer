using Il2CppMonomiPark.SlimeRancher.Slime;
using SR2MP.Packets.Actor;
using Unity.Mathematics;
using UnityEngine;

namespace SR2MP.Components.Actor.Sync;

internal sealed class NetworkSlimeEmotions : NetworkComponent
{
    public override byte Key => DeltaRegistry.KeySlimeEmotions;

    private readonly SlimeEmotions? emotions;
    private float4 lastSentEmotions;
    private bool lastSentSleeping;

    public NetworkSlimeEmotions(NetworkActor actor) : base(actor)
    {
        emotions = GameObject.GetComponent<SlimeEmotions>();
    }

    public override bool IsDirty()
    {
        var currentEmotions = emotions ? emotions._model.Emotions : new float4(0, 0, 0, 0);
        var currentSleeping = emotions && emotions._model.isSleeping;

        return !currentEmotions.Equals(lastSentEmotions) || currentSleeping != lastSentSleeping;
    }

    public override object GetCurrentData()
    {
        var currentEmotions = emotions ? emotions._model.Emotions : new float4(0, 0, 0, 0);
        var currentSleeping = emotions && emotions._model.isSleeping;

        lastSentEmotions = currentEmotions;
        lastSentSleeping = currentSleeping;

        return new SlimeEmotionsData
        {
            Emotions = currentEmotions,
            Sleeping = currentSleeping
        };
    }

    public override void ResetDirty()
    {
    }

    public override void ApplyDelta(object data)
    {
        if (emotions == null || emotions._model == null)
            return;

        var d = (SlimeEmotionsData)data;
        if (!emotions._model.Emotions.Equals(d.Emotions))
            emotions._model.Emotions = d.Emotions;
        if (emotions._model.isSleeping != d.Sleeping)
            emotions._model.isSleeping = d.Sleeping;

        lastSentEmotions = d.Emotions;
        lastSentSleeping = d.Sleeping;
    }
}
