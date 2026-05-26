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
            int converted = FixAllSceneMaterials();
            EnableGPUResidentDrawerAllAssets(silent: true);
            EditorUtility.DisplayDialog(
                "SRP Batcher Fix Complete",
                $"Converted {converted} non-URP material(s) to URP Lit.\n" +
                "GPU Instancing has been enabled on all materials.\n" +
                "Static batching flags applied to city objects.\n" +
                "GPU Resident Drawer enabled on all pipeline assets.\n\n" +
                "SRP Batcher should now show active batches in the Frame Debugger.",
                "OK");
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

        [MenuItem("Egyptian/🚀 Enable GPU Resident Drawer", false, 12)]
        public static void EnableGPUResidentDrawerMenuItem()
        {
            EnableGPUResidentDrawerAllAssets(silent: false);
        }

        public static void EnableGPUResidentDrawerAllAssets(bool silent)
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

                if (prop != null)
                {
                    try
                    {
                        var enumType = prop.PropertyType;
                        var value = System.Enum.Parse(enumType, "InstancedDrawing");
                        prop.SetValue(asset, value);
                        EditorUtility.SetDirty(asset);
                        successCount++;
                        Debug.Log($"[URPSRPBatcherFixer] Enabled GPU Resident Drawer (Instanced Drawing) on asset: {path}");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[URPSRPBatcherFixer] Failed to set gpuResidentDrawerMode on {path}: {ex.Message}");
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
                        $"Successfully enabled GPU Resident Drawer on all {successCount} URP assets in the project!", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("GPU Resident Drawer", 
                        $"Found {totalCount} URP assets. Successfully enabled GPU Resident Drawer on {successCount} of them.\nCheck the Unity console for details.", "OK");
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
                        shaderName == "Sprites/Default";

                    if (isNonURP)
                    {
                        // ── Capture legacy property values before shader swap ──
                        Color albedo    = mat.HasProperty("_Color")       ? mat.GetColor("_Color")            : Color.white;
                        Texture mainTex = mat.HasProperty("_MainTex")     ? mat.GetTexture("_MainTex")        : null;
                        Texture bump    = mat.HasProperty("_BumpMap")     ? mat.GetTexture("_BumpMap")        : null;
                        float metallic  = mat.HasProperty("_Metallic")    ? mat.GetFloat("_Metallic")         : 0f;
                        float gloss     = mat.HasProperty("_Glossiness")  ? mat.GetFloat("_Glossiness")       : 0.5f;

                        mat.shader = urpLit;

                        // ── Restore values using URP Lit property names ──
                        if (mat.HasProperty("_BaseColor"))   mat.SetColor("_BaseColor",   albedo);
                        if (mat.HasProperty("_BaseMap") && mainTex != null) mat.SetTexture("_BaseMap", mainTex);
                        if (mat.HasProperty("_BumpMap") && bump != null)    mat.SetTexture("_BumpMap", bump);
                        if (mat.HasProperty("_Metallic"))    mat.SetFloat("_Metallic",    metallic);
                        if (mat.HasProperty("_Smoothness"))  mat.SetFloat("_Smoothness",  gloss);

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
