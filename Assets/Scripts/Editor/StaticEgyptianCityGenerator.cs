using UnityEngine;
using UnityEditor;
using Unity.AI.Navigation;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

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
                "Assets/Mummy_Assets/mummy_death.fbx"
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

            System.Func<string, string, bool, AnimationClip> getOrCreateClip = (fbxPath, animName, loopTime) => {
                var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
                AnimationClip sourceClip = null;
                foreach (var a in assets) {
                    if (a is AnimationClip) {
                        var clip = (AnimationClip)a;
                        if (clip.name.Contains("__preview__")) continue;
                        sourceClip = clip;
                        if (clip.name.Contains("mixamo.com")) { sourceClip = clip; break; }
                    }
                }
                if (sourceClip == null) return null;

                string destPath = "Assets/Mummy_Assets/" + animName + "_loop.anim";
                AnimationClip destClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(destPath);
                if (destClip == null) {
                    destClip = new AnimationClip();
                    EditorUtility.CopySerialized(sourceClip, destClip);
                    destClip.name = animName + "_loop";
                    AssetDatabase.CreateAsset(destClip, destPath);
                } else {
                    EditorUtility.CopySerialized(sourceClip, destClip);
                }

                var settings = AnimationUtility.GetAnimationClipSettings(destClip);
                settings.loopTime = loopTime;
                AnimationUtility.SetAnimationClipSettings(destClip, settings);
                EditorUtility.SetDirty(destClip);
                AssetDatabase.SaveAssets();
                return destClip;
            };

            var idleClip = getOrCreateClip("Assets/Mummy_Assets/mummy_idle.fbx", "mummy_idle", true);
            var walkClip = getOrCreateClip("Assets/Mummy_Assets/new_Walking.fbx", "new_Walking", true);
            var attackClip = getOrCreateClip("Assets/Mummy_Assets/mummy_attack.fbx", "mummy_attack", true);
            var deathClip = getOrCreateClip("Assets/Mummy_Assets/mummy_death.fbx", "mummy_death", false);

            var idleState = GetOrAddState(rootStateMachine, "Idle", idleClip);
            var walkState = GetOrAddState(rootStateMachine, "Walk", walkClip);
            var attackState = GetOrAddState(rootStateMachine, "Attack", attackClip);
            var dieState = GetOrAddState(rootStateMachine, "Die", deathClip);

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

            Debug.Log("Mummy Animator Setup with Transitions & Looping Attack!");
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
                bool dirty = false;
                if (importer.animationType != ModelImporterAnimationType.Human) {
                    importer.animationType = ModelImporterAnimationType.Human; dirty = true;
                }
                if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel) {
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel; dirty = true;
                }
                if (dirty) importer.SaveAndReimport();
            }
        }

        private void Purge()
        {
            var all = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in all) {
                if (go == null) continue; 
                string lowerName = go.name.ToLower();
                if (lowerName.Contains("egyptiancity") || lowerName.Contains("desertfloor") || lowerName.Contains("floorground") ||
                    lowerName.Contains("groundplane") || lowerName.Contains("desertterrain") ||
                    lowerName.Contains("player_copy") || lowerName.Contains("mobilehud") || lowerName.Contains("p_lpsp_ui_canvas") || 
                    lowerName.StartsWith("mummy") || lowerName.Contains("windowlight") || lowerName.Contains("crater") ||
                    lowerName.Contains("plaza") || lowerName.Contains("house") || lowerName.Contains("pyramid") ||
                    lowerName.Contains("seazone") || lowerName.Contains("beachzone") || lowerName.Contains("coastlinebarrier") ||
                    lowerName.Contains("globalvolume")) 
                {
                    DestroyImmediate(go);
                }
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
            
            // ── AESTHETIC PALETTE ──
            Material wallMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            wallMat.SetColor("_BaseColor", new Color(0.78f, 0.52f, 0.35f)); // Terracotta Ochre
            wallMat.SetFloat("_Smoothness", 0.05f);
            
            Material woodMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            woodMat.SetColor("_BaseColor", new Color(0.25f, 0.15f, 0.08f));
            
            // Reflective polished sandstone floor — creates the mirror-like desert floor look
            Material floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            floorMat.SetColor("_BaseColor", new Color(0.92f, 0.82f, 0.65f)); // Lighter/Brighter
            floorMat.SetFloat("_Metallic", 0.1f);
            floorMat.SetFloat("_Smoothness", 0.88f);   // ← High reflection
            floorMat.SetColor("_EmissionColor", new Color(0.96f, 0.84f, 0.6f) * 0.05f);
            floorMat.EnableKeyword("_EMISSION");

            Material litWindowMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            litWindowMat.SetColor("_BaseColor", new Color(1f, 0.75f, 0.3f));
            litWindowMat.SetColor("_EmissionColor", new Color(1f, 0.65f, 0.2f) * 3.5f);
            litWindowMat.EnableKeyword("_EMISSION");
            
            Material darkWindowMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            darkWindowMat.SetColor("_BaseColor", new Color(0.1f, 0.1f, 0.15f));

            SetupEnvironment();

            TerrainData terrainData = new TerrainData();
            terrainData.heightmapResolution = 513;
            terrainData.size = new Vector3(1000f, 15f, 1000f);

            int resolution = terrainData.heightmapResolution;
            float[,] heights = new float[resolution, resolution];
            for (int i = 0; i < resolution; i++) {
                for (int j = 0; j < resolution; j++) {
                    float tx = (float)i / (resolution - 1);
                    float ty = (float)j / (resolution - 1);
                    float dune1 = Mathf.Sin(tx * 12f + ty * 8f) * 0.35f;
                    float ripple = Mathf.Sin(tx * 120f + ty * 80f) * 0.005f;
                    float townBumps = Mathf.Sin(tx * 35f) * Mathf.Cos(ty * 35f) * 0.045f;

                    float distFromCenter = Mathf.Sqrt((tx - 0.5f) * (tx - 0.5f) + (ty - 0.5f) * (ty - 0.5f));
                    float spawnFlatten = Mathf.SmoothStep(0f, 1f, distFromCenter * 20f);
                    float townFactor = Mathf.SmoothStep(0.12f, 1f, distFromCenter * 4.5f);
                    
                    float baseDune = (dune1 + ripple + 0.5f) * (0.1f + 0.9f * townFactor);
                    heights[i, j] = (baseDune + townBumps) * spawnFlatten;

                    // SEA & COASTLINE: Flatten the SOUTH edge (tx = normalized Z, 0=south, 1=north)
                    // Terrain at (-500,-0.05,-500), size 1000m. world_Z = -500 + tx*1000.
                    // City grid stops at world Z=-40 → tx=0.46.
                    // Sea starts at world Z=-118 (beach) → tx=0.382.
                    // Flatten everything south of tx=0.42 (world Z=-80) fully flat by tx=0.32.
                    // NOTE: tx is the FIRST index (i) = Z direction. ty is X. Don't mix them!
                    if (tx < 0.42f) {
                        float shoreFactor = Mathf.SmoothStep(0.32f, 0.42f, tx);
                        heights[i, j] = Mathf.Lerp(0.001f, heights[i, j], shoreFactor);
                    }
                }
            }
            terrainData.SetHeights(0, 0, heights);

            string layerPath = "Assets/EgyptianAssets/DesertSandLayer_V2.terrainlayer";
            TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
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
                layer.specular = new Color(0.1f, 0.08f, 0.05f, 0f);
                layer.smoothness = 0.45f; // Add some base smoothness to the desert
                AssetDatabase.CreateAsset(layer, layerPath);
            }
            // Always reassign tile size even on existing layers
            layer.tileSize = new Vector2(40f, 40f);
            layer.smoothness = 0.45f;
            EditorUtility.SetDirty(layer);
            terrainData.terrainLayers = new TerrainLayer[] { layer };

            GameObject terrainGo = Terrain.CreateTerrainGameObject(terrainData);
            terrainGo.name = "DesertTerrain";
            terrainGo.transform.SetParent(root.transform);
            terrainGo.transform.position = new Vector3(-500f, -0.05f, -500f);
            terrainGo.isStatic = true;

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

                    if (pos.magnitude > 25f) {
                        if (Random.value < 0.75f) {
                            BuildHouse(root.transform, pos, wallMat, woodMat, litWindowMat, darkWindowMat, crate, barrel, floorMat);
                        } else {
                            PlacePlaza(root.transform, pos, trees, columnPrefab, floorMat);
                        }
                    } else PlacePlaza(root.transform, pos, trees, columnPrefab, floorMat);

                    if (enemyPrefab != null && Random.value < 0.15f) {
                        var e = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab, root.transform);
                        e.transform.position = pos + Vector3.up * 0.5f;
                        var zai = e.GetComponent<TheAlchemistsCrypt.AI.ZombieAI>();
                        if (zai == null) zai = e.AddComponent<TheAlchemistsCrypt.AI.ZombieAI>();
                        zai.maxHealth = 10f; zai.currentHealth = 10f;
                    }
                }
            }

            var surface = root.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.RenderMeshes;
            surface.BuildNavMesh();

            CreateProceduralPyramid(root, new Vector3(-450f, 0f, 400f), 150f, 95f, wallMat, new Color(1f, 0.85f, 0.4f));
            CreateProceduralPyramid(root, new Vector3(450f, 0f, 400f), 160f, 100f, wallMat, new Color(1f, 0.5f, 0.2f)); 
            CreateProceduralPyramid(root, new Vector3(450f, 0f, 120f), 140f, 85f, wallMat, new Color(0.9f, 0.8f, 1f));
            CreateProceduralPyramid(root, new Vector3(-450f, 0f, 120f), 170f, 110f, wallMat, new Color(1f, 0.7f, 0.3f)); 

            CreateSeaAndCoastline(root);
            FixPlayerAndWeapons();
            SetupMummyAnimations();

            var activeScene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("Polished Egyptian City V5.2 Aesthetic Overhaul Regenerated!");
        }

        private void CreateSeaAndCoastline(GameObject root)
        {
            // ── COORDINATE GUIDE ──
            // Terrain: 1000m × 1000m, position at (-500, -0.05, -500). Player spawns at world (0,0,0).
            // City buildings stop at world Z = -40f (posZ < -40 is skipped in grid).
            // Sea should appear SOUTH of Z = -100, well clear of any building.
            // Terrain is flattened south of ty=0.38 → world Z = -500 + 0.38*1000 = -120f.
            // So all sea quads at Z < -120 will sit above flattened terrain (Y≈-0.035f).
            //
            // Barrier sits at Z = -95f so player cannot enter the sea area.

            // ── 1. DEEP OCEAN — fills the far south horizon ──
            GameObject sea = GameObject.CreatePrimitive(PrimitiveType.Quad);
            sea.name = "SeaZone";
            sea.transform.SetParent(root.transform);
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
            sea.GetComponent<Renderer>().sharedMaterial = seaMat;
            sea.isStatic = true;
            DestroyImmediate(sea.GetComponent<Collider>());

            // ── 2. SHALLOWS — tropical teal ──
            GameObject shallows = GameObject.CreatePrimitive(PrimitiveType.Quad);
            shallows.name = "SeaZone_Shallow";
            shallows.transform.SetParent(root.transform);
            shallows.transform.position = new Vector3(0f, 0.17f, -200f);
            shallows.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            shallows.transform.localScale = new Vector3(3000f, 200f, 1f);

            var shallowMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            shallowMat.SetColor("_BaseColor", new Color(0.08f, 0.42f, 0.65f));
            shallowMat.SetFloat("_Metallic", 0.2f);
            shallowMat.SetFloat("_Smoothness", 0.80f);
            shallowMat.SetColor("_EmissionColor", new Color(0.10f, 0.50f, 0.75f) * 1.4f);
            shallowMat.EnableKeyword("_EMISSION");
            shallows.GetComponent<Renderer>().sharedMaterial = shallowMat;
            shallows.isStatic = true;
            DestroyImmediate(shallows.GetComponent<Collider>());

            // ── 3. SURF FOAM — bright white-teal at shoreline ──
            GameObject surf = GameObject.CreatePrimitive(PrimitiveType.Quad);
            surf.name = "SeaZone_Surf";
            surf.transform.SetParent(root.transform);
            surf.transform.position = new Vector3(0f, 0.19f, -140f);
            surf.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            surf.transform.localScale = new Vector3(3000f, 50f, 1f);

            var surfMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            surfMat.SetColor("_BaseColor", new Color(0.55f, 0.82f, 0.92f));
            surfMat.SetFloat("_Smoothness", 0.5f);
            surfMat.SetColor("_EmissionColor", new Color(0.6f, 0.88f, 1.0f) * 1.2f);
            surfMat.EnableKeyword("_EMISSION");
            surf.GetComponent<Renderer>().sharedMaterial = surfMat;
            surf.isStatic = true;
            DestroyImmediate(surf.GetComponent<Collider>());

            // ── 4. BEACH STRIP — warm sand, connects city to ocean ──
            GameObject beach = GameObject.CreatePrimitive(PrimitiveType.Quad);
            beach.name = "BeachZone";
            beach.transform.SetParent(root.transform);
            beach.transform.position = new Vector3(0f, 0.21f, -110f);
            beach.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            beach.transform.localScale = new Vector3(3000f, 40f, 1f);

            var beachMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            beachMat.SetColor("_BaseColor", new Color(0.96f, 0.89f, 0.64f)); // Warm cream sand
            beachMat.SetFloat("_Smoothness", 0.05f);
            beach.GetComponent<Renderer>().sharedMaterial = beachMat;
            beach.isStatic = true;
            DestroyImmediate(beach.GetComponent<Collider>());

            // ── 5. INVISIBLE BARRIER — stop player walking into sea ──
            GameObject barrier = new GameObject("CoastlineBarrier");
            barrier.transform.SetParent(root.transform);
            barrier.transform.position = new Vector3(0f, 5f, -95f); // Just south of beach, before shallows
            var bc = barrier.AddComponent<BoxCollider>();
            bc.size   = new Vector3(3000f, 20f, 2f);
            bc.center = Vector3.zero;
            barrier.isStatic = true;

            Debug.Log("[CityGen] Sea updated south of Z=-95. Barrier at Z=-95.");
        }

        private void SetupEnvironment()
        {
            // NEW: Clear daytime Egyptian sky — bright azure blue
            var skyMat = new Material(Shader.Find("Skybox/Procedural"));
            skyMat.SetFloat("_SunSize", 0.05f);
            skyMat.SetFloat("_SunSizeConvergence", 10f);
            skyMat.SetFloat("_AtmosphereThickness", 1.1f);
            skyMat.SetColor("_SkyTint", new Color(0.38f, 0.62f, 0.92f));       // ← Clear azure blue
            skyMat.SetColor("_GroundColor", new Color(0.72f, 0.58f, 0.38f));   // Sandy dunes at horizon
            skyMat.SetFloat("_Exposure", 1.6f);                                  // Bright midday sun
            RenderSettings.skybox = skyMat;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor    = new Color(0.50f, 0.75f, 1.00f) * 0.65f;   // Bright sky bounce
            RenderSettings.ambientEquatorColor = new Color(0.95f, 0.82f, 0.62f) * 0.90f;  // Warm wall bounce
            RenderSettings.ambientGroundColor  = new Color(0.60f, 0.50f, 0.35f) * 0.40f;  // Sandy ground bounce

            // NEW: Linear fog with long clear range (matches reference image)
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.92f, 0.84f, 0.68f);  // Warm sandy haze at horizon
            RenderSettings.fogStartDistance = 120f;   // Clear up close
            RenderSettings.fogEndDistance  = 400f;   // Fully fogged at 400m
            
            var sun = GameObject.Find("Directional Light")?.GetComponent<Light>();
            if (sun != null) {
                sun.color = new Color(1f, 0.98f, 0.90f);   // Slightly warmer white
                sun.intensity = 1.8f;                        // Brighter midday sun
                sun.shadows = LightShadows.Soft;
                sun.shadowResolution = LightShadowResolution.VeryHigh;
                sun.transform.rotation = Quaternion.Euler(55f, -25f, 0f);  // Higher sun angle = shorter shadows
            }

            SetupPostProcessing();
            
            // Quality settings for shadows
            QualitySettings.shadowDistance = 120f;
            QualitySettings.shadowCascades = 2;
            QualitySettings.shadowProjection = ShadowProjection.CloseFit;

            DynamicGI.UpdateEnvironment();
        }

        private void SetupPostProcessing()
        {
            var volGo = new GameObject("GlobalVolume");
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true; vol.priority = 10;
            
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "VisualOverhaulProfile";
            
            profile.Add<Bloom>().intensity.Override(0.35f);
            var colorAdj = profile.Add<ColorAdjustments>();
            colorAdj.contrast.Override(8f);           // Less crushed blacks
            colorAdj.saturation.Override(18f);        // More vibrant colors
            colorAdj.colorFilter.Override(new Color(1f, 0.97f, 0.92f)); // Slight warm filter
            profile.Add<Tonemapping>().mode.Override(TonemappingMode.ACES);
            profile.Add<Vignette>().intensity.Override(0.25f);

            vol.sharedProfile = profile;
        }

        private void BuildHouse(Transform parent, Vector3 pos, Material wall, Material wood, Material litWindowMat, Material darkWindowMat, GameObject crate, GameObject barrel, Material floorMat = null)
        {
            var h = new GameObject("House"); h.transform.SetParent(parent); h.transform.position = pos; h.isStatic = true;
            int floors = (Random.value < 0.2f) ? 1 : (Random.value < 0.7f ? 2 : 3);
            
            for (int f = 0; f < floors; f++) {
                float width = 20f - f * 4f; float depth = 20f - f * 4f; float windowY = (f * 12f) + 6f;
                var floorBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floorBody.name = "Floor_" + f; floorBody.transform.SetParent(h.transform);
                floorBody.transform.localPosition = new Vector3(0, (f * 12f) + 6f, 0);
                floorBody.transform.localScale = new Vector3(width, 12f, depth);
                floorBody.GetComponent<Renderer>().sharedMaterial = wall; floorBody.isStatic = true;

                Vector3[] localPositions = {
                    new Vector3(0f, windowY, -(depth / 2f) - 0.15f),
                    new Vector3(0f, windowY, (depth / 2f) + 0.15f),
                    new Vector3(-(width / 2f) - 0.15f, windowY, 0f),
                    new Vector3((width / 2f) + 0.15f, windowY, 0f)
                };
                float[] rotations = { 180f, 0f, 90f, -90f };

                for (int side = 0; side < 4; side++) {
                    Material windowMat = (Random.value < 0.85f) ? litWindowMat : darkWindowMat;
                    var win = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    win.transform.SetParent(h.transform); win.transform.localScale = new Vector3(3.6f, 2.6f, 0.3f);
                    win.GetComponent<Renderer>().sharedMaterial = windowMat;
                    DestroyImmediate(win.GetComponent<Collider>());
                    win.transform.localPosition = localPositions[side];
                    win.transform.localRotation = Quaternion.Euler(0, rotations[side], 0);
                    win.isStatic = true;

                    if (windowMat == litWindowMat) {
                        var lightGo = new GameObject("WindowLight"); lightGo.transform.SetParent(win.transform);
                        lightGo.transform.localPosition = new Vector3(0f, 0f, -0.8f);
                        var l = lightGo.AddComponent<Light>(); l.type = LightType.Point;
                        l.color = new Color(1.0f, 0.72f, 0.28f); l.range = 18f; l.intensity = 15.0f;
                    }
                }
            }

            if (crate != null) {
                Vector3 cratePos = pos + new Vector3(13f, 0f, 11f);
                var cObj = (GameObject)PrefabUtility.InstantiatePrefab(crate, parent);
                cObj.transform.localScale = new Vector3(0.875f, 0.875f, 0.875f);
                AlignToGroundAndAddCollider(cObj, cratePos, Quaternion.Euler(-90f, Random.Range(0f, 360f), 0f), 0f);
                
                Vector3 stackedPos = cObj.transform.position + Vector3.up * 0.70f;
                var cObj2 = (GameObject)PrefabUtility.InstantiatePrefab(crate, parent);
                cObj2.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
                AlignToGroundAndAddCollider(cObj2, stackedPos, Quaternion.Euler(-90f, Random.Range(0f, 360f), 0f), 0f);
            }

            if (barrel != null) {
                Vector3 barrelPos = pos + new Vector3(-13f, 0f, -11f);
                var bObj = (GameObject)PrefabUtility.InstantiatePrefab(barrel, parent);
                bObj.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
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
                // Position slightly above terrain to avoid Z-fighting, but low enough to look like ground
                floor.transform.localPosition = new Vector3(0, 0.02f, 0); 
                floor.transform.localScale = new Vector3(3.2f, 1f, 3.2f); // 32m x 32m
                floor.GetComponent<Renderer>().sharedMaterial = floorMat;
                floor.isStatic = true;
            }

            if (columnPrefab != null) {
                var colObj = (GameObject)PrefabUtility.InstantiatePrefab(columnPrefab, p.transform);
                colObj.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                AlignToGroundAndAddCollider(colObj, pos + new Vector3(-8f, 0f, -8f), Quaternion.Euler(-90f, 0f, 0f), 0f);
            }
            if (trees != null && trees.Length > 0) {
                var t = (GameObject)PrefabUtility.InstantiatePrefab(trees[Random.Range(0, trees.Length)], p.transform);
                t.transform.localScale = Vector3.one * 8f; AlignToGroundAndAddCollider(t, pos + new Vector3(8f, 0f, 8f), Quaternion.Euler(-90, 0, 0), -1.8f);
            }
        }

        private void FixPlayerAndWeapons()
        {
            // ── 1. Find the Player object by name OR by the Infima Character component ──
            var p = GameObject.Find("Player");
            if (p == null)
            {
                var character = GameObject.FindAnyObjectByType<InfimaGames.LowPolyShooterPack.Character>();
                if (character != null) p = character.gameObject;
            }

            // ── 2. If no player exists at all, SPAWN the FPS character prefab ──
            if (p == null)
            {
                Debug.LogWarning("[CityGen] No Player found — spawning P_LPSP_FP_CH prefab automatically.");
                string fpCharPath = "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Prefabs/P_LPSP_FP_CH.prefab";
                GameObject fpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fpCharPath);
                if (fpPrefab != null)
                {
                    p = PrefabUtility.InstantiatePrefab(fpPrefab) as GameObject;
                    p.name = "Player";

                    // Also spawn the weapon inventory as a child
                    string invPath = "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Prefabs/Weapons/P_LPSP_Inventory.prefab";
                    GameObject invPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(invPath);
                    if (invPrefab != null)
                    {
                        var inv = PrefabUtility.InstantiatePrefab(invPrefab) as GameObject;
                        inv.name = "Inventory";
                        inv.transform.SetParent(p.transform, false);
                        inv.transform.localPosition = Vector3.zero;
                    }

                    // Spawn the HUD canvas (independent, not child of player)
                    string uiPath = "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Prefabs/Interface/P_LPSP_UI_Canvas.prefab";
                    GameObject uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(uiPath);
                    if (uiPrefab != null)
                    {
                        var ui = PrefabUtility.InstantiatePrefab(uiPrefab) as GameObject;
                        ui.name = "P_LPSP_UI_Canvas";
                    }

                    Debug.Log("[CityGen] FPS Character spawned from prefab.");
                }
                else
                {
                    Debug.LogError("[CityGen] P_LPSP_FP_CH prefab not found — cannot spawn player!");
                    return;
                }
            }

            p.tag = "Player";
            p.transform.position = new Vector3(0f, GetTerrainHeight(Vector3.zero) + 1.2f, 0f);

            // ── 3. Guarantee a Camera exists ──
            Camera mainCam = p.GetComponentInChildren<Camera>(true);
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam == null) mainCam = GameObject.FindAnyObjectByType<Camera>();

            if (mainCam != null)
            {
                mainCam.gameObject.SetActive(true);
                mainCam.enabled = true;
                mainCam.tag = "MainCamera";
                mainCam.targetDisplay = 0;
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

            // ── 4. Alchemical weapon coloring (Aesthetic Upgrade) ──
            var inv2 = p.GetComponentInChildren<InfimaGames.LowPolyShooterPack.Inventory>();
            if (inv2 == null) return;
            
            foreach (Transform weapon in inv2.transform) {
                foreach (var rend in weapon.GetComponentsInChildren<Renderer>()) {
                    Material[] mats = rend.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++) {
                        if (mats[i] == null) continue;
                        var mat = new Material(mats[i]);
                        string mName = mat.name.ToLower();
                        
                        if (mName.Contains("body") || mName.Contains("frame") || mName.Contains("stock")) {
                            mat.SetColor("_BaseColor", new Color(1.0f, 0.6f, 0.0f)); // Bright Alchemist Orange
                            mat.SetFloat("_Smoothness", 0.85f);
                            mat.SetFloat("_Metallic", 0.3f);
                            mat.SetColor("_EmissionColor", new Color(1.0f, 0.4f, 0.0f) * 0.4f);
                            mat.EnableKeyword("_EMISSION");
                        } else if (mName.Contains("barrel") || mName.Contains("grip") || mName.Contains("trigger")) {
                            mat.SetColor("_BaseColor", new Color(0.15f, 0.15f, 0.15f)); // Dark contrast
                            mat.SetFloat("_Smoothness", 0.9f);
                        } else {
                            mat.SetColor("_BaseColor", new Color(1.0f, 0.8f, 0.2f)); // Golden accents
                            mat.SetColor("_EmissionColor", new Color(1.0f, 0.7f, 0.1f) * 1.5f);
                            mat.EnableKeyword("_EMISSION");
                        }
                        mats[i] = mat;
                    }
                    rend.sharedMaterials = mats;
                }
            }
        }

        private float GetTerrainHeight(Vector3 pos) {
            var terrain = Terrain.activeTerrain;
            return (terrain != null) ? terrain.SampleHeight(pos) : 0f;
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
            // DYNAMIC URP SHADER CONVERTER
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
                        var newMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        newMat.name = oldMat.name + "_URP";
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
                
                // ── Enable shadows on all child renderers ──
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                r.receiveShadows = true;
            }

            Vector3 targetPos = basePos;
            if (alignToTerrain) targetPos.y = GetTerrainHeight(basePos);
            obj.transform.position = targetPos; 
            obj.transform.rotation = targetRot;
            
            float worldMinY = GetMeshBottomWorldY(obj);
            float yOffset = (targetPos.y + offsetAdjustment) - worldMinY;
            obj.transform.position = new Vector3(targetPos.x, targetPos.y + yOffset, targetPos.z);

            foreach (var col in obj.GetComponentsInChildren<Collider>(true)) DestroyImmediate(col);
            bool isDynamic = obj.name.ToLower().Contains("crate") || obj.name.ToLower().Contains("barrel");
            if (isDynamic) {
                var boxCol = obj.AddComponent<BoxCollider>();
                var rb = obj.AddComponent<Rigidbody>(); rb.mass = 10f;
                obj.transform.position += Vector3.up * 0.5f;
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

        private void CreateProceduralPyramid(GameObject root, Vector3 pos, float baseSize, float height, Material mat, Color glowColor)
        {
            var pGo = new GameObject("Pyramid"); pGo.transform.SetParent(root.transform); pGo.transform.position = pos; pGo.isStatic = true;
            Mesh mesh = new Mesh(); float half = baseSize / 2f;
            Vector3 apex = new Vector3(0, height, 0); Vector3 fl = new Vector3(-half, 0, -half), fr = new Vector3(half, 0, -half), br = new Vector3(half, 0, half), bl = new Vector3(-half, 0, half);
            mesh.vertices = new Vector3[] { fl, fr, apex, fr, br, apex, br, bl, apex, bl, fl, apex, bl, br, fl, br, fr, fl };
            mesh.triangles = new int[] { 0, 2, 1, 3, 5, 4, 6, 8, 7, 9, 11, 10, 12, 14, 13, 15, 17, 16 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            pGo.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = pGo.AddComponent<MeshRenderer>();
            var pMat = new Material(mat); pMat.SetColor("_EmissionColor", glowColor * 1.5f); pMat.EnableKeyword("_EMISSION"); renderer.sharedMaterial = pMat;
            pGo.AddComponent<MeshCollider>().sharedMesh = mesh;
        }
    }
}
