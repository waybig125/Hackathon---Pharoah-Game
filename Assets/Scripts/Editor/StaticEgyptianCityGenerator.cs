using UnityEngine;
using UnityEditor;
using Unity.AI.Navigation;
using System.Collections.Generic;

namespace TheAlchemistsCrypt.Editor
{
    public class StaticEgyptianCityGenerator : EditorWindow
    {
        [MenuItem("Tools/Generate Egyptian City (V4 - Final)")]
        public static void ShowWindow() => GetWindow<StaticEgyptianCityGenerator>("Egyptian City V4.2");

        private int seed = 999;
        private int gridSize = 12;
        private string rootName = "EgyptianCity_V4_Final";

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("V4.2 POLISHED: Glow Pyramids, Multi-Layered Floor, Purged Duplicates.", MessageType.Info);
            seed = EditorGUILayout.IntField("Seed", seed);
            if (GUILayout.Button("▶ GENERATE POLISHED CITY", GUILayout.Height(50))) GenerateCity();
            if (GUILayout.Button("🗑 CLEANUP", GUILayout.Height(30))) Purge();
        }

        private void Purge()
        {
            var old = GameObject.Find(rootName);
            if (old != null) Undo.DestroyObjectImmediate(old);
            
            // AGGRESSIVE PURGE OF CONFLICTING OBJECTS
            foreach (var go in GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include)) {
                if (go.name.Contains("Player_Copy") || go.name.Contains("MobileHUD") || go.name.Contains("P_LPSP_UI_Canvas")) {
                    DestroyImmediate(go);
                }
            }
        }

        private void GenerateCity()
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
            Material floorMat = CreateLit(new Color(0.85f, 0.75f, 0.5f), 10f, "desert_sand_normal.png"); // Will be updated with normal maps below
            Material woodMat = CreateLit(new Color(0.25f, 0.15f, 0.08f), 1f);
            Material holeMat = CreateLit(new Color(0.05f, 0.03f, 0.01f), 1f);

            SetupEnvironment();
            ApplyFloorTextures(floorMat);

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

            root.AddComponent<NavMeshSurface>().BuildNavMesh();
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

            // Pyramids Glow Visibility
            var pyramids = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var p in pyramids) {
                if (p.name.ToLower().Contains("pyramid")) {
                    foreach (var r in p.GetComponentsInChildren<Renderer>()) {
                        foreach (var m in r.materials) {
                            m.SetColor("_EmissionColor", new Color(1f, 0.8f, 0.4f) * 1.5f);
                            m.EnableKeyword("_EMISSION");
                        }
                    }
                }
            }
        }

        private void ApplyFloorTextures(Material floor)
        {
            var sandNormal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EgyptianAssets/desert_sand_normal.png");
            var egyptNormal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Textures/EgyptianNormalMap.png");
            
            if (sandNormal != null) floor.SetTexture("_BumpMap", sandNormal);
            floor.EnableKeyword("_NORMALMAP");
            // If we want both, we'd need a custom shader or a layered material, 
            // for now we'll favor the sand normal and set a repeating tile.
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

            // Recessed Windows
            for (int i = 0; i < 4; i++) {
                float rot = i * 90f;
                var win = GameObject.CreatePrimitive(PrimitiveType.Cube);
                win.transform.SetParent(body.transform);
                win.transform.localScale = new Vector3(0.18f, 0.22f, 0.1f);
                win.GetComponent<Renderer>().sharedMaterial = hole;
                DestroyImmediate(win.GetComponent<Collider>());
                float rad = rot * Mathf.Deg2Rad;
                win.transform.localPosition = new Vector3(Mathf.Cos(rad) * 0.485f, 0.3f, Mathf.Sin(rad) * 0.485f);
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
                    foreach (var m in r.materials) {
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
