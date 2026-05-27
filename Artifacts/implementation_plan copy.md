# Goal Description

This plan addresses the APK mummy spawning bug, enhances prop placement physics, integrates the complete voice acting and SFX suite, and outlines the integration of the Pharaoh boss and Alchemist character models. 

## User Review Required

> [!IMPORTANT]
> **Alchemist Player Model & Animations**: The game is currently an FPS using the Infima Low Poly Shooter Pack. Integrating a full 3rd-person mesh with 50 animations (like the Pro Rifle Pack) requires rewriting the entire movement, aiming, and camera system, which is highly complex. 
> **Decision needed:** 
> 1. Should we just use the Alchemist mesh for the **arms/hands** in FPS view (and cast a shadow)?
> 2. Should we attempt a complete 3rd-person conversion? (High risk, requires extensive refactoring).
> 3. Should we just stick to the current FPS hands and focus on the Audio/Pharaoh first?

> [!WARNING]
> **Pharaoh Boss & Backend**: I will update the backend payload to include `pharaoh_active: bool` and `nearby_environment: string` (e.g., "5 trees, 2 houses"). Ensure your backend Python/FastAPI code is updated to parse these new fields in the `GameStatePayload`.

## Proposed Changes

### Assets & Prefab Generation
#### [NEW] `Assets/Scripts/Editor/BuildPrepEditor.cs`
- Create an Editor script that automatically packages the Mummy FBX, Pharaoh FBX, and their Animator Controllers into `.prefab` files inside `Assets/Resources/`. 
- **Reason:** The APK failed to spawn mummies because `AssetDatabase.LoadAssetAtPath` is an Editor-only API. By baking them into Prefabs in the `Resources` folder, they can be loaded instantly at runtime on Android using `Resources.Load()`.

### Environment Physics
#### [MODIFY] `Assets/Scripts/Editor/StaticEgyptianCityGenerator.cs`
- Add `Rigidbody` and `BoxCollider` to all spawned crates and barrels.
- Ensure they are spawned slightly above the ground so Unity's physics engine naturally drops them into place, resolving any floating or clipping issues on slopes.

### Audio System Integration
#### [NEW] `Assets/Scripts/Gameplay/AudioManager.cs`
- Create a persistent singleton to manage 3 distinct `AudioSource` components: Music (Main/Combat), Ambient (Sand/Fog), and Voice/SFX.
- Implement random 30-second Hive Mind taunt coroutines.
#### [MODIFY] `Assets/Scripts/Weapons/AlchemicalFocus.cs`
- Inject SFX triggers (`sfx_sulfur_shot`, `sfx_mercury_shot`, etc.) based on the active element.
#### [MODIFY] `Assets/Scripts/AI/ZombieAI.cs`
- Hook up mummy walking loops, attack grunts, and death sounds to the animation state machine events.
#### [MODIFY] `Assets/Scripts/Player/PlayerHealth.cs`
- Add a low-health check (`< 30%`) to loop the `sfx_player_pant.mp3` breathing sound.

### Hive Mind Tactics & Narration
#### [MODIFY] `Assets/Scripts/AI/HiveMindManager.cs`
- Parse the `narration` field and trigger the corresponding voice lines (e.g., `vo_tactical_ambush`, `vo_sulfur_01`, etc.) based on substring matching.
- Add `pharaoh_active` and `nearby_environment` (via `Physics.OverlapSphere`) to the JSON payload sent to your server.

### Boss System
#### [NEW] `Assets/Scripts/AI/PharaohAI.cs`
- Create an advanced AI script inheriting/expanding on `ZombieAI` with 3x health, immunity to basic stun locks, and spell-casting attack states.
#### [MODIFY] `Assets/Scripts/AI/MummySpawner.cs`
- Update the wave spawner to deploy the Pharaoh boss every X intervals, surrounded by 6-10 mummy guards.

## Verification Plan

### Automated/Editor Tests
1. Generate the Prefabs via the new Editor script and verify they exist in `Resources/`.
2. Run the game in the Editor and check the Console to ensure Audio clips load successfully and play without `NullReferenceException`.
3. Verify the `HiveMindManager` JSON payload includes the new environment and boss flags.

### Manual Verification
1. Build the APK again and verify mummies (and the Pharaoh) spawn correctly on the mobile device.
2. Confirm that barrels/crates settle naturally on the sand dunes via rigidbodies.
3. Verify that changing weapons and dropping below 30% health triggers the correct audio files.
