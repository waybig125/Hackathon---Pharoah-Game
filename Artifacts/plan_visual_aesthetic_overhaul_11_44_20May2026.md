# Visual & Aesthetic Overhaul — Low-Poly Desert Style
**Created:** 2026-05-20 11:44 (local)

## Goal
Shift the game's look from the current "muddy orange/brown + flat lighting" to the vibrant
stylized low-poly desert aesthetic visible in the reference image:
- Warm peach/terracotta buildings with crisp facet shading
- Lavender/cool-indigo shadow tones
- Bright gradient skybox (peach horizon → deep teal zenith)
- Glowing emissive gun with stylized flat-shading
- Clean, legible UI with gold circular buttons

---

## 1. Color Palette — The Rule of Three Tones

### Target Zones
| Zone | Role | Color (Hex) | Unity Color |
|---|---|---|---|
| **Highlight** | Direct sunlight on faces | `#F5E6C3` | `(0.96, 0.90, 0.76)` |
| **Mid-tone** | Base sand / wall base | `#D4935A` | `(0.83, 0.58, 0.35)` |
| **Shadow** | Ambient / AO creases | `#6B5B8C` | `(0.42, 0.36, 0.55)` |

### Current vs Target (key materials)
| Asset | Current | Target |
|---|---|---|
| Building walls | Orange-brown `#C07840` | Terracotta ochre `#C8845A` |
| Ground/sand terrain | Warm tan `#D4A878` | Pale pastel `#E8CFA0` + lavender shadow |
| Building shadow faces | Same orange | Cool indigo `#6B5B8C` |
| Sky | Flat fog color `#EBDFA3` | Gradient peach→teal (see Skybox section) |
| Weapon (bright parts) | Flat orange | Emissive orange `#FF6600 * 2.5` with Bloom |
| Weapon (dark parts) | Dark brown | Deep charcoal `#1A1A2E` with slight blue tint |

### Where to Apply
- **`SetupEnvironment()` in `StaticEgyptianCityGenerator.cs`**: Update fog color to warm peach `#F5C895` and reduce density to `0.0015`.
- **`BuildHouse()` in `StaticEgyptianCityGenerator.cs`**: Change `wallMat` base color to terracotta; add a separate `shadowFaceMat` in deep lavender for north/bottom faces.
- **`FixPlayerAndWeapons()` in `StaticEgyptianCityGenerator.cs`**: Apply emissive weapon materials.

---

## 2. Lighting

### Directional Light ("Sun")
| Property | Current | Target |
|---|---|---|
| Color | Warm white | Pale warm yellow `#FFF5D6` |
| Intensity | ~1.0 | `1.4` (brighter baked facets) |
| Shadow Type | Soft | **Hard** (crisp low-poly lines) |
| Shadow Resolution | Medium | High |
| Rotation | Default | ~`(45°, -30°, 0°)` — low-angle desert sun |

**How to apply:**
- In `SetupEnvironment()`, find or create the Directional Light:
  ```csharp
  var sun = GameObject.Find("Directional Light")?.GetComponent<Light>();
  if (sun != null) {
      sun.color = new Color(1f, 0.96f, 0.84f);
      sun.intensity = 1.4f;
      sun.shadows = LightShadows.Hard;
      sun.shadowResolution = UnityEngine.Rendering.LightShadowResolution.High;
      sun.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
  }
  ```

### Ambient Light
- Switch from Skybox ambient to **Gradient** mode:
  - Sky (top): Cool indigo `#3D2B6B` (low intensity `0.6`)
  - Equator: Warm orange-peach `#E8905A` (mid)
  - Ground: Deep shadow purple `#1A1025` (low)
- In Unity Editor: `Window → Rendering → Lighting → Environment → Source: Gradient`
- **Programmatically** in `SetupEnvironment()`:
  ```csharp
  RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
  RenderSettings.ambientSkyColor    = new Color(0.24f, 0.17f, 0.42f) * 0.6f;
  RenderSettings.ambientEquatorColor = new Color(0.91f, 0.56f, 0.35f) * 0.8f;
  RenderSettings.ambientGroundColor  = new Color(0.10f, 0.06f, 0.15f) * 0.4f;
  ```

---

## 3. Skybox — Gradient 2-Tone

Replace the current flat fog/skybox with a procedural gradient skybox.

### Target Look
- **Horizon/bottom**: Warm peach `#F5C07A` blending to soft rose
- **Zenith/top**: Deep teal-blue `#1A4A6B` or indigo `#2D2060`
- Matches the reference image exactly (bright at horizon, deep blue at top)

### Options (choose one)

**Option A — Built-in Procedural Skybox (fastest)**
- Use Unity's built-in `Skybox/Procedural` shader:
  - Sun Size: `0.04`
  - Sun Size Convergence: `5`
  - Atmosphere Thickness: `0.9`
  - Sky Tint: `#1A3D6B`
  - Ground: `#C87840`
  - Exposure: `1.3`
- Apply via `RenderSettings.skybox = proceduralMat;` in `SetupEnvironment()`.

**Option B — Custom Gradient Skybox (better quality, more control)**
Create `Assets/Materials/Skybox_DesertGradient.mat` using `Skybox/6 Sided` or a simple gradient shader:
```hlsl
// Simplified gradient skybox concept
half4 col = lerp(_HorizonColor, _ZenithColor, saturate(IN.worldPos.y * _GradientPower));
```
- `_HorizonColor` = `#F5C07A`
- `_ZenithColor` = `#1A2D5A`
- Apply to RenderSettings.skybox

### In Code (`SetupEnvironment()`)
```csharp
var skyMat = new Material(Shader.Find("Skybox/Procedural"));
skyMat.SetFloat("_SunSize", 0.04f);
skyMat.SetFloat("_AtmosphereThickness", 0.9f);
skyMat.SetColor("_SkyTint", new Color(0.1f, 0.24f, 0.42f));
skyMat.SetColor("_GroundColor", new Color(0.78f, 0.47f, 0.25f));
skyMat.SetFloat("_Exposure", 1.3f);
RenderSettings.skybox = skyMat;
RenderSettings.sun = sun; // link directional light as the sun disc
DynamicGI.UpdateEnvironment();
```

---

## 4. Post-Processing (URP Volume)

### Required Overrides
Add or update the URP Global Volume in `SetupEnvironment()`.

| Override | Property | Value | Why |
|---|---|---|---|
| **Bloom** | Intensity | `0.35` | Weapon glow, lit windows |
| Bloom | Threshold | `0.8` | Only emissive parts glow |
| Bloom | Scatter | `0.4` | Tight, not blurry |
| **Color Adjustments** | Contrast | `+15` | Pops low-poly facets |
| Color Adjustments | Saturation | `+12` | Vivid without oversaturation |
| Color Adjustments | Color Filter | `#FFF3E0` | Warm overall tone |
| **Ambient Occlusion** | Mode | SSAO | Creases in buildings |
| AO | Intensity | `1.2` | Strong crease darkening |
| AO | Radius | `0.15` | Small — only tight corners |
| AO | Quality | Medium | Mobile-safe |
| **Tonemapping** | Mode | ACES | Film-grade color compression |
| **Vignette** | Intensity | `0.25` | Subtle focus pull |
| Vignette | Color | `#2D1A0A` | Dark warm edges |

### Code Location
In `SetupEnvironment()` — find or create the global volume:
```csharp
var volGo = GameObject.Find("GlobalVolume") ?? new GameObject("GlobalVolume");
var vol = volGo.GetComponent<Volume>() ?? volGo.AddComponent<Volume>();
vol.isGlobal = true;
vol.priority = 10;
var profile = ScriptableObject.CreateInstance<VolumeProfile>();
// Add overrides programmatically or reference a pre-made profile asset
vol.sharedProfile = profile;
```

---

## 5. Building Materials — Flat Shading with Purple Shadows

### Core Technique
Low-poly buildings get their drama from **face normals** — each face is a flat polygon
and receives its own lighting calculation. The shadow faces (facing away from sun) 
should shift toward cool lavender/indigo.

### Wall Material Setup
```csharp
// wallMat (sun-facing) — terracotta highlight
wallMat.SetColor("_BaseColor", new Color(0.83f, 0.58f, 0.35f));  // #D4935A
wallMat.SetFloat("_Smoothness", 0.0f);   // Completely matte, no shine

// Shadow faces get a different material OR rely on Trilight ambient
// With Trilight ambient set to purple, shadow faces auto-shift purple
// No extra material needed if ambient is correctly set.
```

### Window Materials (glowing)
```csharp
// litWindowMat — warm amber glow
litWindowMat.SetColor("_BaseColor", new Color(1f, 0.75f, 0.3f));
litWindowMat.SetColor("_EmissionColor", new Color(1f, 0.65f, 0.2f) * 3.5f);
litWindowMat.EnableKeyword("_EMISSION");
```

---

## 6. Weapon Visual Upgrade

### Target Look (from reference image)
- Main body: Deep charcoal `#1A1A2E` — flat shading, zero smoothness
- Hot parts (barrel, charging handle): Bright emissive orange `#FF6600 * 2.5` 
- Glowing runes/engravings: Teal `#00CCAA * 3.0`
- Overall: Looks like an enchanted artifact, not a real gun

### Implementation in `FixPlayerAndWeapons()`
```csharp
// After inventory is found:
foreach (Transform weapon in inv2.transform) {
    foreach (var rend in weapon.GetComponentsInChildren<Renderer>()) {
        foreach (var mat in rend.sharedMaterials) {
            if (mat == null) continue;
            string mName = mat.name.ToLower();
            
            if (mName.Contains("body") || mName.Contains("frame") || mName.Contains("stock")) {
                mat.SetColor("_BaseColor", new Color(0.10f, 0.10f, 0.18f));
                mat.SetFloat("_Smoothness", 0.05f);
            } else if (mName.Contains("barrel") || mName.Contains("grip") || mName.Contains("trigger")) {
                mat.SetColor("_BaseColor", new Color(0.8f, 0.3f, 0.0f));
                mat.SetColor("_EmissionColor", new Color(1f, 0.4f, 0f) * 2.5f);
                mat.EnableKeyword("_EMISSION");
            } else {
                // Default: apply alchemical teal glow
                mat.SetColor("_EmissionColor", new Color(0f, 0.8f, 0.67f) * 2.0f);
                mat.EnableKeyword("_EMISSION");
            }
        }
    }
}
```

---

## 7. Terrain Texture

### Current
Single sandy tan layer.

### Target
- Primary layer: Pale pastel sand `#E8CFA0` — light, almost cream, not orange
- The Trilight ambient with purple shadows handles the shading variation automatically

### In `GeneratePolishedCity()` terrain layer:
```csharp
var layer = new TerrainLayer();
// Soft pale sand — key to making shadows look purple not brown
layer.diffuseTexture = CreateSolidColorTex(new Color(0.91f, 0.81f, 0.62f));
layer.tileSize = new Vector2(15f, 15f);
```

---

## 8. Files to Modify

| File | Changes |
|---|---|
| `Assets/Scripts/Editor/StaticEgyptianCityGenerator.cs` | `SetupEnvironment()`: skybox, ambient trilight, fog color, directional light; `BuildHouse()`: wall material color; `FixPlayerAndWeapons()`: weapon emissive materials; terrain layer color |
| `Assets/Scripts/UI/MinimapUI.cs` | Minor: ensure minimap background is dark sand not generic black |
| (NEW) `Assets/Materials/Skybox_DesertGradient.mat` | If Option B skybox is chosen — create via AssetDatabase in SetupEnvironment |

---

## 9. Implementation Order

```
Step 1 — Ambient & Lighting (biggest visual impact, no code risk)
  SetupEnvironment(): Trilight ambient, directional light hard shadows, 
  low-angle sun rotation, fog color to warm peach.

Step 2 — Skybox
  Create procedural skybox material, assign to RenderSettings.

Step 3 — Terrain Palette
  Update TerrainLayer diffuse to pale pastel sand.

Step 4 — Building Materials
  Update wallMat, woodMat base colors. Boost litWindowMat emission.

Step 5 — Post-Processing Volume
  Add/update GlobalVolume: Bloom, SSAO, Color Adjustments, ACES tonemapping.

Step 6 — Weapon Emissives
  In FixPlayerAndWeapons(), apply per-material emissive logic.

Step 7 — Regenerate City & Validate
  Run Egyptian → Generate & Setup City in editor. Check minimap,
  check sea visibility, check FPS of post-processing on device.
```

---

## Quick Reference — All Color Values

```
Highlight sun face:   #F5E6C3  (0.96, 0.90, 0.76)
Mid-tone wall:        #D4935A  (0.83, 0.58, 0.35)
Shadow ambient:       #6B5B8C  (0.42, 0.36, 0.55)
Sand terrain:         #E8CFA0  (0.91, 0.81, 0.62)
Fog / horizon:        #F5C07A  (0.96, 0.75, 0.48)
Sky zenith:           #1A2D5A  (0.10, 0.18, 0.35)
Emissive window:      #FF9933  * 3.5
Emissive weapon hot:  #FF6600  * 2.5
Emissive rune teal:   #00CCAA  * 3.0
Sea deep:             #0A3D6B  (0.04, 0.24, 0.42)
Sea shallow:          #1A7AAF  (0.10, 0.48, 0.68)
Beach sand:           #F0D080  (0.94, 0.82, 0.50)
```
