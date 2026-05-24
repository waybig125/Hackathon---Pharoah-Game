using UnityEngine;
using UnityEditor;
using Unity.AI.Navigation;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using UnityMeshSimplifier;

namespace TheAlchemistsCrypt.Editor
{
    public class StaticEgyptianCityGenerator : EditorWindow
    {
        [MenuItem("Egyptian/Generate & Setup City", false, 1)]
        public static void QuickRegen() {
            var g = CreateInstance<StaticEgyptianCityGenerator>();
            g.Purge(); 
            g.GeneratePolishedCity();
        }

        [MenuItem("Egyptian/Open Generator Window", false, 2)]
        public static void ShowWindow() => GetWindow<StaticEgyptianCityGenerator>("Egyptian City V5.2");

        private int seed = 999;
        private int gridSize = 8; 
        private string rootName = "EgyptianCity_V5_Final";

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("V5.2 AESTHETIC OVERHAUL: stack floors, terra-cotta walls, purple shadows, gradient sky disc, emissive artifacts.", MessageType.Info);
            seed = EditorGUILayout.IntField("Seed", seed);
            gridSize = EditorGUILayout.IntField("Grid Size", gridSize);

            if (GUILayout.Button("▶ GENERATE & SETUP CITY GAME", GUILayout.Height(40))) {
                Purge();
                GeneratePolishedCity();
            }
            
            EditorGUILayout.Space();
            if (GUILayout.Button("🗑 CLEANUP", GUILayout.Height(30))) Purge();
        }

        private void SetupMummyAnimations()
        {
            string[] fbxPaths = {
                "Assets/Mummy_Assets/base.fbx",
                "Assets/Mummy_Assets/base_basic_pbr.fbx",
                "Assets/Mummy_Assets/base_basic_shaded.fbx",
                "Assets/Mummy_Assets/mummy_base.fbx",
                "Assets/Mummy_Assets/mummy_idle.fbx",
                "Assets/Mummy_Assets/new_Walking.fbx",
                "Assets/Mummy_Assets/mummy_attack.fbx",
                "Assets/Mummy_Assets/mummy_death.fbx",
                "Assets/Resources/Pharaoh/base_basic_shaded (3).fbx"
            };
            foreach (var p in fbxPaths) {
                ConfigureFbxToHumanoid(p);
            }

            string controllerPath = "Assets/Mummy_Assets/MummyTestController.controller";
            var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
            if (controller == null) {
                controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            }

            // Ensure parameters exist
            bool hasSpeed = false;
            bool hasAttack = false;
            foreach (var p in controller.parameters) {
                if (p.name == "Speed") hasSpeed = true;
                if (p.name == "Attack") hasAttack = true;
            }
            if (!hasSpeed) controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            if (!hasAttack) controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

            var layer = controller.layers[0];
            var rootStateMachine = layer.stateMachine;

            if (!System.IO.Directory.Exists("Assets/Mummy_Assets")) {
                System.IO.Directory.CreateDirectory("Assets/Mummy_Assets");
            }

            var idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Mummy_Assets/mummy_idle.fbx");
            var walkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Mummy_Assets/new_Walking.fbx");
            var attackClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Mummy_Assets/mummy_attack.fbx");

            var idleState = GetOrAddState(rootStateMachine, "Idle", idleClip);
            var walkState = GetOrAddState(rootStateMachine, "Walk", walkClip);
            var attackState = GetOrAddState(rootStateMachine, "Attack", attackClip);

            if (idleState.transitions.Length == 0) {
                var t = idleState.AddTransition(walkState);
                t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.1f, "Speed");
                t.hasExitTime = false; t.duration = 0.25f;
            }
            if (walkState.transitions.Length == 0) {
                var t = walkState.AddTransition(idleState);
                t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.1f, "Speed");
                t.hasExitTime = false; t.duration = 0.25f;
            }
            
            bool attackTransExists = false;
            foreach(var t in rootStateMachine.anyStateTransitions) if(t.destinationState == attackState) attackTransExists = true;
            if(!attackTransExists) {
                var t = rootStateMachine.AddAnyStateTransition(attackState);
                t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "Attack");
                t.duration = 0.1f;
            }

            Debug.Log("Mummy & Pharaoh Animator Setup with Transitions!");

            // Apply 80% Mesh Decimation to Mummies for Mobile Performance
            // var mummies = GameObject.FindObjectsByType<TheAlchemistsCrypt.AI.ZombieAI>(FindObjectsInactive.Include);
            // foreach (var m in mummies) {
            //     DecimateMesh(m.gameObject, 0.8f);
            // }

            // Also decimate the prefabs in Resources to ensure spawned ones are optimized
            // string[] resourcePrefabs = { "Assets/Resources/Mummy_Dynamic_Prefab.prefab", "Assets/Resources/Pharaoh_Prefab.prefab" };
            // foreach (var path in resourcePrefabs) {
            //     var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            //     if (prefab != null) {
            //         DecimateMesh(prefab, 0.8f);
            //         EditorUtility.SetDirty(prefab);
            //     }
            // }
        }

        private UnityEditor.Animations.AnimatorState GetOrAddState(UnityEditor.Animations.AnimatorStateMachine sm, string name, AnimationClip clip) {
            foreach (var s in sm.states) if (s.state.name == name) {
                s.state.motion = clip;
                return s.state;
            }
            var newState = sm.AddState(name);
            newState.motion = clip;
            return newState;
        }

        private void ConfigureFbxToHumanoid(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null) {
                // Skip if already configured to avoid redundant re-imports and Rig Error spam
                if (importer.animationType == ModelImporterAnimationType.Human && 
                    importer.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel)
                {
                    return;
                }

                bool dirty = false;
                if (importer.animationType != ModelImporterAnimationType.Human) {
                    importer.animationType = ModelImporterAnimationType.Human; 
                    dirty = true;
                }
                if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel) {
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel; 
                    dirty = true;
                }
                
                if (dirty) {
                    try {
                        Debug.Log($"[CityGen] Reimporting {path} as Humanoid...");
                        importer.SaveAndReimport();
                    } catch (System.Exception ex) {
                        Debug.LogError($"[CityGen] Failed to configure {path} as Humanoid: {ex.Message}");
                    }
                }
            }
        }

        private void Purge()
        {
            // 1. Destroy procedural city root if it exists
            var cityRoot = GameObject.Find(rootName);
            if (cityRoot != null) DestroyImmediate(cityRoot);

            // 2. Loop through root GameObjects and destroy any non-essential ones
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var r in roots)
            {
                if (r == null) continue;
                string lowerName = r.name.ToLower().Replace(" ", "");

                // Keep Player, cameras, lights, EventSystem, GameController, and Terrain
                if (lowerName.Contains("player") || lowerName.Contains("camera") || lowerName.Contains("controller") || 
                    lowerName.Contains("eventsystem") || lowerName.Contains("terrain") || lowerName.Contains("light") || 
                    lowerName.Contains("sun") || lowerName.Contains("sky") || r.name == "TestRoot")
                {
                    continue;
                }

                DestroyImmediate(r);
            }

            // 3. Keep fallback keyword check for childless / hidden / empty name GameObjects
            var all = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in all) {
                if (go == null) continue;
                
                // Clear any empty / unnamed remnants immediately
                if (string.IsNullOrEmpty(go.name) || string.IsNullOrEmpty(go.name.Trim()))
                {
                    DestroyImmediate(go);
                    continue;
                }

                string lowerName = go.name.ToLower().Replace(" ", ""); 
                if (lowerName.Contains("egyptiancity") || lowerName.Contains("desertfloor") || lowerName.Contains("floorground") ||
                    lowerName.Contains("groundplane") || lowerName.Contains("desertterrain") ||
                    lowerName.Contains("player_copy") || lowerName.Contains("mobilehud") || lowerName.Contains("p_lpsp_ui_canvas") || 
                    lowerName.StartsWith("mummy") || lowerName.Contains("windowlight") || lowerName.Contains("crater") ||
                    lowerName.Contains("plaza") || lowerName.Contains("house") || lowerName.Contains("pyramid") ||
                    lowerName.Contains("seazone") || lowerName.Contains("beachzone") || lowerName.Contains("coastlinebarrier") ||
                    lowerName.Contains("globalvolume") || lowerName.Contains("reflectionprobe") ||
                    lowerName.Contains("claritylight") || lowerName.Contains("environmentvolume") ||
                    lowerName.Contains("audiomanager") || lowerName.Contains("hivemindmanager") || lowerName.Contains("mummyspawner") ||
                    lowerName.Contains("escapemanager") || lowerName.Contains("dynamicprops")) 
                {
                    DestroyImmediate(go);
                }
            }
            
            // Clean up unreferenced procedural meshes from editor memory to prevent scene file bloating
            EditorUtility.UnloadUnusedAssetsImmediate();
        }

        private void SetupManagers(GameObject root)
        {
            var am = new GameObject("AudioManager");
            am.transform.SetParent(root.transform);
            am.AddComponent<TheAlchemistsCrypt.Gameplay.AudioManager>();

            var hm = new GameObject("HiveMindManager");
            hm.transform.SetParent(root.transform);
            hm.AddComponent<TheAlchemistsCrypt.AI.HiveMindManager>();

            var ms = new GameObject("MummySpawner");
            ms.transform.SetParent(root.transform);
            ms.AddComponent<TheAlchemistsCrypt.AI.MummySpawner>();

            var em = new GameObject("EscapeManager");
            em.transform.SetParent(root.transform);
            em.AddComponent<TheAlchemistsCrypt.Gameplay.EscapeManager>();
            
            Debug.Log("[CityGen] AudioManager, HiveMindManager, MummySpawner, and EscapeManager injected into scene.");
        }

        private void DecimateMesh(GameObject obj, float quality)
        {
            var filters = obj.GetComponentsInChildren<MeshFilter>();
            foreach (var mf in filters) {
                if (mf.sharedMesh == null) continue;
                try {
                    var simplifier = new MeshSimplifier();
                    simplifier.Initialize(mf.sharedMesh);
                    simplifier.SimplifyMesh(quality);
                    mf.sharedMesh = simplifier.ToMesh();
                } catch {}
            }
        }

        private Mesh GetSharedDecimatedColumnMesh(GameObject columnPrefab)
        {
            if (columnPrefab == null) return null;
            
            string decimatedMeshPath = "Assets/EgyptianAssets/egyptian_column_decimated.mesh";
            Mesh decimatedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(decimatedMeshPath);
            if (decimatedMesh != null) return decimatedMesh;
            
            // Generate it once
            var mf = columnPrefab.GetComponentInChildren<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return null;
            
            try {
                var simplifier = new MeshSimplifier();
                simplifier.Initialize(mf.sharedMesh);
                simplifier.SimplifyMesh(0.15f);
                decimatedMesh = simplifier.ToMesh();
                decimatedMesh.name = "egyptian_column_decimated";
                AssetDatabase.CreateAsset(decimatedMesh, decimatedMeshPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[CityGen] Created low-poly decimated column mesh asset at: " + decimatedMeshPath);
                return decimatedMesh;
            } catch (System.Exception e) {
                Debug.LogError("[CityGen] Failed to decimate column mesh: " + e.Message);
                return mf.sharedMesh;
            }
        }

        private Mesh GetSharedDecimatedMesh(Mesh originalMesh, float quality)
        {
            if (originalMesh == null) return null;
            
            string meshName = originalMesh.name;
            if (string.IsNullOrEmpty(meshName)) meshName = "unnamed_mesh_" + originalMesh.GetInstanceID();
            
            string sanitizedName = System.Text.RegularExpressions.Regex.Replace(meshName, @"[^a-zA-Z0-9_\-]", "_");
            string decimatedDir = "Assets/EgyptianAssets/DecimatedMeshes";
            if (!System.IO.Directory.Exists(decimatedDir)) {
                System.IO.Directory.CreateDirectory(decimatedDir);
            }
            
            string decimatedMeshPath = $"{decimatedDir}/{sanitizedName}_dec_{quality:F2}.mesh";
            Mesh decimatedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(decimatedMeshPath);
            if (decimatedMesh != null) return decimatedMesh;
            
            try {
                var simplifier = new MeshSimplifier();
                simplifier.Initialize(originalMesh);
                simplifier.SimplifyMesh(quality);
                decimatedMesh = simplifier.ToMesh();
                decimatedMesh.name = originalMesh.name + "_decimated";
                AssetDatabase.CreateAsset(decimatedMesh, decimatedMeshPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[CityGen] Created decimated mesh asset at: " + decimatedMeshPath);
                return decimatedMesh;
            } catch (System.Exception e) {
                Debug.LogError("[CityGen] Failed to decimate mesh: " + e.Message);
                return originalMesh;
            }
        }

        public void GeneratePolishedCity()
        {
            Random.InitState(seed);
            Purge();

            var root = new GameObject(rootName);
            root.isStatic = true;

            var trees = new GameObject[] {
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/realistic_hd_date_palm_2178.glb"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/realistic_hd_date_palm_378.glb")
            };
            var crate = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/crate.glb");
            var barrel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/barrel.glb");
            var columnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/egyptian_column.glb");
            if (columnPrefab == null) columnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/egyptian_pillar_column.glb");
            
            // Create warm golden sandstone gradient texture for houses
            string houseTexPath = "Assets/EgyptianAssets/HouseGradientTex.png";
            if (System.IO.File.Exists(houseTexPath)) {
                System.IO.File.Delete(houseTexPath);
                System.IO.File.Delete(houseTexPath + ".meta");
                AssetDatabase.Refresh();
            }
            Texture2D houseTex = null;
            if (houseTex == null) {
                if (!System.IO.Directory.Exists("Assets/EgyptianAssets")) System.IO.Directory.CreateDirectory("Assets/EgyptianAssets");
                int texSize = 512;
                houseTex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, true);
                houseTex.wrapMode = TextureWrapMode.Clamp;
                houseTex.filterMode = FilterMode.Bilinear;
                Color bottomColor = new Color(0.75f, 0.55f, 0.47f); // Desaturated warm sand bottom
                Color topColor = new Color(0.86f, 0.62f, 0.50f);    // Desaturated warm sand top
                Color[] pixels = new Color[texSize * texSize];
                for (int y = 0; y < texSize; y++) {
                    float t = (float)y / (texSize - 1);
                    Color rowColor = Color.Lerp(bottomColor, topColor, t);
                    for (int x = 0; x < texSize; x++) pixels[y * texSize + x] = rowColor;
                }
                houseTex.SetPixels(pixels);
                houseTex.Apply(true);
                byte[] bytes = houseTex.EncodeToPNG();
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, "EgyptianAssets/HouseGradientTex.png"), bytes);
                AssetDatabase.Refresh();
                houseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(houseTexPath);
            }

            // ── AESTHETIC PALETTE (Golden Sandstone Desert) ──
            string normalMapPath = "Assets/Resources/Textures/EgyptianNormalMap.png";
            var importer = AssetImporter.GetAtPath(normalMapPath) as TextureImporter;
            if (importer != null)
            {
                bool changed = false;
                if (importer.textureType != TextureImporterType.NormalMap)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    changed = true;
                }
                if (!importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = true;
                    changed = true;
                }
                if (changed)
                {
                    importer.SaveAndReimport();
                    AssetDatabase.Refresh();
                }
            }

            Material wallMat = new Material(GetLitShader());
            if (houseTex != null)
            {
                wallMat.SetTexture("_BaseMap", houseTex);
                wallMat.SetTextureScale("_BaseMap", new Vector2(10, 10));
            }
            else
            {
                wallMat.SetColor("_BaseColor", new Color(0.96f, 0.85f, 0.75f));
            }

            Texture2D normalMapTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalMapPath);
            if (normalMapTex != null)
            {
                wallMat.SetTexture("_BumpMap", normalMapTex);
                wallMat.SetTextureScale("_BumpMap", new Vector2(10, 10));
                wallMat.EnableKeyword("_NORMALMAP");
                wallMat.SetFloat("_BumpScale", 1.0f);
            }

            wallMat.SetFloat("_Smoothness", 0.0f);   // Matte finish
            wallMat.enableInstancing = true;
            
            Material woodMat = new Material(GetLitShader());
            woodMat.SetColor("_BaseColor", new Color(0.20f, 0.12f, 0.08f)); // Dark Wood
            woodMat.enableInstancing = true;
            
            Material floorMat = new Material(GetLitShader());
            floorMat.SetColor("_BaseColor", new Color(0.95f, 0.85f, 0.70f)); // Warm Pastel Sand
            floorMat.SetFloat("_Metallic", 0.0f);
            floorMat.SetFloat("_Smoothness", 0.10f);
            floorMat.SetColor("_EmissionColor", new Color(1.0f, 0.95f, 0.8f) * 0.02f);
            floorMat.EnableKeyword("_EMISSION");
            floorMat.enableInstancing = true;

            Material litWindowMat = new Material(GetLitShader());
            litWindowMat.SetColor("_BaseColor", new Color(1f, 0.95f, 0.8f)); // Bright warm white-gold
            litWindowMat.SetColor("_EmissionColor", new Color(1.0f, 0.75f, 0.25f) * 15.0f); // Intense warm amber glow
            litWindowMat.EnableKeyword("_EMISSION");
            litWindowMat.enableInstancing = true;
            
            Material darkWindowMat = new Material(GetLitShader());
            darkWindowMat.SetColor("_BaseColor", new Color(0.15f, 0.1f, 0.15f)); // Dark cool-purple contrast
            darkWindowMat.enableInstancing = true;

            SetupEnvironment(root);
            SetupManagers(root);
            
            var probeGo = new GameObject("GlobalReflectionProbe");
            probeGo.transform.SetParent(root.transform);
            var probe = probeGo.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Baked;
            probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.renderDynamicObjects = true;
            probe.size = new Vector3(2000f, 500f, 2000f);
            probe.importance = 1;

            try {
                string exrPath = "Assets/Materials/GlobalReflectionProbe.exr";
                if (!System.IO.Directory.Exists("Assets/Materials")) System.IO.Directory.CreateDirectory("Assets/Materials");
#if UNITY_EDITOR
                UnityEditor.Lightmapping.BakeReflectionProbe(probe, exrPath);
                Debug.Log($"[CityGen] Baked GlobalReflectionProbe to {exrPath}");
#endif
            } catch (System.Exception e) {
                Debug.LogWarning($"[CityGen] Failed to bake GlobalReflectionProbe: {e.Message}");
            }

            TerrainData terrainData = new TerrainData();
            terrainData.heightmapResolution = 513;
            terrainData.size = new Vector3(1000f, 10f, 1000f);

            int resolution = terrainData.heightmapResolution;
            float[,] heights = new float[resolution, resolution];
            for (int i = 0; i < resolution; i++) {
                for (int j = 0; j < resolution; j++) {
                    float tx = (float)i / (resolution - 1);
                    float ty = (float)j / (resolution - 1);
                    
                    float dune1 = Mathf.PerlinNoise(tx * 3f, ty * 3f) * 0.6f;
                    float dune2 = Mathf.PerlinNoise(tx * 8f + 10f, ty * 8f + 10f) * 0.12f;
                    float baseDune = dune1 + dune2;

                    float distFromCenter = Mathf.Sqrt((tx - 0.5f) * (tx - 0.5f) + (ty - 0.5f) * (ty - 0.5f));
                    float spawnFlatten = Mathf.SmoothStep(0f, 1f, (distFromCenter - 0.02f) * 25f);
                    if (distFromCenter < 0.02f) spawnFlatten = 0f;
                    
                    heights[i, j] = baseDune * spawnFlatten;

                    // SEA & COASTLINE FLATTENING
                    if (tx < 0.42f) { // Z < -80f
                        if (tx <= 0.40f) { // Z <= -100f
                            heights[i, j] = 0f;
                        } else {
                            float shoreFactor = Mathf.SmoothStep(0f, 1f, (tx - 0.40f) / 0.02f);
                            heights[i, j] *= shoreFactor;
                        }
                    }
                }
            }
            terrainData.SetHeights(0, 0, heights);

            string layerPath = "Assets/EgyptianAssets/DesertSandLayer_V2.terrainlayer";
            TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
            if (layer == null) {
                layer = new TerrainLayer();
                if (!System.IO.Directory.Exists("Assets/EgyptianAssets")) System.IO.Directory.CreateDirectory("Assets/EgyptianAssets");
                AssetDatabase.CreateAsset(layer, layerPath);
            }

            // Always recreate the sand gradient texture with new golden yellow sandstone color
            string sandTexPath = "Assets/EgyptianAssets/SandTexGradient_1024.png";
            if (System.IO.File.Exists(sandTexPath)) {
                System.IO.File.Delete(sandTexPath);
                System.IO.File.Delete(sandTexPath + ".meta");
                AssetDatabase.Refresh();
            }
            Texture2D sandTex = null;
            if (sandTex == null) {
                int texSize = 1024;
                sandTex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, true);
                sandTex.wrapMode = TextureWrapMode.Clamp;
                sandTex.filterMode = FilterMode.Trilinear;
                Color topColor = new Color(0.86f, 0.72f, 0.58f);    // Desaturated sand top
                Color bottomColor = new Color(0.76f, 0.62f, 0.50f); // Desaturated sand bottom
                Color[] pixels = new Color[texSize * texSize];
                for (int y = 0; y < texSize; y++) {
                    float t = (float)y / (texSize - 1);
                    Color rowColor = Color.Lerp(bottomColor, topColor, t);
                    for (int x = 0; x < texSize; x++) pixels[y * texSize + x] = rowColor;
                }
                sandTex.SetPixels(pixels);
                sandTex.Apply(true);
                byte[] pngBytes = sandTex.EncodeToPNG();
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, "EgyptianAssets/SandTexGradient_1024.png"), pngBytes);
                AssetDatabase.Refresh();
                sandTex = AssetDatabase.LoadAssetAtPath<Texture2D>(sandTexPath);
            }

            layer.diffuseTexture = sandTex;
            layer.tileSize = new Vector2(1000f, 1000f); // Stretch across entire terrain to prevent repeating grid
            layer.specular = new Color(0.03f, 0.03f, 0.02f, 0f);
            layer.smoothness = 0.05f;
            EditorUtility.SetDirty(layer);
            AssetDatabase.SaveAssets();
            terrainData.terrainLayers = new TerrainLayer[] { layer };

            GameObject terrainGo = Terrain.CreateTerrainGameObject(terrainData);
            terrainGo.name = "DesertTerrain";
            terrainGo.transform.SetParent(root.transform);
            terrainGo.transform.position = new Vector3(-500f, -0.05f, -500f);
            terrainGo.isStatic = true;

            var terrainComp = terrainGo.GetComponent<Terrain>();
            if (terrainComp != null) {
                terrainComp.basemapDistance = 2000f;
                terrainComp.drawInstanced = true;
            }

            float spacing = 32f;
            float halfSpan = (gridSize * spacing) / 2f;
            var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Inspiration-Thirdperson-Controller-Update372022/Assets/Enemy-AI/Prefabs/TestZombie.prefab");

            for (int x = 0; x < gridSize; x++) {
                for (int z = 0; z < gridSize; z++) {
                    float posX = -halfSpan + (x * spacing) + (spacing / 2f);
                    float posZ = -halfSpan + (z * spacing) + (spacing / 2f);
                    if (posZ < -40f) continue;

                    Vector3 pos = new Vector3(posX, 0, posZ);
                    pos.y = GetTerrainHeight(pos);

                    bool isCentralSpawnPlaza = (Mathf.Approximately(posX, 16f) && Mathf.Approximately(posZ, 48f));
                    if (pos.magnitude > 25f && !isCentralSpawnPlaza) {
                        float roll = Random.value;
                        
                        if (roll < 0.12f) {
                            // 12% chance to spawn the monumental Tomb Gateway instead of a normal house
                            BuildAlchemistTomb(root.transform, pos, wallMat);
                        } 
                        else if (roll < 0.75f) {
                            BuildHouse(root.transform, pos, wallMat, woodMat, litWindowMat, darkWindowMat, crate, barrel, floorMat);
                        } 
                        else {
                            PlacePlaza(root.transform, pos, trees, columnPrefab, floorMat);
                            
                            // 60% chance to drop a procedural obelisk in empty plazas
                            if (Random.value < 0.6f) {
                                Vector3 obeliskPos = pos + new Vector3(Random.Range(-6f, 6f), 0f, Random.Range(-6f, 6f));
                                obeliskPos.y = GetTerrainHeight(obeliskPos);
                                // 35% chance the obelisk is broken
                                bool isBroken = Random.value < 0.35f; 
                                BuildProceduralObelisk(root.transform, obeliskPos, wallMat, isBroken);
                            }
                        }
                    } else {
                        PlacePlaza(root.transform, pos, trees, columnPrefab, floorMat);
                    }

                    if (enemyPrefab != null && Random.value < 0.15f) {
                        var e = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab, root.transform);
                        e.transform.position = pos + Vector3.up * 0.5f;
                        var zai = e.GetComponent<TheAlchemistsCrypt.AI.ZombieAI>();
                        if (zai == null) zai = e.AddComponent<TheAlchemistsCrypt.AI.ZombieAI>();
                        zai.maxHealth = 10f; zai.currentHealth = 10f;
                    }
                }
            }

            // Cleanup any columns that overlap with houses to prevent clipping inside walls
            CleanupOverlappingColumns(root);

            var surface = root.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.RenderMeshes;
            surface.BuildNavMesh();

            // Add dynamic distance-based culling for mobile optimization
            root.AddComponent<TheAlchemistsCrypt.Utils.DistanceCuller>();

            CreateProceduralPyramid(root, new Vector3(-450f, 0f, 400f), 150f, 95f, wallMat, new Color(1f, 0.85f, 0.4f));
            CreateProceduralPyramid(root, new Vector3(450f, 0f, 400f), 160f, 100f, wallMat, new Color(1f, 0.5f, 0.2f)); 
            CreateProceduralPyramid(root, new Vector3(450f, 0f, 120f), 140f, 85f, wallMat, new Color(1f, 0.82f, 0.45f));
            CreateProceduralPyramid(root, new Vector3(-450f, 0f, 120f), 170f, 110f, wallMat, new Color(1f, 0.7f, 0.3f)); 

            SpawnDesertBrokenPillars(root, wallMat);
            SpawnPalmTreeOasis(root, trees);

            CreateSeaAndCoastline(root);
            CreateWorldBounds(root);
            FixPlayerAndWeapons();
            SetupMummyAnimations();

            // Combine all static meshes under the city root to minimize draw calls and maximize mobile FPS!
            StaticBatchingUtility.Combine(root);

            var activeScene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            
            // Clean up any remaining unused assets before saving to minimize scene file size
            EditorUtility.UnloadUnusedAssetsImmediate();
            
            EditorSceneManager.SaveScene(activeScene);
            
            Debug.Log("Polished Egyptian City V5.2 Warm Sunset Aesthetic Overhaul Regenerated!");
        }

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

        private Shader GetLitShader()
        {
            var s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("URP/Lit");
            if (s == null) s = Shader.Find("Lit");
            if (s == null) s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Sprites/Default");
            return s;
        }

        private void SetupEnvironment(GameObject root)
        {
            Color peachColor = new Color(0.98f, 0.62f, 0.42f);     // Warm sunset/orangey peach
            Color sunsetRose = new Color(0.85f, 0.44f, 0.60f);     // Sunset pink/rose
            Color twilightBlue = new Color(0.24f, 0.44f, 0.74f);   // Twilight blue
            Color deepBlueColor = new Color(0.06f, 0.12f, 0.35f);  // Deep twilight/space blue

            Material skyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/SkyGradientBox.mat");
            if (skyMat == null) {
                skyMat = new Material(Shader.Find("Custom/SkyboxGradient"));
                if (!System.IO.Directory.Exists("Assets/Resources")) System.IO.Directory.CreateDirectory("Assets/Resources");
                if (!System.IO.Directory.Exists("Assets/Resources/Materials")) System.IO.Directory.CreateDirectory("Assets/Resources/Materials");
                AssetDatabase.CreateAsset(skyMat, "Assets/Resources/Materials/SkyGradientBox.mat");
            }
            else
            {
                skyMat.shader = Shader.Find("Custom/SkyboxGradient");
            }
            skyMat.SetColor("_ColorBottom", peachColor);
            skyMat.SetColor("_ColorMiddle1", sunsetRose);
            skyMat.SetColor("_ColorMiddle2", twilightBlue);
            skyMat.SetColor("_ColorTop", deepBlueColor);
            EditorUtility.SetDirty(skyMat);
            
            RenderSettings.skybox = skyMat;
            RenderSettings.ambientMode = AmbientMode.Skybox; 

            RenderSettings.fog = true;
            RenderSettings.fogColor = peachColor; // Warm sunset peach fog matching horizon
            RenderSettings.fogStartDistance = 150f;
            RenderSettings.fogEndDistance  = 1000f;   
            
            var sun = GameObject.Find("Directional Light")?.GetComponent<Light>();
            if (sun != null) {
                sun.color = new Color(1.0f, 0.90f, 0.80f); // Warm sunset sunlight
                sun.intensity = 2.0f;
                sun.shadows = LightShadows.Soft; // Soft shadows!
                sun.transform.rotation = Quaternion.Euler(25f, -60f, 0f); // Lower angle to cast nice shadows
            }
            SetupPostProcessing(root.transform);
            DynamicGI.UpdateEnvironment(); // Update lighting reflections
            AssetDatabase.SaveAssets();
        }

        private void SetupPostProcessing(Transform parent)
        {
            var volGo = new GameObject("GlobalVolume");
            volGo.transform.SetParent(parent);
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true; vol.priority = 10;
            
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/VisualOverhaulProfile.asset");
            if (profile == null) {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "VisualOverhaulProfile";
                if (!System.IO.Directory.Exists("Assets/Settings")) System.IO.Directory.CreateDirectory("Assets/Settings");
                AssetDatabase.CreateAsset(profile, "Assets/Settings/VisualOverhaulProfile.asset");
            }
            
            if (!profile.TryGet<Bloom>(out var bloom)) bloom = profile.Add<Bloom>();
            bloom.intensity.Override(0.5f);
            bloom.threshold.Override(0.85f);
            bloom.scatter.Override(0.6f);

            if (!profile.TryGet<ColorAdjustments>(out var colorAdj)) colorAdj = profile.Add<ColorAdjustments>();
            colorAdj.contrast.Override(25f);
            colorAdj.saturation.Override(20f);
            colorAdj.postExposure.Override(0.1f);
            colorAdj.colorFilter.Override(new Color(1f, 0.95f, 0.9f)); 

            if (!profile.TryGet<Tonemapping>(out var tone)) tone = profile.Add<Tonemapping>();
            tone.mode.Override(TonemappingMode.ACES);
            
            if (!profile.TryGet<Vignette>(out var vignette)) vignette = profile.Add<Vignette>();
            vignette.intensity.Override(0.25f);
            vignette.color.Override(new Color(0.15f, 0.12f, 0.2f)); 

            if (!profile.TryGet<LiftGammaGain>(out var lgg)) lgg = profile.Add<LiftGammaGain>();
            lgg.lift.Override(new Vector4(0.05f, 0.05f, 0.20f, 0f)); // Pushes shadows toward deep blue
            lgg.gamma.Override(new Vector4(1.0f, 1.0f, 1.0f, 0f));   // Keep midtones neutral/bright
            lgg.gain.Override(new Vector4(1.05f, 1.0f, 0.95f, 0f));  // Slight warm pop on highlights

            EditorUtility.SetDirty(profile);
            
            vol.sharedProfile = profile;
        }

        private void BuildHouse(Transform parent, Vector3 pos, Material wall, Material wood, Material litWindowMat, Material darkWindowMat, GameObject crate, GameObject barrel, Material floorMat = null)
        {
            var h = new GameObject("House"); h.transform.SetParent(parent); h.transform.position = pos; h.isStatic = true;
            
            // Randomly vary height slightly for organic skyline silhouette
            float heightScale = Random.Range(0.85f, 1.25f);
            
            // 1. Central main building hall (extended 3.0f below ground to prevent floating foundations on slopes)
            float hallWidth = 20f; float hallDepth = 15f; float hallHeight = 14f * heightScale;
            var hall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hall.name = "MainHall";
            hall.transform.SetParent(h.transform);
            hall.transform.localPosition = new Vector3(0f, (hallHeight - 3.0f) / 2f, 0f);
            hall.transform.localScale = new Vector3(hallWidth, hallHeight + 3.0f, hallDepth);
            hall.GetComponent<Renderer>().sharedMaterial = wall;
            hall.isStatic = true;

            // Dark wooden border ring (cornice) at the top of the main hall
            var hallTopRing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hallTopRing.name = "HallTopRing";
            hallTopRing.transform.SetParent(h.transform);
            hallTopRing.transform.localPosition = new Vector3(0f, hallHeight, 0f);
            hallTopRing.transform.localScale = new Vector3(hallWidth + 0.3f, 0.4f, hallDepth + 0.3f);
            hallTopRing.GetComponent<Renderer>().sharedMaterial = wood;
            DestroyImmediate(hallTopRing.GetComponent<Collider>());
            hallTopRing.isStatic = true;

            // Side decorative pillars embedded in walls (extends 3.0f below ground)
            float[] pillarX = { -hallWidth / 2f - 0.1f, hallWidth / 2f + 0.1f };
            float[] pillarZs = { -3f, 0f, 3f };
            foreach (var px in pillarX)
            {
                foreach (var pz in pillarZs)
                {
                    var sidePillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    sidePillar.name = "SideDecorPillar";
                    sidePillar.transform.SetParent(h.transform);
                    sidePillar.transform.localPosition = new Vector3(px, (hallHeight - 3.0f) / 2f, pz);
                    sidePillar.transform.localScale = new Vector3(0.3f, hallHeight + 3.0f, 0.6f);
                    sidePillar.GetComponent<Renderer>().sharedMaterial = wood;
                    DestroyImmediate(sidePillar.GetComponent<Collider>());
                    sidePillar.isStatic = true;
                }
            }

            // 2. Corner Step-Sloped Towers (4 corners)
            float tDistX = (hallWidth / 2f) + 0.5f;
            float tDistZ = (hallDepth / 2f) + 0.5f;
            Vector3[] towerPositions = {
                new Vector3(-tDistX, 0f, -tDistZ), // Left Front
                new Vector3(tDistX, 0f, -tDistZ),  // Right Front
                new Vector3(-tDistX, 0f, tDistZ),  // Left Back
                new Vector3(tDistX, 0f, tDistZ)   // Right Back
            };

            foreach (var tPos in towerPositions)
            {
                var towerRoot = new GameObject("TaperedTower");
                towerRoot.transform.SetParent(h.transform);
                towerRoot.transform.localPosition = tPos;
                towerRoot.isStatic = true;

                // Tapered look via 3 stacked stepped segments
                int numSegments = 3;
                float segHeight = (18f * heightScale) / numSegments;
                float baseSize = 5.0f;
                float taperDelta = 0.5f;

                for (int s = 0; s < numSegments; s++)
                {
                    float size = baseSize - (s * taperDelta);
                    var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    seg.name = $"Seg_{s}";
                    seg.transform.SetParent(towerRoot.transform);
                    
                    float yLoc = (s * segHeight) + (segHeight / 2f);
                    float ySize = segHeight;
                    if (s == 0)
                    {
                        // Extend bottom segment downward by 3.0f to act as a solid foundation
                        ySize = segHeight + 3.0f;
                        yLoc = (segHeight - 3.0f) / 2f;
                    }
                    seg.transform.localPosition = new Vector3(0f, yLoc, 0f);
                    seg.transform.localScale = new Vector3(size, ySize, size);
                    seg.GetComponent<Renderer>().sharedMaterial = wall;
                    seg.isStatic = true;

                    // Horizontal dark transition ring at the top of each segment
                    var ring = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    ring.name = $"Ring_{s}";
                    ring.transform.SetParent(towerRoot.transform);
                    ring.transform.localPosition = new Vector3(0f, (s + 1) * segHeight, 0f);
                    ring.transform.localScale = new Vector3(size + 0.25f, 0.4f, size + 0.25f);
                    ring.GetComponent<Renderer>().sharedMaterial = wood;
                    DestroyImmediate(ring.GetComponent<Collider>());
                    ring.isStatic = true;
                }

                // Add roof battlements (crenelations) on the top segment of each tower
                float topSize = baseSize - ((numSegments - 1) * taperDelta);
                float topY = 18f * heightScale;
                float crenSize = 0.5f;
                Vector3[] crenOffsets = {
                    new Vector3(-topSize/2f + 0.25f, topY + 0.25f, -topSize/2f + 0.25f),
                    new Vector3(topSize/2f - 0.25f, topY + 0.25f, -topSize/2f + 0.25f),
                    new Vector3(-topSize/2f + 0.25f, topY + 0.25f, topSize/2f - 0.25f),
                    new Vector3(topSize/2f - 0.25f, topY + 0.25f, topSize/2f - 0.25f)
                };
                foreach (var cOff in crenOffsets)
                {
                    var cren = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cren.name = "Crenelation";
                    cren.transform.SetParent(towerRoot.transform);
                    cren.transform.localPosition = cOff;
                    cren.transform.localScale = new Vector3(crenSize, crenSize, crenSize);
                    cren.GetComponent<Renderer>().sharedMaterial = wall;
                    cren.isStatic = true;
                }
            }

            // 3. Central Temple Gateway Frame (Front Face, extended 3.0f below ground)
            float gateHeight = 8f * heightScale;
            var lPillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lPillar.name = "GatePillar_L";
            lPillar.transform.SetParent(h.transform);
            lPillar.transform.localPosition = new Vector3(-2.8f, (gateHeight - 3.0f) / 2f, -(hallDepth / 2f) - 0.3f);
            lPillar.transform.localScale = new Vector3(1.2f, gateHeight + 3.0f, 0.8f);
            lPillar.GetComponent<Renderer>().sharedMaterial = wood;
            lPillar.isStatic = true;

            var rPillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rPillar.name = "GatePillar_R";
            rPillar.transform.SetParent(h.transform);
            rPillar.transform.localPosition = new Vector3(2.8f, (gateHeight - 3.0f) / 2f, -(hallDepth / 2f) - 0.3f);
            rPillar.transform.localScale = new Vector3(1.2f, gateHeight + 3.0f, 0.8f);
            rPillar.GetComponent<Renderer>().sharedMaterial = wood;
            rPillar.isStatic = true;

            var lintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lintel.name = "GateLintel";
            lintel.transform.SetParent(h.transform);
            lintel.transform.localPosition = new Vector3(0f, gateHeight + 0.6f, -(hallDepth / 2f) - 0.3f);
            lintel.transform.localScale = new Vector3(6.8f, 1.2f, 1.2f);
            lintel.GetComponent<Renderer>().sharedMaterial = wood;
            lintel.isStatic = true;

            // 4. Central Glowing Entryway (Emissive Light Portal, extended 3.0f below ground)
            var entryway = GameObject.CreatePrimitive(PrimitiveType.Cube);
            entryway.name = "TempleEntrance";
            entryway.transform.SetParent(h.transform);
            entryway.transform.localPosition = new Vector3(0f, (gateHeight - 3.0f) / 2f, -(hallDepth / 2f) - 0.1f);
            entryway.transform.localScale = new Vector3(4.0f, gateHeight + 3.0f, 0.2f);
            entryway.GetComponent<Renderer>().sharedMaterial = litWindowMat;
            DestroyImmediate(entryway.GetComponent<Collider>()); // Trigger/walk-through
            entryway.isStatic = true;

            // 5. Narrow Vertical Slot Windows (Sides)
            Vector3[] winLeftLocs = {
                new Vector3(-(hallWidth / 2f) - 0.1f, 8f * heightScale, -2.5f),
                new Vector3(-(hallWidth / 2f) - 0.1f, 8f * heightScale, 2.5f)
            };
            foreach (var wLoc in winLeftLocs)
            {
                Material winMat = (Random.value < 0.6f) ? litWindowMat : darkWindowMat;
                var win = GameObject.CreatePrimitive(PrimitiveType.Cube);
                win.name = "SlotWindow";
                win.transform.SetParent(h.transform);
                win.transform.localPosition = wLoc;
                win.transform.localScale = new Vector3(0.18f, 4.5f, 0.8f);
                win.GetComponent<Renderer>().sharedMaterial = winMat;
                DestroyImmediate(win.GetComponent<Collider>());
                win.isStatic = true;
            }

            Vector3[] winRightLocs = {
                new Vector3((hallWidth / 2f) + 0.1f, 8f * heightScale, -2.5f),
                new Vector3((hallWidth / 2f) + 0.1f, 8f * heightScale, 2.5f)
            };
            foreach (var wLoc in winRightLocs)
            {
                Material winMat = (Random.value < 0.6f) ? litWindowMat : darkWindowMat;
                var win = GameObject.CreatePrimitive(PrimitiveType.Cube);
                win.name = "SlotWindow";
                win.transform.SetParent(h.transform);
                win.transform.localPosition = wLoc;
                win.transform.localScale = new Vector3(0.18f, 4.5f, 0.8f);
                win.GetComponent<Renderer>().sharedMaterial = winMat;
                DestroyImmediate(win.GetComponent<Collider>());
                win.isStatic = true;
            }

            Vector3[] winBackLocs = {
                new Vector3(-4f, 8f * heightScale, (hallDepth / 2f) + 0.1f),
                new Vector3(4f, 8f * heightScale, (hallDepth / 2f) + 0.1f)
            };
            foreach (var wLoc in winBackLocs)
            {
                Material winMat = (Random.value < 0.6f) ? litWindowMat : darkWindowMat;
                var win = GameObject.CreatePrimitive(PrimitiveType.Cube);
                win.name = "SlotWindow";
                win.transform.SetParent(h.transform);
                win.transform.localPosition = wLoc;
                win.transform.localScale = new Vector3(0.8f, 4.5f, 0.18f);
                win.GetComponent<Renderer>().sharedMaterial = winMat;
                DestroyImmediate(win.GetComponent<Collider>());
                win.isStatic = true;
            }

            // 6. Rooftop Access Ladder (Angles at 35 degrees from the back, acting as a clean ramp)
            float ladderLength = (hallHeight + 2f) / Mathf.Sin(35f * Mathf.Deg2Rad);
            var ladderRamp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ladderRamp.name = "LadderRamp";
            ladderRamp.transform.SetParent(h.transform);
            ladderRamp.transform.localScale = new Vector3(3f, 0.3f, ladderLength);
            
            float zOffset = (hallDepth / 2f) + (ladderLength * Mathf.Cos(35f * Mathf.Deg2Rad) / 2f);
            ladderRamp.transform.localPosition = new Vector3(0f, hallHeight / 2f, zOffset);
            ladderRamp.transform.localRotation = Quaternion.Euler(35f, 0f, 0f);
            ladderRamp.GetComponent<Renderer>().sharedMaterial = wood;
            ladderRamp.isStatic = true;

            // Add NavMeshObstacle to the house root to carve the NavMesh
            var nmoHouse = h.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            nmoHouse.carving = true;
            nmoHouse.size = new Vector3(25f, 20f, 19f);
            nmoHouse.center = new Vector3(0f, 7f, 0f);

            if (crate != null) {
                Vector3 cratePos = pos + new Vector3(15f, 0f, 13f);
                var cObj = (GameObject)PrefabUtility.InstantiatePrefab(crate, parent);
                cObj.transform.localScale = new Vector3(0.875f, 0.875f, 0.875f);
                AlignToGroundAndAddCollider(cObj, cratePos, Quaternion.Euler(-90f, Random.Range(0f, 360f), 0f), 0f);
                
                var bottomCrateRenderer = cObj.GetComponentInChildren<Renderer>();
                if (bottomCrateRenderer != null)
                {
                    Vector3 stackedPos = cObj.transform.position;
                    stackedPos.y = bottomCrateRenderer.bounds.max.y;
                    var cObj2 = (GameObject)PrefabUtility.InstantiatePrefab(crate, parent);
                    cObj2.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
                    AlignToGroundAndAddCollider(cObj2, stackedPos, Quaternion.Euler(-90f, Random.Range(0f, 360f), 0f), 0f, false);
                }
            }

            if (barrel != null) {
                Vector3 barrelPos = pos + new Vector3(-15f, 0f, -13f);
                var bObj = (GameObject)PrefabUtility.InstantiatePrefab(barrel, parent);
                bObj.transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
                AlignToGroundAndAddCollider(bObj, barrelPos, Quaternion.Euler(-90f, 0f, 0f), 0f);
            }
        }

        private void PlacePlaza(Transform parent, Vector3 pos, GameObject[] trees, GameObject columnPrefab, Material floorMat = null)
        {
            var p = new GameObject("Plaza"); p.transform.SetParent(parent); p.transform.position = pos; p.isStatic = true;
            if (floorMat != null) {
                var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "PlazaFloor";
                floor.transform.SetParent(p.transform);
                floor.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                floor.transform.localScale = new Vector3(3.2f, 1f, 3.2f);
                floor.GetComponent<Renderer>().sharedMaterial = floorMat;
                floor.isStatic = true;
            }
            
            if (columnPrefab != null) {
                var colObj = (GameObject)PrefabUtility.InstantiatePrefab(columnPrefab, p.transform);
                colObj.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                var decimatedMesh = GetSharedDecimatedColumnMesh(columnPrefab);
                if (decimatedMesh != null) {
                    var mf = colObj.GetComponentInChildren<MeshFilter>();
                    if (mf != null) mf.sharedMesh = decimatedMesh;
                }
                AlignToGroundAndAddCollider(colObj, pos + new Vector3(-14.5f, 0f, -14.5f), Quaternion.Euler(-90f, 0f, 0f), 0f);

                // Add NavMeshObstacle to column to carve the NavMesh
                var nmoCol = colObj.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                nmoCol.carving = true;
                nmoCol.size = new Vector3(3f, 12f, 3f);
                nmoCol.center = new Vector3(0f, 6f, 0f);
            }
            
            if (trees != null && trees.Length > 0) {
                int numTrees = Random.Range(1, 4); // 1 to 3 trees Max
                var sectors = new List<Vector3>() {
                    new Vector3(14.5f, 0f, 14.5f),   // Top-Right
                    new Vector3(-14.5f, 0f, 14.5f),  // Top-Left
                    new Vector3(14.5f, 0f, -14.5f)   // Bottom-Right
                };

                // Shuffle sectors to randomize spawn locations
                for (int i = 0; i < sectors.Count; i++) {
                    int tempIndex = Random.Range(i, sectors.Count);
                    Vector3 hold = sectors[i];
                    sectors[i] = sectors[tempIndex];
                    sectors[tempIndex] = hold;
                }

                for (int i = 0; i < numTrees; i++) {
                    Vector3 offset = sectors[i] + new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));
                    Vector3 spawnLoc = pos + offset;

                    // Prevent spawning trees too close to the origin to avoid spawning inside player
                    if (Vector3.Distance(new Vector3(spawnLoc.x, 0f, spawnLoc.z), Vector3.zero) < 12f) {
                        continue;
                    }

                    GameObject treePrefab = trees[Random.Range(0, trees.Length)];
                    var t = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, p.transform);
                    t.transform.localScale = Vector3.one * 8f; 

                    // Decimate tree meshes first using shared asset caching
                    var filters = t.GetComponentsInChildren<MeshFilter>();
                    foreach (var mf in filters) {
                        if (mf.sharedMesh == null) continue;
                        var decimated = GetSharedDecimatedMesh(mf.sharedMesh, 0.80f);
                        if (decimated != null) mf.sharedMesh = decimated;
                    }

                    AlignToGroundAndAddCollider(t, spawnLoc, Quaternion.Euler(-90, 0, 0), -1.8f);
                }
            }

            if (Random.value < 0.35f || pos.magnitude < 30f) { 
                Vector3 medPos = pos + new Vector3(Random.Range(-5f, 5f), 1f, Random.Range(-5f, 5f));
                medPos.y = GetTerrainHeight(medPos) + 0.5f;
                var medGo = new GameObject("MedicinePickup");
                medGo.transform.SetParent(p.transform);
                medGo.transform.position = medPos;
                var pickup = medGo.AddComponent<TheAlchemistsCrypt.Gameplay.MedicinePickup>();
                pickup.healAmount = 25f;
            }
        }

        private void FixPlayerAndWeapons()
        {
            var p = GameObject.Find("Player");
            if (p == null)
            {
                var character = GameObject.FindAnyObjectByType<InfimaGames.LowPolyShooterPack.Character>();
                if (character != null) p = character.gameObject;
            }

            if (p == null)
            {
                Debug.LogWarning("[CityGen] No Player found — spawning P_LPSP_FP_CH prefab automatically.");
                string fpCharPath = "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Prefabs/P_LPSP_FP_CH.prefab";
                GameObject fpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fpCharPath);
                if (fpPrefab != null)
                {
                    p = PrefabUtility.InstantiatePrefab(fpPrefab) as GameObject;
                    p.name = "Player";
                }
                else
                {
                    Debug.LogError("[CityGen] P_LPSP_FP_CH prefab not found — cannot spawn player!");
                    return;
                }
            }

            p.tag = "Player";
            p.transform.position = new Vector3(16f, GetTerrainHeight(new Vector3(16f, 0f, 48f)) + 1.2f, 48f);

            // Ensure AlchemicalFocus is attached to the player GameObject
            var focus = p.GetComponent<TheAlchemistsCrypt.Weapons.AlchemicalFocus>();
            if (focus == null)
            {
                focus = p.AddComponent<TheAlchemistsCrypt.Weapons.AlchemicalFocus>();
                Debug.Log("[CityGen] Attached AlchemicalFocus component to Player.");
            }

            Camera mainCam = p.GetComponentInChildren<Camera>(true);
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam == null) mainCam = GameObject.FindAnyObjectByType<Camera>();

            if (mainCam != null)
            {
                mainCam.gameObject.SetActive(true);
                mainCam.enabled = true;
                mainCam.tag = "MainCamera";
                mainCam.targetDisplay = 0;
                try { mainCam.farClipPlane = Mathf.Max(mainCam.farClipPlane, 4000f); } catch { }
            }
            else
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                camGo.transform.SetParent(p.transform);
                camGo.transform.localPosition = new Vector3(0f, 1.7f, 0f);
                var cam = camGo.AddComponent<Camera>();
                cam.targetDisplay = 0;
                cam.nearClipPlane = 0.01f;
                cam.farClipPlane = 2000f;
                cam.fieldOfView = 70f;
                camGo.AddComponent<AudioListener>();
            }

            // Find all Inventory components under the player and clean up duplicates
            var inventories = p.GetComponentsInChildren<InfimaGames.LowPolyShooterPack.Inventory>(true);
            InfimaGames.LowPolyShooterPack.Inventory mainInv = null;
            if (inventories != null && inventories.Length > 0)
            {
                mainInv = inventories[0];
                // Destroy duplicates
                for (int i = 1; i < inventories.Length; i++)
                {
                    if (inventories[i] != null && inventories[i].gameObject != null)
                    {
                        Debug.LogWarning("[CityGen] Destroying duplicate inventory: " + inventories[i].gameObject.name);
                        DestroyImmediate(inventories[i].gameObject);
                    }
                }
            }

            // If we don't have an inventory at all, spawn one
            if (mainInv == null)
            {
                string invPath = "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Prefabs/Weapons/P_LPSP_Inventory.prefab";
                GameObject invPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(invPath);
                if (invPrefab != null)
                {
                    var invGo = PrefabUtility.InstantiatePrefab(invPrefab) as GameObject;
                    invGo.name = "Inventory";
                    invGo.transform.SetParent(p.transform, false);
                    invGo.transform.localPosition = Vector3.zero;
                    mainInv = invGo.GetComponent<InfimaGames.LowPolyShooterPack.Inventory>();
                }
            }

            if (mainInv == null)
            {
                Debug.LogError("[CityGen] Failed to resolve Player inventory!");
                return;
            }

            // Clear old weapons from inventory transform
            for (int i = mainInv.transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(mainInv.transform.GetChild(i).gameObject);
            }

            // Populate the inventory with the three elemental weapon prefabs
            string[] wPrefabs = {
                "Assets/Prefabs/WEP_Sulfur.prefab",
                "Assets/Prefabs/WEP_Mercury.prefab",
                "Assets/Prefabs/WEP_Salt.prefab"
            };

            foreach (var wpPath in wPrefabs)
            {
                GameObject wpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(wpPath);
                if (wpPrefab != null)
                {
                    var wpObj = PrefabUtility.InstantiatePrefab(wpPrefab, mainInv.transform) as GameObject;
                    wpObj.name = System.IO.Path.GetFileNameWithoutExtension(wpPath);
                    Debug.Log("[CityGen] Instantiated element weapon prefab: " + wpObj.name);
                }
                else
                {
                    Debug.LogError("[CityGen] Failed to load element weapon prefab: " + wpPath);
                }
            }

            // Bake the alchemical weapon prefabs directly to avoid scene instance material leaks/serialization errors
            UpdateWeaponPrefabMaterials();
        }

        private static Material GetOrCreateMaterial(string name, Color baseColor, float smoothness, float metallic, Color emissionColor, bool useEmission)
        {
            string path = "Assets/Materials/" + name + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (!System.IO.Directory.Exists("Assets/Materials")) System.IO.Directory.CreateDirectory("Assets/Materials");
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = Shader.Find("Universal Render Pipeline/Lit");
            }
            
            mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);
            
            if (useEmission)
            {
                mat.SetColor("_EmissionColor", emissionColor);
                mat.EnableKeyword("_EMISSION");
            }
            else
            {
                mat.SetColor("_EmissionColor", Color.clear);
                mat.DisableKeyword("_EMISSION");
            }
            
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void UpdateWeaponPrefabMaterials()
        {
            // 1. Create or update the material assets
            var matCasings = GetOrCreateMaterial("M_WEP_Casings_Gold", new Color(0.8f, 0.6f, 0.2f), 0.8f, 0.9f, Color.clear, false);

            var matSulfurBody = GetOrCreateMaterial("M_WEP_Sulfur_Body", new Color(0.15f, 0.08f, 0.04f), 0.75f, 0.9f, Color.clear, false);
            var matSulfurAccent = GetOrCreateMaterial("M_WEP_Sulfur_Accent", new Color(1.0f, 0.55f, 0.05f), 0.1f, 0.0f, new Color(1.0f, 0.55f, 0.05f) * 8f, true);
            var matSulfurSecondary = GetOrCreateMaterial("M_WEP_Sulfur_Secondary", new Color(0.12f, 0.12f, 0.12f), 0.8f, 0.9f, Color.clear, false);

            var matMercuryBody = GetOrCreateMaterial("M_WEP_Mercury_Body", new Color(0.05f, 0.12f, 0.15f), 0.75f, 0.9f, Color.clear, false);
            var matMercuryAccent = GetOrCreateMaterial("M_WEP_Mercury_Accent", new Color(0.0f, 0.85f, 1.0f), 0.1f, 0.0f, new Color(0.0f, 0.85f, 1.0f) * 8f, true);
            var matMercurySecondary = GetOrCreateMaterial("M_WEP_Mercury_Secondary", new Color(0.12f, 0.12f, 0.12f), 0.8f, 0.9f, Color.clear, false);

            var matSaltBody = GetOrCreateMaterial("M_WEP_Salt_Body", new Color(0.12f, 0.06f, 0.15f), 0.75f, 0.9f, Color.clear, false);
            var matSaltAccent = GetOrCreateMaterial("M_WEP_Salt_Accent", new Color(0.85f, 0.55f, 1.0f), 0.1f, 0.0f, new Color(0.85f, 0.55f, 1.0f) * 8f, true);
            var matSaltSecondary = GetOrCreateMaterial("M_WEP_Salt_Secondary", new Color(0.12f, 0.12f, 0.12f), 0.8f, 0.9f, Color.clear, false);

            AssetDatabase.SaveAssets();

            // 2. Load and configure the weapon prefabs
            string[] wPrefabs = {
                "Assets/Prefabs/WEP_Sulfur.prefab",
                "Assets/Prefabs/WEP_Mercury.prefab",
                "Assets/Prefabs/WEP_Salt.prefab"
            };

            foreach (var path in wPrefabs)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                string wName = prefab.name.ToLower();
                Material bodyMat = matSulfurBody;
                Material accentMat = matSulfurAccent;
                Material secMat = matSulfurSecondary;

                if (wName.Contains("mercury"))
                {
                    bodyMat = matMercuryBody;
                    accentMat = matMercuryAccent;
                    secMat = matMercurySecondary;
                }
                else if (wName.Contains("salt"))
                {
                    bodyMat = matSaltBody;
                    accentMat = matSaltAccent;
                    secMat = matSaltSecondary;
                }

                // Modify the prefab's renderers directly
                foreach (var rend in prefab.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] mats = rend.sharedMaterials;
                    bool changed = false;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        if (mats[i] == null) continue;
                        string mName = mats[i].name.ToLower();

                        Material targetMat = null;
                        if (mName.Contains("body") || mName.Contains("frame") || mName.Contains("stock") || mName.Contains("metal") || mName.Contains("camo"))
                        {
                            targetMat = bodyMat;
                        }
                        else if (mName.Contains("mag") || mName.Contains("rail") || mName.Contains("stripe") || mName.Contains("runes") || mName.Contains("glow") || mName.Contains("ammo") || mName.Contains("bullet") || mName.Contains("sulfur") || mName.Contains("mercury") || mName.Contains("salt"))
                        {
                            targetMat = accentMat;
                        }
                        else if (mName.Contains("barrel") || mName.Contains("grip") || mName.Contains("trigger") || mName.Contains("scope") || mName.Contains("basic") || mName.Contains("carbon"))
                        {
                            targetMat = secMat;
                        }
                        else if (mName.Contains("casing"))
                        {
                            targetMat = matCasings;
                        }

                        if (targetMat != null && mats[i] != targetMat)
                        {
                            mats[i] = targetMat;
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        rend.sharedMaterials = mats;
                        EditorUtility.SetDirty(rend);
                    }
                }

                EditorUtility.SetDirty(prefab);
            }
            AssetDatabase.SaveAssets();
        }

        private float GetTerrainHeight(Vector3 pos) {
            var terrain = Terrain.activeTerrain;
            return (terrain != null) ? terrain.SampleHeight(pos) : 0f;
        }

        private Vector3 GetTerrainNormal(Vector3 worldPos) {
            var terrain = Terrain.activeTerrain;
            if (terrain == null || terrain.terrainData == null) return Vector3.up;
            
            Vector3 terrainLocalPos = worldPos - terrain.transform.position;
            float normX = Mathf.Clamp01(terrainLocalPos.x / terrain.terrainData.size.x);
            float normZ = Mathf.Clamp01(terrainLocalPos.z / terrain.terrainData.size.z);
            return terrain.terrainData.GetInterpolatedNormal(normX, normZ);
        }

        private float GetMeshBottomWorldY(GameObject obj)
        {
            float worldMinY = float.MaxValue;
            var filters = obj.GetComponentsInChildren<MeshFilter>(true);
            foreach (var filter in filters)
            {
                if (filter.sharedMesh == null) continue;
                Vector3[] vertices = filter.sharedMesh.vertices;
                foreach (var v in vertices)
                {
                    Vector3 worldV = filter.transform.TransformPoint(v);
                    if (worldV.y < worldMinY) worldMinY = worldV.y;
                }
            }
            if (worldMinY != float.MaxValue) return worldMinY;
            
            var renderers = obj.GetComponentsInChildren<Renderer>(true);
            float minY = float.MaxValue;
            foreach (var r in renderers)
            {
                if (r is ParticleSystemRenderer) continue;
                if (r.bounds.min.y < minY) minY = r.bounds.min.y;
            }
            return (minY != float.MaxValue) ? minY : obj.transform.position.y;
        }

        private void AlignToGroundAndAddCollider(GameObject obj, Vector3 basePos, Quaternion targetRot, float offsetAdjustment, bool alignToTerrain = true)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(obj))
            {
                PrefabUtility.UnpackPrefabInstance(obj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }

            var allRenderers = obj.GetComponentsInChildren<Renderer>(true);
            foreach (var r in allRenderers)
            {
                if (r.sharedMaterials == null) continue;
                Material[] mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    string shaderName = mats[i].shader.name;
                    if (shaderName.Contains("Standard") || shaderName.Contains("Built-in") || shaderName == "Legacy Shaders/Diffuse" || shaderName == "Diffuse")
                    {
                        var oldMat = mats[i];
                        var newMat = new Material(GetLitShader());
                        newMat.name = oldMat.name + "_URP";
                        newMat.enableInstancing = true;
                        if (oldMat.HasProperty("_Color")) newMat.SetColor("_BaseColor", oldMat.GetColor("_Color"));
                        if (oldMat.HasProperty("_MainTex") && oldMat.GetTexture("_MainTex") != null) newMat.SetTexture("_BaseMap", oldMat.GetTexture("_MainTex"));
                        if (oldMat.HasProperty("_BumpMap") && oldMat.GetTexture("_BumpMap") != null) {
                            newMat.SetTexture("_BumpMap", oldMat.GetTexture("_BumpMap"));
                            newMat.EnableKeyword("_NORMALMAP");
                        }
                        if (oldMat.HasProperty("_Metallic")) newMat.SetFloat("_Metallic", oldMat.GetFloat("_Metallic"));
                        if (oldMat.HasProperty("_Glossiness")) newMat.SetFloat("_Smoothness", oldMat.GetFloat("_Glossiness"));
                        mats[i] = newMat; changed = true;
                    }
                }
                if (changed) r.sharedMaterials = mats;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                r.receiveShadows = true;
            }

            Vector3 targetPos = basePos;
            Quaternion finalRot = targetRot;
            if (alignToTerrain) {
                targetPos.y = GetTerrainHeight(basePos);
                Vector3 normal = GetTerrainNormal(basePos);
                Quaternion normalRot = Quaternion.FromToRotation(Vector3.up, normal);
                finalRot = normalRot * targetRot;
            }
            obj.transform.position = targetPos; 
            obj.transform.rotation = finalRot;
            
            float worldMinY = GetMeshBottomWorldY(obj);
            float yOffset = (targetPos.y + offsetAdjustment) - worldMinY;
            
            bool isDynamic = obj.name.ToLower().Contains("crate") || obj.name.ToLower().Contains("barrel");
            if (isDynamic) {
                var dynamicProps = GameObject.Find("DynamicProps");
                if (dynamicProps == null)
                {
                    dynamicProps = new GameObject("DynamicProps");
                    dynamicProps.isStatic = false;
                }
                obj.transform.SetParent(dynamicProps.transform);
                obj.transform.position = new Vector3(targetPos.x, targetPos.y + yOffset + 0.05f, targetPos.z);
            } else {
                obj.transform.position = new Vector3(targetPos.x, targetPos.y + yOffset, targetPos.z);
            }

            foreach (var col in obj.GetComponentsInChildren<Collider>(true)) DestroyImmediate(col);
            
            if (isDynamic) {
                obj.isStatic = false;
                foreach (Transform t in obj.GetComponentsInChildren<Transform>(true)) {
                    t.gameObject.isStatic = false;
                }
                var rb = obj.GetComponent<Rigidbody>();
                if (rb == null) rb = obj.AddComponent<Rigidbody>();
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.mass = 10f;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.linearDamping = 0.5f;
                rb.angularDamping = 0.5f;
                
                // Aggregate local bounds in the root object's local space
                Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);
                bool hasBounds = false;
                var meshFilters = obj.GetComponentsInChildren<MeshFilter>(true);
                foreach (var mf in meshFilters) {
                    if (mf.sharedMesh == null) continue;
                    Bounds meshBounds = mf.sharedMesh.bounds;
                    
                    // Transform to root's local space
                    Matrix4x4 childToRoot = obj.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                    Vector3[] corners = GetBoundsCorners(meshBounds);
                    foreach (var corner in corners) {
                        Vector3 localCorner = childToRoot.MultiplyPoint3x4(corner);
                        if (!hasBounds) {
                            localBounds = new Bounds(localCorner, Vector3.zero);
                            hasBounds = true;
                        } else {
                            localBounds.Encapsulate(localCorner);
                        }
                    }
                }
                
                var bc = obj.AddComponent<BoxCollider>();
                if (hasBounds) {
                    bc.center = localBounds.center;
                    bc.size = localBounds.size;
                }
            } else {
                var filters = obj.GetComponentsInChildren<MeshFilter>(true);
                if (filters.Length > 0) {
                    foreach (var filterObj in filters) {
                        if (filterObj.sharedMesh == null) continue;
                        var mc = filterObj.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = filterObj.sharedMesh;
                    }
                } else obj.AddComponent<BoxCollider>();
            }
        }

        private void CreateProceduralPyramid(GameObject root, Vector3 pos, float baseSize, float height, Material mat, Color glowColor, bool isStatic = true)
        {
            var pGo = new GameObject("Pyramid"); pGo.transform.SetParent(root.transform); pGo.transform.position = pos; pGo.isStatic = isStatic;
            Mesh mesh = new Mesh(); float half = baseSize / 2f;
            Vector3 apex = new Vector3(0, height, 0); Vector3 fl = new Vector3(-half, 0, -half), fr = new Vector3(half, 0, -half), br = new Vector3(half, 0, half), bl = new Vector3(-half, 0, half);
            mesh.vertices = new Vector3[] { fl, fr, apex, fr, br, apex, br, bl, apex, bl, fl, apex, bl, br, fl, br, fr, fl };
            mesh.triangles = new int[] { 0, 2, 1, 3, 5, 4, 6, 8, 7, 9, 11, 10, 12, 14, 13, 15, 17, 16 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            pGo.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = pGo.AddComponent<MeshRenderer>();
            var pMat = new Material(mat);
            pMat.SetColor("_BaseColor", new Color(0.78f, 0.52f, 0.35f)); 
            pMat.SetColor("_EmissionColor", glowColor * 1.5f);
            if (glowColor != Color.clear) pMat.EnableKeyword("_EMISSION");
            renderer.sharedMaterial = pMat;
            
            var mc = pGo.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            mc.convex = true;
        }

        private static Vector3[] GetBoundsCorners(Bounds b)
        {
            return new Vector3[] {
                b.min,
                b.max,
                new Vector3(b.min.x, b.min.y, b.max.z),
                new Vector3(b.min.x, b.max.y, b.min.z),
                new Vector3(b.max.x, b.min.y, b.min.z),
                new Vector3(b.min.x, b.max.y, b.max.z),
                new Vector3(b.max.x, b.min.y, b.max.z),
                new Vector3(b.max.x, b.max.y, b.min.z)
            };
        }

        private void BuildProceduralObelisk(Transform parent, Vector3 pos, Material stoneMat, bool isBroken = false)
        {
            var obRoot = new GameObject(isBroken ? "BrokenObelisk" : "Obelisk");
            
            // Find or create DynamicProps as parent (never use a static parent for active Rigidbodies)
            var dynamicProps = GameObject.Find("DynamicProps");
            if (dynamicProps == null)
            {
                dynamicProps = new GameObject("DynamicProps");
                dynamicProps.isStatic = false;
            }
            obRoot.transform.SetParent(dynamicProps.transform);
            
            // Spawn slightly above terrain to prevent initial collider penetration/stuck states
            Vector3 spawnPos = pos;
            spawnPos.y = pos.y + 0.1f;
            obRoot.transform.position = spawnPos;
            obRoot.isStatic = false;

            float height = isBroken ? Random.Range(4f, 7f) : 14f;
            float baseWidth = 2.0f;
            float topWidth = isBroken ? 1.4f : 0.8f;

            // Stack segments to create a smooth taper without needing a custom mesh
            int segments = isBroken ? Random.Range(3, 5) : 8;
            float segHeight = height / segments;

            for (int i = 0; i < segments; i++)
            {
                float currentWidth = Mathf.Lerp(baseWidth, topWidth, (float)i / (segments - 1));
                var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seg.transform.SetParent(obRoot.transform);
                // Stack segments perfectly on top of each other starting at local y = 0
                seg.transform.localPosition = new Vector3(0f, (i * segHeight) + (segHeight / 2f), 0f);
                seg.transform.localScale = new Vector3(currentWidth, segHeight, currentWidth);
                seg.GetComponent<Renderer>().sharedMaterial = stoneMat;
                seg.isStatic = false;
            }

            // Add the pyramidion cap if it isn't a broken obelisk
            if (!isBroken)
            {
                CreateProceduralPyramid(obRoot, new Vector3(0f, height, 0f), topWidth, topWidth * 1.5f, stoneMat, Color.clear, false);
            }

            // Add Compound Rigidbody so it drops to the uneven terrain and has massive weight
            var rb = obRoot.AddComponent<Rigidbody>();
            rb.mass = 10000000f; // 10,000 tonnes (extremely heavy)
            rb.useGravity = true;
            // Freeze horizontal translation and all rotation to make them completely immovable by player or mummies, only allowing vertical gravity drop
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Add NavMeshObstacle to carve the NavMesh around the obelisk
            var nmoObelisk = obRoot.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            nmoObelisk.carving = true;
            nmoObelisk.size = new Vector3(3f, 16f, 3f);
            nmoObelisk.center = new Vector3(0f, 8f, 0f);
        }

        private void BuildAlchemistTomb(Transform parent, Vector3 pos, Material stoneMat)
        {
            var root = new GameObject("AlchemistTomb");
            
            // Find or create DynamicProps as parent (never use a static parent for active Rigidbodies)
            var dynamicProps = GameObject.Find("DynamicProps");
            if (dynamicProps == null)
            {
                dynamicProps = new GameObject("DynamicProps");
                dynamicProps.isStatic = false;
            }
            root.transform.SetParent(dynamicProps.transform);
            
            // Spawn slightly above terrain to prevent initial collider penetration/stuck states
            Vector3 spawnPos = pos;
            spawnPos.y = pos.y + 0.1f;
            root.transform.position = spawnPos;
            root.isStatic = false;

            float heightScale = 1.3f;

            // 1. Left and Right Massive Pylons (Tapered)
            float[] sideX = { -5.5f, 5.5f };
            foreach (float x in sideX)
            {
                var pylon = new GameObject("TombPylon");
                pylon.transform.SetParent(root.transform);
                pylon.transform.localPosition = new Vector3(x, 0f, 0f);
                pylon.isStatic = false;

                float pBase = 6f;
                float pTaper = 0.5f;
                for (int s = 0; s < 4; s++)
                {
                    float sizeX = pBase - (s * pTaper);
                    float sizeZ = pBase - (s * pTaper);
                    float segH = 4f * heightScale;

                    var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    seg.transform.SetParent(pylon.transform);
                    seg.transform.localPosition = new Vector3(0f, (s * segH) + (segH / 2f), 0f);
                    seg.transform.localScale = new Vector3(sizeX, segH, sizeZ);
                    seg.GetComponent<Renderer>().sharedMaterial = stoneMat;
                    seg.isStatic = false;
                }
            }

            // 2. Stepped Arch Structure (Approximating a curve with blocks)
            float archStartY = 16f * heightScale;
            for (int i = 0; i < 3; i++)
            {
                var archL = GameObject.CreatePrimitive(PrimitiveType.Cube);
                archL.transform.SetParent(root.transform);
                archL.transform.localPosition = new Vector3(-3.5f + (i * 0.8f), archStartY + (i * 1.5f), 0f);
                archL.transform.localScale = new Vector3(3f + i, 2f, 4f);
                archL.GetComponent<Renderer>().sharedMaterial = stoneMat;
                archL.isStatic = false;

                var archR = GameObject.CreatePrimitive(PrimitiveType.Cube);
                archR.transform.SetParent(root.transform);
                archR.transform.localPosition = new Vector3(3.5f - (i * 0.8f), archStartY + (i * 1.5f), 0f);
                archR.transform.localScale = new Vector3(3f + i, 2f, 4f);
                archR.GetComponent<Renderer>().sharedMaterial = stoneMat;
                archR.isStatic = false;
            }

            // 3. Top Massive Lintel Block
            var lintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lintel.transform.SetParent(root.transform);
            lintel.transform.localPosition = new Vector3(0f, archStartY + 5f, 0f);
            lintel.transform.localScale = new Vector3(12f, 3f, 5f);
            lintel.GetComponent<Renderer>().sharedMaterial = stoneMat;
            lintel.isStatic = false;

            // Add Compound Rigidbody so it drops to the uneven terrain and has massive weight
            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 20000000f; // 20,000 tonnes (extremely heavy)
            rb.useGravity = true;
            // Freeze horizontal translation and all rotation to make them completely immovable by player or mummies, only allowing vertical gravity drop
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Add NavMeshObstacle to carve the NavMesh around the tomb
            var nmoTomb = root.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            nmoTomb.carving = true;
            nmoTomb.size = new Vector3(20f, 25f, 20f);
            nmoTomb.center = new Vector3(0f, 12.5f, 0f);
        }

        private void CleanupOverlappingColumns(GameObject root)
        {
            var columns = new List<GameObject>();
            var houses = new List<GameObject>();

            // Find all columns and houses
            foreach (Transform t in root.transform)
            {
                if (t.name.Contains("Plaza"))
                {
                    foreach (Transform child in t)
                    {
                        if (child.name.ToLower().Contains("column") || child.name.ToLower().Contains("pillar"))
                        {
                            columns.Add(child.gameObject);
                        }
                    }
                }
                else if (t.name.Contains("House") || t.name.Contains("AlchemistTomb"))
                {
                    houses.Add(t.gameObject);
                }
            }

            // For each column, check if it intersects any house's renderer bounds
            foreach (var col in columns)
            {
                var colRenderer = col.GetComponentInChildren<Renderer>();
                if (colRenderer == null) continue;
                Bounds colBounds = colRenderer.bounds;
                colBounds.Expand(1.5f); // Expand bounds to prevent columns clipping walls

                foreach (var house in houses)
                {
                    var houseRenderers = house.GetComponentsInChildren<Renderer>();
                    foreach (var hr in houseRenderers)
                    {
                        if (hr.name.Contains("Floor") || hr.name.Contains("floor") || hr.name.Contains("TransmutationCircle")) continue;
                        if (hr.bounds.Intersects(colBounds))
                        {
                            // Overlap detected! Destroy the column
                            DestroyImmediate(col);
                            goto NextColumn;
                        }
                    }
                }
                NextColumn:;
            }
        }

        private void SpawnDesertBrokenPillars(GameObject root, Material stoneMat)
        {
            var folder = new GameObject("DesertBrokenPillars");
            folder.transform.SetParent(root.transform);

            int spawnedCount = 0;
            int attempts = 0;
            while (spawnedCount < 50 && attempts < 500)
            {
                attempts++;
                float rx = Random.Range(-480f, 480f);
                float rz = Random.Range(-480f, 480f);
                Vector3 pos = new Vector3(rx, 0f, rz);
                
                if (pos.magnitude > 150f && rz >= -75f)
                {
                    pos.y = GetTerrainHeight(pos);
                    if (pos.y < 0.5f) continue;

                    var pillar = new GameObject("DesertBrokenPillar");
                    pillar.transform.SetParent(folder.transform);
                    pillar.transform.position = pos;
                    pillar.isStatic = true;

                    float height = Random.Range(4f, 8f);
                    float baseWidth = Random.Range(1.8f, 2.5f);
                    float topWidth = baseWidth * Random.Range(0.6f, 0.85f);

                    int segments = Random.Range(3, 6);
                    float segHeight = height / segments;

                    for (int i = 0; i < segments; i++)
                    {
                        float currentWidth = Mathf.Lerp(baseWidth, topWidth, (float)i / (segments - 1));
                        var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        seg.transform.SetParent(pillar.transform);
                        seg.transform.localPosition = new Vector3(0f, (i * segHeight) + (segHeight / 2f), 0f);
                        seg.transform.localScale = new Vector3(currentWidth, segHeight, currentWidth);
                        seg.GetComponent<Renderer>().sharedMaterial = stoneMat;
                        seg.isStatic = true;
                    }

                    var col = pillar.AddComponent<BoxCollider>();
                    col.center = new Vector3(0f, height / 2f, 0f);
                    col.size = new Vector3(baseWidth, height, baseWidth);

                    Vector3 normal = GetTerrainNormal(pos);
                    Quaternion normalRot = Quaternion.FromToRotation(Vector3.up, normal);
                    Quaternion randomTilt = Quaternion.Euler(Random.Range(-20f, 20f), Random.Range(0f, 360f), Random.Range(-20f, 20f));
                    pillar.transform.rotation = normalRot * randomTilt;

                    spawnedCount++;
                }
            }
        }

        private void SpawnPalmTreeOasis(GameObject root, GameObject[] treePrefabs)
        {
            if (treePrefabs == null || treePrefabs.Length == 0 || treePrefabs[0] == null) return;

            var folder = new GameObject("PalmTreeOasis");
            folder.transform.SetParent(root.transform);

            int spawnedCount = 0;
            int attempts = 0;
            while (spawnedCount < 80 && attempts < 800)
            {
                attempts++;
                float rx = Random.Range(-450f, 450f);
                float rz = Random.Range(-95f, -60f);
                Vector3 pos = new Vector3(rx, 0f, rz);
                pos.y = GetTerrainHeight(pos);

                if (pos.y < 0.2f || pos.y > 6.0f) continue;

                var prefab = treePrefabs[Random.Range(0, treePrefabs.Length)];
                if (prefab == null) continue;

                var palm = (GameObject)PrefabUtility.InstantiatePrefab(prefab, folder.transform);
                palm.transform.position = pos;
                palm.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                palm.transform.localScale = Vector3.one * Random.Range(0.8f, 1.4f);
                palm.isStatic = true;

                var cc = palm.GetComponent<CapsuleCollider>();
                if (cc == null)
                {
                    cc = palm.AddComponent<CapsuleCollider>();
                    cc.center = new Vector3(0f, 4.0f, 0f);
                    cc.height = 8.0f;
                    cc.radius = 0.6f;
                }

                spawnedCount++;
            }
        }
    }
}
