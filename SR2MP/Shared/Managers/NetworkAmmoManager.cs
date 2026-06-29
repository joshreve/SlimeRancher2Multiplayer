using System.Collections;
using Il2CppMonomiPark.SlimeRancher.Caretaker;
using Il2CppMonomiPark.SlimeRancher.Player;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Ammo;
using SR2MP.Shared.Utils;
// ReSharper disable InconsistentNaming

namespace SR2MP.Shared.Managers;

internal static class NetworkAmmoManager
{
    public static void Initialize()
    {
        ClearAmmoCache();
        slotDefinitions.Clear();

        // Right now, I don't know where the definitions are actually stored,
        // but it isn't in the normal places like LookupDirector or SaveReferenceTranslation.
        //
        // However, I can see that only the definitions for plots or gadgets are loaded on the main menu,
        // so they are probably just stored where they are used only.
        // -PinkTarr
        foreach (var def in Resources.FindObjectsOfTypeAll<AmmoSlotDefinition>())
            slotDefinitions[def.name.Hash16()] = def;
    }

    public static int GetNextSlot(this AmmoSlotManager ammo, IdentifiableType id)
    {
        for (var i = 0; i < ammo._ammoModel.Slots.Count; i++)
        {
            var isSlotEmptyOrSameType = ammo.Slots[i]!._count == 0 || ammo.Slots[i]!._id == id;

            var isSlotFull = ammo.Slots[i]!.Count >= ammo.Slots[i]!.MaxCount;

            if (isSlotEmptyOrSameType && isSlotFull) break;

            if (isSlotEmptyOrSameType)
                return i;
        }

        return -1;
    }
    
    public static void ApplySlotData(this AmmoSlotManager ammo, NetworkAmmo networkAmmo)
    {
        foreach (var (slotIndex, networkSlot) in networkAmmo.AmmoSlots)
        {
            var ammoSlot = ammo.Slots[slotIndex];
            if (ammoSlot != null)
            {
                ammoSlot.Count = networkSlot.Count;
                ammoSlot.MaxCount = networkSlot.MaxCount;
            }

            var ammoModelSlot = ammo._ammoModel?.Slots[slotIndex];
            if (ammoModelSlot != null)
            {
                ammoModelSlot.Count = networkSlot.Count;
                ammoModelSlot.MaxCount = networkSlot.MaxCount;
            }
        }
    }
    
    private static readonly Dictionary<ushort, AmmoSlotDefinition> slotDefinitions = new();
    private static readonly Dictionary<IntPtr, string> ammoToID = new();
    private static readonly Dictionary<string, AmmoSlotManager> IDToAmmo = new();
    private static readonly Dictionary<IntPtr, (AmmoSlotManager ammo, int index)> slotToAmmo = new();

    public static string? GetPlotID(this AmmoSlotManager ammo) => ammoToID.GetValueOrDefault(ammo.Pointer);

    public static string? GetPlotID(this AmmoSlot slot)
        => slotToAmmo.TryGetValue(slot.Pointer, out var ammoTuple) ? ammoTuple.ammo.GetPlotID() : null;

    public static int? GetSlotIndex(this AmmoSlot slot)
    {
        if (slotToAmmo.TryGetValue(slot.Pointer, out var ammoTuple))
            return ammoTuple.index;

        return null;
    }

    // public static AmmoSlotManager? GetAmmo(this AmmoSlot slot)
    //     => slotToAmmo.TryGetValue(slot.Pointer, out var ammoTuple) ? ammoTuple.ammo : null;

    public static AmmoSlotManager? GetAmmo(string? id) => IDToAmmo!.GetValueOrDefault(id);

    private static void ClearAmmoCache()
    {
        ammoToID.Clear();
        IDToAmmo.Clear();
        slotToAmmo.Clear();
    }

    internal static void RegisterAmmoPointer(this AmmoSlotManager ammo, string id)
    {
        ammoToID[ammo.Pointer] = id;
        IDToAmmo[id] = ammo;

        for (var i = 0; i < ammo.Slots.Count; i++)
        {
            var slot = ammo.Slots[i];
            slotToAmmo[slot!.Pointer] = (ammo, i);
        }
    }

    internal static void UnregisterAmmoPointer(string id)
    {
        if (IDToAmmo.Remove(id, out var ammo))
        {
            if (ammo != null)
            {
                ammoToID.Remove(ammo.Pointer);
                for (var i = 0; i < ammo.Slots.Count; i++)
                {
                    var slot = ammo.Slots[i];
                    if (slot != null)
                    {
                        slotToAmmo.Remove(slot.Pointer);
                    }
                }
            }
        }
    }

    // todo: review
    // not sure about the whole coroutine and inactive stuff

    public static void RegisterAmmoPointer(this SiloStorage siloStorage)
    {
        StartCoroutine(RegisterAmmoPointerCoroutine(siloStorage));
    }

    private static IEnumerator RegisterAmmoPointerCoroutine(SiloStorage siloStorage)
    {
        yield return new WaitFrames(3);

        if (siloStorage == null || siloStorage.Ammo == null || siloStorage.AmmoSetReference == null)
            yield break;

        // needs to include inactive ones, don't question why
        var plot = siloStorage.GetComponentInParent<LandPlotLocation>(true);
        var gadget = siloStorage.GetComponentInParent<Gadget>(true);
        var sprinkle = siloStorage.GetComponentInParent<SprinkleCanister>(true);

        if (gadget != null)
        {
            int attempts = 0;
            while (gadget.GetActorId().Value == 0 && attempts < 100)
            {
                yield return null;
                attempts++;
            }
        }

        if (plot != null)
        {
            siloStorage.Ammo.RegisterAmmoPointer($"{plot._id}_{siloStorage.AmmoSetReference.name}");
            yield break;
        }

        if (gadget != null)
        {
            siloStorage.Ammo.RegisterAmmoPointer($"gadget{gadget.GetActorId()}_{siloStorage.AmmoSetReference.name}");
            yield break;
        }

        if (sprinkle != null)
        {
            siloStorage.Ammo.RegisterAmmoPointer($"{sprinkle.GetComponent<IdHandler>().Id}_{siloStorage.AmmoSetReference.name}");
            yield break;
        }

        SrLogger.LogWarning($"SiloStorage has no known parent type: {siloStorage.name}");
    }

    public static AmmoSlotDefinition GetSlotDefinition(ushort id) => slotDefinitions[id];

    public static ushort GetId(AmmoSlotDefinition def)
    {
        if (def.name == null)
        {
            SrLogger.LogError("GetId called with a null definition name.");
            return 0;
        }

        var hash = def.name.Hash16();
        slotDefinitions.TryAdd(hash, def);
        return hash;
    }

    public static void ApplyInventory(AmmoSlotManager localAmmo, Dictionary<int, NetworkAmmoSlot> inventory)
    {
        for (int i = 0; i < localAmmo.Slots.Count; i++)
        {
            var slot = localAmmo.Slots[i];
            if (inventory.TryGetValue(i, out var netSlot))
            {
                slot._count = netSlot.Count;
                if (netSlot.Count > 0 && netSlot.Identifiable != -1)
                {
                    slot._id = GlobalVariables.ActorManager.ActorTypes.TryGetValue(netSlot.Identifiable, out var type) ? type : null!;
                }
                else
                {
                    slot._id = null;
                }
            }
            else
            {
                slot._count = 0;
                slot._id = null;
            }
        }
    }

    public static List<AmmoSlotManager> GetLinkedAmmoManagers(string? id)
    {
        var managers = new List<AmmoSlotManager>();
        if (id == null)
            return managers;

        var primary = GetAmmo(id);
        if (primary != null)
        {
            managers.Add(primary);
        }

        if (id.StartsWith("gadget"))
        {
            try
            {
                var firstUnderscore = id.IndexOf('_');
                if (firstUnderscore > 6)
                {
                    var actorIdStr = id.Substring(6, firstUnderscore - 6);
                    if (long.TryParse(actorIdStr, out var actorIdVal))
                    {
                        var model = GameState.GetIdentifiableModel(new ActorId(actorIdVal));
                        if (model != null && model.TryCast<GadgetModel>(out var gadgetModel))
                        {
                            var partner = GameState.identifiables._entries.FirstOrDefault(x =>
                                x.value != null &&
                                gadgetModel != null &&
                                x.value.ident == gadgetModel?.ident
                                && gadgetModel != x.value
                                && (gadgetModel.ident.Cast<GadgetDefinition>().BuyInPairs
                                    || gadgetModel.ident.Cast<GadgetDefinition>().LinkedDefinition
                                    || System.Math.Abs(gadgetModel.ident.Cast<GadgetDefinition>().LinkedGadgetRange) > 0.0001f))?
                                .value.Cast<GadgetModel>();

                            if (partner != null)
                            {
                                var partnerAmmoSet = id.Substring(firstUnderscore);
                                var partnerId = $"gadget{partner.actorId.Value}{partnerAmmoSet}";
                                var partnerAmmo = GetAmmo(partnerId);
                                if (partnerAmmo != null)
                                {
                                    managers.Add(partnerAmmo);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SrLogger.LogWarning($"GetLinkedAmmoManagers error: {ex.Message}");
            }
        }

        return managers;
    }
}