# Implementation Plan - City Generation, Physics Overlap, HUD Guide Arrow Redesign & Warm Sunset Aesthetic

This plan details the implementation of sea and boat positioning adjustments, fixing floating and clipping physics crates/barrels, dynamically spawning the papyrus at a random plaza, targeting the boat with the HUD guide arrow post-pickup, adding a target indicator text, moving and animating the chevron arrow at the top-center of the screen, and overhauling the game vibe to a warm sunset desert environment.

## User Review Required

> [!IMPORTANT]
> The coastline terrain heightmap calculation will be updated to flatten the terrain to exactly `0f` (which places the ground at world `Y = -0.05f`) at and past `Z = -100f` (`tx <= 0.40f`). Between `Z = -80f` and `Z = -100f`, the terrain height is smoothly scaled down to `0f`. This ensures the beach is completely flat near the water, preventing dunes from clipping through the sea or burying the boat.
> 
> The HUD Guide Arrow will be redesigned as a double-chevron racing-style arrow, positioned at the top-center of the screen with a text indicator ("FIND PAPYRUS" or "ESCAPE TO BOAT") below it. It will bob up and down and scale-pulse when moving.

## Proposed Changes

---

### 1. City Generation & Layout (Aesthetics & Physics Alignment)

#### [MODIFY] [StaticEgyptianCityGenerator.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/Editor/StaticEgyptianCityGenerator.cs)
- **Terrain Coastline Flattening**: Update the height generation loop in `GeneratePolishedCity()`:
  - If `tx < 0.42f` (Z < -80f):
    - If `tx <= 0.40f` (Z <= -100f): set `heights[i, j] = 0f`.
    - Otherwise, scale by `Mathf.SmoothStep(0f, 1f, (tx - 0.40f) / 0.02f)`.
- **Environment Lighting & Vibe (Sunset Mood)**:
  - Set procedural skybox colors: `_SkyTint` to warm blue (`new Color(0.45f, 0.6f, 0.75f)`), `_GroundColor` to warm sand reflection (`new Color(0.85f, 0.70f, 0.55f)`), `_AtmosphereThickness` to `1.0f`, `_Exposure` to `1.2f`.
  - Set ambient trilight colors: Sky `new Color(0.5f, 0.55f, 0.65f)`, Equator `new Color(0.75f, 0.65f, 0.55f)`, Ground `new Color(0.4f, 0.35f, 0.3f)`.
  - Set fog: color to warm sandy cream (`new Color(0.88f, 0.8f, 0.7f)`), range from `60f` to `1200f`.
  - Directional Light: color to warm golden sunlight (`new Color(1.0f, 0.88f, 0.75f)`), intensity to `1.3f`, rotation to `(22f, 215f, 0f)` (low angle sunset).
  - Post Processing Color Filter: change filter from cold blue to warm golden (`new Color(1f, 0.96f, 0.9f)`). Adjust lift/gamma/gain to warm sunset values.
- **House Colors**: Change `wallMat` base color to sandy cream/beige (`new Color(0.92f, 0.86f, 0.76f)`).
- **Crate & Barrel Physics Bounds setup**:
  - In `BuildHouse()`, change the stacked crate instantiation to pass `alignToTerrain = false` to `AlignToGroundAndAddCollider()`.
  - Dynamically find the top Y boundary of the bottom crate and place the stacked crate exactly above it: `bottomCrateTopY = bottomCrateRenderer.bounds.max.y`.
  - In `AlignToGroundAndAddCollider()`, fit the `BoxCollider` to the bounds of the child renderers:
    - Encapsulate child renderers' bounds into a single `Bounds` object.
    - Set the `BoxCollider` center and size using local space coordinates relative to the object's transform.
    - Remove the hardcoded `obj.transform.position += Vector3.up * 0.5f;` shift for dynamic physics objects to prevent offsets.

---

### 2. Gameplay & Key Spawning

#### [MODIFY] [EscapeManager.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/Gameplay/EscapeManager.cs)
- Add `using System.Collections.Generic;` if not already present.
- **Random Plaza Spawning**:
  - In `SpawnKey()`, query all active and inactive GameObjects in the scene.
  - Find all GameObjects whose names contain `"Plaza"`.
  - Select a random plaza, add a small horizontal random offset, and position the Ancient Papyrus at terrain height + 0.8f.
  - Fall back to the hardcoded locations if no plazas are found in the scene.

---

### 3. HUD Guide Arrow & Target Indicator

#### [MODIFY] [MobileHUDButtons.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/UI/MobileHUDButtons.cs)
- Declare private fields: `private Text guideArrowText;` and `private Image guideArrowImage;`.
- **Double Chevron Sprite**:
  - Update `CreateProceduralArrowSprite()` to draw a white double chevron racing-style shape.
- **Top-Center Layout**:
  - In `BuildHUD()`, instantiate `HUD_GuideContainer` anchored to top-center `(0.5f, 1.0f)` at `(0f, -110f)` with a `CanvasGroup`.
  - Add `HUD_GuideArrow` (image with chevron) shifted slightly up in the container, and `HUD_GuideText` (Text with Outline) positioned below it.
- **Dynamic targeting and animation**:
  - Update `UpdateGuideArrow()`:
    - If Papyrus is not collected, target the Papyrus, set text to `"FIND PAPYRUS"`, and tint arrow/text Cyan.
    - If Papyrus is collected, target the Escape Boat, set text to `"ESCAPE TO BOAT"`, and tint arrow/text Amber Gold.
    - Fade container alpha in when player is moving and target is found, otherwise fade out.
    - Rotate the arrow towards the target relative to the player's forward vector.
    - Apply a smooth bobbing animation to the arrow's local anchored Y position and a pulse scale animation to its scale.

---

## Verification Plan

### Automated / Compiler Verification
- Ensure zero C# compiler warnings or errors.

### Manual Verification
1. Run the `Egyptian/Generate & Setup City` menu command to rebuild the city scene.
2. In the editor/scene view, check:
   - Terrain is flat and empty at `Z < -100f`, and the coastline rises smoothly starting from `Z = -80f`.
   - The Escape Boat is floating on the water without being buried or hidden by sand.
   - Crates and barrels are resting cleanly on the ground or on top of each other without floating or intersecting house geometry.
   - House colors are beige/sandy-cream, and the skybox is a warm sunset/sunrise color with golden light rays.
3. Play the game in the Unity Editor:
   - Ensure the HUD Guide Arrow container appears at the top-center of the screen.
   - Start moving: verify that the chevron arrow appears and points toward the Papyrus, with `"FIND PAPYRUS"` displayed in Cyan.
   - Verify the arrow bobs and pulses dynamically during movement.
   - Acquire the Papyrus from its random plaza location.
   - Verify that the arrow turns Amber Gold, points towards the boat, and displays `"ESCAPE TO BOAT"`.
   - Verify you can run to the boat and escape cleanly.
