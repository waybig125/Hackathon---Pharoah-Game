# Technical Audit & Roadmap

## 🔍 Findings & Root Causes

### 1. Rendering: The "Magenta Line" Bug
*   **Discovery**: Assets generated at runtime were missing URP-compatible shaders, leading to magenta/pink visual artifacts.
*   **Resolution**: Implemented a dynamic material converter in `EgyptianCityGenerator.cs`. It now forces every procedural mesh to use the `Universal Render Pipeline/Lit` shader.
*   **Status**: **FIXED.**

### 2. Physics: NaN Velocity Crashes
*   **Discovery**: Touch inputs were injecting `NaN` values into the physics engine, crashing the simulation.
*   **Resolution**: Patched the `Velocity` setter in `Movement.cs` to sanitize all incoming vectors.
*   **Status**: **STABILIZED.**

### 3. UI: Ultrawide Aspect Stretching
*   **Discovery**: HUD elements were becoming ovals on modern phones due to improper `CanvasScaler` settings.
*   **Resolution**: Locked the HUD scaling to `Match Height` in `MobileHUDButtons.cs`.
*   **Status**: **FIXED.**

---

## 🛠️ What Still Needs to be Fixed
*   **Mummy AI Navigation**: The current AI logic occasionally clips through the procedural walls. Need to bake a `NavMesh` dynamically after city generation.
*   **Touch Precision**: The "Fire" button and "Jump" button occasionally overlap on smaller screens. Need to implement a responsive anchor system.
*   **Texture Tiling**: The desert sand texture shows visible tiling patterns over long distances. Need to add a multi-layered detail map or noise-based tiling.

---

## 🚀 Next Steps (Roadmap)
1.  **Dynamic NavMesh Baking**: Hook the NavMesh surface update into the `EgyptianCityGenerator` completion event.
2.  **Torch & Battery System**: (Requested in `Ideas.md`) Implement the torch helmet and battery pickups to interact with the procedural fog.
3.  **Hive Mind WebSocket**: Finalize the connection to Gemini 3 Flash for real-time tactical mummy coordination.
