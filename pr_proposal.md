# PR Proposal: Multiplayer Sync Improvements & Cyclical Anti-Drift Updater

## Branch Details
* **Branch Name**: `feature/multiplayer-sync-improvements`
* **Target Branch**: `master` on upstream repository (`Egor935/SR2MP`)

---

## Proposed PR Template

### Title
`feat: multiplayer sync improvements (resource nodes, labyrinth, drones, weather map, and cyclical anti-drift updater)`

### Description
This PR introduces a comprehensive suite of multiplayer synchronization fixes, features, and drift correction stability:

1. **Wild Resource Nodes Synchronization**:
   - Added `ResourceNodePacket` to synchronize breakable nodes (Jellystone, Radiant Ore, etc.).
   - Patches intercept and forward node shake, wiggle, and depletion states in sync.
   - Clients delegate item spawns to the host, preventing duplicate drops.

2. **Player Inventory Saving & Recovery**:
   - Saves client inventories to `player_data.json` upon slot updates and restores them on rejoin.
   - Adds a `/recover [player]` command to spawn a disconnected client's inventory as a fountain.

3. **Grey Labyrinth Synchronization**:
   - Synchronizes Labyrinth puzzle slots/plort statues, plort depositors, and prisma barrier forcefield durations.
   - Syncs Labyrinth states both in real-time and during initial slot/resync loading cycles.

4. **Drone Task Synchronization**:
   - Added task configuration packets to synchronize drone program setups in real-time.

5. **Weather & Map Forecast Synchronization**:
   - Allows weather simulation progression on the server, syncing storm and lightning strikes without duplicate loot drops.
   - Fixes Map weather icons on client screens by calculating zone forecast lists on packet updates.
   - Replaces static player marker loops with a dynamic map marker lifecycle manager (correctly handling late joins, leaves, and teleports across islands).

6. **Cyclical Sync Updater (Anti-Desynchronization)**:
   - Added a server-side staggered sync updater that ticks periodically.
   - Staggers currency, upgrades, landplots, doors, labyrinth components, weather, and map states in a 7-step round-robin cycle to prevent network spikes while correcting any lost packets or state drift.

---

## How to Test

### 1. Build Verification
Confirm the project compiles cleanly:
```bash
dotnet build SlimeRancher2Multiplayer.sln
```

### 2. Verification Steps
1. **Map & Weather Icons**: Connect a client and verify remote player map markers update on join, leave, and movements. Open the map as a client and confirm weather icons correctly display the forecast.
2. **Resource Nodes**: Verify breakable nodes wiggle and deplete in sync, and drops do not double-spawn.
3. **Inventory Recovery**: Disconnect a client and verify `/recover [username]` ejects their items as a fountain.
4. **Drift Recovery**: Modify client currency locally during gameplay and verify the Cyclical Sync Updater resets it back to match the server state at the next interval.
