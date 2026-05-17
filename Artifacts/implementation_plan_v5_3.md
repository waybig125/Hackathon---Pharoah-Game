# Implementation Plan - Pharaoh Game V5.3 Mechanics Polish & HUD Redesign

This plan covers the implementation of a custom touch look sensitivity settings menu, Egyptian gold-themed health and ammo HUD elements, a circular rotating Minimap/Compass in the top-right corner, critical player/zombie jumping physics stabilization, mobile touch event clickability fixes, zombie damageability, and button scale adjustments.

## User Review Required

> [!IMPORTANT]
> 1. **Physics Jumping Fix**: The player "jumping" is caused by capsule colliders intersecting with the ground (submerged by y=-1m). Aligning the capsule bottom to `y = 0` (local) and utilizing `Time.fixedDeltaTime` in `FixedUpdate` will solve all jittering and jumping.
> 2. **EventSystem Spawning**: Touches/clicks do not work on mobile/desktop because there is no active `EventSystem` in the scene. We will inject an automatic `EventSystem` creator at initialization.
> 3. **High-Performance Minimap**: Instead of using a slow orthographic camera + Render Texture (which drops mobile FPS), we will build a highly optimized procedural 2D radar/minimap that rotates dynamically to act as a compass.
> 4. **Zombie Damageability**: Bullets currently ignore zombies. We will add a health script to zombies and connect the projectiles to deal damage and destroy them on 4 bullet hits (100 HP, 25 damage per hit).

---

## Proposed Changes

### Component: Physics & Movement Stabilization (Jumping & Movement Fixes)

#### [MODIFY] [Movement.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Infima%20Games/Low%20Poly%20Shooter%20Pack%20-%20Free%20Sample/Code/Character/Movement.cs)
*   **Correct Capsule Bounds**: Adjust the capsule height and center calculations dynamically so that the capsule bottom remains perfectly anchored at the player's feet (`y = 0` in local space).
    *   **Stand/Walk/Run**: Set target height to `1.8f` and center to `new Vector3(0f, 0.9f, 0f)`.
    *   **Crouch**: Set target height to `1.0f` and center to `new Vector3(0f, 0.5f, 0f)`.
*   **Physics Timing Alignment**: In `FixedUpdate()`, replace `Time.deltaTime` with `Time.fixedDeltaTime` for all movement and velocity calculations to prevent framerate-dependent micro-stutters and physical jitter.

---

### Component: EventSystem & UI Raycasting (Unclickable Buttons Fix)

#### [MODIFY] [MobileHUDButtons.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/UI/MobileHUDButtons.cs)
*   **EventSystem Auto-Injection**: In `Awake()` or `Start()`, check if `EventSystem.current == null`. If missing, dynamically create a new GameObject `"EventSystem"` with an `EventSystem` and a `StandaloneInputModule` (with dynamic fallback to `InputSystemUIInputModule` if required).
*   **Ensure Raycast Targets**: Explicitly verify and set `.raycastTarget = true` on all interactive buttons, including settings close, restart, and sensitivity slider controls.
*   **Block Overlap Interception**: Disable raycasting on transparent filler panels in the default HUD `P_LPSP_UI_Canvas(Clone)` so they don't block clicks from reaching our canvas.

---

### Component: Custom Minimap & Compass UI

#### [NEW] [MinimapUI.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/UI/MinimapUI.cs)
*   **Egyptian Gold Aesthetics**: A gorgeous circular radar in the top-right corner framed by an Obsidian-Gold gradient border.
*   **Zoom Functionality**: Touching/clicking the minimap toggles between two zoom scales (e.g. `2.0x` normal view vs `0.8x` zoomed-out layout).
*   **Real-time Compass Rotation**: The entire map content rotates dynamically in the opposite direction of the player's Y-rotation, with a static golden forward-pointing arrow in the center representing the player. This acts as a highly responsive compass.
*   **Dynamic Map Elements**:
    *   **Zombies**: Spooky glowing green dots that update dynamically.
    *   **Buildings & Pyramids**: Warm sand-colored rectangular silhouettes scanned and positioned relative to the player at startup.
    *   **Trees**: Deep forest green circles.
    *   **Crates & Barrels**: Wooden-brown squares.

---

### Component: Zombie Damageability & Combat Loop

#### [MODIFY] [ZombieAI.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/AI/ZombieAI.cs)
*   **Health State**: Implement `public float currentHealth = 100f;` and `public float maxHealth = 100f;`.
*   **Take Damage Method**: Add a robust `TakeDamage(float damage)` method that reduces health, plays hit animations/effects, and handles death.
*   **Death Actions**: On death (`currentHealth <= 0`), disable the NavMeshAgent, set colliders to triggers, play a ragdoll or falling animation, sink the zombie slowly into the desert sand, and destroy the object after 3 seconds.

#### [MODIFY] [Projectile.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Infima%20Games/Low%20Poly%20Shooter%20Pack%20-%20Free%20Sample/Code/Legacy/Projectile.cs) & [ProjectileScript.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Infima%20Games/Low%20Poly%20Shooter%20Pack%20-%20Free%20Sample/Code/Legacy/ProjectileScript.cs)
*   **Zombie Hit Detection**: In `OnCollisionEnter()`, check if the collided object contains a `ZombieAI` component.
*   **Deal Damage**: If detected, call `zombie.TakeDamage(25f)` (4 hits to kill), instantiate a blood splatter impact prefab at the collision contact point, and destroy the bullet.

---

### Component: HUD Layout & Button Scaling

#### [MODIFY] [MobileHUDButtons.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/UI/MobileHUDButtons.cs)
*   **Button Scaling**: Scale down all movement/action buttons on the left side of the screen by 20% to free up more swipe space for looking around.
*   **Label Positioning**: Place the primary **Health label** elegantly on the top-left, and keep the **Ammo count label** situated cleanly on the bottom-left with the custom progress bars.

---

## Verification Plan

### Automated Tests
*   Trigger `mcp_unityMCP_refresh_unity` with compile requested to verify the workspace compiles without C# errors.

### Manual Verification
*   **Clickability & UI Interaction**: Play in the Unity Editor or on mobile, verify all buttons (Sensitivity slider, Settings Close, and Restart) are fully clickable and responsive.
*   **Jumping Fix**: Walk and sprint around. Confirm there is no visual camera stutter or player physical bouncing.
*   **Minimap & Compass**: Verify the circular minimap appears in the top right, rotates as the player turns, zooms when tapped, and correctly represents player (center arrow), zombies (green dots), buildings (sandy blocks), and trees (green dots).
*   **Combat Loop**: Shoot at the spawning zombies. Verify they take damage (shown via blood splatters) and die on the 4th hit, playing a death animation and sinking into the ground.
