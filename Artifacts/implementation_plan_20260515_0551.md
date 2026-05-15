# Egyptian Environment Modular Overhaul

The previous imported `.glb` city caused persistent collision issues (invisible walls) due to the unpredictable nature of auto-generated mesh colliders on complex imported geometry. To gain total control and adhere to the "dark ancient Egyptian shooter" vibe, we will rebuild the environment modularly.

## User Review Required

> [!IMPORTANT]  
> We will use Unity's native tools (Terrain and ProBuilder/Primitives) to construct the new environment. This gives us 100% control over colliders and layout. Please review this approach and let me know if you prefer this over hunting for another pre-made `.glb` online.

## Open Questions

> [!WARNING]  
> 1. Do you want me to attempt downloading and importing free 3D models via Python/Web Search for specific props (like detailed statues), or should we stick entirely to Unity Primitives and ProBuilder for the architecture?
> 2. Should we keep the existing `MainGame` scene and just replace the city object, or create a brand new scene for this environment?

## Proposed Changes

### Phase 1: Environment Foundation
#### [NEW] Assets/Scenes/DesertEnvironment (or update MainGame)
- Remove the existing `EgyptianCity` object.
- Create a large Unity Terrain to act as the rolling desert dunes.
- Apply a sand texture (we can generate one or use existing materials).

### Phase 2: Modular Architecture (ProBuilder)
#### [NEW] Assets/Prefabs/Environment/...
We will create reusable prefabs for the city blocks:
- **Pyramid.prefab**: A massive square-based pyramid in the background.
- **SandstoneWall.prefab**: Thick, blocky walls to create the city streets.
- **EgyptianHouse.prefab**: Flat-roofed ancient dwellings.
- **Pillar.prefab**: Decorative columns for palace areas.

### Phase 3: Assembly & Atmosphere
- Assemble a dense, labyrinthine city layout using the prefabs.
- Apply the existing `MarbleTiles` and `BrickOld` materials to the structures.
- Update `AtmosphereManager.cs` to enhance the "dark ancient Egyptian" vibe (lower ambient light, thicker fog, deep sunset/night skybox).

## Verification Plan

### Automated Tests
- Run the `ColliderAudit` tool to ensure no degenerate or invisible colliders exist in the new modular prefabs.

### Manual Verification
- You will be asked to playtest the scene on your mobile device to verify that movement through the new modular streets is perfectly smooth without any invisible walls.
