using UnityEngine;
using UnityEditor;
using Unity.AI.Navigation;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
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
        public static void ShowWindow() => GetWindow<StaticEgyptianCityGenerator>("Egyptian City V5.0");

        private int seed = 999;
        private int gridSize = 8; 
        private string rootName = "EgyptianCity_V5_Final";

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("V5.0 ELITE MOBILE: stack floors (1-3), wooden trim divider belts, back-side glowing windows, real window PointLights, majestic columns, sandy craters, zero overlap grid.", MessageType.Info);
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

            // Create target folder if missing
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
                        // Prefer mixamo.com clip if available
                        if (clip.name.Contains("mixamo.com")) {
                            sourceClip = clip;
                            break;
                        }
                    }
                }
                if (sourceClip == null) {
                    Debug.LogError($"[AnimGenerator] No AnimationClip found inside FBX: {fbxPath}");
                    return null;
                }

                string destPath = "Assets/Mummy_Assets/" + animName + "_loop.anim";
                AnimationClip destClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(destPath);
                if (destClip == null) {
                    destClip = new AnimationClip();
                    EditorUtility.CopySerialized(sourceClip, destClip);
                    destClip.name = animName + "_loop";
                    AssetDatabase.CreateAsset(destClip, destPath);
                } else {
                    EditorUtility.CopySerialized(sourceClip, destClip);
                    destClip.name = animName + "_loop";
                }

                // Force loop settings on native serialized asset
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

            // Build/Update States
            var idleState = GetOrAddState(rootStateMachine, "Idle", idleClip);
            var walkState = GetOrAddState(rootStateMachine, "Walk", walkClip);
            var attackState = GetOrAddState(rootStateMachine, "Attack", attackClip);
            var dieState = GetOrAddState(rootStateMachine, "Die", deathClip);

            // Transitions (Fixes "dragged" movement)
            if (idleState.transitions.Length == 0) {
                var t = idleState.AddTransition(walkState);
                t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.1f, "Speed");
                t.hasExitTime = false;
                t.duration = 0.25f;
            }
            if (walkState.transitions.Length == 0) {
                var t = walkState.AddTransition(idleState);
                t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.1f, "Speed");
                t.hasExitTime = false;
                t.duration = 0.25f;
            }
            
            // Global attack transition or from any state
            bool attackTransExists = false;
            foreach(var t in rootStateMachine.anyStateTransitions) if(t.destinationState == attackState) attackTransExists = true;
            if(!attackTransExists) {
                var t = rootStateMachine.AddAnyStateTransition(attackState);
                t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "Attack");
                t.duration = 0.1f;
            }

            // Cleanup scene instances
            var allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in allObjects) {
                if (go != null && (go.name.ToLower().StartsWith("mummy") || go.name.ToLower().Contains("_test"))) {
                    DestroyImmediate(go);
                }
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
                    importer.animationType = ModelImporterAnimationType.Human;
                    dirty = true;
                }
                if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel) {
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    dirty = true;
                }
                
                // Configure clip loops permanently inside FBX importer settings
                if (path.Contains("Walking") || path.Contains("attack") || path.Contains("idle")) {
                    var clips = importer.clipAnimations;
                    if (clips == null || clips.Length == 0) {
                        clips = importer.defaultClipAnimations;
                    }
                    if (clips != null && clips.Length > 0) {
                        bool clipDirty = false;
                        foreach (var c in clips) {
                            if (!c.loopTime) {
                                c.loopTime = true;
                                clipDirty = true;
                            }
                        }
                        if (clipDirty) {
                            importer.clipAnimations = clips;
                            dirty = true;
                        }
                    }
                }

                if (dirty) {
                    importer.SaveAndReimport();
                }
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
                    lowerName.Contains("plaza") || lowerName.Contains("house") || lowerName.Contains("pyramid")) 
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
            
            string sandAlbedoPath = "Assets/EgyptianAssets/desert_sand_albedo.png";
            Material wallMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            var wallTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EgyptianAssets/egyptian_wall_albedo.png");
            if (wallTex != null) wallMat.SetTexture("_BaseMap", wallTex);
            else wallMat.color = new Color(0.92f, 0.85f, 0.7f);
            wallMat.mainTextureScale = new Vector2(4f, 4f);
            Material woodMat = CreateLit(new Color(0.25f, 0.15f, 0.08f), 1f);
            Material holeMat = CreateLit(new Color(0.05f, 0.03f, 0.01f), 1f);

            Material floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            Texture2D sandAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>(sandAlbedoPath);
            if (sandAlbedo != null) floorMat.SetTexture("_BaseMap", sandAlbedo);
            else floorMat.color = new Color(0.9f, 0.8f, 0.6f);
            floorMat.mainTextureScale = new Vector2(gridSize * 4f, gridSize * 4f);

            Material litWindowMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            litWindowMat.color = new Color(1f, 0.85f, 0.4f);
            litWindowMat.SetColor("_EmissionColor", new Color(1f, 0.75f, 0.3f) * 6f);
            litWindowMat.EnableKeyword("_EMISSION");
            Material darkWindowMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            darkWindowMat.color = new Color(0.1f, 0.08f, 0.05f);

            SetupEnvironment();

            // Create a gorgeous Unity Terrain for the Desert dunes
            string layerPath = "Assets/EgyptianAssets/DesertSandLayer.terrainlayer";
            TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
            if (layer == null) {
                layer = new TerrainLayer();
                layer.diffuseTexture = sandAlbedo != null ? sandAlbedo : Texture2D.whiteTexture;
                layer.tileSize = new Vector2(10f, 10f);
                AssetDatabase.CreateAsset(layer, layerPath);
            }

            TerrainData terrainData = new TerrainData();
            terrainData.heightmapResolution = 513;
            terrainData.size = new Vector3(1000f, 15f, 1000f);

            int resolution = terrainData.heightmapResolution;
            float[,] heights = new float[resolution, resolution];
            for (int i = 0; i < resolution; i++) {
                for (int j = 0; j < resolution; j++) {
                    float tx = (float)i / (resolution - 1);
                    float ty = (float)j / (resolution - 1);
                    // Beautiful layered sine waves for desert dunes
                    float dune1 = Mathf.Sin(tx * 12f + ty * 8f) * 0.35f;
                    float dune2 = Mathf.Cos(tx * 6f - ty * 14f) * 0.15f;
                    float ripple = Mathf.Sin(tx * 120f + ty * 80f) * 0.005f;

                    // Add smooth rolling sand mounds/bumps in the town and plaza area
                    float townBumps = Mathf.Sin(tx * 35f) * Mathf.Cos(ty * 35f) * 0.045f;

                    // Smooth center flattening:
                    // Player spawn at very center (within 5% radius) is flat.
                    // Town area (within 25% radius) has gentle rolling bumps/hills.
                    // Beyond the town, dunes rise majestically.
                    float distFromCenter = Mathf.Sqrt((tx - 0.5f) * (tx - 0.5f) + (ty - 0.5f) * (ty - 0.5f));
                    float spawnFlatten = Mathf.SmoothStep(0f, 1f, distFromCenter * 20f);
                    float townFactor = Mathf.SmoothStep(0.12f, 1f, distFromCenter * 4.5f);
                    
                    float baseDune = (dune1 + dune2 + ripple + 0.5f) * (0.1f + 0.9f * townFactor);
                    heights[i, j] = (baseDune + townBumps) * spawnFlatten;

                    // SEA & COASTLINE: Flatten the south quarter (ty < 0.35) for sea level
                    // Real-world Z roughly corresponds to ty.
                    if (ty < 0.35f) {
                        float seaLevel = 0.0f;
                        float shoreFactor = Mathf.SmoothStep(0.28f, 0.35f, ty);
                        heights[i, j] = Mathf.Lerp(seaLevel, heights[i, j], shoreFactor);
                    }
                }
            }
            terrainData.SetHeights(0, 0, heights);
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

                    // HOUSE SPAWN GUARD: Skip if in the sea/beach zone
                    if (posZ < -80f) continue;

                    Vector3 pos = new Vector3(posX, 0, posZ);
                    pos.x += Random.Range(-2f, 2f); pos.z += Random.Range(-2f, 2f);
                    pos.y = GetTerrainHeight(pos);

                    if (pos.magnitude > 25f) {
                        if (Random.value < 0.75f) {
                            BuildHouse(root.transform, pos, wallMat, woodMat, litWindowMat, darkWindowMat, crate, barrel);
                        } else {
                            PlacePlaza(root.transform, pos, trees, columnPrefab, floorMat, holeMat);
                        }
                    } else PlacePlaza(root.transform, pos, trees, columnPrefab, floorMat, holeMat);

                    // Spawn Enemies (Fixed visibility and health)
                    if (enemyPrefab != null && Random.value < 0.15f) {
                        var e = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab, root.transform);
                        e.transform.position = pos + Vector3.up * 0.5f;
                        
                        // Force assign the new animator controller
                        var anim = e.GetComponent<Animator>();
                        if (anim == null) anim = e.GetComponentInChildren<Animator>();
                        if (anim != null) {
                            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Mummy_Assets/MummyTestController.controller");
                            if (controller != null) anim.runtimeAnimatorController = controller;
                        }

                        // Set Health to 10 (10x lower than player 100)
                        var zai = e.GetComponent<TheAlchemistsCrypt.AI.ZombieAI>();
                        if (zai == null) zai = e.AddComponent<TheAlchemistsCrypt.AI.ZombieAI>();
                        zai.maxHealth = 10f;
                        zai.currentHealth = 10f;
                    }
                }
            }

            var surface = root.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.RenderMeshes;
            surface.overrideVoxelSize = true; surface.voxelSize = 0.2f; 
            surface.BuildNavMesh();

            // Relocate south pyramids to the north/east for the new coastline
            CreateProceduralPyramid(root, new Vector3(-220f, 0f, 220f), 150f, 95f, wallMat, new Color(1f, 0.85f, 0.4f));
            CreateProceduralPyramid(root, new Vector3(220f, 0f, 250f), 160f, 100f, wallMat, new Color(1f, 0.5f, 0.2f)); // Moved from Z -220
            CreateProceduralPyramid(root, new Vector3(220f, 0f, 220f), 140f, 85f, wallMat, new Color(0.9f, 0.8f, 1f));
            CreateProceduralPyramid(root, new Vector3(-250f, 0f, 200f), 170f, 110f, wallMat, new Color(1f, 0.7f, 0.3f)); // Moved from Z -220

            CreateSeaAndCoastline(root);

            FixPlayerAndWeapons();
            StaticBatchingUtility.Combine(root);
            SetupMummyAnimations();

            // Mark Scene Dirty to fix persistence issue
            var activeScene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            
            Debug.Log("Polished Egyptian City V5.1 Regenerated and Saved!");
        }

        private void CreateSeaAndCoastline(GameObject root)
        {
            // 1. Open Sea Zone (Z < -100)
            GameObject sea = GameObject.CreatePrimitive(PrimitiveType.Quad);
            sea.name = "SeaZone";
            sea.transform.SetParent(root.transform);
            // Giant horizontal quad at sea level
            sea.transform.position = new Vector3(0f, -0.3f, -300f);
            sea.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            sea.transform.localScale = new Vector3(1000f, 400f, 1f);

            var seaMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            seaMat.color = new Color(0.04f, 0.24f, 0.42f); // Deep blue
            seaMat.SetColor("_EmissionColor", new Color(0.1f, 0.47f, 0.68f) * 2.5f); // Glowing teal
            seaMat.EnableKeyword("_EMISSION");
            sea.GetComponent<Renderer>().sharedMaterial = seaMat;
            sea.isStatic = true;
            DestroyImmediate(sea.GetComponent<Collider>());

            // 2. Beach Strip (Z -80 to -100)
            GameObject beach = GameObject.CreatePrimitive(PrimitiveType.Quad);
            beach.name = "BeachZone";
            beach.transform.SetParent(root.transform);
            beach.transform.position = new Vector3(0f, 0.02f, -90f);
            beach.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            beach.transform.localScale = new Vector3(1000f, 20f, 1f);

            var beachMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            beachMat.color = new Color(0.94f, 0.82f, 0.5f); // Light sand
            beach.GetComponent<Renderer>().sharedMaterial = beachMat;
            beach.isStatic = true;
            DestroyImmediate(beach.GetComponent<Collider>());
        }

        private void SetupEnvironment()
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.92f, 0.88f, 0.7f); 
            RenderSettings.fogDensity = 0.0022f;
            RenderSettings.ambientLight = new Color(0.45f, 0.42f, 0.38f);
            
            var sun = GameObject.Find("Directional Light");
            if (sun) {
                var l = sun.GetComponent<Light>();
                if (l) { l.color = new Color(1, 0.95f, 0.85f); l.intensity = 1.3f; }
            }
        }

        private void BuildHouse(Transform parent, Vector3 pos, Material wall, Material wood, Material litWindowMat, Material darkWindowMat, GameObject crate, GameObject barrel)
        {
            var h = new GameObject("House"); h.transform.SetParent(parent); h.transform.position = pos; h.isStatic = true;
            int floors = (Random.value < 0.2f) ? 1 : (Random.value < 0.7f ? 2 : 3);
            
            // Build stacked stepped floors: wider lower floor, smaller upper floors
            for (int f = 0; f < floors; f++) {
                float width = 20f - f * 4f;
                float depth = 20f - f * 4f;
                float windowY = (f * 12f) + 6f;

                var floorBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floorBody.name = "Floor_" + f;
                floorBody.transform.SetParent(h.transform);
                floorBody.transform.localPosition = new Vector3(0, (f * 12f) + 6f, 0);
                floorBody.transform.localScale = new Vector3(width, 12f, depth);
                floorBody.GetComponent<Renderer>().sharedMaterial = wall;
                floorBody.isStatic = true;

                // Windows on all four sides (South, North, West, East)
                Vector3[] localPositions = new Vector3[]
                {
                    new Vector3(0f, windowY, -(depth / 2f) - 0.15f), // South
                    new Vector3(0f, windowY, (depth / 2f) + 0.15f),  // North
                    new Vector3(-(width / 2f) - 0.15f, windowY, 0f), // West
                    new Vector3((width / 2f) + 0.15f, windowY, 0f)   // East
                };

                float[] rotations = new float[] { 180f, 0f, 90f, -90f };

                for (int side = 0; side < 4; side++) {
                    Material windowMat = (Random.value < 0.85f) ? litWindowMat : darkWindowMat;
                    var win = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    win.name = "Window_Floor_" + f + "_Side_" + side;
                    win.transform.SetParent(h.transform);
                    win.transform.localScale = new Vector3(3.6f, 2.6f, 0.3f);
                    win.GetComponent<Renderer>().sharedMaterial = windowMat;
                    DestroyImmediate(win.GetComponent<Collider>());
                    win.transform.localPosition = localPositions[side];
                    win.transform.localRotation = Quaternion.Euler(0, rotations[side], 0);
                    win.isStatic = true;

                    if (windowMat == litWindowMat) {
                        var lightGo = new GameObject("WindowLight");
                        lightGo.transform.SetParent(win.transform);
                        lightGo.transform.localPosition = new Vector3(0f, 0f, -0.8f); // Offset outward slightly
                        lightGo.transform.localRotation = Quaternion.identity;
                        var l = lightGo.AddComponent<Light>();
                        l.type = LightType.Point;
                        l.color = new Color(1.0f, 0.72f, 0.28f); // Warm atmospheric amber lamp glow
                        l.range = 18f;
                        l.intensity = 15.0f;
                        l.shadows = LightShadows.None;
                    }
                }
            }

            // Door on Floor 0 (back wall)
            var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "Door";
            door.transform.SetParent(h.transform);
            door.transform.localPosition = new Vector3(0, 4, 10.1f);
            door.transform.localScale = new Vector3(6, 8, 0.2f);
            door.GetComponent<Renderer>().sharedMaterial = wood;
            door.isStatic = true;

            // Spawn breakable crates and barrels around the house!
            if (crate != null) {
                Vector3 cratePos = pos + new Vector3(13f, 0f, 11f);
                var cObj = (GameObject)PrefabUtility.InstantiatePrefab(crate, parent);
                cObj.name = "HouseCrate_1";
                cObj.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f); // Calibrated scale
                AlignToGroundAndAddCollider(cObj, cratePos, Quaternion.Euler(-90f, Random.Range(0f, 360f), 0f), 0f);
                cObj.isStatic = false;

                // Stack a second crate on top!
                Vector3 stackedPos = cObj.transform.position + Vector3.up * 0.70f; // Sits perfectly on top of aligned first crate
                var cObj2 = (GameObject)PrefabUtility.InstantiatePrefab(crate, parent);
                cObj2.name = "HouseCrate_2";
                cObj2.transform.localScale = new Vector3(0.30f, 0.30f, 0.30f); // Calibrated scale
                AlignToGroundAndAddCollider(cObj2, stackedPos, Quaternion.Euler(-90f, Random.Range(0f, 360f), 0f), 0f, false);
                cObj2.isStatic = false;

                // 35% chance to spawn Medicine on the ground nearby!
                if (Random.value < 0.35f) {
                    Vector3 medPos = pos + new Vector3(15f, 0f, 9f);
                    medPos.y = GetTerrainHeight(medPos) + 0.6f;
                    SpawnMedicine(parent, medPos);
                }
            }

            if (barrel != null) {
                Vector3 barrelPos = pos + new Vector3(-13f, 0f, -11f);
                var bObj = (GameObject)PrefabUtility.InstantiatePrefab(barrel, parent);
                bObj.name = "HouseBarrel_1";
                bObj.transform.localScale = new Vector3(0.14f, 0.14f, 0.14f); // Calibrated scale
                AlignToGroundAndAddCollider(bObj, barrelPos, Quaternion.Euler(-90f, 0f, 0f), 0f);
                bObj.isStatic = false;

                Vector3 barrelPos2 = pos + new Vector3(-11f, 0f, -13f);
                var bObj2 = (GameObject)PrefabUtility.InstantiatePrefab(barrel, parent);
                bObj2.name = "HouseBarrel_2";
                bObj2.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f); // Calibrated scale
                AlignToGroundAndAddCollider(bObj2, barrelPos2, Quaternion.Euler(-90f, 0f, 0f), 0f);
                bObj2.isStatic = false;

                // 35% chance to spawn Medicine on the ground nearby!
                if (Random.value < 0.35f) {
                    Vector3 medPos = pos + new Vector3(-15f, 0f, -9f);
                    medPos.y = GetTerrainHeight(medPos) + 0.6f;
                    SpawnMedicine(parent, medPos);
                }
            }
        }

        private void SpawnMedicine(Transform parent, Vector3 spawnPos)
        {
            var med = new GameObject("MedicinePickup", typeof(TheAlchemistsCrypt.Gameplay.MedicinePickup));
            med.transform.SetParent(parent, false);
            med.transform.position = spawnPos;
            med.isStatic = false; // Floating pickup rotates, not static!
        }

        private void PlacePlaza(Transform parent, Vector3 pos, GameObject[] trees, GameObject columnPrefab, Material sandMat, Material craterMat)
        {
            var p = new GameObject("Plaza"); p.transform.SetParent(parent); p.transform.position = pos; p.isStatic = true;

            // Spawns ancient columns in the plaza for ruins detailing
            if (columnPrefab != null) {
                var colObj = (GameObject)PrefabUtility.InstantiatePrefab(columnPrefab, p.transform);
                colObj.name = "RuinedColumn";
                colObj.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                AlignToGroundAndAddCollider(colObj, pos + new Vector3(-8f, 0f, -8f), Quaternion.Euler(-90f, 0f, 0f), 0f);
                colObj.isStatic = true;
            }

            // Also spawn trees if available (offset to avoid overlapping)
            if (trees != null && trees.Length > 0) {
                var t = (GameObject)PrefabUtility.InstantiatePrefab(trees[Random.Range(0, trees.Length)], p.transform);
                t.transform.localScale = Vector3.one * 8f; AlignToGroundAndAddCollider(t, pos + new Vector3(8f, 0f, 8f), Quaternion.Euler(-90, 0, 0), -1.8f);
                t.isStatic = true;
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

            // ── 3. Ensure PlayerImmersiveBody is attached ──
            if (p.GetComponent<TheAlchemistsCrypt.Player.PlayerImmersiveBody>() == null)
                p.AddComponent<TheAlchemistsCrypt.Player.PlayerImmersiveBody>();

            // ── 4. Guarantee a Camera exists — find in children (incl inactive), then globally,
            //       and as a last resort create one. This prevents "No cameras rendering" forever. ──
            Camera mainCam = p.GetComponentInChildren<Camera>(true);
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam == null) mainCam = GameObject.FindAnyObjectByType<Camera>();

            if (mainCam != null)
            {
                mainCam.gameObject.SetActive(true);
                mainCam.enabled = true;
                mainCam.tag = "MainCamera";
                mainCam.targetDisplay = 0;
                Debug.Log($"[CityGen] Camera enabled: {mainCam.gameObject.name}");
            }
            else
            {
                Debug.LogWarning("[CityGen] No camera found — creating emergency MainCamera.");
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

            // ── 5. Alchemical weapon coloring ──
            var inv2 = p.GetComponentInChildren<InfimaGames.LowPolyShooterPack.Inventory>();
            if (inv2 == null) return;
            Color[] colors = { new Color(1, 0.4f, 0), Color.white, new Color(0, 0.7f, 1) };
            int idx = 0;
            foreach (Transform t in inv2.transform) {
                if (t.name.ToLower().Contains("pistol") || t.name.ToLower().Contains("assault")) {
                    if (idx >= 3) { t.gameObject.SetActive(false); continue; }
                }
                foreach (var r in t.GetComponentsInChildren<Renderer>()) {
                    var sharedMats = r.sharedMaterials;
                    foreach (var m in sharedMats) {
                        if (m == null) continue;
                        m.SetColor("_EmissionColor", colors[idx % 3] * 4f); m.EnableKeyword("_EMISSION"); m.color = colors[idx % 3] * 0.5f;
                    }
                }
                idx++;
            }
        }

        private Material CreateLit(Color c, float tile, string normalPath = null) {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.color = c; m.mainTextureScale = new Vector2(tile, tile);
            if (!string.IsNullOrEmpty(normalPath)) {
                var n = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EgyptianAssets/" + normalPath);
                if (n != null) { m.SetTexture("_BumpMap", n); m.EnableKeyword("_NORMALMAP"); }
            }
            return m;
        }

        private float GetTerrainHeight(Vector3 pos)
        {
            var terrain = Terrain.activeTerrain;
            if (terrain != null && terrain.terrainData != null)
            {
                return terrain.transform.position.y + terrain.SampleHeight(pos);
            }
            return 0f;
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
                    if (worldV.y < worldMinY)
                    {
                        worldMinY = worldV.y;
                    }
                }
            }
            if (worldMinY != float.MaxValue)
            {
                return worldMinY;
            }
            
            // Fallback to renderer bounds if no meshes found
            var renderers = obj.GetComponentsInChildren<Renderer>(true);
            float minY = float.MaxValue;
            foreach (var r in renderers)
            {
                if (r is ParticleSystemRenderer) continue;
                if (r.bounds.min.y < minY) minY = r.bounds.min.y;
            }
            if (minY != float.MaxValue)
            {
                return minY;
            }
            return obj.transform.position.y;
        }

        private void AlignToGroundAndAddCollider(GameObject obj, Vector3 basePos, Quaternion targetRot, float offsetAdjustment, bool alignToTerrain = true)
        {
            // DYNAMIC URP SHADER CONVERTER
            // Convert standard shaders on imported meshes to URP to avoid pink-material artifacts
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
                        if (oldMat.HasProperty("_BumpMap") && oldMat.GetTexture("_BumpMap") != null)
                        {
                            newMat.SetTexture("_BumpMap", oldMat.GetTexture("_BumpMap"));
                            newMat.EnableKeyword("_NORMALMAP");
                        }
                        if (oldMat.HasProperty("_Metallic")) newMat.SetFloat("_Metallic", oldMat.GetFloat("_Metallic"));
                        if (oldMat.HasProperty("_Glossiness")) newMat.SetFloat("_Smoothness", oldMat.GetFloat("_Glossiness"));
                        
                        mats[i] = newMat;
                        changed = true;
                    }
                }
                if (changed) r.sharedMaterials = mats;
            }

            Vector3 targetPos = basePos;
            if (alignToTerrain)
            {
                targetPos.y = GetTerrainHeight(basePos);
            }
            obj.transform.position = targetPos; 
            obj.transform.rotation = targetRot;
            
            // Precise grounding using world-space mesh vertices
            float worldMinY = GetMeshBottomWorldY(obj);
            float yOffset = (targetPos.y + offsetAdjustment) - worldMinY;
            obj.transform.position = new Vector3(targetPos.x, targetPos.y + yOffset, targetPos.z);

            foreach (var col in obj.GetComponentsInChildren<Collider>(true)) DestroyImmediate(col);
            
            bool isDynamicProp = obj.name.ToLower().Contains("crate") || obj.name.ToLower().Contains("barrel");
            
            if (isDynamicProp)
            {
                // Dynamic physics objects
                var boxCol = obj.AddComponent<BoxCollider>();
                // Adjust box collider bounds slightly
                var rb = obj.AddComponent<Rigidbody>();
                rb.mass = 10f;
                // Lift them slightly so physics drops them naturally on runtime
                obj.transform.position += Vector3.up * 0.5f;
            }
            else
            {
                // Static environment objects
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
            var filter = pGo.AddComponent<MeshFilter>(); filter.sharedMesh = mesh;
            var renderer = pGo.AddComponent<MeshRenderer>(); var pMat = new Material(mat); pMat.color = new Color(0.85f, 0.75f, 0.6f);
            pMat.SetColor("_EmissionColor", glowColor * 1.5f); pMat.EnableKeyword("_EMISSION"); renderer.sharedMaterial = pMat;
            pGo.AddComponent<MeshCollider>().sharedMesh = mesh;
            var lightGo = new GameObject("PyramidBeacon"); lightGo.transform.SetParent(pGo.transform); lightGo.transform.localPosition = new Vector3(0f, height + 2f, 0f);
            var l = lightGo.AddComponent<Light>(); l.type = LightType.Point; l.color = glowColor; l.range = 400f; l.intensity = 20f;
        }
    }
}
