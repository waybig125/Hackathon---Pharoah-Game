# Performance Optimization Plan (Zero Visual Compromise)

## Objective
To significantly improve frame rates, remove gameplay stuttering, and reduce memory overhead without compromising the game's artistic style, animations, or overall feel.

## Background & Motivation
The initial technical audit revealed several major performance bottlenecks:
1.  **AI Scripting:** `ZombieAI.cs` calculates NavMesh paths every single frame and performs expensive component searches constantly.
2.  **Runtime Instantiation:** `MummySpawner.cs` uses inefficient `Instantiate` calls and adds complex components at runtime, rather than using the existing `ObjectPooler`. It also uses `AssetDatabase` which breaks builds.
3.  **Rendering Overhead:** The active URP asset (`PC_RPAsset`) is using extremely heavy settings (4096 shadow maps, 4 shadow cascades, 500 shadow distance).
4.  **Physics:** The collision matrix allows overlapping collision checks for irrelevant layers.

## Scope & Impact
*   **Files Modified:** `ZombieAI.cs`, `MummySpawner.cs`, `PC_RPAsset.asset`, Project Physics Settings.
*   **Impact:** Massive reduction in CPU overhead per frame, elimination of stuttering during enemy spawns, and significantly lowered GPU rendering times, preserving battery life and increasing FPS on lower-end devices.

## Phased Implementation Steps

### Phase 1: Script & Logic Optimization (High Impact)
*   **Refactor `ZombieAI.cs`:**
    *   Throttle `agent.SetDestination` to run on a timer (e.g., 5-10 times a second) instead of every frame.
    *   Cache `Player` and `PlayerHealth` references upon discovery to avoid constant `GameObject.FindAnyObjectByType` calls.
    *   Replace `string` based Animator parameters with cached hashed integers (`Animator.StringToHash`).
    *   Replace `Vector3.Distance` with `Vector3.sqrMagnitude` for faster math.

### Phase 2: Spawning & Architecture (Memory Impact)
*   **Refactor `MummySpawner.cs`:**
    *   Remove all `AssetDatabase` references (these break builds).
    *   Pre-configure a single "Mummy" Prefab with all required components (`ZombieAI`, `NavMeshAgent`, `CapsuleCollider`, `Rigidbody`).
    *   Implement the existing `ObjectPooler` to spawn and manage Mummies instead of instantiating and destroying them at runtime.

### Phase 3: Non-Destructive Rendering (GPU Impact)
*   **Tune `PC_RPAsset`:**
    *   Reduce `shadowDistance` from `500` to `150` (maintaining high quality nearby).
    *   Reduce Shadowmap resolutions from `4096` to `2048`.
    *   Reduce Shadow Cascades from `4` to `2`.
    *   Ensure SRP Batcher is enabled to group draw calls efficiently.

### Phase 4: Physics Cleanup (CPU Impact)
*   **Optimize Collision Matrix:**
    *   Disable collision checks between layers that don't need to interact (e.g., `Post Processing` vs `UI`, `Wall` vs `Invisible Wall`).

## Verification & Testing
1.  **Play Mode Profiling:** Run the game and observe the Unity Profiler. Verify that `ZombieAI.Update` takes significantly less ms/frame.
2.  **Spawn Testing:** Verify that spawning multiple mummies does not cause a spike in GC Alloc or frame rate stutter.
3.  **Visual Audit:** Walk through the main scene to ensure shadows, lighting, and animations look identical to the pre-optimization state.
4.  **Build Verification:** Attempt a standalone build to ensure the removal of `AssetDatabase` calls fixed build errors.

## Alternatives Considered
*   *Aggressive Mobile Focus:* Considered switching completely to `Mobile_RPAsset` and disabling all soft shadows, but this was rejected to strictly maintain visual fidelity and feel.