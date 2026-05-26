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

        [MenuItem("Egyptian/Extract and Convert GLB Materials", false, 15)]
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
            
            string targetFolder = "Assets/Materials/Extracted";
            if (!AssetDatabase.IsValidFolder(targetFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                    AssetDatabase.CreateFolder("Assets", "Materials");
                AssetDatabase.CreateFolder("Assets/Materials", "Extracted");
            }
            
            string texFolder = "Assets/Materials/Extracted/Textures";
            if (!AssetDatabase.IsValidFolder(texFolder))
            {
                AssetDatabase.CreateFolder("Assets/Materials/Extracted", "Textures");
            }

            // Extract textures first to break the circular dependency
            var textureMap = ExtractAndImportGLBTextures(glbPath, assets, texFolder);

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
                
                CopyGltfPropertiesToUrpLit(subMat, extMat, textureMap);
                
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

        public static Dictionary<Texture, Texture> ExtractAndImportGLBTextures(string glbPath, UnityEngine.Object[] assets, string texFolder)
        {
            var textureMap = new Dictionary<Texture, Texture>();
            string glbName = System.IO.Path.GetFileNameWithoutExtension(glbPath);

            foreach (var asset in assets)
            {
                if (asset is Texture2D srcTex)
                {
                    if (srcTex == null) continue;
                    
                    string texName = srcTex.name;
                    if (string.IsNullOrEmpty(texName)) texName = "texture_" + srcTex.GetInstanceID();
                    
                    // Create clean file name
                    string safeName = texName.Replace(":", "_").Replace("/", "_").Replace(" ", "_");
                    string texPath = $"{texFolder}/{glbName}_{safeName}.png";
                    
                    Texture existingTex = AssetDatabase.LoadAssetAtPath<Texture>(texPath);
                    if (existingTex != null)
                    {
                        textureMap[srcTex] = existingTex;
                        continue;
                    }

                    try
                    {
                        // Copy texture using RenderTexture (works for non-readable textures)
                        RenderTexture renderTex = RenderTexture.GetTemporary(
                            srcTex.width,
                            srcTex.height,
                            0,
                            RenderTextureFormat.Default,
                            RenderTextureReadWrite.Linear);

                        Graphics.Blit(srcTex, renderTex);
                        RenderTexture previous = RenderTexture.active;
                        RenderTexture.active = renderTex;
                        
                        Texture2D readableText = new Texture2D(srcTex.width, srcTex.height, TextureFormat.RGBA32, false);
                        readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
                        readableText.Apply();
                        
                        RenderTexture.active = previous;
                        RenderTexture.ReleaseTemporary(renderTex);

                        byte[] bytes = readableText.EncodeToPNG();
                        GameObject.DestroyImmediate(readableText);
                        
                        string absolutePath = System.IO.Path.Combine(Application.dataPath, texPath.Substring(7));
                        System.IO.File.WriteAllBytes(absolutePath, bytes);
                        
                        AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);
                        
                        // Set texture import settings to match color space and bypass compression issues
                        var texImporter = AssetImporter.GetAtPath(texPath) as TextureImporter;
                        if (texImporter != null)
                        {
                            // If it was a normal map, configure it as one!
                            if (srcTex.name.ToLower().Contains("normal") || srcTex.name.ToLower().Contains("bump"))
                            {
                                texImporter.textureType = TextureImporterType.NormalMap;
                            }
                            texImporter.SaveAndReimport();
                        }
                        
                        Texture newTex = AssetDatabase.LoadAssetAtPath<Texture>(texPath);
                        if (newTex != null)
                        {
                            textureMap[srcTex] = newTex;
                            Debug.Log($"[URPSRPBatcherFixer] Extracted texture '{srcTex.name}' from '{glbName}' to '{texPath}'");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[URPSRPBatcherFixer] Failed to extract texture '{srcTex.name}' from '{glbName}': {ex.Message}");
                    }
                }
            }
            return textureMap;
        }

        public static void CopyGltfPropertiesToUrpLit(Material src, Material dst, Dictionary<Texture, Texture> textureMap)
        {
            // Helper to get mapped texture
            Texture GetMappedTexture(Texture original)
            {
                if (original == null) return null;
                if (textureMap.TryGetValue(original, out Texture mapped))
                    return mapped;
                return original; // Fallback
            }

            // 1. Albedo Color & Map
            Color albedo = Color.white;
            if (src.HasProperty("baseColorFactor")) albedo = src.GetColor("baseColorFactor");
            else if (src.HasProperty("_BaseColor")) albedo = src.GetColor("_BaseColor");
            else if (src.HasProperty("_Color")) albedo = src.GetColor("_Color");
            dst.SetColor("_BaseColor", albedo);
            
            Texture mainTex = null;
            if (src.HasProperty("baseColorTexture")) mainTex = src.GetTexture("baseColorTexture");
            else if (src.HasProperty("_BaseMap")) mainTex = src.GetTexture("_BaseMap");
            else if (src.HasProperty("_MainTex")) mainTex = src.GetTexture("_MainTex");
            
            if (mainTex != null)
            {
                dst.SetTexture("_BaseMap", GetMappedTexture(mainTex));
                
                Vector4 tilingOffset = Vector4.one;
                if (src.HasProperty("baseColorTexture_ST")) tilingOffset = src.GetVector("baseColorTexture_ST");
                else if (src.HasProperty("_BaseMap_ST")) tilingOffset = src.GetVector("_BaseMap_ST");
                else if (src.HasProperty("_MainTex_ST")) tilingOffset = src.GetVector("_MainTex_ST");
                dst.SetVector("_BaseMap_ST", tilingOffset);
            }
            
            // 2. Normal Map
            Texture normalTex = null;
            if (src.HasProperty("normalTexture")) normalTex = src.GetTexture("normalTexture");
            else if (src.HasProperty("_BumpMap")) normalTex = src.GetTexture("_BumpMap");
            else if (src.HasProperty("_NormalMap")) normalTex = src.GetTexture("_NormalMap");
            if (normalTex != null)
            {
                dst.SetTexture("_BumpMap", GetMappedTexture(normalTex));
                dst.EnableKeyword("_NORMALMAP");
                
                Vector4 tilingOffset = Vector4.one;
                if (src.HasProperty("normalTexture_ST")) tilingOffset = src.GetVector("normalTexture_ST");
                else if (src.HasProperty("_BumpMap_ST")) tilingOffset = src.GetVector("_BumpMap_ST");
                dst.SetVector("_BumpMap_ST", tilingOffset);
                
                float normalScale = 1.0f;
                if (src.HasProperty("normalTexture_scale")) normalScale = src.GetFloat("normalTexture_scale");
                else if (src.HasProperty("_BumpScale")) normalScale = src.GetFloat("_BumpScale");
                dst.SetFloat("_BumpScale", normalScale);
            }
            else
            {
                dst.DisableKeyword("_NORMALMAP");
            }
            
            // 3. Metallic & Smoothness / Roughness
            float metallic = 0.0f;
            if (src.HasProperty("metallicFactor")) metallic = src.GetFloat("metallicFactor");
            else if (src.HasProperty("_Metallic")) metallic = src.GetFloat("_Metallic");
            
            // Safety: Force non-metallic for dielectric environment assets (stone, wood, plaster)
            string lowerName = src.name.ToLower();
            string lowerPath = dst.name.ToLower();
            if (lowerName.Contains("house") || lowerName.Contains("building") || lowerName.Contains("city") ||
                lowerName.Contains("stall") || lowerName.Contains("market") || lowerName.Contains("column") ||
                lowerName.Contains("pillar") || lowerName.Contains("temple") || lowerName.Contains("stone") ||
                lowerName.Contains("wood") || lowerName.Contains("sand") || lowerName.Contains("obelisk") ||
                lowerName.Contains("sphinx") || lowerName.Contains("door") || lowerName.Contains("mastaba") ||
                lowerPath.Contains("house") || lowerPath.Contains("building") || lowerPath.Contains("city") ||
                lowerPath.Contains("stall") || lowerPath.Contains("market") || lowerPath.Contains("column") ||
                lowerPath.Contains("pillar") || lowerPath.Contains("temple") || lowerPath.Contains("stone") ||
                lowerPath.Contains("wood") || lowerPath.Contains("sand") || lowerPath.Contains("obelisk") ||
                lowerPath.Contains("sphinx") || lowerPath.Contains("door") || lowerPath.Contains("mastaba"))
            {
                metallic = 0.0f;
            }
            dst.SetFloat("_Metallic", metallic);
            
            float roughness = 0.5f;
            if (src.HasProperty("roughnessFactor")) roughness = src.GetFloat("roughnessFactor");
            else if (src.HasProperty("_Roughness")) roughness = src.GetFloat("_Roughness");
            else if (src.HasProperty("_Glossiness")) roughness = 1.0f - src.GetFloat("_Glossiness");
            else if (src.HasProperty("_Smoothness")) roughness = 1.0f - src.GetFloat("_Smoothness");
            dst.SetFloat("_Smoothness", 1.0f - roughness);
            
            Texture metallicGlossMap = null;
            if (src.HasProperty("metallicRoughnessTexture")) metallicGlossMap = src.GetTexture("metallicRoughnessTexture");
            else if (src.HasProperty("_MetallicGlossMap")) metallicGlossMap = src.GetTexture("_MetallicGlossMap");
            if (metallicGlossMap != null)
            {
                dst.SetTexture("_MetallicGlossMap", GetMappedTexture(metallicGlossMap));
                dst.EnableKeyword("_METALLICSPECGLOSSMAP");
                
                Vector4 tilingOffset = Vector4.one;
                if (src.HasProperty("metallicRoughnessTexture_ST")) tilingOffset = src.GetVector("metallicRoughnessTexture_ST");
                else if (src.HasProperty("_MetallicGlossMap_ST")) tilingOffset = src.GetVector("_MetallicGlossMap_ST");
                dst.SetVector("_MetallicGlossMap_ST", tilingOffset);
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
            if (src.HasProperty("emissiveTexture")) emissiveMap = src.GetTexture("emissiveTexture");
            else if (src.HasProperty("_EmissionMap")) emissiveMap = src.GetTexture("_EmissionMap");
            if (emissiveMap != null)
            {
                dst.SetTexture("_EmissionMap", GetMappedTexture(emissiveMap));
                Vector4 tilingOffset = Vector4.one;
                if (src.HasProperty("emissiveTexture_ST")) tilingOffset = src.GetVector("emissiveTexture_ST");
                else if (src.HasProperty("_EmissionMap_ST")) tilingOffset = src.GetVector("_EmissionMap_ST");
                dst.SetVector("_EmissionMap_ST", tilingOffset);
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

        [MenuItem("Egyptian/🗑 Clear Occlusion Data", false, 11)]
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

        [MenuItem("Egyptian/🛑 Disable GPU Resident Drawer", false, 12)]
        public static void DisableGPUResidentDrawerMenuItem()
        {
            DisableGPUResidentDrawerAllAssets(silent: false);
        }

        [MenuItem("Egyptian/🔍 Inspect Scene Shaders", false, 13)]
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

        [MenuItem("Egyptian/Run Scene Diagnostics", false, 16)]
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

        [MenuItem("Egyptian/🧪 Test Material Properties", false, 14)]
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
                    int count = ShaderUtil.GetPropertyCount(shader);
                    for (int i = 0; i < count; i++)
                    {
                        string name = ShaderUtil.GetPropertyName(shader, i);
                        var type = ShaderUtil.GetPropertyType(shader, i);
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
                        int count = ShaderUtil.GetPropertyCount(shader);
                        for (int i = 0; i < count; i++)
                        {
                            string name = ShaderUtil.GetPropertyName(shader, i);
                            var type = ShaderUtil.GetPropertyType(shader, i);
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
            int converted = 0;

            Renderer[] allRenderers = Object.FindObjectsByType<Renderer>(
                FindObjectsInactive.Include);

            foreach (Renderer renderer in allRenderers)
            {
                if (renderer == null) continue;

                Material[] mats = renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material mat = mats[i];
                    if (mat == null || processed.Contains(mat)) continue;
                    processed.Add(mat);

                    string shaderName = mat.shader != null ? mat.shader.name : string.Empty;

                    bool isNonURP =
                        shaderName.StartsWith("Standard") ||
                        shaderName.StartsWith("Legacy Shaders") ||
                        shaderName.StartsWith("Mobile/") ||
                        shaderName == "Diffuse" ||
                        shaderName == "VertexLit" ||
                        shaderName == "Sprites/Default" ||
                        shaderName.Contains("glTF-pbr");

                    if (isNonURP)
                    {
                        // ── Capture property values before shader swap ──
                        Color albedo = Color.white;
                        if (mat.HasProperty("baseColorFactor")) albedo = mat.GetColor("baseColorFactor");
                        else if (mat.HasProperty("_BaseColor")) albedo = mat.GetColor("_BaseColor");
                        else if (mat.HasProperty("_Color")) albedo = mat.GetColor("_Color");

                        Texture mainTex = null;
                        if (mat.HasProperty("baseColorTexture")) mainTex = mat.GetTexture("baseColorTexture");
                        else if (mat.HasProperty("_BaseMap")) mainTex = mat.GetTexture("_BaseMap");
                        else if (mat.HasProperty("_MainTex")) mainTex = mat.GetTexture("_MainTex");

                        Texture bump = null;
                        if (mat.HasProperty("normalTexture")) bump = mat.GetTexture("normalTexture");
                        else if (mat.HasProperty("_BumpMap")) bump = mat.GetTexture("_BumpMap");
                        else if (mat.HasProperty("_NormalMap")) bump = mat.GetTexture("_NormalMap");

                        float metallic = 0f;
                        if (mat.HasProperty("metallicFactor")) metallic = mat.GetFloat("metallicFactor");
                        else if (mat.HasProperty("_Metallic")) metallic = mat.GetFloat("_Metallic");

                        float smoothness = 0.5f;
                        if (mat.HasProperty("roughnessFactor")) smoothness = 1f - mat.GetFloat("roughnessFactor");
                        else if (mat.HasProperty("_Roughness")) smoothness = 1f - mat.GetFloat("_Roughness");
                        else if (mat.HasProperty("_Smoothness")) smoothness = mat.GetFloat("_Smoothness");
                        else if (mat.HasProperty("_Glossiness")) smoothness = mat.GetFloat("_Glossiness");

                        Color emissive = Color.black;
                        if (mat.HasProperty("emissiveFactor")) emissive = mat.GetColor("emissiveFactor");
                        else if (mat.HasProperty("_EmissionColor")) emissive = mat.GetColor("_EmissionColor");

                        Texture emissiveMap = null;
                        if (mat.HasProperty("emissiveTexture")) emissiveMap = mat.GetTexture("emissiveTexture");
                        else if (mat.HasProperty("_EmissionMap")) emissiveMap = mat.GetTexture("_EmissionMap");

                        mat.shader = urpLit;

                        // ── Restore values using URP Lit property names ──
                        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", albedo);
                        if (mat.HasProperty("_BaseMap") && mainTex != null) mat.SetTexture("_BaseMap", mainTex);
                        if (mat.HasProperty("_BumpMap") && bump != null) mat.SetTexture("_BumpMap", bump);
                        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
                        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

                        if (emissive != Color.black)
                        {
                            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emissive);
                            if (mat.HasProperty("_EmissionMap") && emissiveMap != null) mat.SetTexture("_EmissionMap", emissiveMap);
                            mat.EnableKeyword("_EMISSION");
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
