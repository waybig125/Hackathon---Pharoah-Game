# Implementation Plan: Stabilizing Pharaoh Mobile Experience & Visual HUD Polish

This plan addresses all mobile gameplay, visual HUD, animation, rigging, and project settings issues reported by the user, elevating the game to a high-end, production-ready mobile title.

---

## User Review Required

> [!IMPORTANT]
> The background image **GameStartImage.jpeg** failed to load because it was imported with `spriteMode = 2` (Multiple) with zero sub-sprites. We will automatically fix the editor auto-loader to force all resource textures (under `egyptian_items`) to import as **Sprite (2D and UI)** with `SpriteImportMode.Single`.

> [!TIP]
> The ammunition HUD will be completely redesigned from a simple progress bar into a premium row of **30 individual gold-tinted bullet segments** that dynamically decrease and refill on fire/reload, matching standard high-fidelity arcade shoot-'em-ups!

---

## Proposed Changes

### Component 1: Welcome & Start Screen Design

We will refine the welcome screen to load the high-fidelity `GameStartImage` background, style the Obsidian gold-bordered menu, and add native, beautifully-padded Start and Quit buttons on the right side of the screen.

#### [MODIFY] [MobileHUDButtons.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/UI/MobileHUDButtons.cs)
- Improve `CreateStartScreen` to load `egyptian_items/GameStartImage` successfully and stretch to cover the screen perfectly.
- Create a beautiful procedural radial gradient overlay fallback if any texture load fails.
- Align the Start and Quit buttons on the right panel with gold-leaf accents, hover scales, and responsive touch bounds.
- Disable the joystick and firing controls on welcome screen, and re-enable them along with `Time.timeScale = 1f` once the player clicks "Start"!

---

### Component 2: Custom Bullet Ammo HUD & Health Bar Layout

We will fix the out-of-screen layout issues, add a gorgeous central screen target pointer (crosshair) for shooting precision, and implement the custom 30-bar ammo representation.

#### [MODIFY] [MobileHUDButtons.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/UI/MobileHUDButtons.cs)
- Redesign the Ammo indicator into a horizontal grid of **30 tiny vertical gold columns**.
- Position the Health bar at the bottom-left and the Ammo grid at the bottom-right within standard mobile Safe Area bounds.
- Add a beautiful central crosshair (a tiny golden ring with a center dot) to the gameplay Canvas.

---

### Component 3: Mummy Rigging, Attack Loop, and Death Animations

We will guarantee that when a mummy dies, the correct humanoid death animation is played and they stop sliding/attacking immediately. We will also resolve why the death animation clip wasn't generated.

#### [MODIFY] [StaticEgyptianCityGenerator.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/Editor/StaticEgyptianCityGenerator.cs)
- Enhance `getOrCreateClip` to robustly fall back to the first non-preview animation clip inside `mummy_death.fbx` if the `mixamo.com` sub-asset name differs.
- Ensure that the generated clip is saved correctly and mapped to the `"Die"` state.

#### [MODIFY] [ZombieAI.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/AI/ZombieAI.cs)
- In `Die()`, disable navigation, movement, and path update loops completely to prevent dragging.
- Ensure the animation state is locked to the `"Die"` clip and does not blend back into other clips.
- Limit zombie health to `10f` (10x lower than player's health of `100f`).

---

### Component 4: Fully Transparent Death Screen & Post-Death Layout

When the player dies, the gameplay joystick, attack buttons, and ammo metrics must fade out, transitioning into a beautiful semi-transparent overlay.

#### [MODIFY] [PlayerHealth.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/Player/PlayerHealth.cs)
- On player death, invoke a HUD callback to hide all interactive mobile buttons and joysticks.
- Spawn a premium semi-transparent death screen overlay with a gold-bordered "Respawn" button.

---

### Component 5: Mobile Build & Unity Remote Settings

We will check and document settings required for instant Unity Remote connection over USB.

#### [NEW] [mobile_build_guide.md](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Artifacts/mobile_build_guide.md)
- Step-by-step instructions to configure **Edit > Project Settings > Editor > Unity Remote** to enable instant mobile testing via USB debugging.
- Production build targets (Android SDK, API Levels, and IL2CPP compilation) to guarantee immediate packaging compatibility.

---

## Verification Plan

### Automated Verification
- Recompile all code inside Unity and run the city generation menu item to verify successful asset setup.
- Log confirmation that all 4 clips (`Idle`, `Walk`, `Attack`, `Die`) are cleanly written as `.anim` files under `Assets/Mummy_Assets/`.

### Manual Verification
- Start the game in editor or build. Verify the Welcome Screen renders with full-screen `GameStartImage`.
- Click Start: game time resumes and mobile controls (joystick, fire, reload, settings) show up in high-definition.
- Fire the gun and verify the 30-bar ammo representation decreases tick by tick.
- Engage a zombie, verify their health is exactly 10, and verify they play their death animation and stop sliding when defeated.
- Die to a zombie, verify all controls disappear, and the semi-transparent death screen overlay shows up with a respawn button.
