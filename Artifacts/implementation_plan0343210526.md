# Implementation Plan - Pharaoh Game Mechanics & UI Overhaul

This plan details the fixes for the player spawn locking, minimap styling and scale, mobile settings menu interactions, and key tracking discoverability.

## User Review Required

> [!IMPORTANT]
> - **Spawn Fix**: Changing `PlazaFloor` layer to `Ignore Raycast` (Layer 2) ensures the player's SphereCast ground detection does not get trapped.
> - **Minimap UI**: We will transition the minimap from circular to rectangular (`300x200`) and use a standard rectangular UI Mask to eliminate elliptical distortion and border repeating. We will also add procedurally generated tracking icons for the Key and Boat.
> - **Key Discoverability**: The key (`AncientPapyrus`) will spawn near the center plaza (three close coordinates) and register its position dynamically on the minimap.
> - **Settings UI**: The settings button and other modal buttons on mobile will implement and prioritize `IPointerClickHandler` to prevent slight finger drags on mobile screens from blocking interaction.

## Proposed Changes

### Component 1: Plaza Floor Collision Layer

#### [MODIFY] [StaticEgyptianCityGenerator.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/Editor/StaticEgyptianCityGenerator.cs)
- Set `floor.layer = 2; // Ignore Raycast` on the instantiated `PlazaFloor` plane to prevent player ground detection interference.

---

### Component 2: Rectangular Minimap Layout & Dynamic Key Tracking

#### [MODIFY] [MinimapUI.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/UI/MinimapUI.cs)
- Change size of `minimapFrame` to `300x200`.
- Update `GenerateSprites` to create rectangular background, rectangular border, and custom procedural Key/Boat sprites.
- Change `MaskContainer` sprite to `null` to use the RectTransform's rectangular bounds for clipping, eliminating repeat borders.
- Update `StaticElementIndicator` to track its `sourceGo` so that when the key is collected (destroyed), its indicator is automatically cleaned up from the minimap.
- Add a public method `RegisterDynamicStaticIcon` so the key and boat can register themselves on spawn.
- Adjust `radarWorldRadius = 45f;` to provide a highly readable, zoomed-in view of local streets and streets details.

---

### Component 3: Key Relocation and Minimap Registration

#### [MODIFY] [EscapeManager.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/Gameplay/EscapeManager.cs)
- Change the spawn locations of the `AncientPapyrus` to prominent points near the center plaza:
  - `(0, 0, 25)`
  - `(30, 0, 0)`
  - `(-30, 0, 0)`
- Call `MinimapUI.Instance.RegisterDynamicStaticIcon` for both the key and the boat when they are instantiated.

---

### Component 4: Mobile Input Touch/Click Stability

#### [MODIFY] [MobileHUDButtons.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/UI/MobileHUDButtons.cs)
- Implement `IPointerClickHandler` in `ButtonInputHelper`.
- Add `onClick` action callback to `ButtonInputHelper`.
- Fall back to `onUp` in `OnPointerClick` if `onClick` is null.
- Assign the Settings button, Close button, and Settings action buttons to use the new `onClick` callback to bypass mobile drag cancellation.

## Verification Plan

### Automated Verification
- Compile and run PlayMode tests (if any).
- Capture screenshots using `mcp_unityMCP_manage_camera` to visually verify the rectangular minimap UI layout.
- Query console logs using `mcp_unityMCP_read_console` to confirm compilation is clean.

### Manual Verification
- Verify player is able to move freely at spawn without jumping.
- Tap the mobile Settings button in a mobile emulator / touch-simulation mode and confirm it opens the settings modal.
- Locate the key in its new nearby position and check its icon on the minimap.
