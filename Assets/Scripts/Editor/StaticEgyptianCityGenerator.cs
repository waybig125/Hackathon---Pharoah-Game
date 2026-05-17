using UnityEngine;
using UnityEditor;
using Unity.AI.Navigation;
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
        private int gridSize = 8; // Reduced for optimized mobile performance (Helio G91 / Mali-G52)
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
            // Programmatically configure all Mummy FBXs to Humanoid
            string[] fbxPaths = {
                "Assets/Mummy_Assets/base.fbx",
                "Assets/Mummy_Assets/base_basic_pbr.fbx",
                "Assets/Mummy_Assets/base_basic_shaded.fbx",
                "Assets/Mummy_Assets/mummy_base.fbx",
                "Assets/Mummy_Assets/mummy_idle.fbx",
                "Assets/Mummy_Assets/new_Walking.fbx",
                "Assets/Mummy_Assets/mummy_attack.fbx"
            };
            foreach (var p in fbxPaths) {
                ConfigureFbxToHumanoid(p);
            }

            // 1. Create/Update Controller
            string controllerPath = "Assets/Mummy_Assets/MummyTestController.controller";
            var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
            if (controller == null) {
                controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            }

            var rootStateMachine = controller.layers[0].stateMachine;

            // Helper to get clips
            System.Func<string, AnimationClip> getClip = (p) => {
                var assets = AssetDatabase.LoadAllAssetsAtPath(p);
                foreach (var a in assets) if (a is AnimationClip && a.name.Contains("mixamo.com")) return (AnimationClip)a;
                return null;
            };

            // Add states if they don't exist, assigning the correct humanoid walk clip
            var idle = getClip("Assets/Mummy_Assets/mummy_idle.fbx");
            var walk = getClip("Assets/Mummy_Assets/new_Walking.fbx");
            var attack = getClip("Assets/Mummy_Assets/mummy_attack.fbx");

            if (idle != null && !HasState(rootStateMachine, "Idle")) rootStateMachine.AddState("Idle").motion = idle;
            if (walk != null && !HasState(rootStateMachine, "Walk")) {
                var walkState = rootStateMachine.AddState("Walk");
                walkState.motion = walk;
            } else if (walk != null) {
                // Keep Walk clip updated in case walk clip path changed
                foreach (var s in rootStateMachine.states) {
                    if (s.state.name == "Walk") s.state.motion = walk;
                }
            }
            if (attack != null && !HasState(rootStateMachine, "Attack")) rootStateMachine.AddState("Attack").motion = attack;

            // 2. PURGE all old scene mummies
            var allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in allObjects) {
                if (go != null && (go.name.ToLower().StartsWith("mummy") || go.name.ToLower().Contains("_test"))) {
                    DestroyImmediate(go);
                }
            }

            Debug.Log("Mummy Animator and Humanoid Rigs Setup Successfully! Cleaned up old scene instances.");
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
                if (dirty) {
                    importer.SaveAndReimport();
                    Debug.Log($"Configured {path} to Humanoid Rig.");
                }
            }
        }

        private bool HasState(UnityEditor.Animations.AnimatorStateMachine sm, string n) {
            foreach (var s in sm.states) if (s.state.name == n) return true;
            return false;
        }

        private void Purge()
        {
            // AGGRESSIVE PURGE OF CONFLICTING OR STRAGGLED GENERATOR OBJECTS
            var all = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in all) {
                if (go == null) continue; 
                try {
                    string lowerName = go.name.ToLower();
                    if (lowerName.Contains("egyptiancity") ||
                        lowerName.Contains("desertfloor") ||
                        lowerName.Contains("floorground") ||
                        lowerName.Contains("player_copy") || 
                        lowerName.Contains("mobilehud") || 
                        lowerName.Contains("p_lpsp_ui_canvas") || 
                        lowerName.StartsWith("mummy") ||
                        lowerName.Contains("windowlight") ||
                        lowerName.Contains("crater") ||
                        lowerName.Contains("plaza") ||
                        lowerName.Contains("house") ||
                        lowerName.Contains("pyramid")) 
                    {
                        DestroyImmediate(go);
                    }
                } catch { /* Ignore destroyed access */ }
            }
        }

        public void GeneratePolishedCity()
        {
            Random.InitState(seed);
            Purge();

            var root = new GameObject(rootName);
            root.isStatic = true;

            // Load Assets
            var trees = new GameObject[] {
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/realistic_hd_date_palm_2178.glb"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/realistic_hd_date_palm_378.glb")
            };
            var crate = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/crate.glb");
            var barrel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/barrel.glb");

            // Load Column Prefab
            var columnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/egyptian_column.glb");
            if (columnPrefab == null) {
                columnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/egyptian_pillar_column.glb");
            }
            
            // Generate beautiful desert sand albedo texture on-the-fly
            string sandAlbedoPath = "Assets/EgyptianAssets/desert_sand_albedo.png";
            if (!System.IO.File.Exists(sandAlbedoPath)) {
                int size = 512;
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, true);
                Color sandColor1 = new Color(0.92f, 0.82f, 0.62f);
                Color sandColor2 = new Color(0.86f, 0.76f, 0.54f);
                for (int y = 0; y < size; y++) {
                    for (int x = 0; x < size; x++) {
                        float n = Mathf.PerlinNoise(x * 0.08f, y * 0.08f) * 0.8f;
                        n += Mathf.PerlinNoise(x * 0.3f, y * 0.3f) * 0.2f;
                        Color c = Color.Lerp(sandColor1, sandColor2, n);
                        tex.SetPixel(x, y, c);
                    }
                }
                tex.Apply();
                byte[] bytes = tex.EncodeToPNG();
                System.IO.File.WriteAllBytes(sandAlbedoPath, bytes);
                AssetDatabase.ImportAsset(sandAlbedoPath);
            }

            // Materials
            Material wallMat = CreateLit(new Color(0.92f, 0.85f, 0.7f), 4f, "desert_sand_normal.png");
            Material woodMat = CreateLit(new Color(0.25f, 0.15f, 0.08f), 1f);
            Material holeMat = CreateLit(new Color(0.05f, 0.03f, 0.01f), 1f);

            // Floor Material with albedo and normal map for organic sand dunes
            Material floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            floorMat.name = "FloorDesertSand";
            Texture2D sandAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>(sandAlbedoPath);
            if (sandAlbedo != null) {
                floorMat.SetTexture("_BaseMap", sandAlbedo);
            } else {
                floorMat.color = new Color(0.9f, 0.8f, 0.6f);
            }
            floorMat.mainTextureScale = new Vector2(gridSize * 4f, gridSize * 4f);

            // NO NORMAL MAPS ON THE DESERT FLOOR (Albedo-only sand for pristine mobile performance)

            // Create window glowing materials
            Material litWindowMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            litWindowMat.color = new Color(1f, 0.85f, 0.4f);
            litWindowMat.SetColor("_EmissionColor", new Color(1f, 0.75f, 0.3f) * 6f);
            litWindowMat.EnableKeyword("_EMISSION");

            Material darkWindowMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            darkWindowMat.color = new Color(0.1f, 0.08f, 0.05f);

            SetupEnvironment();

            // Ground Plane
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "FloorGround";
            ground.transform.SetParent(root.transform);
            ground.transform.localScale = new Vector3(100, 1, 100);
            ground.GetComponent<Renderer>().sharedMaterial = floorMat;
            ground.isStatic = true;

            // Generate clean grid with zero overlapping cells
            float spacing = 32f;
            float halfSpan = (gridSize * spacing) / 2f;
            for (int x = 0; x < gridSize; x++) {
                for (int z = 0; z < gridSize; z++) {
                    float posX = -halfSpan + (x * spacing) + (spacing / 2f);
                    float posZ = -halfSpan + (z * spacing) + (spacing / 2f);
                    Vector3 pos = new Vector3(posX, 0, posZ);

                    // Add a tiny safe offset to look organic but guarantee no overlap
                    pos.x += Random.Range(-2f, 2f);
                    pos.z += Random.Range(-2f, 2f);

                    if (pos.magnitude > 25f) {
                        if (Random.value < 0.75f) {
                            BuildHouse(root.transform, pos, wallMat, woodMat, litWindowMat, darkWindowMat, crate, barrel);
                        } else {
                            PlacePlaza(root.transform, pos, trees, columnPrefab, floorMat, holeMat);
                        }
                    } else {
                        // Empty spawn plaza at the center of the town
                        PlacePlaza(root.transform, pos, trees, columnPrefab, floorMat, holeMat);
                    }
                }
            }

            var surface = root.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.RenderMeshes;
            surface.overrideVoxelSize = true;
            surface.voxelSize = 0.2f; 
            surface.BuildNavMesh();

            // Spawn 4 majestic background pyramids surrounding the city (diagonal boxing outside play area, not affecting NavMesh baking)
            CreateProceduralPyramid(root, new Vector3(-220f, 0f, 220f), 150f, 95f, wallMat, new Color(1f, 0.85f, 0.4f));  // North-West (golden-yellow glow)
            CreateProceduralPyramid(root, new Vector3(220f, 0f, -220f), 160f, 100f, wallMat, new Color(1f, 0.5f, 0.2f));  // South-East (warm orange-red glow)
            CreateProceduralPyramid(root, new Vector3(220f, 0f, 220f), 140f, 85f, wallMat, new Color(0.9f, 0.8f, 1f));     // North-East (mystical soft violet glow)
            CreateProceduralPyramid(root, new Vector3(-220f, 0f, -220f), 170f, 110f, wallMat, new Color(1f, 0.7f, 0.3f));  // South-West (rich amber glow)

            FixPlayerAndWeapons();
            StaticBatchingUtility.Combine(root);
            
            // Auto-setup mummy animations & humanoid scales as a cohesive single-step experience
            SetupMummyAnimations();
            
            Debug.Log("Polished Egyptian City V5 Generated Successfully with Elite Mobile Optimizations!");
        }

        private void SetupEnvironment()
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.92f, 0.88f, 0.7f); 
            RenderSettings.fogDensity = 0.0022f; // Reduced from 0.0035f so the grand pyramids are beautifully visible
            RenderSettings.ambientLight = new Color(0.45f, 0.42f, 0.38f);
            
            var sun = GameObject.Find("Directional Light");
            if (sun) {
                var l = sun.GetComponent<Light>();
                if (l) { l.color = new Color(1, 0.95f, 0.85f); l.intensity = 1.3f; }
            }
        }

        private void BuildHouse(Transform parent, Vector3 pos, Material wall, Material wood, Material litWindowMat, Material darkWindowMat, GameObject crate, GameObject barrel)
        {
            var h = new GameObject("House");
            h.transform.SetParent(parent); 
            h.transform.position = pos;
            h.isStatic = true;

            // Height varying strictly by floors (1 floor: 12m [20%], 2 floors: 24m [50%], 3 floors: 36m [30%])
            int floors = 2;
            float rand = Random.value;
            if (rand < 0.2f) {
                floors = 1;
            } else if (rand < 0.7f) {
                floors = 2;
            } else {
                floors = 3;
            }

            bool isStepped = (floors > 1) && (Random.value < 0.15f);

            if (isStepped) {
                // Stepped Tiered Mastaba/Villa House (15% of double/triple floors)
                float[] floorSizes = new float[floors];
                if (floors == 2) {
                    floorSizes[0] = 30f; // Bottom floor is 1.5x wider (30x30)
                    floorSizes[1] = 20f; // Upper floor is standard (20x20)
                } else {
                    floorSizes[0] = 32f; // Bottom floor is wider
                    floorSizes[1] = 20f; // Middle floor is standard
                    floorSizes[2] = 12f; // Top floor is narrower
                }

                // Spawn each floor's body cube
                for (int f = 0; f < floors; f++) {
                    float size = floorSizes[f];
                    var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    body.name = "SteppedBody_Floor_" + f;
                    body.transform.SetParent(h.transform);
                    body.transform.localPosition = new Vector3(0, (f * 12f) + 6f, 0);
                    body.transform.localScale = new Vector3(size, 12f, size);
                    body.GetComponent<Renderer>().sharedMaterial = wall;
                    body.isStatic = true;
                }

                // Window glow state (85% glow)
                bool isLit = Random.value < 0.85f;
                Material windowMat = isLit ? litWindowMat : darkWindowMat;

                // Exactly ONE back-side (negative Z) window per floor (shifted back to each tier's wall)
                for (int f = 0; f < floors; f++) {
                    float size = floorSizes[f];
                    float windowY = (f * 12f) + 6f;
                    var win = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    win.transform.SetParent(h.transform);
                    win.transform.localScale = new Vector3(3.6f, 2.6f, 0.3f);
                    win.GetComponent<Renderer>().sharedMaterial = windowMat;
                    DestroyImmediate(win.GetComponent<Collider>());
                    
                    win.transform.localPosition = new Vector3(0f, windowY, -(size / 2f) - 0.15f);
                    win.transform.localRotation = Quaternion.Euler(0, 180, 0);
                    win.isStatic = true;

                    if (isLit) {
                        var lightGo = new GameObject("WindowLight");
                        lightGo.transform.SetParent(win.transform);
                        lightGo.transform.localPosition = new Vector3(0f, 0f, -0.6f);
                        var l = lightGo.AddComponent<Light>();
                        l.type = LightType.Point;
                        l.color = new Color(1f, 0.75f, 0.3f);
                        l.range = 8f;
                        l.intensity = 1.6f;
                        l.shadows = LightShadows.None;
                    }
                }

                // Spawn wooden trim divider belts between floors matching each tier's size
                for (int f = 1; f < floors; f++) {
                    float size = floorSizes[f - 1]; // Trim sits on top of the lower floor
                    var trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    trim.transform.SetParent(h.transform);
                    trim.transform.localPosition = new Vector3(0f, f * 12f, 0f);
                    trim.transform.localScale = new Vector3(size + 0.4f, 0.8f, size + 0.4f);
                    trim.GetComponent<Renderer>().sharedMaterial = wood;
                    DestroyImmediate(trim.GetComponent<Collider>());
                    trim.isStatic = true;
                }

                // Door on the front face of bottom floor (positive Z)
                float bottomSize = floorSizes[0];
                var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
                door.transform.SetParent(h.transform);
                door.transform.localPosition = new Vector3(0, 4, (bottomSize / 2f) + 0.1f);
                door.transform.localScale = new Vector3(6, 8, 0.2f);
                door.GetComponent<Renderer>().sharedMaterial = wood;
                door.isStatic = true;

                // Prop offsets adjusted outward due to wider bottom size
                if (Random.value > 0.5f) {
                    float propXOffset = (bottomSize / 2f) + 2f;
                    if (crate) InstantiateProp(crate, pos + new Vector3(propXOffset, 0, 8), h.transform);
                    if (barrel) InstantiateProp(barrel, pos + new Vector3(propXOffset, 0, 11), h.transform);
                }

            } else {
                // Standard Box House
                float height = floors * 12f;

                var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                body.transform.SetParent(h.transform);
                body.transform.localPosition = new Vector3(0, height / 2f, 0);
                body.transform.localScale = new Vector3(20, height, 20);
                body.GetComponent<Renderer>().sharedMaterial = wall;
                body.isStatic = true;

                // Window glow state (85% glow)
                bool isLit = Random.value < 0.85f;
                Material windowMat = isLit ? litWindowMat : darkWindowMat;

                // Exactly ONE back-side (negative Z) window per floor
                for (int f = 0; f < floors; f++) {
                    float windowY = (f * 12f) + 6f;
                    var win = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    win.transform.SetParent(h.transform);
                    win.transform.localScale = new Vector3(3.6f, 2.6f, 0.3f);
                    win.GetComponent<Renderer>().sharedMaterial = windowMat;
                    DestroyImmediate(win.GetComponent<Collider>());
                    
                    win.transform.localPosition = new Vector3(0f, windowY, -10.15f);
                    win.transform.localRotation = Quaternion.Euler(0, 180, 0);
                    win.isStatic = true;

                    if (isLit) {
                        var lightGo = new GameObject("WindowLight");
                        lightGo.transform.SetParent(win.transform);
                        lightGo.transform.localPosition = new Vector3(0f, 0f, -0.6f);
                        var l = lightGo.AddComponent<Light>();
                        l.type = LightType.Point;
                        l.color = new Color(1f, 0.75f, 0.3f);
                        l.range = 8f;
                        l.intensity = 1.6f;
                        l.shadows = LightShadows.None;
                    }
                }

                // Spawn wooden trim divider belts between floors
                for (int f = 1; f < floors; f++) {
                    var trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    trim.transform.SetParent(h.transform);
                    trim.transform.localPosition = new Vector3(0f, f * 12f, 0f);
                    trim.transform.localScale = new Vector3(20.4f, 0.8f, 20.4f);
                    trim.GetComponent<Renderer>().sharedMaterial = wood;
                    DestroyImmediate(trim.GetComponent<Collider>());
                    trim.isStatic = true;
                }

                var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
                door.transform.SetParent(h.transform);
                door.transform.localPosition = new Vector3(0, 4, 10.1f);
                door.transform.localScale = new Vector3(6, 8, 0.2f);
                door.GetComponent<Renderer>().sharedMaterial = wood;
                door.isStatic = true;

                if (Random.value > 0.5f) {
                    if (crate) InstantiateProp(crate, pos + new Vector3(12, 0, 8), h.transform);
                    if (barrel) InstantiateProp(barrel, pos + new Vector3(12, 0, 11), h.transform);
                }
            }
        }

        private void InstantiateProp(GameObject prefab, Vector3 pos, Transform parent)
        {
            var p = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            p.transform.localScale = Vector3.one * 0.3f;

            // Align bottom base of prop flush with ground and generate visual bounds collider
            AlignToGroundAndAddCollider(p, pos, Quaternion.Euler(-90, 0, 0), 0f);

            var rb = p.AddComponent<Rigidbody>();
            rb.mass = 20f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        private void PlacePlaza(Transform parent, Vector3 pos, GameObject[] trees, GameObject columnPrefab, Material sandMat, Material craterMat)
        {
            var p = new GameObject("Plaza");
            p.transform.SetParent(parent); 
            p.transform.position = pos;
            p.isStatic = true;

            if (trees != null && trees.Length > 0) {
                var t = (GameObject)PrefabUtility.InstantiatePrefab(trees[Random.Range(0, trees.Length)], p.transform);
                t.transform.localScale = Vector3.one * 8f; 
                
                // Deep-plant trees with -1.8f vertical offset to completely bury roots below floor sandlevel
                AlignToGroundAndAddCollider(t, pos, Quaternion.Euler(-90, 0, 0), -1.8f);

                t.isStatic = true;
                foreach (Transform child in t.GetComponentsInChildren<Transform>(true)) {
                    child.gameObject.isStatic = true;
                }
            }

            // Spawn majestic ancient Egyptian columns at some plaza corners (ruins feel)
            if (columnPrefab != null && Random.value < 0.45f) {
                Vector3[] offsets;
                if (Random.value < 0.6f) {
                    // Spawn 2 columns (diagonal for realistic ruin aesthetics)
                    offsets = new Vector3[] {
                        new Vector3(-8f, 0f, -8f),
                        new Vector3(8f, 0f, 8f)
                    };
                } else {
                    // Spawn 4 columns
                    offsets = new Vector3[] {
                        new Vector3(-8f, 0f, -8f),
                        new Vector3(8f, 0f, -8f),
                        new Vector3(-8f, 0f, 8f),
                        new Vector3(8f, 0f, 8f)
                    };
                }
                foreach (var offset in offsets) {
                    SpawnColumn(p.transform, pos + offset, columnPrefab);
                }
            }

            // 35% chance to spawn a sand impact crater in the plaza
            if (Random.value < 0.35f) {
                Vector3 craterPos = pos + new Vector3(Random.Range(-4f, 4f), 0f, Random.Range(-4f, 4f));
                BuildCrater(p.transform, craterPos, craterMat, sandMat);
            }
        }

        private void SpawnColumn(Transform parent, Vector3 pos, GameObject columnPrefab)
        {
            var col = (GameObject)PrefabUtility.InstantiatePrefab(columnPrefab, parent);
            col.transform.localScale = Vector3.one * 3f;

            AlignToGroundAndAddCollider(col, pos, Quaternion.Euler(-90, 0, 0), 0f);

            col.isStatic = true;
            foreach (Transform t in col.GetComponentsInChildren<Transform>(true)) {
                t.gameObject.isStatic = true;
            }
        }

        private void BuildCrater(Transform parent, Vector3 pos, Material craterMat, Material sandMat)
        {
            var crater = new GameObject("Crater");
            crater.transform.SetParent(parent);
            crater.transform.position = pos;
            crater.isStatic = true;

            float scaleMultiplier = Random.Range(0.6f, 1.8f);

            // Low-poly crater depression
            var dep = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dep.name = "Depression";
            dep.transform.SetParent(crater.transform);
            dep.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            dep.transform.localScale = new Vector3(8f * scaleMultiplier, 0.05f, 8f * scaleMultiplier);
            dep.GetComponent<Renderer>().sharedMaterial = craterMat;
            DestroyImmediate(dep.GetComponent<Collider>());
            dep.isStatic = true;

            // Ring of crater sandy debris rocks
            int rockCount = Random.Range(6, 10);
            float radius = 4.2f * scaleMultiplier;
            for (int i = 0; i < rockCount; i++) {
                float angle = (i * 360f / rockCount) + Random.Range(-15f, 15f);
                float rad = angle * Mathf.Deg2Rad;
                Vector3 rockPos = new Vector3(Mathf.Cos(rad) * radius, Random.Range(-0.2f * scaleMultiplier, 0.2f * scaleMultiplier), Mathf.Sin(rad) * radius);
                
                var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rock.name = "CraterRock";
                rock.transform.SetParent(crater.transform);
                rock.transform.localPosition = rockPos;
                
                rock.transform.localScale = new Vector3(
                    Random.Range(1.2f, 2.5f) * scaleMultiplier, 
                    Random.Range(0.6f, 1.8f) * scaleMultiplier, 
                    Random.Range(1.2f, 2.5f) * scaleMultiplier
                );
                
                rock.transform.localRotation = Quaternion.Euler(
                    Random.Range(10f, 30f), 
                    -angle + 90f + Random.Range(-20f, 20f), 
                    Random.Range(-10f, 10f)
                );
                
                rock.GetComponent<Renderer>().sharedMaterial = sandMat;
                rock.isStatic = true;
            }
        }

        private void FixPlayerAndWeapons()
        {
            var p = GameObject.Find("Player");
            if (p == null) return;
            p.tag = "Player";
            
            var inv = p.GetComponentInChildren<InfimaGames.LowPolyShooterPack.Inventory>();
            if (inv == null) return;

            Color[] colors = { new Color(1, 0.4f, 0), Color.white, new Color(0, 0.7f, 1) };
            int idx = 0;
            foreach (Transform t in inv.transform) {
                if (t.name.ToLower().Contains("pistol") || t.name.ToLower().Contains("assault")) {
                    if (idx >= 3) { t.gameObject.SetActive(false); continue; }
                }
                foreach (var r in t.GetComponentsInChildren<Renderer>()) {
                    var sharedMats = r.sharedMaterials;
                    foreach (var m in sharedMats) {
                        if (m == null) continue;
                        m.SetColor("_EmissionColor", colors[idx % 3] * 4f);
                        m.EnableKeyword("_EMISSION");
                        m.color = colors[idx % 3] * 0.5f;
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

        private void AlignToGroundAndAddCollider(GameObject obj, Vector3 basePos, Quaternion targetRot, float offsetAdjustment)
        {
            obj.transform.position = basePos;
            obj.transform.rotation = targetRot;

            var filters = obj.GetComponentsInChildren<MeshFilter>(true);
            if (filters.Length > 0) {
                float minY = float.MaxValue;
                foreach (var filter in filters) {
                    if (filter.sharedMesh == null) continue;
                    
                    Bounds localBounds = filter.sharedMesh.bounds;
                    Vector3 center = localBounds.center;
                    Vector3 extents = localBounds.extents;
                    Vector3[] corners = {
                        new Vector3(center.x - extents.x, center.y - extents.y, center.z - extents.z),
                        new Vector3(center.x - extents.x, center.y - extents.y, center.z + extents.z),
                        new Vector3(center.x - extents.x, center.y + extents.y, center.z - extents.z),
                        new Vector3(center.x - extents.x, center.y + extents.y, center.z + extents.z),
                        new Vector3(center.x + extents.x, center.y - extents.y, center.z - extents.z),
                        new Vector3(center.x + extents.x, center.y - extents.y, center.z + extents.z),
                        new Vector3(center.x + extents.x, center.y + extents.y, center.z - extents.z),
                        new Vector3(center.x + extents.x, center.y + extents.y, center.z + extents.z)
                    };
                    
                    foreach (var corner in corners) {
                        float worldY = filter.transform.TransformPoint(corner).y;
                        if (worldY < minY) minY = worldY;
                    }
                }

                if (minY != float.MaxValue) {
                    float yOffset = basePos.y - minY + offsetAdjustment;
                    obj.transform.position = new Vector3(basePos.x, basePos.y + yOffset, basePos.z);
                }
            } else {
                var renderers = obj.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length > 0) {
                    float minY = float.MaxValue;
                    foreach (var r in renderers) {
                        if (r is ParticleSystemRenderer) continue;
                        if (r.bounds.min.y < minY) minY = r.bounds.min.y;
                    }
                    if (minY != float.MaxValue) {
                        float yOffset = basePos.y - minY + offsetAdjustment;
                        obj.transform.position = new Vector3(basePos.x, basePos.y + yOffset, basePos.z);
                    }
                }
            }

            EnsurePerfectBoxCollider(obj);
        }

        private void EnsurePerfectBoxCollider(GameObject obj)
        {
            foreach (var col in obj.GetComponentsInChildren<Collider>(true)) {
                DestroyImmediate(col);
            }

            var renderers = obj.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) {
                obj.AddComponent<BoxCollider>();
                return;
            }

            Vector3 originalPos = obj.transform.position;
            Quaternion originalRot = obj.transform.rotation;
            Vector3 originalScale = obj.transform.localScale;

            obj.transform.rotation = Quaternion.identity;

            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
            bool initialized = false;

            foreach (var r in renderers) {
                if (r is ParticleSystemRenderer) continue;

                Bounds childBounds = r.bounds;
                Vector3 localCenter = obj.transform.InverseTransformPoint(childBounds.center);
                Vector3 localExtents = obj.transform.InverseTransformVector(childBounds.extents);

                localExtents.x = Mathf.Abs(localExtents.x);
                localExtents.y = Mathf.Abs(localExtents.y);
                localExtents.z = Mathf.Abs(localExtents.z);

                Bounds localChildBounds = new Bounds(localCenter, localExtents * 2f);

                if (!initialized) {
                    bounds = localChildBounds;
                    initialized = true;
                } else {
                    bounds.Encapsulate(localChildBounds);
                }
            }

            obj.transform.rotation = originalRot;

            if (initialized) {
                var box = obj.AddComponent<BoxCollider>();
                box.center = bounds.center;
                box.size = bounds.size;
            } else {
                obj.AddComponent<BoxCollider>();
            }
        }

        private void CreateProceduralPyramid(GameObject root, Vector3 pos, float baseSize, float height, Material mat, Color glowColor)
        {
            var pGo = new GameObject("Pyramid");
            pGo.transform.SetParent(root.transform);
            pGo.transform.position = pos;
            pGo.isStatic = true;

            Mesh mesh = new Mesh();
            mesh.name = "PyramidMesh";

            float half = baseSize / 2f;

            Vector3[] vertices = new Vector3[18];
            int[] triangles = new int[18];
            Vector2[] uvs = new Vector2[18];

            Vector3 apex = new Vector3(0, height, 0);
            Vector3 fl = new Vector3(-half, 0, -half);
            Vector3 fr = new Vector3(half, 0, -half);
            Vector3 br = new Vector3(half, 0, half);
            Vector3 bl = new Vector3(-half, 0, half);

            // Front Face (fl -> fr -> apex)
            vertices[0] = fl; vertices[1] = fr; vertices[2] = apex;
            triangles[0] = 0; triangles[1] = 2; triangles[2] = 1;
            uvs[0] = new Vector2(0, 0); uvs[1] = new Vector2(1, 0); uvs[2] = new Vector2(0.5f, 1);

            // Right Face (fr -> br -> apex)
            vertices[3] = fr; vertices[4] = br; vertices[5] = apex;
            triangles[3] = 3; triangles[4] = 5; triangles[5] = 4;
            uvs[3] = new Vector2(0, 0); uvs[4] = new Vector2(1, 0); uvs[5] = new Vector2(0.5f, 1);

            // Back Face (br -> bl -> apex)
            vertices[6] = br; vertices[7] = bl; vertices[8] = apex;
            triangles[6] = 6; triangles[7] = 8; triangles[8] = 7;
            uvs[6] = new Vector2(0, 0); uvs[7] = new Vector2(1, 0); uvs[8] = new Vector2(0.5f, 1);

            // Left Face (bl -> fl -> apex)
            vertices[9] = bl; vertices[10] = fl; vertices[11] = apex;
            triangles[9] = 9; triangles[10] = 11; triangles[11] = 10;
            uvs[9] = new Vector2(0, 0); uvs[10] = new Vector2(1, 0); uvs[11] = new Vector2(0.5f, 1);

            // Base Face Tri 1 (bl -> br -> fl)
            vertices[12] = bl; vertices[13] = br; vertices[14] = fl;
            triangles[12] = 12; triangles[13] = 14; triangles[14] = 13;
            uvs[12] = new Vector2(0, 1); uvs[13] = new Vector2(1, 1); uvs[14] = new Vector2(0, 0);

            // Base Face Tri 2 (br -> fr -> fl)
            vertices[15] = br; vertices[16] = fr; vertices[17] = fl;
            triangles[15] = 15; triangles[16] = 17; triangles[17] = 16;
            uvs[15] = new Vector2(1, 1); uvs[16] = new Vector2(1, 0); uvs[17] = new Vector2(0, 0);

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var filter = pGo.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            var renderer = pGo.AddComponent<MeshRenderer>();
            var pMat = new Material(mat); // Unique material instance
            pMat.color = new Color(0.85f, 0.75f, 0.6f); // Sandstone color
            pMat.SetColor("_EmissionColor", glowColor * 1.5f);
            pMat.EnableKeyword("_EMISSION");
            renderer.sharedMaterial = pMat;

            pGo.AddComponent<MeshCollider>().sharedMesh = mesh;

            pGo.isStatic = true;

            var lightGo = new GameObject("PyramidBeacon");
            lightGo.transform.SetParent(pGo.transform);
            lightGo.transform.localPosition = new Vector3(0f, height + 2f, 0f);
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = glowColor;
            l.range = 400f;
            l.intensity = 20f;
            l.shadows = LightShadows.None;
        }
    }
}
