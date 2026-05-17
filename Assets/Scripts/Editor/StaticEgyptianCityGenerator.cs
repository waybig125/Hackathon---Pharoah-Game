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
                "Assets/Mummy_Assets/mummy_attack.fbx"
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

            System.Func<string, string, AnimationClip> getOrCreateLoopingClip = (fbxPath, animName) => {
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
                    AssetDatabase.CreateAsset(destClip, destPath);
                } else {
                    EditorUtility.CopySerialized(sourceClip, destClip);
                }

                // Force loop settings on native serialized asset
                var settings = AnimationUtility.GetAnimationClipSettings(destClip);
                settings.loopTime = true;
                settings.loop = true;
                AnimationUtility.SetAnimationClipSettings(destClip, settings);
                EditorUtility.SetDirty(destClip);
                AssetDatabase.SaveAssets();

                return destClip;
            };

            var idleClip = getOrCreateLoopingClip("Assets/Mummy_Assets/mummy_idle.fbx", "mummy_idle");
            var walkClip = getOrCreateLoopingClip("Assets/Mummy_Assets/new_Walking.fbx", "new_Walking");
            var attackClip = getOrCreateLoopingClip("Assets/Mummy_Assets/mummy_attack.fbx", "mummy_attack");

            // Build/Update States
            var idleState = GetOrAddState(rootStateMachine, "Idle", idleClip);
            var walkState = GetOrAddState(rootStateMachine, "Walk", walkClip);
            var attackState = GetOrAddState(rootStateMachine, "Attack", attackClip);

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
                            if (!c.loopTime || !c.loop) {
                                c.loopTime = true;
                                c.loop = true;
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
            Material wallMat = CreateLit(new Color(0.92f, 0.85f, 0.7f), 4f, "desert_sand_normal.png");
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

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "FloorGround";
            ground.transform.SetParent(root.transform);
            ground.transform.localScale = new Vector3(100, 1, 100);
            ground.GetComponent<Renderer>().sharedMaterial = floorMat;
            ground.isStatic = true;

            float spacing = 32f;
            float halfSpan = (gridSize * spacing) / 2f;
            var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Inspiration-Thirdperson-Controller-Update372022/Assets/Enemy-AI/Prefabs/TestZombie.prefab");

            for (int x = 0; x < gridSize; x++) {
                for (int z = 0; z < gridSize; z++) {
                    float posX = -halfSpan + (x * spacing) + (spacing / 2f);
                    float posZ = -halfSpan + (z * spacing) + (spacing / 2f);
                    Vector3 pos = new Vector3(posX, 0, posZ);
                    pos.x += Random.Range(-2f, 2f); pos.z += Random.Range(-2f, 2f);

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

            CreateProceduralPyramid(root, new Vector3(-220f, 0f, 220f), 150f, 95f, wallMat, new Color(1f, 0.85f, 0.4f));
            CreateProceduralPyramid(root, new Vector3(220f, 0f, -220f), 160f, 100f, wallMat, new Color(1f, 0.5f, 0.2f));
            CreateProceduralPyramid(root, new Vector3(220f, 0f, 220f), 140f, 85f, wallMat, new Color(0.9f, 0.8f, 1f));
            CreateProceduralPyramid(root, new Vector3(-220f, 0f, -220f), 170f, 110f, wallMat, new Color(1f, 0.7f, 0.3f));

            FixPlayerAndWeapons();
            StaticBatchingUtility.Combine(root);
            SetupMummyAnimations();

            // Mark Scene Dirty to fix persistence issue
            var activeScene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            
            Debug.Log("Polished Egyptian City V5.1 Regenerated and Saved!");
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
            float height = floors * 12f;
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(h.transform); body.transform.localPosition = new Vector3(0, height / 2f, 0); body.transform.localScale = new Vector3(20, height, 20);
            body.GetComponent<Renderer>().sharedMaterial = wall; body.isStatic = true;

            Material windowMat = (Random.value < 0.85f) ? litWindowMat : darkWindowMat;
            for (int f = 0; f < floors; f++) {
                float windowY = (f * 12f) + 6f;
                var win = GameObject.CreatePrimitive(PrimitiveType.Cube);
                win.transform.SetParent(h.transform); win.transform.localScale = new Vector3(3.6f, 2.6f, 0.3f); win.GetComponent<Renderer>().sharedMaterial = windowMat;
                DestroyImmediate(win.GetComponent<Collider>());
                win.transform.localPosition = new Vector3(0f, windowY, -10.15f); win.transform.localRotation = Quaternion.Euler(0, 180, 0); win.isStatic = true;
                if (windowMat == litWindowMat) {
                    var lightGo = new GameObject("WindowLight"); lightGo.transform.SetParent(win.transform); lightGo.transform.localPosition = new Vector3(0f, 0f, -0.6f);
                    var l = lightGo.AddComponent<Light>(); l.type = LightType.Point; l.color = new Color(1f, 0.75f, 0.3f); l.range = 8f; l.intensity = 1.6f;
                }
            }
            var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.transform.SetParent(h.transform); door.transform.localPosition = new Vector3(0, 4, 10.1f); door.transform.localScale = new Vector3(6, 8, 0.2f); door.GetComponent<Renderer>().sharedMaterial = wood; door.isStatic = true;
        }

        private void PlacePlaza(Transform parent, Vector3 pos, GameObject[] trees, GameObject columnPrefab, Material sandMat, Material craterMat)
        {
            var p = new GameObject("Plaza"); p.transform.SetParent(parent); p.transform.position = pos; p.isStatic = true;
            if (trees != null && trees.Length > 0) {
                var t = (GameObject)PrefabUtility.InstantiatePrefab(trees[Random.Range(0, trees.Length)], p.transform);
                t.transform.localScale = Vector3.one * 8f; AlignToGroundAndAddCollider(t, pos, Quaternion.Euler(-90, 0, 0), -1.8f);
                t.isStatic = true;
            }
        }

        private void FixPlayerAndWeapons()
        {
            var p = GameObject.Find("Player"); if (p == null) return; p.tag = "Player";
            var inv = p.GetComponentInChildren<InfimaGames.LowPolyShooterPack.Inventory>(); if (inv == null) return;
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

        private void AlignToGroundAndAddCollider(GameObject obj, Vector3 basePos, Quaternion targetRot, float offsetAdjustment)
        {
            obj.transform.position = basePos; obj.transform.rotation = targetRot;
            var renderers = obj.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0) {
                float minY = float.MaxValue;
                foreach (var r in renderers) if (!(r is ParticleSystemRenderer) && r.bounds.min.y < minY) minY = r.bounds.min.y;
                if (minY != float.MaxValue) {
                    float yOffset = basePos.y - minY + offsetAdjustment;
                    obj.transform.position = new Vector3(basePos.x, basePos.y + yOffset, basePos.z);
                }
            }
            foreach (var col in obj.GetComponentsInChildren<Collider>(true)) DestroyImmediate(col);
            var filters = obj.GetComponentsInChildren<MeshFilter>(true);
            if (filters.Length > 0) {
                foreach (var filter in filters) {
                    if (filter.sharedMesh == null) continue;
                    var mc = filter.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = filter.sharedMesh;
                }
            } else obj.AddComponent<BoxCollider>();
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
