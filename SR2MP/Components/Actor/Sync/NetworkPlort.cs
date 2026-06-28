using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Actor;
using System;
using UnityEngine;

namespace SR2MP.Components.Actor.Sync;

internal sealed class NetworkPlort : NetworkComponent
{
    public override byte Key => DeltaRegistry.KeyPlort;

    private PlortModel? plortModel;
    private bool lastSentInvulnerable;
    private float lastSentInvulnerablePeriod;

    public NetworkPlort(NetworkActor actor) : base(actor)
    {
        plortModel = GameObject.GetComponent<PlortModel>();
    }

    public override bool IsDirty()
    {
        plortModel ??= GameObject.GetComponent<PlortModel>();
        if (plortModel == null)
            return false;

        var currentInvulnerable = plortModel._invulnerability?.IsInvulnerable ?? false;
        var currentInvulnerablePeriod = plortModel._invulnerability?.InvulnerabilityPeriod ?? 0f;

        return currentInvulnerable != lastSentInvulnerable ||
               System.Math.Abs(currentInvulnerablePeriod - lastSentInvulnerablePeriod) > 0.0001f;
    }

    public override object GetCurrentData()
    {
        plortModel ??= GameObject.GetComponent<PlortModel>();
        if (plortModel == null)
        {
            return new PlortData { Invulnerable = false, InvulnerablePeriod = 0f };
        }

        var currentInvulnerable = plortModel._invulnerability?.IsInvulnerable ?? false;
        var currentInvulnerablePeriod = plortModel._invulnerability?.InvulnerabilityPeriod ?? 0f;

        lastSentInvulnerable = currentInvulnerable;
        lastSentInvulnerablePeriod = currentInvulnerablePeriod;

        return new PlortData
        {
            Invulnerable = currentInvulnerable,
            InvulnerablePeriod = currentInvulnerablePeriod
        };
    }

    public override void ResetDirty()
    {
    }

    public override void ApplyDelta(object data)
    {
        plortModel ??= GameObject.GetComponent<PlortModel>();
        if (plortModel == null || plortModel._invulnerability == null)
            return;

        var p = (PlortData)data;
        if (plortModel._invulnerability.IsInvulnerable != p.Invulnerable)
            plortModel._invulnerability.IsInvulnerable = p.Invulnerable;
        if (plortModel._invulnerability.InvulnerabilityPeriod != p.InvulnerablePeriod)
            plortModel._invulnerability.InvulnerabilityPeriod = p.InvulnerablePeriod;

        lastSentInvulnerable = p.Invulnerable;
        lastSentInvulnerablePeriod = p.InvulnerablePeriod;
    }
}
