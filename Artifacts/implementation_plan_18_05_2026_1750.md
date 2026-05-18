# Pharaoh Game Systems Refinement & UI/UX Polish Plan (Local Timestamped)

This implementation plan details the architectural and aesthetic changes required to stabilize keyboard movement, enhance the narrative UI, calibrate the procedural assets/lighting, improve combat detection, and deliver a premium, mobile-first and desktop-ready game experience.

## User Review Required

> [!IMPORTANT]
> - **Input System Fix**: Changing `"path": "Dpad"` to `"path": "2DVector"` in `IA_Player.inputactions` resolves the key blockade and enables WASD movement immediately.
> - **Welcome Screen Aesthetics**: Removes the green subtitle and charcoal background, loads the premium `MedievalSharp.ttf` font asset dynamically, and shifts the layout to the left half of the screen.
> - **Combat Collision**: Restores headshot damage (10 HP for Sulfur, 5 HP for Mercury/Salt) by doing a height-based relative check (`relativeY >= 1.4f`) to bypass root capsule colliders.
> - **Static Elements**: Significantly scales breakable props and spawns glowing, floating alchemical medicine pickups (+10 HP) dynamically above barrels and crates.

## Proposed Changes

### Component 1: Desktop WASD Input Fix

#### [MODIFY] [IA_Player.inputactions](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Infima%20Games/Low%20Poly%20Shooter%20Pack%20-%20Free%20Sample/Input/IA_Player.inputactions)
- Change `"path": "Dpad"` on line 421 under the Keyboard composite binding to `"path": "2DVector"` to enable correct composite processing of WASD keys.

---

### Component 2: Start Screen & Settings Polish

#### [NEW] [MedievalSharp.ttf](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Resources/Fonts/MedievalSharp.ttf)
- The high-fidelity medieval font copied from `Assets/Art/UI/Fonts/MedievalSharp.ttf` into `Assets/Resources/Fonts/MedievalSharp.ttf` for runtime dynamic loading.

#### [MODIFY] [MobileHUDButtons.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/UI/MobileHUDButtons.cs)
- **Start Screen Refactor**:
  - Shift `menuPanelGo` to the left half of the screen by anchoring at `new Vector2(0f, 0.5f)` with anchored position `new Vector2(320, 0)`.
  - Set the Obsidian background image color to transparent (`new Color(0,0,0,0)`) to remove the black backing.
  - Delete `subGo` completely to omit the green alchemical subtitle.
  - Load the `MedievalSharp` title font dynamically using `Resources.Load<Font>("Fonts/MedievalSharp")`.
- **Narration Setting Toggle**:
  - Implement a new `CreateSettingsToggleRow` helper to render a golden-bordered dark checkbox that updates setting preferences seamlessly.
  - Swap the master narration selector with the clean toggle checkbox.

---

### Component 3: Hybrid Combat Headshot Detection

#### [MODIFY] [Projectile.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/Weapons/Projectile.cs)
- Perform a height-based collision check in `OnTriggerEnter`:
  - Calculate `float relativeY = transform.position.y - zombie.transform.position.y;`
  - Classify hit as headshot if `relativeY >= 1.4f`, ensuring elemental damage flows correctly (10 HP for Sulfur, 5 HP for Salt/Mercury headshots).

---

### Component 4: Procedural Environment Polish, Scaling, & Medicine Pickups

#### [NEW] [MedicinePickup.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/Gameplay/MedicinePickup.cs)
- High-fidelity script for alchemical health pickups.
- Spawns a floating, rotating emerald-green double-pyramid crystal diamond.
- Outfits it with a glowing point light and trigger zone that restores +10 HP to `PlayerHealth` on overlap.

#### [MODIFY] [StaticEgyptianCityGenerator.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/Editor/StaticEgyptianCityGenerator.cs)
- **Scale Boost**: Set crates scale to `1.5f` (with stacked crates at `1.2f` and vertical stack offset `3.0f`) and barrels to `0.6f` and `0.5f` for realistic proportions.
- **Medicine Spawning**: Add a 35% probability of spawning a procedural `MedicinePickup` above barrels and stacked crates.
- **Window Lighting Upgrade**:
  - Spawns windows on all four sides of building blocks instead of just one.
  - Enhances window point lights with a beautiful warm cast (`intensity = 8f`, `range = 14f`) and soft shadows (`LightShadows.Soft`) for stunning window glow reflections.

---

### Component 5: Minimap Overhaul

#### [MODIFY] [MinimapUI.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/UI/MinimapUI.cs)
- **Static Map & Rotating Pointer**:
  - Freeze `mapContent` and `compassRing` rotations.
  - Store the `playerIndicator` RectTransform and rotate it dynamically by `-playerRot.y` in `Update()` to point in the player's direction.
- **Clean Compass Dots**:
  - Set the compass text directions array to `{"N", ".", ".", "."}`.
  - Apply custom font size styling so dots act as clean direction markers.
- **Interactive Zoom**:
  - Ensure the `minimapFrame` raycasts and captures touch/clicks, toggling frame dimensions from `240` to `480` and updating positions of static symbols accurately.

---

## Verification Plan

### Automated Tests
- Refresh AssetDatabase and compile scripts inside Unity to confirm zero compilation errors.

### Manual Verification
- **WASD Movement**: Confirm WASD keyboard actions move the player smoothly in Play Mode.
- **Start Screen visual review**: Inspect the left-aligned, text-only clean Welcome Start Screen with the gorgeous MedievalSharp font.
- **Hit Detection / Headshots**: Combat mummies and observe console output verifying Sulfur headshots apply 10 damage, and other hits apply standard or elemental stun/slow status effects.
- **City Props & Lights**: Regenerate the city. Verify crates and barrels are sized realistically, warm lights cast from all window facades with soft shadows on the sand, and floating alchemical green medicine orbs populate above crates.
- **Minimap Inspection**: Verify the mini-map remains facing North, player arrow pointer rotates, dots represent E, S, W, and clicking zooms/enlarges the radar canvas cleanly.
