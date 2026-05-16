using UnityEngine;
using UnityEditor;
using Unity.AI.Navigation;
using System.Collections.Generic;

namespace TheAlchemistsCrypt.Editor
{
    public class StaticEgyptianCityGenerator : EditorWindow
    {
        [MenuItem("Egyptian/Generator V4")]
        [MenuItem("Tools/Generate Egyptian City (V4 - Final)")]
        public static void ShowWindow() => GetWindow<StaticEgyptianCityGenerator>("Egyptian City V4.2");

        private int seed = 999;
        private int gridSize = 12;
        private string rootName = "EgyptianCity_V4_Final";

        [MenuItem("Egyptian/Regenerate City")]
        public static void QuickRegen() {
            var g = CreateInstance<StaticEgyptianCityGenerator>();
            g.Purge(); g.GeneratePolishedCity();
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("V4.2 POLISHED: Glow Pyramids, Multi-Layered Floor, Purged Duplicates.", MessageType.Info);
            seed = EditorGUILayout.IntField("Seed", seed);
            if (GUILayout.Button("▶ GENERATE POLISHED CITY", GUILayout.Height(40))) GeneratePolishedCity();
            
            EditorGUILayout.Space();
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("🧟 SETUP MUMMY ANIMATIONS & SCALES", GUILayout.Height(40))) SetupMummyAnimations();
            GUI.backgroundColor = Color.white;
            
            if (GUILayout.Button("🗑 CLEANUP", GUILayout.Height(30))) Purge();
        }

        private void SetupMummyAnimations()
        {
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

            // Add states if they don't exist
            var idle = getClip("Assets/Mummy_Assets/mummy_idle.fbx");
            var walk = getClip("Assets/Mummy_Assets/mummy_walk.fbx");
            var attack = getClip("Assets/Mummy_Assets/mummy_attack.fbx");

            if (idle != null && !HasState(rootStateMachine, "Idle")) rootStateMachine.AddState("Idle").motion = idle;
            if (walk != null && !HasState(rootStateMachine, "Walk")) rootStateMachine.AddState("Walk").motion = walk;
            if (attack != null && !HasState(rootStateMachine, "Attack")) rootStateMachine.AddState("Attack").motion = attack;

            // 2. Apply to scene mummies
            string[] names = { "Mummy_Base_Test", "Mummy_PBR_Test", "Mummy_Shaded_Test" };
            foreach (var n in names) {
                var go = GameObject.Find(n);
                if (go != null) {
                    Undo.RecordObject(go.transform, "Scale Mummies");
                    go.transform.localScale = new Vector3(100, 100, 100);
                    var anim = go.GetComponent<Animator>();
                    if (anim == null) anim = go.AddComponent<Animator>();
                    anim.runtimeAnimatorController = controller;
                }
            }
            Debug.Log("Mummy Setup Complete! Scales set to 100, Animator Controller updated with Idle, Walk, and Attack.");
        }

        private bool HasState(UnityEditor.Animations.AnimatorStateMachine sm, string n) {
            foreach (var s in sm.states) if (s.state.name == n) return true;
            return false;
        }

        private void Purge()
        {
            var old = GameObject.Find(rootName);
            if (old != null) Undo.DestroyObjectImmediate(old);
            
            // AGGRESSIVE PURGE OF CONFLICTING OBJECTS
            var all = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in all) {
                if (go == null) continue; // Skip if already destroyed
                try {
                    if (go.name.Contains("Player_Copy") || go.name.Contains("MobileHUD") || go.name.Contains("P_LPSP_UI_Canvas")) {
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
            var zombie = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TestZombie.prefab");
            var crate = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/crate.glb");
            var barrel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/barrel.glb");
            
            // Materials
            Material wallMat = CreateLit(new Color(0.92f, 0.85f, 0.7f), 4f, "desert_sand_normal.png");
            Material floorMat = CreateLit(new Color(0.9f, 0.8f, 0.6f), gridSize * 2); // No normal map for floor to avoid hieroglyphs
            Material woodMat = CreateLit(new Color(0.25f, 0.15f, 0.08f), 1f);
            Material holeMat = CreateLit(new Color(0.05f, 0.03f, 0.01f), 1f);

            SetupEnvironment();

            // Ground Plane
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.SetParent(root.transform);
            ground.transform.localScale = new Vector3(100, 1, 100);
            ground.GetComponent<Renderer>().sharedMaterial = floorMat;
            ground.isStatic = true;

            float block = 24f;
            float currentX = -150f;
            for (int x = 0; x < gridSize; x++) {
                float streetX = (x % 3 == 0) ? 60f : 16f; 
                float currentZ = -150f;
                for (int z = 0; z < gridSize; z++) {
                    float streetZ = (z % 4 == 0) ? 50f : 16f;
                    
                    Vector3 pos = new Vector3(currentX, 0, currentZ);
                    if (pos.magnitude > 40f) {
                        if (Random.value < 0.8f) {
                            BuildHouse(root.transform, pos, wallMat, woodMat, holeMat, crate, barrel);
                        } else {
                            PlacePlaza(root.transform, pos, trees);
                            if (Random.value > 0.9f && zombie) SpawnZombie(zombie, root.transform, pos);
                        }
                    }
                    currentZ += block + streetZ;
                }
                currentX += block + streetX;
            }

            var surface = root.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.RenderMeshes;
            // Optimize for large cities
            surface.overrideVoxelSize = true;
            surface.voxelSize = 0.2f; 
            surface.BuildNavMesh();

            FixPlayerAndWeapons();
            StaticBatchingUtility.Combine(root);
        }

        private void SetupEnvironment()
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.92f, 0.88f, 0.7f); 
            RenderSettings.fogDensity = 0.0035f;
            RenderSettings.ambientLight = new Color(0.45f, 0.42f, 0.38f);
            
            var sun = GameObject.Find("Directional Light");
            if (sun) {
                var l = sun.GetComponent<Light>();
                if (l) { l.color = new Color(1, 0.95f, 0.85f); l.intensity = 1.3f; }
            }

            // Pyramids Glow Visibility - CRANKED UP
            var pyramids = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var p in pyramids) {
                if (p.name.ToLower().Contains("pyramid")) {
                    p.transform.localScale *= 1.5f; // Scale up for visibility
                    foreach (var r in p.GetComponentsInChildren<Renderer>()) {
                        foreach (var m in r.materials) {
                            m.SetColor("_EmissionColor", new Color(1f, 0.9f, 0.5f) * 4.0f); // Stronger glow
                            m.EnableKeyword("_EMISSION");
                        }
                    }
                }
            }
        }


        private void BuildHouse(Transform parent, Vector3 pos, Material wall, Material wood, Material hole, GameObject crate, GameObject barrel)
        {
            var h = new GameObject("House");
            h.transform.SetParent(parent); h.transform.position = pos;
            h.isStatic = true;

            float height = Random.Range(18, 30);
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(h.transform);
            body.transform.localPosition = new Vector3(0, height/2, 0);
            body.transform.localScale = new Vector3(20, height, 20);
            body.GetComponent<Renderer>().sharedMaterial = wall;

            // Recessed Windows - Deeper interior feel
            for (int i = 0; i < 4; i++) {
                float rot = i * 90f;
                var win = GameObject.CreatePrimitive(PrimitiveType.Cube);
                win.transform.SetParent(body.transform);
                win.transform.localScale = new Vector3(0.18f, 0.22f, 0.15f); // Slightly thicker/deeper
                win.GetComponent<Renderer>().sharedMaterial = hole;
                DestroyImmediate(win.GetComponent<Collider>());
                float rad = rot * Mathf.Deg2Rad;
                // Move further in: 0.45f instead of 0.485f
                win.transform.localPosition = new Vector3(Mathf.Cos(rad) * 0.45f, 0.3f, Mathf.Sin(rad) * 0.45f);
                win.transform.localRotation = Quaternion.Euler(0, -rot + 90, 0);
            }

            // Door
            var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.transform.SetParent(h.transform);
            door.transform.localPosition = new Vector3(0, 4, 10.1f);
            door.transform.localScale = new Vector3(6, 8, 0.2f);
            door.GetComponent<Renderer>().sharedMaterial = wood;

            // Props
            if (Random.value > 0.5f) {
                if (crate) InstantiateProp(crate, pos + new Vector3(12, 0, 8), h.transform);
                if (barrel) InstantiateProp(barrel, pos + new Vector3(12, 0, 11), h.transform);
            }
        }

        private void InstantiateProp(GameObject prefab, Vector3 pos, Transform parent)
        {
            var p = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            p.transform.position = pos; p.transform.localScale = Vector3.one * 0.3f;
            p.transform.rotation = Quaternion.Euler(-90, 0, 0);
            p.AddComponent<Rigidbody>().mass = 20f;
            if (p.GetComponent<Collider>() == null) p.AddComponent<BoxCollider>();
        }

        private void PlacePlaza(Transform parent, Vector3 pos, GameObject[] trees)
        {
            var p = new GameObject("Plaza");
            p.transform.SetParent(parent); p.transform.position = pos;
            if (trees.Length > 0) {
                var t = (GameObject)PrefabUtility.InstantiatePrefab(trees[Random.Range(0, trees.Length)], p.transform);
                t.transform.localScale = Vector3.one * 8f; t.transform.rotation = Quaternion.Euler(-90, 0, 0);
            }
        }

        private void SpawnZombie(GameObject prefab, Transform parent, Vector3 pos)
        {
            var z = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            z.transform.position = pos + Vector3.up; z.SetActive(true);
            if (z.GetComponent<TheAlchemistsCrypt.AI.ZombieAI>() == null)
                z.AddComponent<TheAlchemistsCrypt.AI.ZombieAI>();
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
    }
}
