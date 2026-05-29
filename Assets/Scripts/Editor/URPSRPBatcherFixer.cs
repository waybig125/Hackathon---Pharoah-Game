using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace TheAlchemistsCrypt.Editor
{
    /// <summary>
    /// Editor utility for:
    ///   1) Converting non-SRP-compatible materials to URP Lit (fixes the SRP Batcher)
    ///   2) Marking EgyptianCity_V5_Final children as Batching + ReflectionProbe Static
    ///      (NO OccluderStatic / OccludeeStatic — occlusion culling is disabled; Unity's
    ///      built-in frustum culling is sufficient and avoids the visual artefacts + APK bloat
    ///      that occlusion bake data produces.)
    ///   3) Clearing any previously baked occlusion data from the scene
    ///
    /// Menu items under Egyptian →:
    ///   • "Fix SRP Batcher Materials"   – instant, run after any scene change
    ///   • "🗑 Clear Occlusion Data"     – strips old bake data, shrinks scene file
    /// </summary>
    public static class URPSRPBatcherFixer
    {
        // ─────────────────────────────────────────────────────────────────────
        //  Menu Items
        // ─────────────────────────────────────────────────────────────────────

        [MenuItem("Egyptian/Fix SRP Batcher Materials", false, 10)]
        public static void FixMaterialsMenuItem()
        {
            SetCityObjectsStatic();
            int extracted = ExtractAndConvertAllGLBMaterials();
            int converted = FixAllSceneMaterials();
            DisableGPUResidentDrawerAllAssets(silent: true);
            EditorUtility.DisplayDialog(
                "SRP Batcher Fix Complete",
                $"Extracted & mapped {extracted} GLB material(s).\n" +
                $"Converted {converted} non-URP material(s) to URP Lit.\n" +
                "GPU Instancing has been enabled on all materials.\n" +
                "Static batching flags applied to city objects.\n" +
                "GPU Resident Drawer disabled on all pipeline assets.\n\n" +
                "SRP Batcher should now show active batches in the Frame Debugger.",
                "OK");
        }

        // [MenuItem("Egyptian/Extract and Convert GLB Materials", false, 15)]
        public static void ExtractMaterialsMenuItem()
        {
            int extracted = ExtractAndConvertAllGLBMaterials();
            EditorUtility.DisplayDialog(
                "Extract GLB Materials",
                $"Successfully extracted and mapped {extracted} material(s) from GLB assets to URP Lit.",
                "OK");
        }

        public static void FixMaterialsNoDialog()
        {
            SetCityObjectsStatic();
            ExtractAndConvertAllGLBMaterials();
            FixAllSceneMaterials();
            DisableGPUResidentDrawerAllAssets(silent: true);
            AssetDatabase.SaveAssets();
        }

        public static int ExtractAndConvertAllGLBMaterials()
        {
            string[] guids = AssetDatabase.FindAssets("t:GameObject");
            int extractedTotal = 0;
            var processedPaths = new HashSet<string>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (processedPaths.Contains(path)) continue;
                processedPaths.Add(path);

                if (path.EndsWith(".glb", System.StringComparison.OrdinalIgnoreCase) || 
                    path.EndsWith(".gltf", System.StringComparison.OrdinalIgnoreCase))
                {
                    extractedTotal += ExtractAndMapGLBMaterials(path);
                }
            }
            return extractedTotal;
        }

        public static int ExtractAndMapGLBMaterials(string glbPath)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(glbPath);
            var materials = new List<Material>();
            foreach (var asset in assets)
            {
                if (asset is Material mat)
                {
                    materials.Add(mat);
                }
            }
            
            if (materials.Count == 0) return 0;
            
            var importer = AssetImporter.GetAtPath(glbPath) as UnityEditor.AssetImporters.ScriptedImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[URPSRPBatcherFixer] Importer for {glbPath} is not a ScriptedImporter.");
                return 0;
            }
            
            string targetFolder = "Assets/Art/Materials/Extracted";
            if (!AssetDatabase.IsValidFolder(targetFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Art/Materials"))
                    AssetDatabase.CreateFolder("Assets", "Materials");
                AssetDatabase.CreateFolder("Assets/Art/Materials", "Extracted");
            }
            
            int extractedCount = 0;
            
            foreach (Material subMat in materials)
            {
                if (subMat == null) continue;
                
                string safeName = subMat.name.Replace(":", "_").Replace("/", "_");
                string glbName = System.IO.Path.GetFileNameWithoutExtension(glbPath);
                string matPath = $"{targetFolder}/{glbName}_{safeName}.mat";
                
                Material extMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                bool isNew = false;
                
                if (extMat == null)
                {
                    Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
                    if (urpLit == null)
                    {
                        Debug.LogError("[URPSRPBatcherFixer] Universal Render Pipeline/Lit shader not found.");
                        return 0;
                    }
                    extMat = new Material(urpLit);
                    isNew = true;
                }
                
                CopyGltfPropertiesToUrpLit(subMat, extMat, glbPath);
                
                if (isNew)
                {
                    AssetDatabase.CreateAsset(extMat, matPath);
                }
                else
                {
                    EditorUtility.SetDirty(extMat);
                }
                
                var identifier = new AssetImporter.SourceAssetIdentifier(typeof(Material), subMat.name);
                importer.AddRemap(identifier, extMat);
                extractedCount++;
            }
            
            if (extractedCount > 0)
            {
                AssetDatabase.WriteImportSettingsIfDirty(glbPath);
                AssetDatabase.ImportAsset(glbPath, ImportAssetOptions.ForceUpdate);
            }
            
            return extractedCount;
        }

        public static void CopyGltfPropertiesToUrpLit(Material src, Material dst, string glbPath = "")
        {
            Texture2D commonAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/EgyptianAssets/HouseGradientTex.png");
            Texture2D commonNormal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Textures/EgyptianNormalMap.png");

            // 1. Albedo Color & Map
            Color albedo = Color.white;
            if (src.HasProperty("baseColorFactor")) albedo = src.GetColor("baseColorFactor");
            else if (src.HasProperty("diffuseFactor")) albedo = src.GetColor("diffuseFactor");
            else if (src.HasProperty("_BaseColor")) albedo = src.GetColor("_BaseColor");
            else if (src.HasProperty("_Color")) albedo = src.GetColor("_Color");

            // ── GLOBAL SANDSTONE TINT ──────────────────────────────────────────────────
            // Blend all environment stone/earth materials toward a common warm sandstone
            // colour so that temples, obelisks, pillars, houses, and the sphinx all look
            // like they are carved from the same ancient Egyptian stone.
            string srcLower = src.name.ToLower();
            string glbLower = string.IsNullOrEmpty(glbPath) ? "" : System.IO.Path.GetFileNameWithoutExtension(glbPath).ToLower();
            bool isStoneAsset =
                srcLower.Contains("house") || srcLower.Contains("building") ||
                srcLower.Contains("column") || srcLower.Contains("pillar") ||
                srcLower.Contains("temple") || srcLower.Contains("stone") ||
                srcLower.Contains("wall")   || srcLower.Contains("obelisk") ||
                srcLower.Contains("sphinx") || srcLower.Contains("door")   ||
                srcLower.Contains("mastaba")|| srcLower.Contains("egyptian")||
                srcLower.Contains("stall")  || srcLower.Contains("arabic") ||
                srcLower.Contains("medieval")|| srcLower.Contains("sand")  ||
                srcLower.Contains("brick") || srcLower.Contains("tomb")    ||
                srcLower.Contains("ruin")   || srcLower.Contains("fort")   ||
                glbLower.Contains("house") || glbLower.Contains("building") ||
                glbLower.Contains("column") || glbLower.Contains("pillar") ||
                glbLower.Contains("temple") || glbLower.Contains("stone") ||
                glbLower.Contains("wall")   || glbLower.Contains("obelisk") ||
                glbLower.Contains("sphinx") || glbLower.Contains("door")   ||
                glbLower.Contains("mastaba")|| glbLower.Contains("egyptian")||
                glbLower.Contains("stall")  || glbLower.Contains("arabic") ||
                glbLower.Contains("medieval")|| glbLower.Contains("sand")  ||
                glbLower.Contains("brick") || glbLower.Contains("tomb")    ||
                glbLower.Contains("ruin")   || glbLower.Contains("fort");

            if (isStoneAsset) {
                // Sandstone: warm golden-tan, the colour of Egyptian limestone
                Color sandstone = new Color(0.88f, 0.74f, 0.52f, albedo.a);
                albedo = sandstone; // Force exact sandstone color tint
            }
            // ──────────────────────────────────────────────────────────────────────────

            dst.SetColor("_BaseColor", albedo);
            
            Texture mainTex = null;
            string mainTexProp = null;
            if (src.HasProperty("baseColorTexture")) mainTexProp = "baseColorTexture";
            else if (src.HasProperty("diffuseTexture")) mainTexProp = "diffuseTexture";
            else if (src.HasProperty("_BaseMap")) mainTexProp = "_BaseMap";
            else if (src.HasProperty("_MainTex")) mainTexProp = "_MainTex";
            
            if (isStoneAsset && commonAlbedo != null)
            {
                dst.SetTexture("_BaseMap", commonAlbedo);
                dst.SetTextureScale("_BaseMap", new Vector2(4, 4));
                dst.SetTextureOffset("_BaseMap", Vector2.zero);
            }
            else if (mainTexProp != null)
            {
                mainTex = src.GetTexture(mainTexProp);
                if (mainTex != null)
                {
                    dst.SetTexture("_BaseMap", mainTex);
                    dst.SetTextureScale("_BaseMap", src.GetTextureScale(mainTexProp));
                    dst.SetTextureOffset("_BaseMap", src.GetTextureOffset(mainTexProp));
                }
            }
            
            // 2. Normal Map
            Texture normalTex = null;
            string normalTexProp = null;
            if (src.HasProperty("normalTexture")) normalTexProp = "normalTexture";
            else if (src.HasProperty("_BumpMap")) normalTexProp = "_BumpMap";
            else if (src.HasProperty("_NormalMap")) normalTexProp = "_NormalMap";
            
            if (isStoneAsset && commonNormal != null)
            {
                dst.SetTexture("_BumpMap", commonNormal);
                dst.EnableKeyword("_NORMALMAP");
                dst.SetTextureScale("_BumpMap", new Vector2(4, 4));
                dst.SetTextureOffset("_BumpMap", Vector2.zero);
                dst.SetFloat("_BumpScale", 1.5f);
            }
            else if (normalTexProp != null)
            {
                normalTex = src.GetTexture(normalTexProp);
                if (normalTex != null)
                {
                    dst.SetTexture("_BumpMap", normalTex);
                    dst.EnableKeyword("_NORMALMAP");
                    dst.SetTextureScale("_BumpMap", src.GetTextureScale(normalTexProp));
                    dst.SetTextureOffset("_BumpMap", src.GetTextureOffset(normalTexProp));
                    
                    float normalScale = 1.0f;
                    if (src.HasProperty("normalTexture_scale")) normalScale = src.GetFloat("normalTexture_scale");
                    else if (src.HasProperty("_BumpScale")) normalScale = src.GetFloat("_BumpScale");
                    dst.SetFloat("_BumpScale", normalScale);
                }
            }
            else
            {
                dst.DisableKeyword("_NORMALMAP");
            }
            
            // 3. Metallic & Smoothness / Roughness
            float metallic = 0.0f;
            if (src.HasProperty("metallicFactor")) metallic = src.GetFloat("metallicFactor");
            else if (src.HasProperty("_Metallic")) metallic = src.GetFloat("_Metallic");
            
            // Force non-metallic for all dielectric environment assets (stone, wood, plaster)
            string lowerName = src.name.ToLower();
            string lowerPath = dst.name.ToLower();
            if (isStoneAsset ||
                lowerPath.Contains("house") || lowerPath.Contains("building") || lowerPath.Contains("city") ||
                lowerPath.Contains("stall") || lowerPath.Contains("market") || lowerPath.Contains("column") ||
                lowerPath.Contains("pillar") || lowerPath.Contains("temple") || lowerPath.Contains("stone") ||
                lowerPath.Contains("wood") || lowerPath.Contains("sand") || lowerPath.Contains("obelisk") ||
                lowerPath.Contains("sphinx") || lowerPath.Contains("door") || lowerPath.Contains("mastaba") || lowerPath.Contains("egyptian"))
            {
                metallic = 0.0f;
            }
            dst.SetFloat("_Metallic", metallic);
            
            float roughness = 0.5f;
            if (src.HasProperty("roughnessFactor")) roughness = src.GetFloat("roughnessFactor");
            else if (src.HasProperty("glossinessFactor")) roughness = 1.0f - src.GetFloat("glossinessFactor");
            else if (src.HasProperty("_Roughness")) roughness = src.GetFloat("_Roughness");
            else if (src.HasProperty("_Glossiness")) roughness = 1.0f - src.GetFloat("_Glossiness");
            else if (src.HasProperty("_Smoothness")) roughness = 1.0f - src.GetFloat("_Smoothness");
            // Stone assets: slightly rough (aged limestone texture feel)
            if (isStoneAsset) roughness = Mathf.Max(roughness, 0.75f);
            dst.SetFloat("_Smoothness", 1.0f - roughness);
            
            Texture metallicGlossMap = null;
            string metallicGlossProp = null;
            if (src.HasProperty("metallicRoughnessTexture")) metallicGlossProp = "metallicRoughnessTexture";
            else if (src.HasProperty("_MetallicGlossMap")) metallicGlossProp = "_MetallicGlossMap";
            
            if (metallicGlossProp != null) metallicGlossMap = src.GetTexture(metallicGlossProp);
            if (metallicGlossMap != null)
            {
                dst.SetTexture("_MetallicGlossMap", metallicGlossMap);
                dst.EnableKeyword("_METALLICSPECGLOSSMAP");
                dst.SetTextureScale("_MetallicGlossMap", src.GetTextureScale(metallicGlossProp));
                dst.SetTextureOffset("_MetallicGlossMap", src.GetTextureOffset(metallicGlossProp));
            }
            else
            {
                dst.DisableKeyword("_METALLICSPECGLOSSMAP");
            }
            
            // 4. Emission
            Color emissive = Color.black;
            if (src.HasProperty("emissiveFactor")) emissive = src.GetColor("emissiveFactor");
            else if (src.HasProperty("_EmissionColor")) emissive = src.GetColor("_EmissionColor");
            dst.SetColor("_EmissionColor", emissive);
            
            Texture emissiveMap = null;
            string emissiveProp = null;
            if (src.HasProperty("emissiveTexture")) emissiveProp = "emissiveTexture";
            else if (src.HasProperty("_EmissionMap")) emissiveProp = "_EmissionMap";
            
            if (emissiveProp != null) emissiveMap = src.GetTexture(emissiveProp);
            if (emissiveMap != null)
            {
                dst.SetTexture("_EmissionMap", emissiveMap);
                dst.SetTextureScale("_EmissionMap", src.GetTextureScale(emissiveProp));
                dst.SetTextureOffset("_EmissionMap", src.GetTextureOffset(emissiveProp));
            }
            
            if (emissive != Color.black || emissiveMap != null)
            {
                dst.EnableKeyword("_EMISSION");
            }
            else
            {
                dst.DisableKeyword("_EMISSION");
            }
            
            // 5. GPU Instancing
            dst.enableInstancing = true;
        }

        // [MenuItem("Egyptian/🗑 Clear Occlusion Data", false, 11)]
        public static void ClearOcclusionDataMenuItem()
        {
            bool proceed = EditorUtility.DisplayDialog(
                "Clear Occlusion Data",
                "This will strip the baked occlusion culling data from the scene.\n\n" +
                "Why? The occlusion bake:\n" +
                "  • Added 100–200 MB to the scene file (APK bloat)\n" +
                "  • Caused visual pop-in artefacts on mobile\n\n" +
                "Unity's built-in frustum culling works well for this open-city layout\n" +
                "and has zero overhead or artefacts.",
                "Clear Now",
                "Cancel");

            if (!proceed) return;
            ClearOcclusionData();
        }

        // [MenuItem("Egyptian/🛑 Disable GPU Resident Drawer", false, 12)]
        public static void DisableGPUResidentDrawerMenuItem()
        {
            DisableGPUResidentDrawerAllAssets(silent: false);
        }

        // [MenuItem("Egyptian/🔍 Inspect Scene Shaders", false, 13)]
        public static void InspectSceneShadersMenuItem()
        {
            var shaderCounts = new Dictionary<string, int>();
            Renderer[] allRenderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include);
            
            foreach (Renderer r in allRenderers)
            {
                if (r == null) continue;
                foreach (Material mat in r.sharedMaterials)
                {
                    if (mat == null) continue;
                    string name = mat.shader != null ? mat.shader.name : "Null Shader";
                    if (!shaderCounts.ContainsKey(name))
                        shaderCounts[name] = 0;
                    shaderCounts[name]++;
                }
            }

            string report = "Shaders used in the active scene:\n";
            foreach (var kvp in shaderCounts)
            {
                report += $"- {kvp.Key}: {kvp.Value} materials/renderers\n";
            }
            
            Debug.Log(report);
            EditorUtility.DisplayDialog("Scene Shaders Inspection", report, "OK");
        }

        // [MenuItem("Egyptian/Run Scene Diagnostics", false, 16)]
        public static void RunSceneDiagnostics()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== SCENE DIAGNOSTICS REPORT ===");
            report.AppendLine($"Time: {System.DateTime.Now}");
            
            // --- 1. General AlchemicalFocus & Weapons Check ---
            report.AppendLine("\n--- ALCHEMICAL FOCUS & WEAPON DIAGNOSTICS ---");
            var focuses = Object.FindObjectsByType<TheAlchemistsCrypt.Weapons.AlchemicalFocus>(FindObjectsInactive.Include);
            report.AppendLine($"Found {focuses.Length} AlchemicalFocus component(s) in the scene.");
            foreach (var f in focuses)
            {
                report.AppendLine($"- AlchemicalFocus on '{f.gameObject.name}' | ActiveInHierarchy: {f.gameObject.activeInHierarchy} | Enabled: {f.enabled} | Mode: {f.CurrentMode}");
            }

            var character = Object.FindAnyObjectByType<InfimaGames.LowPolyShooterPack.Character>(FindObjectsInactive.Include);
            if (character != null)
            {
                report.AppendLine($"Found Player Character: {character.gameObject.name}");
                var weapon = character.GetEquippedWeapon();
                if (weapon != null)
                    report.AppendLine($"- Equipped Weapon: {weapon.name}");
                else
                    report.AppendLine("- No weapon equipped.");
            }
            else
            {
                report.AppendLine("No Infima Character found in the scene.");
            }

            // --- 2. Missing Script Check ---
            report.AppendLine("\n--- MISSING COMPONENTS CHECK ---");
            var allGo = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            int missingCount = 0;
            foreach (var go in allGo)
            {
                var components = go.GetComponents<Component>();
                foreach (var c in components)
                {
                    if (c == null)
                    {
                        report.AppendLine($"- Missing Script component on GameObject: '{GetGameObjectPath(go)}'");
                        missingCount++;
                    }
                }
            }
            report.AppendLine($"Total missing components found: {missingCount}");

            // --- 3. Lights Check ---
            report.AppendLine("\n--- LIGHT SOURCE DIAGNOSTICS ---");
            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
            report.AppendLine($"Found {lights.Length} light(s) in the scene.");
            foreach (var l in lights)
            {
                report.AppendLine($"- Path: {GetGameObjectPath(l.gameObject)} | Type: {l.type} | Range: {l.range} | Intensity: {l.intensity} | Enabled: {l.enabled} | Color: {l.color}");
            }

            // --- 4. Player Hierarchy Check ---
            report.AppendLine("\n--- PLAYER HIERARCHY DUMP ---");
            var player = GameObject.Find("Player");
            if (player == null && character != null)
            {
                player = character.gameObject;
            }
            if (player != null)
            {
                report.AppendLine("Player GameObject Root Name: " + player.name);
                DumpTransform(player.transform, "", report);
            }
            else
            {
                report.AppendLine("Player GameObject not found in scene.");
            }

            // --- 5. Renderers & Shaders (Original Diagnostics) ---
            report.AppendLine("\n--- RENDERER & SHADER DIAGNOSTICS ---");
            var allRenderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include);
            report.AppendLine($"Total renderers found: {allRenderers.Length}");
            
            int gltfShaders = 0;
            int urpShaders = 0;
            int nullShaders = 0;
            int otherShaders = 0;
            
            var texturelessColumns = new List<string>();
            var detailLog = new System.Text.StringBuilder();

            foreach (var r in allRenderers)
            {
                if (r == null) continue;
                string goPath = GetGameObjectPath(r.gameObject);
                
                for (int i = 0; i < r.sharedMaterials.Length; i++)
                {
                    var mat = r.sharedMaterials[i];
                    if (mat == null)
                    {
                        detailLog.AppendLine($"[NULL MAT] GameObject: {goPath}, Index: {i}");
                        continue;
                    }
                    
                    string shaderName = mat.shader != null ? mat.shader.name : "Null Shader";
                    detailLog.AppendLine($"GameObject: {goPath}, Mat: {mat.name}, Shader: {shaderName}");
                    
                    if (shaderName == "Null Shader") nullShaders++;
                    else if (shaderName.Contains("glTF-pbr")) gltfShaders++;
                    else if (shaderName == "Universal Render Pipeline/Lit") urpShaders++;
                    else otherShaders++;
                    
                    // Check for columns/pillars
                    if (goPath.ToLower().Contains("column") || goPath.ToLower().Contains("pillar"))
                    {
                        Texture mainTex = mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") : null;
                        if (mainTex == null)
                        {
                            texturelessColumns.Add($"[COLUMN NO TEXTURE] GameObject: {goPath}, Mat: {mat.name}, Shader: {shaderName}");
                        }
                    }
                }
            }
            
            report.AppendLine($"glTF Shaders: {gltfShaders}");
            report.AppendLine($"URP Lit Shaders: {urpShaders}");
            report.AppendLine($"Null Shaders: {nullShaders}");
            report.AppendLine($"Other Shaders: {otherShaders}");
            
            report.AppendLine("\n=== TEXTURELESS COLUMNS ===");
            foreach (var col in texturelessColumns)
                report.AppendLine(col);
                
            report.AppendLine("\n=== DETAILED RENDERER LOG ===");
            report.AppendLine(detailLog.ToString());
                
            System.IO.File.WriteAllText("Assets/diagnostics_log.txt", report.ToString());
            Debug.Log("[URPSRPBatcherFixer] Diagnostics report written to Assets/diagnostics_log.txt");
            EditorUtility.DisplayDialog("Scene Diagnostics", $"Diagnostics written to Assets/diagnostics_log.txt\n\nglTF Shaders: {gltfShaders}\nURP Shaders: {urpShaders}\nMissing Scripts: {missingCount}\nLights Check: {lights.Length} light(s)", "OK");
        }

        private static void DumpTransform(Transform t, string indent, System.Text.StringBuilder sb)
        {
            if (t == null) return;
            sb.AppendLine($"{indent}- {t.name} (Position: {t.localPosition}, Active: {t.gameObject.activeSelf})");
            foreach (var comp in t.GetComponents<Component>())
            {
                if (comp != null && comp != t)
                {
                    sb.AppendLine($"{indent}  [Comp] {comp.GetType().Name}");
                }
            }
            for (int i = 0; i < t.childCount; i++)
            {
                DumpTransform(t.GetChild(i), indent + "  ", sb);
            }
        }
        
        private static string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = obj.name + "/" + path;
            }
            return path;
        }

        // [MenuItem("Egyptian/🧪 Test Material Properties", false, 14)]
        public static void TestMaterialProperties()
        {
            // Search all assets in the project for glTF materials
            string[] guids = AssetDatabase.FindAssets("t:Material");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null || mat.shader == null) continue;
                if (mat.shader.name.Contains("glTF-pbr"))
                {
                    string msg = $"Asset Material: {mat.name} ({path}), Shader: {mat.shader.name}\nProperties:\n";
                    var shader = mat.shader;
                    int count = shader.GetPropertyCount();
                    for (int i = 0; i < count; i++)
                    {
                        string name = shader.GetPropertyName(i);
                        var type = shader.GetPropertyType(i);
                        msg += $"- {name} ({type})\n";
                    }
                    Debug.Log(msg);
                    EditorUtility.DisplayDialog("Material Properties", msg, "OK");
                    return;
                }
            }

            // If no standalone material assets, try finding a gltf/glb asset and loading its sub-assets
            string[] glbGuids = AssetDatabase.FindAssets("t:GameObject");
            foreach (string guid in glbGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".glb") && !path.EndsWith(".gltf")) continue;
                
                var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var asset in assets)
                {
                    var mat = asset as Material;
                    if (mat == null || mat.shader == null) continue;
                    if (mat.shader.name.Contains("glTF-pbr"))
                    {
                        string msg = $"GLB Sub-Material: {mat.name} in {path}, Shader: {mat.shader.name}\nProperties:\n";
                        var shader = mat.shader;
                        int count = shader.GetPropertyCount();
                        for (int i = 0; i < count; i++)
                        {
                            string name = shader.GetPropertyName(i);
                            var type = shader.GetPropertyType(i);
                            msg += $"- {name} ({type})\n";
                        }
                        Debug.Log(msg);
                        EditorUtility.DisplayDialog("Material Properties", msg, "OK");
                        return;
                    }
                }
            }

            EditorUtility.DisplayDialog("Material Properties", "No glTF material found in the project.", "OK");
        }

        public static void DisableGPUResidentDrawerAllAssets(bool silent)
        {
            // Set BatchRendererGroup Stripping to KeepAll in EditorGraphicsSettings
            try
            {
                var assembly = System.Reflection.Assembly.Load("UnityEditor");
                var editorGraphicsSettingsType = assembly.GetType("UnityEditor.Rendering.EditorGraphicsSettings");
                var brgStrippingModeType = assembly.GetType("UnityEditor.Rendering.BrgStrippingMode");
                if (editorGraphicsSettingsType != null && brgStrippingModeType != null)
                {
                    var prop = editorGraphicsSettingsType.GetProperty("batchRendererGroupShaderStrippingMode", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (prop != null)
                    {
                        var keepAllVal = System.Enum.Parse(brgStrippingModeType, "KeepAll");
                        prop.SetValue(null, keepAllVal);
                        Debug.Log("[URPSRPBatcherFixer] Programmatically set EditorGraphicsSettings.batchRendererGroupShaderStrippingMode to KeepAll.");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[URPSRPBatcherFixer] Failed to set BatchRendererGroup stripping mode: {ex.Message}");
            }

            string[] guids = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset");
            int successCount = 0;
            int totalCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.RenderPipelineAsset>(path);
                if (asset == null) continue;

                totalCount++;
                var type = asset.GetType();
                var prop = type.GetProperty("gpuResidentDrawerMode", 
                    System.Reflection.BindingFlags.Public | 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance);

                // 1. Disable GPU Resident Drawer
                if (prop != null)
                {
                    try
                    {
                        var enumType = prop.PropertyType;
                        var value = System.Enum.Parse(enumType, "Disabled");
                        prop.SetValue(asset, value);
                        EditorUtility.SetDirty(asset);
                        successCount++;
                        Debug.Log($"[URPSRPBatcherFixer] Disabled GPU Resident Drawer on asset: {path}");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[URPSRPBatcherFixer] Failed to set gpuResidentDrawerMode on {path}: {ex.Message}");
                    }
                }

                // 2. Enable SRP Batcher
                var srpProp = type.GetProperty("useSRPBatcher", 
                    System.Reflection.BindingFlags.Public | 
                    System.Reflection.BindingFlags.Instance);
                if (srpProp != null)
                {
                    try
                    {
                        srpProp.SetValue(asset, true);
                        EditorUtility.SetDirty(asset);
                        Debug.Log($"[URPSRPBatcherFixer] Enabled SRP Batcher on asset: {path}");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[URPSRPBatcherFixer] Failed to set useSRPBatcher on {path}: {ex.Message}");
                    }
                }
            }

            if (successCount > 0)
            {
                AssetDatabase.SaveAssets();
            }

            if (!silent)
            {
                if (successCount == totalCount && totalCount > 0)
                {
                    EditorUtility.DisplayDialog("GPU Resident Drawer", 
                        $"Successfully disabled GPU Resident Drawer on all {successCount} URP assets in the project!", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("GPU Resident Drawer", 
                        $"Found {totalCount} URP assets. Successfully disabled GPU Resident Drawer on {successCount} of them.\nCheck the Unity console for details.", "OK");
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Public API (also called by StaticEgyptianCityGenerator)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Scans all Renderers in the active scene and converts any non-URP
        /// materials to Universal Render Pipeline/Lit, preserving albedo, main
        /// texture, normal map, metallic, and smoothness values.
        /// Also enables GPU Instancing on every material.
        /// Returns the number of materials converted.
        /// </summary>
        public static int FixAllSceneMaterials()
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
            {
                Debug.LogError("[URPSRPBatcherFixer] 'Universal Render Pipeline/Lit' shader not found. Is URP installed?");
                return 0;
            }

            // Track processed material assets to avoid double-processing shared materials
            var processed = new HashSet<Material>();
            var clonedHotfixCache = new Dictionary<Material, Material>();
            int converted = 0;

            Renderer[] allRenderers = Object.FindObjectsByType<Renderer>(
                FindObjectsInactive.Include);

            foreach (Renderer renderer in allRenderers)
            {
                if (renderer == null) continue;

                // HOTFIX: Native GLB importer leaves collision meshes visible. We must hide/destroy them so they don't render as blue/red boxes.
                bool isCollider = renderer.name.Contains("COLLIDER", System.StringComparison.OrdinalIgnoreCase) ||
                                  renderer.name.Contains("Collider", System.StringComparison.OrdinalIgnoreCase);

                if (!isCollider && renderer.sharedMaterials != null)
                {
                    foreach (var mat in renderer.sharedMaterials)
                    {
                        if (mat != null && (mat.name.Contains("COLLIDER", System.StringComparison.OrdinalIgnoreCase) ||
                                            mat.name.Contains("Collider", System.StringComparison.OrdinalIgnoreCase)))
                        {
                            isCollider = true;
                            break;
                        }
                    }
                }

                if (isCollider)
                {
                    renderer.enabled = false;
                    Object.DestroyImmediate(renderer);
                    continue; // Skip further material processing on destroyed renderer
                }

                Material[] mats = renderer.sharedMaterials;
                bool changedRenderer = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material mat = mats[i];
                    if (mat == null) continue;

                    // Swap internal read-only GLB/FBX materials with external extracted assets
                    string assetPath = AssetDatabase.GetAssetPath(mat);
                    if (!string.IsNullOrEmpty(assetPath) && 
                        (assetPath.EndsWith(".glb", System.StringComparison.OrdinalIgnoreCase) || 
                         assetPath.EndsWith(".gltf", System.StringComparison.OrdinalIgnoreCase) || 
                         assetPath.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase) || 
                         assetPath.EndsWith(".obj", System.StringComparison.OrdinalIgnoreCase)))
                    {
                        string glbName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                        string safeName = mat.name.Replace(":", "_").Replace("/", "_");
                        string extPath = $"Assets/Art/Materials/Extracted/{glbName}_{safeName}.mat";
                        Material extMat = AssetDatabase.LoadAssetAtPath<Material>(extPath);
                        if (extMat != null)
                        {
                            mats[i] = extMat;
                            mat = extMat;
                            changedRenderer = true;
                        }
                    }

                    // Apply cached hotfix clones to ALL instances that share this material
                    if (clonedHotfixCache.TryGetValue(mat, out Material cachedClone)) {
                        mats[i] = cachedClone;
                        mat = cachedClone;
                        changedRenderer = true;
                    }

                    if (processed.Contains(mat)) continue;
                    processed.Add(mat);

                    // HOTFIX: Native GLB importer channel mismatch for Metallic/Roughness makes stalls pitch black.
                    if (mat.name.Contains("TD_Checker") || mat.name.Contains("low_poly_market_stall_pack_Medieval"))
                    {
                        // We must clone and fix it to remove the broken metallic map
                        Material newMat = new Material(mat);
                        newMat.name = mat.name + "_Fixed";
                        
                        string colorProp = newMat.HasProperty("baseColorFactor") ? "baseColorFactor" : (newMat.HasProperty("_BaseColor") ? "_BaseColor" : null);
                        if (colorProp != null) {
                            if (mat.name.Contains("low_poly_market_stall_pack_Medieval")) {
                                // Tint the neon cartoon colors to a warm sandy desert hue so they blend into the environment
                                newMat.SetColor(colorProp, new Color(0.85f, 0.70f, 0.50f, 1f)); 
                            } else {
                                newMat.SetColor(colorProp, Color.white); // Restore texture visibility
                            }
                        }
                        
                        // Fix the pitch black issue caused by GLTF metallic-roughness map channel mismatch in URP Lit
                        if (newMat.HasProperty("_MetallicGlossMap")) newMat.SetTexture("_MetallicGlossMap", null);
                        if (newMat.HasProperty("metallicRoughnessTexture")) newMat.SetTexture("metallicRoughnessTexture", null);
                        if (newMat.HasProperty("_Metallic")) newMat.SetFloat("_Metallic", 0f);
                        if (newMat.HasProperty("metallicFactor")) newMat.SetFloat("metallicFactor", 0f);
                        if (newMat.HasProperty("_Smoothness")) newMat.SetFloat("_Smoothness", 0.1f);
                        if (newMat.HasProperty("roughnessFactor")) newMat.SetFloat("roughnessFactor", 0.9f);
                        
                        if (mat.name.Contains("TD_Checker")) {
                            // TD_Checker requires alpha blending for its complex structures
                            newMat.SetFloat("_Surface", 1);
                            newMat.SetOverrideTag("RenderType", "Transparent");
                            newMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                            newMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                            newMat.SetInt("_ZWrite", 0);
                            newMat.EnableKeyword("_ALPHABLEND_ON");
                            // For glTF-pbr transparency modes (if using UnityGLTF / glTFast)
                            newMat.SetFloat("alphaMode", 2); // BLEND mode
                            newMat.EnableKeyword("ALPHAMODE_BLEND");
                            newMat.renderQueue = 3000;
                        }
                        
                        mats[i] = newMat;
                        changedRenderer = true;
                        clonedHotfixCache[mat] = newMat; // Save mapping from ORIGINAL to CLONED
                        mat = newMat; // Update local ref for subsequent checks
                        processed.Add(newMat); // Also mark clone as processed
                    }

                    string shaderName = mat.shader != null ? mat.shader.name : string.Empty;

                    bool isNonURP =
                        shaderName.StartsWith("Standard") ||
                        shaderName.StartsWith("Legacy Shaders") ||
                        shaderName.StartsWith("Mobile/") ||
                        shaderName == "Diffuse" ||
                        shaderName == "VertexLit" ||
                        shaderName == "Sprites/Default";

                    if (isNonURP)
                    {
                        // ── Capture property values before shader swap ──
                        Color albedo = Color.white;
                        if (mat.HasProperty("baseColorFactor")) albedo = mat.GetColor("baseColorFactor");
                        else if (mat.HasProperty("diffuseFactor")) albedo = mat.GetColor("diffuseFactor");
                        else if (mat.HasProperty("_BaseColor")) albedo = mat.GetColor("_BaseColor");
                        else if (mat.HasProperty("_Color")) albedo = mat.GetColor("_Color");

                        Texture mainTex = null;
                        string mainTexProp = null;
                        if (mat.HasProperty("baseColorTexture")) mainTexProp = "baseColorTexture";
                        else if (mat.HasProperty("diffuseTexture")) mainTexProp = "diffuseTexture";
                        else if (mat.HasProperty("_BaseMap")) mainTexProp = "_BaseMap";
                        else if (mat.HasProperty("_MainTex")) mainTexProp = "_MainTex";
                        if (mainTexProp != null) mainTex = mat.GetTexture(mainTexProp);

                        Vector2 mainTexScale = Vector2.one;
                        Vector2 mainTexOffset = Vector2.zero;
                        if (mainTexProp != null && mainTex != null)
                        {
                            mainTexScale = mat.GetTextureScale(mainTexProp);
                            mainTexOffset = mat.GetTextureOffset(mainTexProp);
                        }

                        Texture bump = null;
                        string bumpProp = null;
                        if (mat.HasProperty("normalTexture")) bumpProp = "normalTexture";
                        else if (mat.HasProperty("_BumpMap")) bumpProp = "_BumpMap";
                        else if (mat.HasProperty("_NormalMap")) bumpProp = "_NormalMap";
                        if (bumpProp != null) bump = mat.GetTexture(bumpProp);

                        Vector2 bumpScale = Vector2.one;
                        Vector2 bumpOffset = Vector2.zero;
                        if (bumpProp != null && bump != null)
                        {
                            bumpScale = mat.GetTextureScale(bumpProp);
                            bumpOffset = mat.GetTextureOffset(bumpProp);
                        }

                        float normalScale = 1.0f;
                        if (mat.HasProperty("normalTexture_scale")) normalScale = mat.GetFloat("normalTexture_scale");
                        else if (mat.HasProperty("_BumpScale")) normalScale = mat.GetFloat("_BumpScale");

                        float metallic = 0f;
                        if (mat.HasProperty("metallicFactor")) metallic = mat.GetFloat("metallicFactor");
                        else if (mat.HasProperty("_Metallic")) metallic = mat.GetFloat("_Metallic");

                        // Safety: Force non-metallic for dielectric environment assets (stone, wood, plaster)
                        string lowerName = mat.name.ToLower();
                        if (lowerName.Contains("house") || lowerName.Contains("building") || lowerName.Contains("city") ||
                            lowerName.Contains("stall") || lowerName.Contains("market") || lowerName.Contains("column") ||
                            lowerName.Contains("pillar") || lowerName.Contains("temple") || lowerName.Contains("stone") ||
                            lowerName.Contains("wood") || lowerName.Contains("sand") || lowerName.Contains("obelisk") ||
                            lowerName.Contains("sphinx") || lowerName.Contains("door") || lowerName.Contains("mastaba") || lowerName.Contains("stall") || lowerName.Contains("egyptian"))
                        {
                            metallic = 0.0f;
                        }

                        float smoothness = 0.5f;
                        if (mat.HasProperty("roughnessFactor")) smoothness = 1f - mat.GetFloat("roughnessFactor");
                        else if (mat.HasProperty("glossinessFactor")) smoothness = mat.GetFloat("glossinessFactor");
                        else if (mat.HasProperty("_Roughness")) smoothness = 1f - mat.GetFloat("_Roughness");
                        else if (mat.HasProperty("_Smoothness")) smoothness = mat.GetFloat("_Smoothness");
                        else if (mat.HasProperty("_Glossiness")) smoothness = mat.GetFloat("_Glossiness");

                        Texture metallicGlossMap = null;
                        string metallicGlossProp = null;
                        if (mat.HasProperty("metallicRoughnessTexture")) metallicGlossProp = "metallicRoughnessTexture";
                        else if (mat.HasProperty("_MetallicGlossMap")) metallicGlossProp = "_MetallicGlossMap";
                        if (metallicGlossProp != null) metallicGlossMap = mat.GetTexture(metallicGlossProp);

                        Vector2 metallicGlossScale = Vector2.one;
                        Vector2 metallicGlossOffset = Vector2.zero;
                        if (metallicGlossProp != null && metallicGlossMap != null)
                        {
                            metallicGlossScale = mat.GetTextureScale(metallicGlossProp);
                            metallicGlossOffset = mat.GetTextureOffset(metallicGlossProp);
                        }

                        Color emissive = Color.black;
                        if (mat.HasProperty("emissiveFactor")) emissive = mat.GetColor("emissiveFactor");
                        else if (mat.HasProperty("_EmissionColor")) emissive = mat.GetColor("_EmissionColor");

                        Texture emissiveMap = null;
                        string emissiveProp = null;
                        if (mat.HasProperty("emissiveTexture")) emissiveProp = "emissiveTexture";
                        else if (mat.HasProperty("_EmissionMap")) emissiveProp = "_EmissionMap";
                        if (emissiveProp != null) emissiveMap = mat.GetTexture(emissiveProp);

                        Vector2 emissiveScale = Vector2.one;
                        Vector2 emissiveOffset = Vector2.zero;
                        if (emissiveProp != null && emissiveMap != null)
                        {
                            emissiveScale = mat.GetTextureScale(emissiveProp);
                            emissiveOffset = mat.GetTextureOffset(emissiveProp);
                        }

                        mat.shader = urpLit;

                        // ── Restore values using URP Lit property names ──
                        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", albedo);
                        if (mat.HasProperty("_BaseMap") && mainTex != null)
                        {
                            mat.SetTexture("_BaseMap", mainTex);
                            mat.SetTextureScale("_BaseMap", mainTexScale);
                            mat.SetTextureOffset("_BaseMap", mainTexOffset);
                        }
                        if (mat.HasProperty("_BumpMap") && bump != null)
                        {
                            mat.SetTexture("_BumpMap", bump);
                            mat.SetTextureScale("_BumpMap", bumpScale);
                            mat.SetTextureOffset("_BumpMap", bumpOffset);
                            mat.EnableKeyword("_NORMALMAP");
                            if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", normalScale);
                        }
                        else
                        {
                            mat.DisableKeyword("_NORMALMAP");
                        }
                        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
                        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

                        if (metallicGlossMap != null)
                        {
                            if (mat.HasProperty("_MetallicGlossMap"))
                            {
                                mat.SetTexture("_MetallicGlossMap", metallicGlossMap);
                                mat.SetTextureScale("_MetallicGlossMap", metallicGlossScale);
                                mat.SetTextureOffset("_MetallicGlossMap", metallicGlossOffset);
                            }
                            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                        }
                        else
                        {
                            mat.DisableKeyword("_METALLICSPECGLOSSMAP");
                        }

                        if (emissive != Color.black || emissiveMap != null)
                        {
                            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emissive);
                            if (mat.HasProperty("_EmissionMap") && emissiveMap != null)
                            {
                                mat.SetTexture("_EmissionMap", emissiveMap);
                                mat.SetTextureScale("_EmissionMap", emissiveScale);
                                mat.SetTextureOffset("_EmissionMap", emissiveOffset);
                            }
                            mat.EnableKeyword("_EMISSION");
                        }
                        else
                        {
                            mat.DisableKeyword("_EMISSION");
                        }

                        converted++;
                        EditorUtility.SetDirty(mat);
                        Debug.Log($"[URPSRPBatcherFixer] Converted '{mat.name}' from '{shaderName}' to URP Lit.");
                    }

                    // Enable GPU Instancing regardless of shader (helps batch identical meshes)
                    if (!mat.enableInstancing)
                    {
                        mat.enableInstancing = true;
                        EditorUtility.SetDirty(mat);
                    }
                }
                if (changedRenderer)
                {
                    renderer.sharedMaterials = mats;
                }
            }

            if (converted > 0)
                AssetDatabase.SaveAssets();

            Debug.Log($"[URPSRPBatcherFixer] Done — {converted} material(s) converted. {processed.Count} total materials processed.");
            return converted;
        }

        /// <summary>
        /// Marks all static geometry inside EgyptianCity_V5_Final as
        /// BatchingStatic and ReflectionProbeStatic ONLY.
        ///
        /// OccluderStatic and OccludeeStatic are intentionally NOT set.
        /// Occlusion baking produced visual artefacts and ~100–200 MB of extra
        /// data in the scene file. Unity frustum culling handles the open city fine.
        /// </summary>
        public static void SetCityObjectsStatic()
        {
            GameObject cityRoot = GameObject.Find("EgyptianCity_V5_Final");
            if (cityRoot == null)
            {
                Debug.LogWarning("[URPSRPBatcherFixer] 'EgyptianCity_V5_Final' not found — static flags not set.");
                return;
            }

            // Keywords that identify dynamic / non-static objects
            string[] dynamicKeywords = {
                "player", "mummy", "zombie", "pharaoh", "projectile", "orb",
                "pickup", "weapon", "camera", "navmesh", "manager", "hud",
                "canvas", "spawner", "hive", "health", "light"
            };

            // CHANGED: Removed OccluderStatic and OccludeeStatic to avoid triggering
            // the occlusion bake and the associated APK bloat / visual artefacts.
            StaticEditorFlags staticFlags =
                StaticEditorFlags.BatchingStatic |
                StaticEditorFlags.ReflectionProbeStatic;

            int marked  = 0;
            int skipped = 0;

            Transform[] children = cityRoot.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in children)
            {
                if (t == null) continue;
                string nameLower = t.gameObject.name.ToLower();
                bool isDynamic = false;
                foreach (string kw in dynamicKeywords)
                {
                    if (nameLower.Contains(kw)) { isDynamic = true; break; }
                }

                if (isDynamic) { skipped++; continue; }

                GameObjectUtility.SetStaticEditorFlags(t.gameObject, staticFlags);
                marked++;
            }

            Debug.Log($"[URPSRPBatcherFixer] Batching+Reflection static flags set on {marked} objects. Skipped {skipped} dynamic objects.");
        }

        /// <summary>
        /// Strips previously baked occlusion culling data from the active scene.
        /// This reclaims 100–200 MB from the scene file that was added by a prior bake.
        /// </summary>
        public static void ClearOcclusionData()
        {
            StaticOcclusionCulling.Clear();

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[URPSRPBatcherFixer] ✅ Occlusion data cleared. Scene saved. Scene file should now be significantly smaller.");
            EditorUtility.DisplayDialog(
                "Occlusion Data Cleared",
                "Baked occlusion data has been removed from the scene.\n\n" +
                "The scene has been saved. Rebuild the APK for a smaller package size.",
                "OK");
        }
    }
}
