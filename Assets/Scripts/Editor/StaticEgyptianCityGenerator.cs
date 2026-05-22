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
            var mummies = GameObject.FindObjectsByType<TheAlchemistsCrypt.AI.ZombieAI>(FindObjectsInactive.Include);
            foreach (var m in mummies) {
                DecimateMesh(m.gameObject, 0.8f);
            }

            // Also decimate the prefabs in Resources to ensure spawned ones are optimized
            string[] resourcePrefabs = { "Assets/Resources/Mummy_Dynamic_Prefab.prefab", "Assets/Resources/Pharaoh_Prefab.prefab" };
            foreach (var path in resourcePrefabs) {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null) {
                    DecimateMesh(prefab, 0.8f);
                    EditorUtility.SetDirty(prefab);
                }
            }
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
                    lowerName.Contains("escapemanager")) 
                {
                    DestroyImmediate(go);
                }
            }
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
            
            // ── AESTHETIC PALETTE (Warm Sunset Desert) ──
            Material wallMat = new Material(GetLitShader());
            wallMat.SetColor("_BaseColor", new Color(0.92f, 0.86f, 0.76f)); // Sandy Cream/Beige
            wallMat.SetFloat("_Smoothness", 0.0f);   // Matte finish
            
            Material woodMat = new Material(GetLitShader());
            woodMat.SetColor("_BaseColor", new Color(0.25f, 0.15f, 0.08f));
            
            Material floorMat = new Material(GetLitShader());
            floorMat.SetColor("_BaseColor", new Color(0.91f, 0.81f, 0.62f)); // Pale Pastel Sand
            floorMat.SetFloat("_Metallic", 0.0f);
            floorMat.SetFloat("_Smoothness", 0.12f);
            floorMat.SetColor("_EmissionColor", new Color(1.0f, 0.95f, 0.8f) * 0.01f);
            floorMat.EnableKeyword("_EMISSION");

            Material litWindowMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            litWindowMat.SetColor("_BaseColor", new Color(1f, 0.75f, 0.3f));
            litWindowMat.SetColor("_EmissionColor", new Color(1f, 0.65f, 0.2f) * 3.5f);
            litWindowMat.EnableKeyword("_EMISSION");
            
            Material darkWindowMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            darkWindowMat.SetColor("_BaseColor", new Color(0.1f, 0.1f, 0.15f));

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
                Texture2D sandTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EgyptianAssets/desert_sand_albedo.png");
                if (sandTex == null) sandTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EgyptianAssets/sand_diffuse.png");
                if (sandTex == null) {
                    int texSize = 1024;
                    sandTex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, true);
                    sandTex.wrapMode = TextureWrapMode.Clamp;
                    sandTex.filterMode = FilterMode.Trilinear;
                    Color topColor = new Color(0.98f, 0.90f, 0.72f);
                    Color bottomColor = new Color(0.86f, 0.75f, 0.55f);
                    Color[] pixels = new Color[texSize * texSize];
                    for (int y = 0; y < texSize; y++) {
                        float t = (float)y / (texSize - 1);
                        Color rowColor = Color.Lerp(bottomColor, topColor, t);
                        for (int x = 0; x < texSize; x++) pixels[y * texSize + x] = rowColor;
                    }
                    sandTex.SetPixels(pixels);
                    sandTex.Apply(true);
                    AssetDatabase.CreateAsset(sandTex, "Assets/EgyptianAssets/SandTexGradient_1024.asset");
                }
                layer.diffuseTexture = sandTex;
                layer.tileSize = new Vector2(80f, 80f);
                layer.specular = new Color(0.03f, 0.03f, 0.02f, 0f);
                layer.smoothness = 0.12f;
                AssetDatabase.CreateAsset(layer, layerPath);
            }
            layer.tileSize = new Vector2(80f, 80f);
            layer.smoothness = 0.12f;
            EditorUtility.SetDirty(layer);
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
            CreateProceduralPyramid(root, new Vector3(450f, 0f, 120f), 140f, 85f, wallMat, new Color(1f, 0.82f, 0.45f));
            CreateProceduralPyramid(root, new Vector3(-450f, 0f, 120f), 170f, 110f, wallMat, new Color(1f, 0.7f, 0.3f)); 

            CreateSeaAndCoastline(root);
            FixPlayerAndWeapons();
            SetupMummyAnimations();

            var activeScene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
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
            beach.GetComponent<Renderer>().sharedMaterial = beachMat;
            beach.isStatic = true;
            DestroyImmediate(beach.GetComponent<Collider>());

            GameObject barrier = new GameObject("CoastlineBarrier");
            barrier.transform.SetParent(root.transform);
            barrier.transform.position = new Vector3(0f, 10f, -85f); 
            var bc = barrier.AddComponent<BoxCollider>();
            bc.size = new Vector3(5000f, 30f, 5f); 
            barrier.isStatic = true;

            Debug.Log("[CityGen] Sea visible and ultra reflective. Substantial barrier at Z=-85.");
        }

        private Shader GetLitShader()
        {
            var s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("URP/Lit");
            if (s == null) s = Shader.Find("Lit");
            return s;
        }

        private void SetupEnvironment(GameObject root)
        {
            var skyMat = new Material(Shader.Find("Skybox/Procedural"));
            skyMat.SetColor("_SkyTint", new Color(0.5f, 0.7f, 0.9f)); 
            skyMat.SetColor("_GroundColor", new Color(1.0f, 0.6f, 0.5f)); // Pinkish horizon
            skyMat.SetFloat("_AtmosphereThickness", 1.1f);
            skyMat.SetFloat("_Exposure", 1.3f);
            
            RenderSettings.skybox = skyMat;
            RenderSettings.ambientMode = AmbientMode.Trilight; 
            RenderSettings.ambientSkyColor    = new Color(0.6f, 0.7f, 0.8f);
            RenderSettings.ambientEquatorColor = new Color(1.0f, 0.6f, 0.5f);
            RenderSettings.ambientGroundColor  = new Color(0.5f, 0.4f, 0.3f);

            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.95f, 0.75f, 0.65f);
            RenderSettings.fogStartDistance = 60f;   
            RenderSettings.fogEndDistance  = 1200f;   
            
            var sun = GameObject.Find("Directional Light")?.GetComponent<Light>();
            if (sun != null) {
                sun.color = new Color(1.0f, 0.9f, 0.8f); 
                sun.intensity = 1.4f;
                sun.transform.rotation = Quaternion.Euler(15f, 220f, 0f); // Lower angle for sunset
            }
            SetupPostProcessing(root.transform);
        }

        private void SetupPostProcessing(Transform parent)
        {
            var volGo = new GameObject("GlobalVolume");
            volGo.transform.SetParent(parent);
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true; vol.priority = 10;
            
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "VisualOverhaulProfile";
            
            if (!profile.TryGet<Bloom>(out var bloom)) bloom = profile.Add<Bloom>();
            bloom.intensity.Override(0.5f);
            bloom.threshold.Override(0.85f);
            bloom.scatter.Override(0.6f);

            if (!profile.TryGet<ColorAdjustments>(out var colorAdj)) colorAdj = profile.Add<ColorAdjustments>();
            colorAdj.contrast.Override(15f);
            colorAdj.saturation.Override(10f);
            colorAdj.postExposure.Override(0.1f);
            colorAdj.colorFilter.Override(new Color(1f, 0.96f, 0.9f)); 

            if (!profile.TryGet<Tonemapping>(out var tone)) tone = profile.Add<Tonemapping>();
            tone.mode.Override(TonemappingMode.ACES);
            
            if (!profile.TryGet<Vignette>(out var vignette)) vignette = profile.Add<Vignette>();
            vignette.intensity.Override(0.25f);
            vignette.color.Override(new Color(0.2f, 0.15f, 0.1f)); 

            if (!profile.TryGet<LiftGammaGain>(out var lgg)) lgg = profile.Add<LiftGammaGain>();
            lgg.lift.Override(new Vector4(0.05f, 0.02f, 0.0f, 0f));
            lgg.gamma.Override(new Vector4(1.05f, 1.0f, 0.95f, 0f));
            lgg.gain.Override(new Vector4(1.1f, 1.05f, 1.0f, 0f));

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
                    new Vector3(0f, windowY, -(depth / 2f) - 0.18f),
                    new Vector3(0f, windowY, (depth / 2f) + 0.18f),
                    new Vector3(-(width / 2f) - 0.18f, windowY, 0f),
                    new Vector3((width / 2f) + 0.18f, windowY, 0f)
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
                }
            }

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
            
            if (columnPrefab != null) {
                var colObj = (GameObject)PrefabUtility.InstantiatePrefab(columnPrefab, p.transform);
                colObj.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                AlignToGroundAndAddCollider(colObj, pos + new Vector3(-8f, 0f, -8f), Quaternion.Euler(-90f, 0f, 0f), 0f);
            }
            
            if (trees != null && trees.Length > 0) {
                var t = (GameObject)PrefabUtility.InstantiatePrefab(trees[Random.Range(0, trees.Length)], p.transform);
                t.transform.localScale = Vector3.one * 8f; AlignToGroundAndAddCollider(t, pos + new Vector3(8f, 0f, 8f), Quaternion.Euler(-90, 0, 0), -1.8f);

                var filters = t.GetComponentsInChildren<MeshFilter>();
                foreach (var mf in filters) {
                    if (mf.sharedMesh == null) continue;
                    try {
                        var simplifier = new MeshSimplifier();
                        simplifier.Initialize(mf.sharedMesh);
                        simplifier.SimplifyMesh(0.80f); 
                        mf.sharedMesh = simplifier.ToMesh();
                    } catch (System.Exception e) {
                        Debug.LogWarning($"[CityGen] Failed to decimate tree mesh: {mf.name} - {e.Message}");
                    }
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

                    string invPath = "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Prefabs/Weapons/P_LPSP_Inventory.prefab";
                    GameObject invPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(invPath);
                    if (invPrefab != null)
                    {
                        var inv = PrefabUtility.InstantiatePrefab(invPrefab) as GameObject;
                        inv.name = "Inventory";
                        inv.transform.SetParent(p.transform, false);
                        inv.transform.localPosition = Vector3.zero;
                    }

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
                            mat.SetColor("_BaseColor", new Color(0.08f, 0.08f, 0.10f)); 
                            mat.SetFloat("_Smoothness", 0.85f);
                            mat.SetFloat("_Metallic", 1.0f);
                        } else if (mName.Contains("barrel") || mName.Contains("grip") || mName.Contains("trigger")) {
                            mat.SetColor("_BaseColor", new Color(0.12f, 0.12f, 0.12f));
                            mat.SetFloat("_Smoothness", 0.9f);
                            mat.SetFloat("_Metallic", 1.0f);
                            if (mat.HasProperty("_EmissionColor")) {
                                var emis = mat.GetColor("_EmissionColor");
                                mat.SetColor("_EmissionColor", emis * 0.25f);
                                if (emis.maxColorComponent > 0.01f) mat.EnableKeyword("_EMISSION");
                            }
                        } else {
                            if (mat.HasProperty("_EmissionColor")) {
                                var emis = mat.GetColor("_EmissionColor");
                                mat.SetColor("_EmissionColor", emis * 0.5f);
                                if (emis.maxColorComponent > 0.01f) mat.EnableKeyword("_EMISSION");
                            }
                            mat.SetFloat("_Smoothness", 0.6f);
                            mat.SetFloat("_Metallic", 0.6f);
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
                var rb = obj.GetComponent<Rigidbody>();
                if (rb == null) rb = obj.AddComponent<Rigidbody>();
                rb.mass = 10f;
                
                Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
                bool first = true;
                foreach (var r in allRenderers) {
                    if (r is ParticleSystemRenderer) continue;
                    if (first) { bounds = r.bounds; first = false; } else bounds.Encapsulate(r.bounds);
                }
                
                var boxCol = obj.AddComponent<BoxCollider>();
                boxCol.center = obj.transform.InverseTransformPoint(bounds.center);
                boxCol.size = Vector3.Scale(bounds.size, new Vector3(1f/obj.transform.lossyScale.x, 1f/obj.transform.lossyScale.y, 1f/obj.transform.lossyScale.z));
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
            var pMat = new Material(mat);
            pMat.SetColor("_BaseColor", new Color(0.78f, 0.52f, 0.35f)); 
            pMat.SetColor("_EmissionColor", glowColor * 1.5f); pMat.EnableKeyword("_EMISSION"); renderer.sharedMaterial = pMat;
            pGo.AddComponent<MeshCollider>().sharedMesh = mesh;
        }
    }
}
