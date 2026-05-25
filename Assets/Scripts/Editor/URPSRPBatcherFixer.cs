using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace TheAlchemistsCrypt.Editor
{
    /// <summary>
    /// Editor utility for:
    ///   1) Converting non-SRP-compatible materials to URP Lit (fixes the SRP Batcher)
    ///   2) Marking EgyptianCity_V5_Final children as Occluder/Occludee/Batching Static
    ///   3) Baking occlusion culling data on demand via a dedicated menu item
    ///
    /// Menu items under Egyptian →:
    ///   • "Fix SRP Batcher Materials"   – instant, run after any scene change
    ///   • "🔥 Bake Occlusion Culling"   – slow (1–5 min), run before final builds
    /// </summary>
    public static class URPSRPBatcherFixer
    {
        // ─────────────────────────────────────────────────────────────────────
        //  Menu Items
        // ─────────────────────────────────────────────────────────────────────

        [MenuItem("Egyptian/Fix SRP Batcher Materials", false, 10)]
        public static void FixMaterialsMenuItem()
        {
            int converted = FixAllSceneMaterials();
            EditorUtility.DisplayDialog(
                "SRP Batcher Fix Complete",
                $"Converted {converted} non-URP material(s) to URP Lit.\n" +
                "GPU Instancing has been enabled on all materials.\n\n" +
                "SRP Batcher should now show active batches in the Frame Debugger.",
                "OK");
        }

        [MenuItem("Egyptian/🔥 Bake Occlusion Culling", false, 11)]
        public static void BakeOcclusionMenuItem()
        {
            bool proceed = EditorUtility.DisplayDialog(
                "Bake Occlusion Culling",
                "This will:\n" +
                "  1. Mark EgyptianCity_V5_Final children as Static\n" +
                "  2. Fix any remaining non-URP materials\n" +
                "  3. Bake occlusion data (Unity may be unresponsive for 1–5 min)\n\n" +
                "Run this when you are happy with the current city layout.\n" +
                "You do NOT need to re-bake after enemy / UI / gameplay code changes.",
                "Bake Now",
                "Cancel");

            if (!proceed) return;
            BakeOcclusion();
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
                FindObjectsInactive.Include, FindObjectsSortMode.None);

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
        /// OccluderStatic, OccludeeStatic, BatchingStatic and ReflectionProbeStatic.
        /// Dynamic objects (enemies, player, weapons, pickups, managers) are skipped.
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

            StaticEditorFlags staticFlags =
                StaticEditorFlags.OccluderStatic |
                StaticEditorFlags.OccludeeStatic  |
                StaticEditorFlags.BatchingStatic  |
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

            Debug.Log($"[URPSRPBatcherFixer] Static flags set on {marked} objects. Skipped {skipped} dynamic objects.");
        }

        /// <summary>
        /// Full occlusion culling bake pipeline:
        ///   1. Set city objects as Static
        ///   2. Fix non-URP materials
        ///   3. Save scene
        ///   4. Compute occlusion (blocking — editor shows progress bar)
        ///   5. Save scene again
        /// </summary>
        public static void BakeOcclusion()
        {
            SetCityObjectsStatic();
            FixAllSceneMaterials();

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[URPSRPBatcherFixer] Starting occlusion culling bake — Unity may be unresponsive for 1–5 minutes...");
            StaticOcclusionCulling.Compute();

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[URPSRPBatcherFixer] ✅ Occlusion culling bake complete. Scene saved.");
        }
    }
}
