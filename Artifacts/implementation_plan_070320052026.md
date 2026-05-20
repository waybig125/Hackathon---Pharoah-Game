# Egyptian Desert Polish — Full Fix Plan
## For Gemini CLI Execution

---

## Summary of Issues

| # | Issue | Root Cause | File to Edit |
|---|-------|-----------|--------------|
| 1 | **Checkerboard terrain** | 2×2 pixel texture tiles too aggressively | `StaticEgyptianCityGenerator.cs` |
| 2 | **Sea not blue / not visible** | Terrain still covers sea; Quad is single-sided | `StaticEgyptianCityGenerator.cs` |
| 3 | **Sea not blocking player** | City not regenerated since barrier fix | Regenerate City |
| 4 | **Sky wrong color** | `_SkyTint` is dark navy, should be azure blue | `StaticEgyptianCityGenerator.cs` |
| 5 | **Fog wrong** | Color and mode don't match clear-day reference | `StaticEgyptianCityGenerator.cs` |
| 6 | **No shadows on trees/assets** | `shadowCastingMode` never explicitly enabled on GLB renderers | `StaticEgyptianCityGenerator.cs` |
| 7 | **Crates too small** | Scale 0.35 → needs 2.5× = 0.875 | `StaticEgyptianCityGenerator.cs` |
| 8 | **No low-poly conversion** | No mesh simplifier installed | New `manifest.json` + new Editor script |

---

## Fix 1 — Terrain Checkerboard Texture

**File:** `Assets/Scripts/Editor/StaticEgyptianCityGenerator.cs`  
**Lines:** ~266–276 (the terrain layer block)

**Root cause:** The code creates a 2×2 pixel `Texture2D` for the sand. Unity tiles this at `15×15` world units → creates a visible checkerboard grid at play distance.

**Replace this block:**
```csharp
if (layer == null) {
    layer = new TerrainLayer();
    var sandTex = new Texture2D(2, 2);
    sandTex.SetPixels(new Color[] { new Color(0.91f, 0.81f, 0.62f), new Color(0.91f, 0.81f, 0.62f), new Color(0.91f, 0.81f, 0.62f), new Color(0.91f, 0.81f, 0.62f) });
    sandTex.Apply();
    layer.diffuseTexture = sandTex;
    layer.tileSize = new Vector2(15f, 15f);
    AssetDatabase.CreateAsset(layer, layerPath);
}
```

**With this:**
```csharp
if (layer == null) {
    layer = new TerrainLayer();

    // Try to load a real sand texture from project assets first
    Texture2D sandTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EgyptianAssets/desert_sand_albedo.png");
    if (sandTex == null) sandTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EgyptianAssets/sand_diffuse.png");
    if (sandTex == null) sandTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EgyptianAssets/sand.png");

    if (sandTex == null) {
        // Fallback: create a 128×128 smooth gradient noise texture (no checkerboard)
        sandTex = new Texture2D(128, 128, TextureFormat.RGB24, true);
        Color baseColor = new Color(0.88f, 0.78f, 0.58f);
        Color[] pixels = new Color[128 * 128];
        for (int py = 0; py < 128; py++)
            for (int px = 0; px < 128; px++) {
                float n = Mathf.PerlinNoise(px * 0.12f, py * 0.12f);
                pixels[py * 128 + px] = Color.Lerp(baseColor * 0.88f, baseColor * 1.08f, n);
            }
        sandTex.SetPixels(pixels);
        sandTex.Apply(true);
        AssetDatabase.CreateAsset(sandTex, "Assets/EgyptianAssets/SandTexProc_128.asset");
    }

    layer.diffuseTexture = sandTex;
    layer.tileSize = new Vector2(40f, 40f); // Large tile = no visible repetition
    layer.specular = new Color(0.05f, 0.04f, 0.02f, 0f); // Very low specular for matte sand
    AssetDatabase.CreateAsset(layer, layerPath);
}
// Always reassign tile size even on existing layers
layer.tileSize = new Vector2(40f, 40f);
EditorUtility.SetDirty(layer);
```

---

## Fix 2 — Sea: Blue Color + Double-Sided Material + Correct Y Height

**File:** `Assets/Scripts/Editor/StaticEgyptianCityGenerator.cs`  
**Method:** `CreateSeaAndCoastline()` (~line 330)

**Replace the `seaMat` block and sea position:**
```csharp
// OLD (broken):
sea.transform.position = new Vector3(0f, 0.12f, -700f);
var seaMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
seaMat.SetColor("_BaseColor", new Color(0.03f, 0.18f, 0.38f)); // Deep navy
```

**With:**
```csharp
// ── DEEP OCEAN ──
// Terrain at z=-500, flattened south of tx=0.42 → world Z=-80. Terrain Y at flat zone ≈ -0.05f + 0.001*15 = -0.035f.
// Sea Y must be above -0.035f. Set Y=0.15 to ensure it's visible above terrain.
sea.transform.position = new Vector3(0f, 0.15f, -450f);   // centered in the south flat zone
sea.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
sea.transform.localScale = new Vector3(3000f, 700f, 1f);   // 3km wide, 700m depth (Z:-100 to -800)

var seaMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
seaMat.SetColor("_BaseColor", new Color(0.02f, 0.25f, 0.55f));     // Rich ocean blue
seaMat.SetFloat("_Metallic", 0.0f);
seaMat.SetFloat("_Smoothness", 0.95f);                              // Near-mirror water surface
seaMat.SetColor("_EmissionColor", new Color(0.05f, 0.30f, 0.60f) * 1.5f);
seaMat.EnableKeyword("_EMISSION");
seaMat.SetFloat("_Cull", 0f);                                       // Double-sided: visible from above AND below
seaMat.SetInt("_ZWrite", 1);
```

Also update shallows, surf, beach Y to be slightly higher than sea:
```csharp
// Shallows at Z=-200, Y=0.17
shallows.transform.position = new Vector3(0f, 0.17f, -200f);

// Surf at Z=-140, Y=0.19 (brightest, closest to city)
surf.transform.position = new Vector3(0f, 0.19f, -140f);

// Beach at Z=-110, Y=0.21 (sand strip between city and surf)
beach.transform.position = new Vector3(0f, 0.21f, -110f);
beach.transform.localScale = new Vector3(3000f, 40f, 1f);
```

---

## Fix 3 — Sky Color (Match Reference Image)

**File:** `Assets/Scripts/Editor/StaticEgyptianCityGenerator.cs`  
**Method:** `SetupEnvironment()` (~line 427)

The reference image shows a **bright clear blue sky** with warm golden buildings. The current sky is dark navy.

**Replace:**
```csharp
// OLD:
var skyMat = new Material(Shader.Find("Skybox/Procedural"));
skyMat.SetFloat("_SunSize", 0.04f);
skyMat.SetFloat("_AtmosphereThickness", 0.9f);
skyMat.SetColor("_SkyTint", new Color(0.1f, 0.24f, 0.42f));
skyMat.SetColor("_GroundColor", new Color(0.78f, 0.47f, 0.25f));
skyMat.SetFloat("_Exposure", 1.3f);
```

**With:**
```csharp
// NEW: Clear daytime Egyptian sky — bright azure blue
var skyMat = new Material(Shader.Find("Skybox/Procedural"));
skyMat.SetFloat("_SunSize", 0.05f);
skyMat.SetFloat("_SunSizeConvergence", 10f);
skyMat.SetFloat("_AtmosphereThickness", 1.1f);
skyMat.SetColor("_SkyTint", new Color(0.38f, 0.62f, 0.92f));       // ← Clear azure blue
skyMat.SetColor("_GroundColor", new Color(0.72f, 0.58f, 0.38f));   // Sandy dunes at horizon
skyMat.SetFloat("_Exposure", 1.6f);                                  // Bright midday sun
```

Also update ambient lighting to match bright day:
```csharp
// OLD:
RenderSettings.ambientSkyColor    = new Color(0.24f, 0.17f, 0.42f) * 0.6f;
RenderSettings.ambientEquatorColor = new Color(0.91f, 0.56f, 0.35f) * 0.8f;
RenderSettings.ambientGroundColor  = new Color(0.10f, 0.06f, 0.15f) * 0.4f;

// NEW:
RenderSettings.ambientSkyColor    = new Color(0.50f, 0.75f, 1.00f) * 0.65f;   // Bright sky bounce
RenderSettings.ambientEquatorColor = new Color(0.95f, 0.82f, 0.62f) * 0.90f;  // Warm wall bounce
RenderSettings.ambientGroundColor  = new Color(0.60f, 0.50f, 0.35f) * 0.40f;  // Sandy ground bounce
```

And the sun:
```csharp
// OLD:
sun.color = new Color(1f, 0.96f, 0.84f); sun.intensity = 1.4f;
sun.shadows = LightShadows.Hard; sun.shadowResolution = LightShadowResolution.High;
sun.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

// NEW:
sun.color = new Color(1f, 0.98f, 0.90f);   // Slightly warmer white
sun.intensity = 1.8f;                        // Brighter midday sun
sun.shadows = LightShadows.Soft;
sun.shadowResolution = LightShadowResolution.VeryHigh;
sun.shadowDistance = 150f;                   // Enough for city shadows
sun.transform.rotation = Quaternion.Euler(55f, -25f, 0f);  // Higher sun angle = shorter shadows
```

---

## Fix 4 — Fog (Match Reference: Clear + Warm Haze at Horizon)

**File:** `Assets/Scripts/Editor/StaticEgyptianCityGenerator.cs`  
**Method:** `SetupEnvironment()` (~line 442)

Reference shows clear visibility up close, gentle sandy haze far away.

**Replace:**
```csharp
// OLD:
RenderSettings.fog = true;
RenderSettings.fogColor = new Color(0.82f, 0.58f, 0.32f);
RenderSettings.fogDensity = 0.0018f;

// NEW: Linear fog with long clear range (matches reference image)
RenderSettings.fog = true;
RenderSettings.fogMode = FogMode.Linear;
RenderSettings.fogColor = new Color(0.92f, 0.84f, 0.68f);  // Warm sandy haze at horizon
RenderSettings.fogStartDistance = 120f;   // Clear up close
RenderSettings.fogEndDistance  = 400f;   // Fully fogged at 400m
```

Also update PostProcessing ColorAdjustments to be less dark:
```csharp
// OLD:
colorAdj.contrast.Override(15f); colorAdj.saturation.Override(12f);
colorAdj.colorFilter.Override(new Color(1f, 0.95f, 0.88f));

// NEW:
colorAdj.contrast.Override(8f);           // Less crushed blacks
colorAdj.saturation.Override(18f);        // More vibrant colors
colorAdj.colorFilter.Override(new Color(1f, 0.97f, 0.92f)); // Slight warm filter
```

---

## Fix 5 — Enable Shadows on Trees and All GLB Assets

**File:** `Assets/Scripts/Editor/StaticEgyptianCityGenerator.cs`  
**Method:** `AlignToGroundAndAddCollider()` (~line 695, after the URP shader converter loop)

Trees and GLB models from the asset store sometimes import with `shadowCastingMode = Off`.

**Add this block** right after the `foreach (var r in allRenderers)` shader conversion loop (~line 723):
```csharp
// ── Enable shadows on all child renderers ──
foreach (var r in allRenderers)
{
    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
    r.receiveShadows = true;
}
```

Also increase shadow distance on the camera. In `SetupEnvironment()` or a new `SetupShadowQuality()` method:
```csharp
// Add this in SetupEnvironment():
QualitySettings.shadowDistance = 120f;
QualitySettings.shadowCascades = 2;
QualitySettings.shadowProjection = ShadowProjection.CloseFit;
```

---

## Fix 6 — Crate Scale (2.5× Current)

**File:** `Assets/Scripts/Editor/StaticEgyptianCityGenerator.cs`  
**Method:** `BuildHouse()` (~line 517)

Current scales: `0.35f` (base crate), `0.30f` (stacked crate), `0.14f` (barrel).  
2.5× = `0.875f`, `0.75f`, `0.35f`.

**Replace:**
```csharp
// OLD:
cObj.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
// ...
cObj2.transform.localScale = new Vector3(0.30f, 0.30f, 0.30f);
// ...
bObj.transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);

// NEW (2.5× all):
cObj.transform.localScale = new Vector3(0.875f, 0.875f, 0.875f);
// ...
cObj2.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
// ...
bObj.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);  // barrels 2.5× too
```

---

## Fix 7 — Low-Poly Mesh Simplifier

### Step A: Add Package to manifest.json

**File:** `Assets/../Packages/manifest.json`  
**Full path:** `Hackathon - Pharoah Game/Packages/manifest.json`

Add this line inside the `"dependencies"` block:
```json
"com.whinarn.unitymeshsimplifier": "https://github.com/Whinarn/UnityMeshSimplifier.git#v3.1.0",
```

### Step B: Create the Editor Script

**New file:** `Assets/Scripts/Editor/MeshDecimatorTool.cs`

```csharp
using UnityEngine;
using UnityEditor;
using UnityMeshSimplifier;

namespace TheAlchemistsCrypt.Editor
{
    public class MeshDecimatorTool : EditorWindow
    {
        [MenuItem("Egyptian/Low-Poly Decimator", false, 10)]
        public static void ShowWindow() => GetWindow<MeshDecimatorTool>("Low-Poly Decimator");

        [Range(0.05f, 1.0f)]
        private float quality = 0.3f; // 30% of original polygon count
        private bool processChildren = true;
        private bool saveMeshAsset = true;

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Select one or more GameObjects in the Hierarchy, set quality, then click Decimate.\n" +
                "Quality 1.0 = original. 0.3 = 30% of original polygon count.",
                MessageType.Info);

            quality = EditorGUILayout.Slider("Quality (polygon %)", quality, 0.05f, 1.0f);
            processChildren = EditorGUILayout.Toggle("Process Children", processChildren);
            saveMeshAsset = EditorGUILayout.Toggle("Save Mesh Asset", saveMeshAsset);

            EditorGUILayout.Space();
            if (GUILayout.Button("▶ DECIMATE SELECTED", GUILayout.Height(40)))
            {
                DecimateSelected();
            }
        }

        private void DecimateSelected()
        {
            var selected = Selection.gameObjects;
            if (selected.Length == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "Select at least one GameObject in the Hierarchy.", "OK");
                return;
            }

            int meshCount = 0;
            foreach (var go in selected)
            {
                var filters = processChildren
                    ? go.GetComponentsInChildren<MeshFilter>(true)
                    : go.GetComponents<MeshFilter>();

                foreach (var mf in filters)
                {
                    if (mf.sharedMesh == null) continue;

                    Mesh original = mf.sharedMesh;
                    int originalTris = original.triangles.Length / 3;

                    var simplifier = new MeshSimplifier();
                    simplifier.Initialize(original);
                    simplifier.SimplifyMesh(quality);

                    Mesh simplified = simplifier.ToMesh();
                    simplified.name = original.name + "_LP";

                    if (saveMeshAsset)
                    {
                        string dir = "Assets/GeneratedMeshes";
                        if (!System.IO.Directory.Exists(dir))
                            System.IO.Directory.CreateDirectory(dir);

                        string path = $"{dir}/{simplified.name}.asset";
                        AssetDatabase.CreateAsset(simplified, path);
                        simplified = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    }

                    mf.sharedMesh = simplified;
                    meshCount++;

                    Debug.Log($"[Decimator] {go.name}/{mf.name}: {originalTris} → {simplified.triangles.Length / 3} tris ({quality * 100:F0}%)");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Decimator] Done. Decimated {meshCount} meshes.");
        }
    }
}
```

---

## Fix 8 — HiveMind API Key (Revoked)

> [!CAUTION]
> The Gemini API key `AIzaSyBylvZf-NW1r_jMvy5nMPqBl3jYDwZY5bI` was committed to git and **Google revoked it** (403 PERMISSION_DENIED). All HiveMind AI calls fail silently → fallback mode.

**Required action (manual):**
1. Go to https://aistudio.google.com/apikey
2. Create a **new** API key
3. In Railway dashboard → your `alchemists-crypt-ai` service → **Variables** tab
4. Update `GEMINI_API_KEY` to the new value
5. Redeploy the service

The Unity-side fallback now returns `chase/flank` behavior instead of `idle`, so mummies move even with the API down.

---

## Execution Order

After making all code changes, in Unity Editor:

1. **Do NOT manually save** — the generator calls `EditorSceneManager.SaveScene()` automatically
2. Go to menu: `Egyptian → Generate & Setup City`
3. Wait ~30 seconds for generation + NavMesh bake
4. Press Play and verify:
   - Terrain = smooth sandy texture (no checkerboard)
   - Sky = bright blue
   - Sea = visible blue south of Z=-95 barrier
   - Fog = clear close up, warm haze at distance
   - Trees/columns cast shadows
   - Crates noticeably larger
   - Mummies patrol/chase (not frozen)
