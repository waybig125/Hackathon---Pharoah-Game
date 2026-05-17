# Implementation Plan - Egyptian City V5.1 Bug Fixes & Refinements

This plan outlines the professional-grade modifications to resolve gameplay collision issues, visual bugs, sensitivity calibration, and environment generation bugs in the procedural Egyptian City.

---

## User Review Required

> [!IMPORTANT]
> **Key Quality-of-Life & Visual Enhancements:**
> 1. **10x Swipe Sensitivity Reduction:** Calibrating `LookSwipeZone` sensitivity to `0.025f` (from `0.25f`) for ultra-smooth and precise mobile aiming controls.
> 2. **Complete Ground Normal Map Removal:** The sand floor will be perfectly flat and silky-smooth (albedo only) as requested, eliminating all bump maps on the desert floor.
> 3. **Perfect Prop Colliders (No Phasing):** Creating a robust `AddBoxColliderFromChildren` helper that automatically calculates visual bounds to fit tight colliders on barrels, crates, trees, and columns, making them 100% solid.
> 4. **Float & Sink Prevention:** Aligning prop positions strictly to `y = 0` using calculated local bottom boundaries, ensuring crates and barrels touch the ground perfectly in edit mode.
> 5. **Procedural 3D Pyramids:** Generating pristine, flat-shaded 4-sided Pyramids procedurally in the distant background (outside the city borders) with gold emission and majestic peak lights.
> 6. **Aggressive Purge:** Automatically finding and destroying old `EgyptianCity_Static`, `EgyptianCity_V4_Final`, and generic `EgyptianCity` objects to eliminate the "textureless duplicate models" visual clutter.

---

## Proposed Changes

### UI Component

#### [MODIFY] [MobileHUDButtons.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/UI/MobileHUDButtons.cs)
- Decrease `LookSwipeZone`'s default `sensitivity` from `0.25f` to `0.025f` (exactly 10x less sensitivity).

### Editor Component

#### [MODIFY] [StaticEgyptianCityGenerator.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/Editor/StaticEgyptianCityGenerator.cs)
- **Aggressive Purge:** Update `Purge()` to search for and destroy *all* root objects containing `"EgyptianCity"` or `"FloorGround"` (including older static, V4, and V5 versions) to clear out textureless clutter.
- **Normal Map Removal:** Comment out or delete the `_BumpMap` and `_NORMALMAP` configuration block on `floorMat`.
- **Collider Bounds Helper:** Add `AddBoxColliderFromChildren` to automatically fit BoxColliders to props, trees, and columns.
- **Ground Touch Alignment:** Offset prop positions during instantiation based on their calculated local bottom bounds so they sit perfectly flush on the sand floor.
- **Procedural 3D Pyramids Spawner:** Implement `CreateProceduralPyramid` to procedurally build clean low-poly, 4-sided pyramids in the distant background, avoiding overlapping city roads.

---

## Verification Plan

### Automated & Manual Tests
- Trigger **▶ GENERATE POLISHED CITY** inside the Unity Editor.
- Verify that only ONE root city object (`EgyptianCity_V5_Final`) exists in the hierarchy, with all old versions completely cleaned up.
- Play test: confirm barrels, columns, and trees are solid (the player and mummies cannot cross into them).
- Confirm crates and barrels sit flush on the sand and do not float or clip under the floor.
- Verify that the swipe camera rotation sensitivity is beautifully calibrated and 10x smoother.
- Visually inspect the distant gold-glowing procedural pyramids.
