# Egyptian Procedural Environment Optimization and AI Boundary Control

This plan addresses critical URP Render Graph crashes, optimizes reflection probes, enhances the visual style of the daytime environment, scales props for visual prominence, and restricts HiveMind AI/mummy movement to safe city boundaries.

## User Review Required

> [!IMPORTANT]
> **Reflection Probe Mode Change**: We are converting the Global Reflection Probe from `Realtime` (which triggers a `NullReferenceException` in URP's internal `ReflectionProbeManager.UpdateGpuData` when editing) to `Baked`. The generator will now programmatically bake the cubemap to `Assets/Materials/GlobalReflectionProbe.exr` during generation, stabilizing the URP Render Graph in Edit Mode.

## Open Questions

No open questions.

## Proposed Changes

### Environment & Rendering

#### [MODIFY] [StaticEgyptianCityGenerator.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/Editor/StaticEgyptianCityGenerator.cs)
- Change `GlobalReflectionProbe` setup: convert from `Realtime` to `Baked` mode and programmatically invoke `Lightmapping.BakeReflectionProbe(probe, "Assets/Materials/GlobalReflectionProbe.exr")` to create a valid cubemap asset on disk.
- Apply high-clarity daytime skybox: update skybox material parameters (`_SkyTint` to azure blue, `_Exposure` to 1.6f).
- Revise ambient lighting settings: adjust sky/equator/ground ambient values to be brighter and daytime-aligned.
- Improve sun light settings: use soft shadows, high resolution, and adjust angle for clear low-poly shading.
- Enhance linear fog parameters: start at 120m, end at 400m, with a warm sandy horizon color.
- Adjust post-processing: tone down color adjustments contrast to 8f (less crushed shadows) and increase saturation to 18f.
- Ensure pyramids cast and receive shadows.

---

### AI & Navigation Boundaries

#### [MODIFY] [ZombieAI.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/AI/ZombieAI.cs)
- Enforce the Z-boundary limit (`Z = -105f` barrier) on all mummy destinations (`currentTargetPos.z`, `wanderTarget.z`, `tacticalTarget.z`) to prevent mummies from entering the sea area.

#### [MODIFY] [HiveMindManager.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/AI/HiveMindManager.cs)
- Enforce the `Z = -105f` boundary check on all incoming tactics instructions before applying them to mummies.

## Verification Plan

### Automated Tests
- Verify that compiling the codebase succeeds without error.
- Check the Unity console for any remaining `Render Graph` or `ReflectionProbeManager` exceptions.

### Manual Verification
- Execute `Egyptian -> Generate & Setup City` menu item.
- Verify that the desert, sky, and sea look correct and that the global reflection probe generates a valid `.exr` file.
- Play the game and inspect the mummies; check that they never cross the `Z = -105f` boundary.
