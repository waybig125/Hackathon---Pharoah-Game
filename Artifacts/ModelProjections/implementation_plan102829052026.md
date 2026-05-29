# Implementation Plan - Cleaning, Loading Screen, UI Revamp, Low-Poly Terrain, and Material Fixes

This plan outlines the steps to perform codebase cleanup (removing unused generated sprites, scripts, legacy asset packs, and massive scene lightmaps), update the loading screen, revamp the HUD ammo bar, implement health-responsive blood overlays/vignettes, introduce a subtle horror night-effect overlay, fix the joystick glow, resolve the Sphinx and Mastaba material mappings, replace the escape boat with the high-quality white motorboat, and replace the standard Unity terrain with the low-poly environment terrain mesh.

## User Review Required
> [!IMPORTANT]
> - **Exclusion of `egypt_themed_icons_generated/` folder:** Since all UI elements are fully procedural or loaded from the original `egypt_themed_icons/` folder, we are deleting the generated folder and its associated `.meta` files to clean up project assets.
> - **Removal of `#NVJOB Dynamic Sky/Example Scenes`:** We are moving `Smog.png` to `Assets/Resources/Textures/` and removing the entire `Example Scenes` directory. This will permanently clear the obsolete standard image effect script warnings (Bloom, Tonemapping) and reduce build size.
> - **Relocating Enemy AI from Inspiration Pack:** The original Inspiration Pack (`Inspiration-Thirdperson-Controller-Update372022`) contains a full duplicate Unity project. We will relocate the essential `Assets/Enemy-AI` folder to the active `Assets/Enemy-AI` folder, then delete the entire redundant root folder to free up space.
> - **Unused Asset Purges:** We will delete the demo folders for `HQ Boats` (`1.demo/`) and `LowPoly Environment Pack` (`Demo/`), the black boat assets (`boat_1.prefab`, `boat.fbx`, `boat_1_tex.tga` - which is a massive 64MB texture!), duplicate `Assets/Art/UI/Inspiration` icons, the unused `Infima Games` demo scene folder (containing hundreds of MB of baked EXR lightmaps), and legacy Python files at the root level.
> - **White Motorboat Override:** We will spawn the high-quality white motorboat (`boat_2.prefab`) instead of the old `boat.glb`. We will prevent the material override from turning its white hull into dark wood.

## Proposed Changes

---

### [Component] Legacy Assets & Code Cleanup

#### [MOVE] [Enemy-AI folder](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Inspiration-Thirdperson-Controller-Update372022/Assets/Enemy-AI) to [Assets/Enemy-AI](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Enemy-AI)
- Move the entire `Enemy-AI` directory (including its `.meta` files) into the active `Assets/` directory.

#### [DELETE] [Inspiration Project folder](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Inspiration-Thirdperson-Controller-Update372022)
- Safely delete the entire redundant project folder from the root of the workspace.

#### [DELETE] [egypt_themed_icons_generated folder](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Resources/egypt_themed_icons_generated)
- Permanently delete this folder as we now rely on the procedural button generator and raw assets.

#### [DELETE] [NVJOB Example Scenes folder](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/%23NVJOB%20Dynamic%20Sky/Example%20Scenes)
- Delete this folder to remove unused ground meshes, legacy image effect scripts, sounds, and demo scenes.
- **Note:** `Smog.png` (and its `.meta` file) will first be moved to `Assets/Resources/Textures/` to preserve the smog layer.

#### [DELETE] [LowPoly Environment Pack Demo folder](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/LowPoly%20Environment%20Pack/Demo)
- Delete the demo folder to remove unused assets.

#### [DELETE] [HQ Boats Demo and Black Boat Assets](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/HQ%20Boats)
- Delete `HQ Boats/1.demo/`.
- Delete the black boat assets to save space:
  - `HQ Boats/2.prefabs/boat_1.prefab` and `.meta`
  - `HQ Boats/3.models/boat.fbx` and `.meta`
  - `HQ Boats/4.materials/boat_1_mat.mat`, `boat_1_glass_mat.mat` and `.meta`
  - `HQ Boats/5.textures/boat_1_tex.tga` and `.meta` (saves 64MB!)

#### [DELETE] [Duplicate Inspiration UI folder](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Art/UI/Inspiration)
- Delete this duplicate icon folder; the active icons are loaded from `Assets/Resources/UI/Icons/Inspiration/`.

#### [DELETE] [Infima Games Demo Scenes](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Infima%20Games/Low%20Poly%20Shooter%20Pack%20-%20Free%20Sample/Scenes)
- Delete the entire `Scenes` directory which contains large EXR lightmaps to reduce project size.

#### [DELETE] [Legacy Python and Shell Scripts](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/)
- Delete the following unused files at the workspace root:
  - `process_ui_assets.py`
  - `remove_bg_circle.py`
  - `generate_ui_icons.py`
  - `remove_white_bg.py`
  - `blender_inspect.py`
  - `deep_asset_analysis.py`
  - `fix_namespaces.py`
  - `fix_paths.py`
  - `split_city_gen.py`
  - `split_mobile_hud.py`
  - `remove_bg.py`
  - `move_assets.sh`

#### [MODIFY] [SpriteImporter.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/Editor/SpriteImporter.cs)
- Remove the old paths referencing `egypt_themed_icons_generated/...`.
- Retain the raw paths for `egypt_themed_icons/...` so they import correctly as Sprite (2D and UI).

#### [MODIFY] [MobileHUDButtons_Sprites.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/UI/MobileHUDButtons_Sprites.cs)
- Modify `GenerateProceduralSprites()` to remove references to `egypt_themed_icons_generated/...` for sandstone frame, obsidian background, and gold trimmed button.
- Make them load directly from the procedural creators (`CreateObsidianSprite`, `CreateSlicedSandstoneFrameSprite`, and `CreateSlicedGoldTrimmedButtonSprite`).

---

### [Component] Loading Screen Redesign

#### [MODIFY] [BootManager.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/Gameplay/BootManager.cs)
- Change background load path to `"egyptian_items/BootBackground"`.
- Remove `Title` GameObject (separating text) and `LoadingIcon` GameObject (circular GPU loader), since both title text and glow designs are already baked into `BootBackground.jpeg`.
- Center and size the progress bar to perfectly overlay the hieroglyphs horizontal slot in `BootBackground.jpeg`:
  - **Width:** `755`
  - **Height:** `45`
  - **Position (anchoredPosition):** `new Vector2(0, -72)`
  - **Background color:** `new Color(0, 0, 0, 0f)` (fully transparent so the glyphs show through until covered).
  - **Fill color:** `new Color(0.0f, 0.85f, 0.35f, 0.55f)` (glowing emerald green matching the crystals).

---

### [Component] HUD Revamp (Ammo Bar & Health Bar)

#### [MODIFY] [MobileHUDButtons.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/UI/MobileHUDButtons.cs)
- Declare `private Image ammoBarFill;` and `private TextMeshProUGUI ammoCountValueText;`.
- Declare `private Image gameplayBloodVignette;`.

#### [MODIFY] [MobileHUDButtons_Layout.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/UI/MobileHUDButtons_Layout.cs)
- **Remove backgrounds:** Disable the `Image` component on the parent panels by setting `hpPanelImg.enabled = false` and `amPanelImg.enabled = false`. This removes the rectangular panels, allowing the HUD elements to float elegantly.
- **Ammo Bar redesign:**
  - Remove the 2x15 diamond tick grid completely.
  - Create a single horizontal fill bar `ammoBarFill` identical in layout to the health bar (placed at `anchoredPosition = new Vector2(90, 0)` with size `208x22`).
  - Create `ammoCountValueText` on the right of the bar (`anchoredPosition = new Vector2(308, 0)`) to display the text version (e.g. `30/30`).
- **Blood Vignette overlay:**
  - Create a full screen overlay image `GameplayBloodVignette` at the bottom of the HUD hierarchy with raycast disabled, initialized to `0` alpha.
- **Horror Night overlay:**
  - Create a full screen overlay image `HorrorOverlay` under `HUD_Root` as the first sibling, styled with a subtle green-black radial gradient (`new Color(0.02f, 0.15f, 0.05f, 0.05f)` to `new Color(0f, 0.04f, 0.01f, 0.35f)`) and set to `0.45f` alpha to overlay a creepy night/horror hue across the rendering.
- **Joystick shadow color:**
  - In `KnobGlow` setup, change the glow image color from orange to green (`new Color(0.0f, 0.9f, 0.4f, 0.5f)`).

#### [MODIFY] [MobileHUDButtons_Updates.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/UI/MobileHUDButtons_Updates.cs)
- **Update Ammo Bar Fill:**
  - Set `ammoBarFill.fillAmount = (float)c / 30f;`.
  - Dynamically assign the alchemical fill sprites based on active weapon: `sulfurBarSprite` for SULPHUR, `mercuryBarSprite` for MERCURY, and `saltBarSprite` for SALT.
  - Update `ammoCountValueText.text` to `"{c}/30"`.
- **Low-health blood vignette pulse:**
  - If player health falls below `35%` (`fillTarget < 0.35f`), fade in and dynamically pulse the `gameplayBloodVignette` opacity and rate based on how low the health is.
  - Reset opacity to `0` if health is above `35%`.

---

### [Component] Low-Poly Environment & Material Fixes

#### [MODIFY] [URPSRPBatcherFixer.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/Editor/URPSRPBatcherFixer.cs)
- Refine `isUnifiedAlbedoAsset` to ONLY exempt assets containing `column`, `pillar`, and `ladder`.
- Do NOT exempt assets containing `door` or `gate` (which was causing the `false_door_chamber_mastaba` tomb model to render with fallback textures).
- Ensure Sphinx (`sphinx`) is correctly matched as a stone asset to receive the unified albedo sandstone texture.

#### [MODIFY] [CityEnvironmentBuilder.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/Editor/CityEnvironmentBuilder.cs)
- Update the `SmogLayer` texture load path to the new moved path: `"Assets/Resources/Textures/Smog.png"`.

#### [MODIFY] [StaticEgyptianCityGenerator.partial files / CityOptimizationUtils.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/Editor/CityOptimizationUtils.cs)
- Modify `GetTerrainHeight(Vector3 pos)` and `GetTerrainNormal(Vector3 pos)`:
  - If standard terrain `Terrain.activeTerrain` is null, perform a downward raycast against `MeshCollider` objects from `Y = 250f` to locate the exact mesh height and normal.

#### [MODIFY] [StaticEgyptianCityGenerator.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/Editor/StaticEgyptianCityGenerator.cs)
- Replace standard terrain creation logic:
  - Load `"Assets/LowPoly Environment Pack/Prefabs/Terrain_1.prefab"`.
  - Instantiate and center it at `(0, 0, 0)`.
  - Scale it to `(5f, 1f, 5f)` to fit the 1000m x 1000m game boundary.
  - Convert all material shaders on the terrain to `Universal Render Pipeline/Lit`.
  - Ensure the terrain has a `MeshCollider`.
- Update the path to `"Assets/Enemy-AI/Prefabs/TestZombie.prefab"`.
- After creating the city and terrain, call a new method `SpawnLowPolyEnvironmentObjects(root)`.

#### [NEW] [CityEnvironmentSpawner_LowPoly.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/Editor/CityEnvironmentSpawner_LowPoly.cs)
- Implement `SpawnLowPolyEnvironmentObjects(GameObject root)`:
  - Spawn background mountains (`Mounting_1`, `Mounting_2`, `Mounting_3`) along the outer limits (Z > 260f for North, X > 300f for East, X < -300f for West) to create natural rocky borders.
  - Scatter rocks/stones (`Rock_1` to `Rock_6`, `Stone_1`) near mountains and around the outer desert.
  - Scatter low-poly vegetation (`Plant_1` to `Plant_7`, `Bush_1` to `Bush_3`, `Tree_1` to `Tree_3`, `Grass_1` to `Grass_2`) in open areas.
  - Ensure all instantiated objects have URP Lit materials to prevent shader fallback.

---

### [Component] Boat Replacement

#### [NEW] [boat.prefab](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Resources/boat.prefab)
- Copy the white motorboat prefab `Assets/HQ Boats/2.prefabs/boat_2.prefab` (and its `.meta`) to `Assets/Resources/boat.prefab` (and `.meta`).

#### [DELETE] [boat.glb](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Resources/boat.glb)
- Delete the old glb boat file and its `.meta` file.

#### [MODIFY] [EscapeManager.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/Gameplay/EscapeManager.cs)
- In `SpawnBoat()`:
  - Adjust spawn Y offset: spawn at `groundY + 0.5f` (sits nicely on the sand).
  - Scale the boat to `1.5f` (making it appropriately sized).
  - Rotate the boat to `Quaternion.Euler(0f, 180f, 0f)` (since it is an FBX prefab, it doesn't need the legacy glTF `-90` X-rotation offset).
  - Update `ConvertBoatMaterials()` to NOT override materials with the generic dark wood texture, keeping the original white boat color schemes intact (while still ensuring it compiles under URP Lit).
  - Update `groundY` calculation to use Physics Raycast directly if `Terrain.activeTerrain` is null.

---

## Verification Plan

### Automated Tests
- Build and compile check via Editor scripts.
- Execute `URPSRPBatcherFixer.FixMaterialsNoDialog()` to confirm correct material remapping.

### Manual Verification
- **Terrain:** Confirm that the low-poly terrain mesh is loaded, textured correctly with URP Lit shaders, and colliding correctly.
- **Mountains & Vegetation:** Check background mountains and scattered low-poly plants/rocks.
- **Escape Boat:** Ensure the white motorboat spawns at the correct scale, height, and rotation on the shoreline, and the escape sequence operates smoothly.
- **Loading Screen:** Verify background is `BootBackground.jpeg`, loading bar aligns with the hieroglyphs slot, and no duplicate text/spinner exists.
- **HUD:** Verify floating bars, continuous ammo fill bar, ammo labels (e.g. `30/30`), green joystick glow, full-screen horror overlay, and health-based blood vignettes.
- **Remapping:** Verify Sphinx and Mastaba are fully textured in sandstone.
