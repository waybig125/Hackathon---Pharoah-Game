using UnityEngine;
using Unity.AI.Navigation;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityMeshSimplifier;

namespace TheAlchemistsCrypt.Editor
{
    public partial class StaticEgyptianCityGenerator
    {
        private void CreateSeaAndCoastline(GameObject root)
                {
                    GameObject sea = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    sea.name = "SeaZone";
                    sea.transform.SetParent(root.transform);
                    sea.transform.position = new Vector3(0f, 0.8f, -300f); 
                    sea.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    sea.transform.localScale = new Vector3(3000f, 400f, 1f); 

                    var seaMat = new Material(GetLitShader());
                    Color ultraBlue = new Color(0f, 0.4f, 1f, 1f);
                    seaMat.SetColor("_BaseColor", ultraBlue); 
                    seaMat.SetColor("_EmissionColor", ultraBlue * 6f); 
                    seaMat.EnableKeyword("_EMISSION");
                    seaMat.SetFloat("_Smoothness", 0.99f); 
                    seaMat.SetFloat("_Metallic", 0.95f);
                    seaMat.enableInstancing = true;
                    sea.GetComponent<Renderer>().sharedMaterial = seaMat;
                    sea.isStatic = true;
                    DestroyImmediate(sea.GetComponent<Collider>());

                    GameObject shallows = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    shallows.name = "SeaZone_Shallow";
                    shallows.transform.SetParent(root.transform);
                    shallows.transform.position = new Vector3(0f, 0.85f, -100f);
                    shallows.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    shallows.transform.localScale = new Vector3(3000f, 40f, 1f);

                    var shallowMat = new Material(GetLitShader());
                    Color shallowBlue = new Color(0.1f, 0.6f, 1f, 0.95f);
                    shallowMat.SetColor("_BaseColor", shallowBlue);
                    shallowMat.SetColor("_EmissionColor", shallowBlue * 2f);
                    shallowMat.EnableKeyword("_EMISSION");
                    shallowMat.SetFloat("_Smoothness", 0.95f);
                    shallowMat.enableInstancing = true;
                    shallows.GetComponent<Renderer>().sharedMaterial = shallowMat;
                    shallows.isStatic = true;
                    DestroyImmediate(shallows.GetComponent<Collider>());

                    // BeachZone sand floor quad restored
                    GameObject beach = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    beach.name = "BeachZone";
                    beach.transform.SetParent(root.transform);
                    beach.transform.position = new Vector3(0f, 0.9f, -60f);
                    beach.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    beach.transform.localScale = new Vector3(3000f, 40f, 1f);

                    var beachMat = new Material(GetLitShader());
                    beachMat.SetColor("_BaseColor", new Color(0.85f, 0.75f, 0.6f, 1f)); 
                    beachMat.SetFloat("_Smoothness", 0.1f);
                    beachMat.enableInstancing = true;
                    beach.GetComponent<Renderer>().sharedMaterial = beachMat;
                    beach.isStatic = true;
                    DestroyImmediate(beach.GetComponent<Collider>());

                    // Split the coastline barrier into a left section and a right section to leave a gap at X = 0
                    GameObject barrierLeft = new GameObject("CoastlineBarrierLeft");
                    barrierLeft.transform.SetParent(root.transform);
                    barrierLeft.transform.position = new Vector3(-2504f, 10f, -100f);
                    var bcLeft = barrierLeft.AddComponent<BoxCollider>();
                    bcLeft.size = new Vector3(5000f, 30f, 5f);
                    barrierLeft.isStatic = true;

                    GameObject barrierRight = new GameObject("CoastlineBarrierRight");
                    barrierRight.transform.SetParent(root.transform);
                    barrierRight.transform.position = new Vector3(2504f, 10f, -100f);
                    var bcRight = barrierRight.AddComponent<BoxCollider>();
                    bcRight.size = new Vector3(5000f, 30f, 5f);
                    barrierRight.isStatic = true;

                    Debug.Log("[CityGen] Sea visible and ultra reflective. Substantial barrier at Z=-100.");
                }

        private void CreateWorldBounds(GameObject root)
                {
                    var boundsObj = new GameObject("WorldBounds");
                    boundsObj.transform.SetParent(root.transform);
                    boundsObj.isStatic = true;

                    // North
                    var bcN = boundsObj.AddComponent<BoxCollider>();
                    bcN.center = new Vector3(0f, 100f, 495f);
                    bcN.size = new Vector3(1000f, 200f, 10f);
                    
                    // South
                    var bcS = boundsObj.AddComponent<BoxCollider>();
                    bcS.center = new Vector3(0f, 100f, -495f);
                    bcS.size = new Vector3(1000f, 200f, 10f);

                    // East
                    var bcE = boundsObj.AddComponent<BoxCollider>();
                    bcE.center = new Vector3(495f, 100f, 0f);
                    bcE.size = new Vector3(10f, 200f, 1000f);

                    // West
                    var bcW = boundsObj.AddComponent<BoxCollider>();
                    bcW.center = new Vector3(-495f, 100f, 0f);
                    bcW.size = new Vector3(10f, 200f, 1000f);

                    // Floor (underneath to catch any fallers just in case)
                    var bcF = boundsObj.AddComponent<BoxCollider>();
                    bcF.center = new Vector3(0f, -10f, 0f);
                    bcF.size = new Vector3(1200f, 5f, 1200f);
                }

        private void SetupEnvironment(GameObject root)
                {
                    // Warm amber desert fog — matches the sandy horizon
                    Color amberFogColor = new Color(0.85f, 0.62f, 0.30f);

                    // ── Remove any old NVJOB sky domes from previous generations ──
                    foreach (var oldSky in new string[] { "Sky 2 (Red)", "Sky 2 (Day)", "NVJOBSky", "DynamicSky" }) {
                        var old = GameObject.Find(oldSky);
                        if (old != null) DestroyImmediate(old);
                    }

                    // Use Sky 2 (Day) — renders warm amber/orange through URP without the pink shift
                    // Sky 2 (Red) looks pink under URP post-processing because its horizon hue
                    // sits in the magenta range before tonemapping is applied.
                    string nvjobSkyPrefabPath = "Assets/#NVJOB Dynamic Sky/Examples Sky/Sky 2 (Day)/Sky 2 (Day).prefab";
                    GameObject nvjobSkyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(nvjobSkyPrefabPath);
                    if (nvjobSkyPrefab != null) {
                        var skyDome = PrefabUtility.InstantiatePrefab(nvjobSkyPrefab, root.transform) as GameObject;
                        if (skyDome != null) {
                            skyDome.name = "NVJOBSky";
                            var dynamicSkyComp = skyDome.GetComponent<DynamicSky>() ?? skyDome.AddComponent<DynamicSky>();
                            var playerGo = GameObject.FindWithTag("Player");
                            if (playerGo != null) dynamicSkyComp.player = playerGo.transform;
                            // Very slow cloud rotation for mobile performance
                            dynamicSkyComp.ssgUvRotateSpeed = 0.15f;
                            dynamicSkyComp.sky2d = true;
                        }
                    }

                    // Point the scene skybox at the Day horizon material (warm orange, not pink)
                    string horizonMatPath = "Assets/#NVJOB Dynamic Sky/Examples Sky/Sky 2 (Day)/Horizon.mat";
                    Material skyMat = AssetDatabase.LoadAssetAtPath<Material>(horizonMatPath);
                    if (skyMat == null) {
                        // Fallback: procedural gradient skybox if NVJOB Day mat is missing
                        skyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/SkyGradientBox.mat");
                    }
                    if (skyMat != null) RenderSettings.skybox = skyMat;
                    RenderSettings.ambientMode = AmbientMode.Skybox;

                    // Android texture compression for sky textures
                    string[] skyTextures = {
                        "Assets/#NVJOB Dynamic Sky/Examples Sky/Textures/Tx1.png",
                        "Assets/#NVJOB Dynamic Sky/Examples Sky/Textures/Tx2.png",
                        "Assets/#NVJOB Dynamic Sky/Examples Sky/Textures/Tx3.png"
                    };
                    foreach (var txPath in skyTextures) {
                        var importer = AssetImporter.GetAtPath(txPath) as TextureImporter;
                        if (importer != null) {
                            TextureImporterPlatformSettings androidSettings = importer.GetPlatformTextureSettings("Android");
                            if (!androidSettings.overridden || androidSettings.maxTextureSize > 512) {
                                androidSettings.overridden = true;
                                androidSettings.maxTextureSize = 512;
                                androidSettings.format = TextureImporterFormat.ASTC_6x6;
                                importer.SetPlatformTextureSettings(androidSettings);
                                importer.SaveAndReimport();
                            }
                        }
                    }

                    RenderSettings.fog = true;
                    RenderSettings.fogColor = amberFogColor;
                    RenderSettings.fogMode = FogMode.Linear;
                    RenderSettings.fogStartDistance = 60f;
                    RenderSettings.fogEndDistance = 350f;

                    var sun = GameObject.Find("Directional Light")?.GetComponent<Light>();
                    if (sun != null) {
                        sun.color = new Color(1.0f, 0.82f, 0.55f); // Warm golden sunlight, not orange-red
                        sun.intensity = 1.8f;
                        sun.shadows = LightShadows.Soft;
                        sun.shadowStrength = 0.75f;
                        sun.transform.rotation = Quaternion.Euler(30f, -60f, 0f); // Long shadows but not extreme
                    }

                    SetupPostProcessing(root.transform);
                    DynamicGI.UpdateEnvironment();
                    AssetDatabase.SaveAssets();
                }

        private void SetupPostProcessing(Transform parent)
                {
                    // Remove any existing GlobalVolume to avoid stacking profiles
                    var existingVol = parent.Find("GlobalVolume");
                    if (existingVol != null) DestroyImmediate(existingVol.gameObject);

                    var volGo = new GameObject("GlobalVolume");
                    volGo.transform.SetParent(parent);
                    var vol = volGo.AddComponent<Volume>();
                    vol.isGlobal = true; vol.priority = 10;

                    // Always recreate profile fresh to avoid stale pink-tinted overrides
                    string profilePath = "Assets/Settings/VisualOverhaulProfile.asset";
                    if (System.IO.File.Exists(profilePath)) {
                        System.IO.File.Delete(profilePath);
                        System.IO.File.Delete(profilePath + ".meta");
                        AssetDatabase.Refresh();
                    }
                    var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                    profile.name = "VisualOverhaulProfile";
                    if (!System.IO.Directory.Exists("Assets/Settings")) System.IO.Directory.CreateDirectory("Assets/Settings");
                    AssetDatabase.CreateAsset(profile, profilePath);

                    // Bloom — subtle, not too aggressive
                    var bloom = profile.Add<Bloom>();
                    bloom.intensity.Override(0.35f);
                    bloom.threshold.Override(0.9f);
                    bloom.scatter.Override(0.5f);

                    // Color adjustments — warm, punchy but NOT pink
                    var colorAdj = profile.Add<ColorAdjustments>();
                    colorAdj.contrast.Override(18f);
                    colorAdj.saturation.Override(15f);
                    colorAdj.postExposure.Override(0f);
                    colorAdj.colorFilter.Override(new Color(1f, 0.97f, 0.88f)); // Warm golden-white, not pink

                    // Tonemapping — ACES gives the cinematic look
                    var tone = profile.Add<Tonemapping>();
                    tone.mode.Override(TonemappingMode.ACES);

                    // Vignette — dark edges, slightly warm, not blue
                    var vignette = profile.Add<Vignette>();
                    vignette.intensity.Override(0.22f);
                    vignette.color.Override(new Color(0.12f, 0.09f, 0.06f)); // Dark warm brown, not purple

                    // Lift/Gamma/Gain — warm highlights, NEUTRAL shadows (no blue push)
                    var lgg = profile.Add<LiftGammaGain>();
                    lgg.lift.Override(new Vector4(0f, 0f, 0f, 0f));          // Neutral shadows — no blue tint
                    lgg.gamma.Override(new Vector4(1.0f, 0.98f, 0.95f, 0f)); // Very subtle warm midtones
                    lgg.gain.Override(new Vector4(1.05f, 1.02f, 0.92f, 0f)); // Warm golden highlights

                    EditorUtility.SetDirty(profile);
                    vol.sharedProfile = profile;
                }

                [MenuItem("Egyptian/Generate Sky Cloud Normal Map", false, 2)]
        public static void GenerateSkyCloudNormalMap()
                {
                    string folderPath = "Assets/Resources/Textures";
                    if (!System.IO.Directory.Exists(folderPath))
                    {
                        System.IO.Directory.CreateDirectory(folderPath);
                    }
                    string path = System.IO.Path.Combine(folderPath, "SkyCloudNormalMap.png");

                    // If the high-res texture already exists (e.g. generated via python), use it!
                    if (System.IO.File.Exists(path))
                    {
                        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                        if (importer != null && (importer.sRGBTexture || !importer.mipmapEnabled || importer.wrapMode != TextureWrapMode.Repeat))
                        {
                            importer.textureType = TextureImporterType.Default;
                            importer.sRGBTexture = false;
                            importer.alphaSource = TextureImporterAlphaSource.FromInput;
                            importer.alphaIsTransparency = false;
                            importer.mipmapEnabled = true;
                            importer.wrapMode = TextureWrapMode.Repeat;
                            importer.SaveAndReimport();
                            AssetDatabase.Refresh();
                        }

                        Texture2D cloudTex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                        Material skyboxMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/SkyGradientBox.mat");
                        if (skyboxMat != null && cloudTex != null)
                        {
                            skyboxMat.SetTexture("_CloudTex", cloudTex);
                            skyboxMat.SetFloat("_CloudScale", 0.8f);
                            skyboxMat.SetFloat("_CloudThreshold", 0.35f);
                            skyboxMat.SetFloat("_CloudThickness", 2.5f);
                            EditorUtility.SetDirty(skyboxMat);
                        }
                        Debug.Log("[SkyCloudNormalMap] Loaded and assigned high-resolution cloud texture.");
                        return;
                    }

                    int size = 512;
                    Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
                    tex.wrapMode = TextureWrapMode.Repeat;
                    tex.filterMode = FilterMode.Bilinear;

                    float eps = 1f / size;
                    float bumpStrength = 2.0f;

                    Color[] pixels = new Color[size * size];

                    for (int y = 0; y < size; y++)
                    {
                        float v = (float)y / size;
                        for (int x = 0; x < size; x++)
                        {
                            float u = (float)x / size;

                            float density = GetFBmNoise(u, v);
                            float densityU = GetFBmNoise(u + eps, v);
                            float densityV = GetFBmNoise(u, v + eps);

                            float du = (densityU - density) / eps;
                            float dv = (densityV - density) / eps;

                            Vector3 normal = new Vector3(-du * bumpStrength, -dv * bumpStrength, 1.0f).normalized;

                            pixels[y * size + x] = new Color(
                                normal.x * 0.5f + 0.5f,
                                normal.y * 0.5f + 0.5f,
                                normal.z * 0.5f + 0.5f,
                                density
                            );
                        }
                    }

                    tex.SetPixels(pixels);
                    tex.Apply(true);

                    byte[] fileBytes = tex.EncodeToPNG();
                    System.IO.File.WriteAllBytes(path, fileBytes);
                    AssetDatabase.Refresh();

                    var newImporter = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (newImporter != null)
                    {
                        newImporter.textureType = TextureImporterType.Default;
                        newImporter.sRGBTexture = false;
                        newImporter.alphaSource = TextureImporterAlphaSource.FromInput;
                        newImporter.alphaIsTransparency = false;
                        newImporter.mipmapEnabled = true;
                        newImporter.wrapMode = TextureWrapMode.Repeat;
                        newImporter.SaveAndReimport();
                        AssetDatabase.Refresh();
                    }

                    Texture2D fallbackCloudTex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    Material fallbackSkyboxMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/SkyGradientBox.mat");
                    if (fallbackSkyboxMat != null && fallbackCloudTex != null)
                    {
                        fallbackSkyboxMat.SetTexture("_CloudTex", fallbackCloudTex);
                        fallbackSkyboxMat.SetFloat("_CloudScale", 0.8f);
                        fallbackSkyboxMat.SetFloat("_CloudThreshold", 0.35f);
                        fallbackSkyboxMat.SetFloat("_CloudThickness", 2.5f);
                        EditorUtility.SetDirty(fallbackSkyboxMat);
                    }
                    Debug.Log("[SkyCloudNormalMap] Generated and imported fallback seamless normal/density texture successfully.");
                }

        private static float GetFBmNoise(float u, float v)
                {
                    float val = 0f;
                    val += SeamlessNoise(u, v, 4f) * 0.6f;
                    val += SeamlessNoise(u, v, 8f) * 0.3f;
                    val += SeamlessNoise(u, v, 16f) * 0.1f;
                    return val;
                }

        private static float SeamlessNoise(float u, float v, float scale)
                {
                    float x = u * scale;
                    float y = v * scale;
                    
                    float n00 = Mathf.PerlinNoise(x, y);
                    float n10 = Mathf.PerlinNoise(x + scale, y);
                    float n01 = Mathf.PerlinNoise(x, y + scale);
                    float n11 = Mathf.PerlinNoise(x + scale, y + scale);
                    
                    float n0 = Mathf.Lerp(n00, n10, 1.0f - u);
                    float n1 = Mathf.Lerp(n01, n11, 1.0f - u);
                    
                    return Mathf.Lerp(n0, n1, 1.0f - v);
                }

        private Shader GetLitShader()
                {
                    var s = Shader.Find("Universal Render Pipeline/Simple Lit");
                    if (s == null) s = Shader.Find("Universal Render Pipeline/Lit");
                    if (s == null) s = Shader.Find("URP/Simple Lit");
                    if (s == null) s = Shader.Find("URP/Lit");
                    if (s == null) s = Shader.Find("Lit");
                    if (s == null) s = Shader.Find("Standard");
                    if (s == null) s = Shader.Find("Sprites/Default");
                    return s;
                }

    }
}
