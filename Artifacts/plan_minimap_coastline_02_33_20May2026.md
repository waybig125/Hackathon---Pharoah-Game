# Minimap Overhaul + Sea & Coastline — Implementation Plan
**Created:** 2026-05-20 02:33 (local)

---

## Overview

Two independent features:
1. **Minimap v2** — Proximity radar (shows only area around player, not full map), tap to expand into a fullscreen rectangular overview map, tap again to close.
2. **Sea & Coastline** — A procedural ocean/beach on the south edge of the city, visible in the minimap, differentiated by color. Pyramids on the south side relocated to north/east.

---

## Feature 1 — Minimap v2

### Current Behaviour (Problems)
- `mapContent` is a 2000x2000 coordinate space; the entire world is translated into it. This means all static icons exist at absolute world coordinates, so the circle shows the full condensed world.
- Tap currently toggles between `scaleNormal = 1.5` and `scaleZoomedOut = 0.6` — neither is a fullscreen rectangular overview.
- No smooth animation between states.

### Desired Behaviour
| State | Shape | Content | Scale |
|---|---|---|---|
| **Default (Radar)** | 240x240 circle, top-right | Only area ~120m radius around player | ~2.0 px/m — local detail |
| **Expanded (Full Map)** | 90% screen rectangle, centered | Entire city (~1000x1000m) overview | ~0.5 px/m — full world |

---

### 1A — Radar Mode (default minimap)

**Key Change:** The minimap should NOT show the full world. It should act as a radar — only render indicators that are within a configurable `radarRadius` (e.g. 120m) of the player.

**In `MinimapUI.cs`:**

- Add `public float radarWorldRadius = 120f;` — the world-space radius shown in the minimap circle.
- Remove the current `scaleNormal` / `scaleZoomedOut` duality. In radar mode, scale is derived from the circle's pixel radius divided by `radarWorldRadius`:
  ```
  radarPixelRadius = 110f  (half of 220px display)
  radarScale = radarPixelRadius / radarWorldRadius  =>  110 / 120 ~= 0.917 px/m
  ```
- **Static icons** — in `Update()`, show/hide each `StaticElementIndicator` based on distance from player:
  ```csharp
  float dist = Vector3.Distance(playerPos, s.worldPos);
  s.iconRect.gameObject.SetActive(!isExpanded && dist <= radarWorldRadius);
  ```
  Their `anchoredPosition` is `(worldPos - playerPos) * radarScale` (relative to player, not absolute).
- **Dynamic indicators (zombies, medicine)** — same filter: only show within `radarWorldRadius`.
- The `mapContent` anchor offset remains `(-playerPos.x * scale, -playerPos.z * scale)` so the player is always centered.

---

### 1B — Expanded Fullscreen Map

**Layout:**
- Size: `Screen.width * 0.90` x `Screen.height * 0.85`, centered on screen.
- Shape: **Rectangle** (not circle) — disable the circular Mask for the expanded state, enable a rect mask.
- Background: dark sand colour (`#1A1208, a=0.93`) with a subtle grid overlay.
- A visible **"X Tap to close"** label appears at top-right corner of the expanded panel.

**Scale in expanded mode:**
- City spans roughly -500 to +500 world units (1000m).
- Expanded map pixel width ~= `Screen.width * 0.90`.
- `expandedScale = (Screen.width * 0.90 * 0.5) / 500` — fits full city width in the panel.
- Static indicators: use absolute world position `(worldPos.x * expandedScale, worldPos.z * expandedScale)`.
- A **player position dot** (bright gold, 16px) placed at `(playerPos.x * expandedScale, playerPos.z * expandedScale)` — NOT centered, so you can see where you are on the full map.

**Tap behavior:**
- `OnPointerDown` detects state and toggles.
- Radar to Expanded: animate `sizeDelta` and `anchoredPosition` over 0.25s using a coroutine with `Mathf.Lerp`.
- Expanded to Radar: animate back, re-enable circular mask.
- Keep `Time.timeScale = 1` (game stays running, enemies still move — adds urgency).

**Implementation approach for two mask shapes:**
- Keep the existing `MaskContainer` (circular Mask + Image).
- Add a second `ExpandedMaskContainer` (rect mask, full-panel size, hidden by default).
- On expand: disable circular mask GO, enable rect mask GO, rescale `mapContent`, update all positions.
- On close: reverse.

---

### 1C — Color Legend (minimap differentiation)

| Element | Color | Icon Shape |
|---|---|---|
| Player | Gold #F2CC33 | Arrow (existing) |
| Mummy | Red #E8221A | Triangle (existing) |
| Pharaoh boss | Orange #FF6600 | Larger triangle (24px) |
| Medicine | Green #1AE83C | Cross (existing) |
| Building/House | Sandy #D4A85A | Filled rect |
| Pyramid | Amber #FFB347 | Larger upward triangle |
| Palm tree | Dark green #2D7A35 | Small circle |
| Crate/Barrel | Brown #8B5E3C | Tiny dot |
| Sea / Coast | Teal-blue #1A7AAF | Filled zone rect |
| Beach | Light sand #F0D080 | Thin strip rect |

- In expanded view, add a color legend panel (bottom-left corner, semi-transparent) listing each icon type.

---

### 1D — Files to Modify

#### [MODIFY] Assets/Scripts/UI/MinimapUI.cs

- **Remove** `scaleNormal`, `scaleZoomedOut`, `isZoomedOut`.
- **Add** `radarWorldRadius`, `expandedScale`, `isExpanded`, `expandedPanel` (RectTransform).
- **Refactor** `BuildMinimapUI()`:
  - Build radar circle (existing, clean up sizing).
  - Build expanded panel (`ExpandedMapPanel`) as a sibling, initially `SetActive(false)`.
- **Refactor** `OnPointerDown()`:
  - Toggle `isExpanded`.
  - Start transition coroutine `AnimateMapTransition(bool expanding)`.
- **Refactor** `Update()`:
  - Split into `UpdateRadarPositions()` and `UpdateExpandedPositions()`.
  - Filter static/dynamic icons by radar radius when in radar mode.
- **Add** `CreateSeaZoneIcon()` — wide teal rectangle at sea world-space bounds.
- **Add** `CreateLegendPanel()` — spawned inside expanded panel.
- **Add** Pharaoh indicator — distinct from regular zombie (larger, orange).
- **Add** coroutine `AnimateMapTransition(bool expanding)`.

---

## Feature 2 — Sea & Coastline

### Design

- **Location:** South edge of city, world Z ~= -80 to -500 (beach begins 80m south of spawn).
- **Visible from spawn:** Player spawns at (0, y, 0). Coast at Z = -80 is exactly 80m away — within the 120m radar radius, so visible in the first frame.
- **Width:** Full X span of city, approximately -450 to +450.
- **Layers (south to north):**
  1. **Open sea** — Z < -100: deep blue #0A3D6B.
  2. **Shoreline / beach strip** — Z -80 to -100: sandy colour, 20m wide.
  3. **Shallow water** — Z -100 to -140: lighter teal #1A7AAF.
  4. **Deep water** — Z < -140: dark navy #0A2040.

### Terrain Modification

In `StaticEgyptianCityGenerator.GeneratePolishedCity()`, after terrain height generation:
- The south quarter (normalised Z < 0.35, roughly real-world Z < -150) gets flattened to y ~= 0.0 (sea level).
- A **Sea GameObject** (flat Quad, scaled to cover the sea zone) is placed at y = -0.3f with a procedural URP teal emission material.
- A **Beach strip** GameObject (flat Quad, 20m wide) with sandy material.

### Pyramid Relocation

Current south-side pyramids (z < 0) need to be moved north:

```
(220,  0, -220)  →  move to  (220,  0, 250)
(-220, 0, -220)  →  move to  (-250, 0, 200)
```

Only the two north-positioned pyramids remain on their original coordinates:
```
(-220, 0, 220)   →  keep
(220,  0, 220)   →  keep
```

### House Spawning Guard

In the grid loop in `GeneratePolishedCity()`:
- Add: if `posZ < -80f`, skip building/plaza spawning (it's in the sea/beach zone).

### Minimap Sea Representation

In `MinimapUI.CacheStaticElements()`:
- After scanning world objects, also search for GameObjects named "SeaZone" and "BeachZone".
- Call `CreateSeaZoneIcon()` with teal/sand colors and large rect sizes.
- In expanded map, they render as a clear teal zone at the south portion of the overview.

### Files to Modify

#### [MODIFY] Assets/Scripts/Editor/StaticEgyptianCityGenerator.cs

- **`GeneratePolishedCity()`**:
  - Modify terrain height array: flatten Z < normalised 0.35 to ~0.
  - Call `CreateSeaAndCoastline(root)` after pyramid creation.
  - Move south pyramids to north positions (change 2 of the 4 `CreateProceduralPyramid` calls).
  - Add house spawn guard: `if (posZ < -80f) continue;`

- **Add** `private void CreateSeaAndCoastline(GameObject root)`:
  - Spawn wide flat Quad at y=-0.3 for sea (teal emission URP material, name="SeaZone").
  - Spawn beach strip Quad at y=0.02 (sand colour, name="BeachZone").

#### [MODIFY] Assets/Scripts/UI/MinimapUI.cs

- In `CacheStaticElements()`, find "SeaZone" and "BeachZone" GameObjects and call `CreateSeaZoneIcon()`.
- Add `CreateSeaZoneIcon(Vector3 worldCenter, Vector2 worldSizeMetres, Color col)`.

---

## Implementation Order

```
1. StaticEgyptianCityGenerator.cs
   a. Flatten south terrain (height array modification)
   b. Move south pyramids north
   c. Add house spawn guard for Z < -80
   d. Add CreateSeaAndCoastline() method

2. MinimapUI.cs
   a. Replace scale system with radarWorldRadius + expandedScale
   b. Build ExpandedMapPanel (rect, fullscreen)
   c. Refactor Update() to filter by radar radius in radar mode
   d. Add sea/beach zone icon rendering
   e. Add Pharaoh indicator (orange, 24px triangle)
   f. Add color legend in expanded view
   g. Add AnimateMapTransition() coroutine
   h. Add "X Tap to close" label in expanded panel
```

---

## Key Constants Reference

```csharp
// MinimapUI.cs
float radarWorldRadius  = 120f;   // metres shown in radar circle
float radarPixelRadius  = 110f;   // half the 220px display circle
float radarScale        = radarPixelRadius / radarWorldRadius; // ~0.917
float expandedScale     = (Screen.width * 0.45f) / 500f;      // fits 1000m city
float animDuration      = 0.25f;  // expand/collapse animation seconds

// StaticEgyptianCityGenerator.cs
float coastlineZ        = -80f;   // world Z where beach begins (80m south of spawn)
float seaStartZ         = -100f;  // world Z where open water begins
float seaWidth          = 900f;   // X extent of sea zone
```

---

## Notes

- The coastline Z=-80 placement ensures the beach is exactly 80m south of player spawn (0,0,0), well within the 120m radar radius — player sees the teal sea zone immediately without moving.
- For the sea material, use `Shader.Find("Universal Render Pipeline/Lit")` with `_EmissionColor = new Color(0.1f, 0.47f, 0.68f) * 2.5f` and `EnableKeyword("_EMISSION")`. Gives a glowing teal ocean without a custom shader.
- All icon colors are defined as static readonly fields at the top of MinimapUI for easy tuning.
- The expanded map blocks touch input to the game world by intercepting pointer events on the overlay panel (the panel's `raycastTarget = true` and it covers the full screen).
