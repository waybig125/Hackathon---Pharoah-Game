using UnityEngine;
using UnityEditor;
using Unity.AI.Navigation;
using System.Collections.Generic;

namespace TheAlchemistsCrypt.Editor
{
    public class StaticEgyptianCityGenerator : EditorWindow
    {
        [MenuItem("Tools/Generate Egyptian City (V3 - Final)")]
        public static void ShowWindow() =>
            GetWindow<StaticEgyptianCityGenerator>("Egyptian City V3");

        private int seed = 777;
        private int gridSize = 12;
        private float streetWidth = 18f; // WIDER STREETS
        private float blockSize = 24f;
        private float houseDensity = 0.8f;
        
        private string rootName = "EgyptianCity_V3";

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("High-Fidelity Egyptian Horror Environment. Fixed Grounding, Windows, Doors, and AI.", MessageType.Info);
            seed = EditorGUILayout.IntField("Seed", seed);
            gridSize = EditorGUILayout.IntSlider("Grid Size", gridSize, 5, 20);
            streetWidth = EditorGUILayout.Slider("Street Width", streetWidth, 10, 30);
            
            if (GUILayout.Button("▶ GENERATE FINAL CITY", GUILayout.Height(50))) GenerateCity();
            if (GUILayout.Button("🗑 CLEANUP", GUILayout.Height(30))) Purge();
        }

        private void Purge()
        {
            var old = GameObject.Find(rootName);
            if (old != null) Undo.DestroyObjectImmediate(old);
        }

        private void GenerateCity()
        {
            Random.InitState(seed);
            Purge();

            var root = new GameObject(rootName);
            Undo.RegisterCreatedObjectUndo(root, "Gen City");
            root.isStatic = true;
            root.AddComponent<TheAlchemistsCrypt.Environment.AmbientHorrorSFX>();

            // Asset Loading
            var trees = new GameObject[] {
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/realistic_hd_date_palm_2178.glb"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/realistic_hd_date_palm_378.glb"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/realistic_hd_date_palm_4778.glb")
            };
            var pillar = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/egyptian_pillar_column.glb"); 
            var zombie = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TestZombie.prefab");
            var crate = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/crate.glb");
            var barrel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/barrel.glb");
            
            Texture2D wallN = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EgyptianAssets/desert_sand_normal.png");
            Texture2D floorN1 = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EgyptianAssets/desert_sand_normal.png");
            Texture2D floorN2 = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Textures/EgyptianNormalMap.png");

            Material wallMat = CreateLit(new Color(0.88f, 0.8f, 0.65f), wallN, 2f);
            Material floorMat = CreateLayeredFloor(new Color(0.85f, 0.78f, 0.65f), floorN1, floorN2);
            Material woodMat = CreateLit(new Color(0.35f, 0.22f, 0.12f), null, 1f); // Dark Brown
            Material darkWindowMat = CreateLit(new Color(0.15f, 0.1f, 0.05f), null, 1f); // Recessed Window
            Material emissiveWindow = CreateLit(Color.black, null, 1f, true);

            SetupLighting();

            float totalSize = gridSize * (blockSize + streetWidth);
            
            // GROUND
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "SOLID_GROUND";
            floor.transform.SetParent(root.transform);
            floor.transform.localScale = new Vector3(totalSize / 5f, 1f, totalSize / 5f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;
            floor.isStatic = true;

            // PYRAMIDS (Visible)
            CreatePyramid(root, new Vector3(-totalSize, -10, totalSize), 400f, wallMat, new Color(0.8f, 0.4f, 1f));
            CreatePyramid(root, new Vector3(totalSize, -10, -totalSize), 500f, wallMat, new Color(1f, 0.5f, 0f));

            float start = -totalSize * 0.5f + blockSize * 0.5f;
            for (int x = 0; x < gridSize; x++) {
                for (int z = 0; z < gridSize; z++) {
                    float px = start + x * (blockSize + streetWidth);
                    float pz = start + z * (blockSize + streetWidth);
                    var pos = new Vector3(px, 0, pz);

                    if (Vector3.Distance(pos, Vector3.zero) < 50f) continue;

                    int rand = Random.Range(0, 100);
                    if (rand > houseDensity * 100) {
                        PlacePlaza(root.transform, pos, trees, pillar, crate, barrel);
                        if (rand > 95 && zombie) SpawnZombie(zombie, root.transform, pos);
                        continue;
                    }

                    PlaceDetailedHouse(root.transform, pos, wallMat, woodMat, darkWindowMat, emissiveWindow, crate, barrel);
                }
            }

            // NAVMESH
            var surface = root.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.BuildNavMesh();

            TintWeapons();

            StaticBatchingUtility.Combine(root);
            Debug.Log("Egyptian City V3 Generated Successfully.");
        }

        private void SetupLighting()
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.7f, 0.5f, 0.2f);
            RenderSettings.fogDensity = 0.005f; // LESS FOG
            RenderSettings.fogStartDistance = 50f;
            RenderSettings.fogEndDistance = 800f;
            
            RenderSettings.ambientLight = new Color(0.35f, 0.32f, 0.28f);
        }

        private void CreatePyramid(GameObject root, Vector3 pos, float size, Material mat, Color glow)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.name = "GreatPyramid";
            p.transform.SetParent(root.transform);
            p.transform.position = pos + Vector3.up * (size * 0.4f);
            p.transform.localScale = new Vector3(size, size * 0.8f, size);
            p.transform.rotation = Quaternion.Euler(0, 45, 0);
            p.GetComponent<Renderer>().sharedMaterial = mat;
            
            var l = new GameObject("PyramidLight").AddComponent<Light>();
            l.transform.SetParent(p.transform); l.transform.localPosition = Vector3.up * 0.6f;
            l.type = LightType.Point; l.color = glow; l.intensity = 500f; l.range = 1000f;
        }

        private void PlaceDetailedHouse(Transform parent, Vector3 pos, Material wall, Material wood, Material darkWin, Material emissiveWin, GameObject crate, GameObject barrel)
        {
            var house = new GameObject("House");
            house.transform.SetParent(parent); house.transform.position = pos;
            house.isStatic = true;

            float h = Random.Range(15, 25);
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(house.transform);
            body.transform.localPosition = new Vector3(0, h/2, 0);
            body.transform.localScale = new Vector3(20, h, 20);
            body.GetComponent<Renderer>().sharedMaterial = wall;
            DestroyImmediate(body.GetComponent<Collider>());

            // DOOR
            var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "Door";
            door.transform.SetParent(house.transform);
            door.transform.localPosition = new Vector3(0, 4, 10.05f);
            door.transform.localScale = new Vector3(5, 8, 0.2f);
            door.GetComponent<Renderer>().sharedMaterial = wood;

            // WINDOWS (Recessed Holes)
            AddRecessedWindows(body.transform, darkWin, emissiveWin);

            // PROPS (Outside in the yard)
            if (Random.value > 0.4f) {
                var propPos = pos + new Vector3(14, 0, 8);
                if (Random.value > 0.5f && crate) InstantiateProp(crate, propPos, house.transform);
                else if (barrel) InstantiateProp(barrel, propPos, house.transform);
            }

            var bc = house.AddComponent<BoxCollider>();
            bc.center = new Vector3(0, h/2, 0); bc.size = new Vector3(22, h, 22);
        }

        private void AddRecessedWindows(Transform body, Material dark, Material emissive)
        {
            for (int i = 0; i < 4; i++) {
                float angle = i * 90f;
                var win = GameObject.CreatePrimitive(PrimitiveType.Cube);
                win.transform.SetParent(body);
                win.transform.localScale = new Vector2(0.2f, 0.25f);
                win.transform.localScale = new Vector3(0.25f, 0.25f, 0.1f);
                
                bool isOpen = Random.value > 0.7f;
                win.GetComponent<Renderer>().sharedMaterial = isOpen ? emissive : dark;
                DestroyImmediate(win.GetComponent<Collider>());

                float rad = angle * Mathf.Deg2Rad;
                win.transform.localPosition = new Vector3(Mathf.Cos(rad) * 0.501f, 0.2f, Mathf.Sin(rad) * 0.501f);
                win.transform.localRotation = Quaternion.Euler(0, -angle + 90, 0);

                if (isOpen) {
                    var l = new GameObject("Glow").AddComponent<Light>();
                    l.transform.SetParent(win.transform); l.transform.localPosition = Vector3.zero;
                    l.type = LightType.Point; l.intensity = 1.5f; l.range = 5f;
                    l.color = (Random.value > 0.6f) ? new Color(1, 0.5f, 0) : new Color(0, 0.8f, 1);
                }
            }
        }

        private void InstantiateProp(GameObject prefab, Vector3 pos, Transform parent)
        {
            var count = Random.Range(1, 3);
            for (int i = 0; i < count; i++) {
                var p = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                p.transform.position = pos + new Vector3(i * 1.5f, 0, 0);
                p.transform.localScale = Vector3.one * 0.25f;
                p.transform.rotation = Quaternion.Euler(-90, Random.Range(0, 360), 0);
                
                // Add Physics
                var rb = p.AddComponent<Rigidbody>();
                rb.mass = 20f; rb.drag = 1f; rb.angularDrag = 1f;
                var coll = p.GetComponent<Collider>();
                if (coll == null) p.AddComponent<MeshCollider>().convex = true;
            }
        }

        private void PlacePlaza(Transform parent, Vector3 pos, GameObject[] trees, GameObject pillar, GameObject crate, GameObject barrel)
        {
            var plaza = new GameObject("Plaza");
            plaza.transform.SetParent(parent); plaza.transform.position = pos;
            
            if (trees.Length > 0 && Random.value > 0.3f) {
                var t = (GameObject)PrefabUtility.InstantiatePrefab(trees[Random.Range(0, trees.Length)], plaza.transform);
                t.transform.localScale = Vector3.one * 6f;
                t.transform.rotation = Quaternion.Euler(-90, 0, 0);
            } else if (pillar) {
                var p = (GameObject)PrefabUtility.InstantiatePrefab(pillar, plaza.transform);
                p.transform.localScale = Vector3.one * 0.7f;
                p.transform.rotation = Quaternion.Euler(-90, 0, 0);
            }
        }

        private void SpawnZombie(GameObject prefab, Transform parent, Vector3 pos)
        {
            var z = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            z.transform.position = pos + Vector3.up;
            z.SetActive(true);
            if (z.GetComponent<TheAlchemistsCrypt.AI.ZombieAI>() == null)
                z.AddComponent<TheAlchemistsCrypt.AI.ZombieAI>();
        }

        private void TintWeapons()
        {
            var inv = GameObject.FindObjectOfType<InfimaGames.LowPolyShooterPack.Inventory>();
            if (inv == null) return;

            // Sulfur (Red), Salt (White), Mercury (Aqua)
            Color[] colors = { new Color(1, 0.4f, 0), Color.white, new Color(0, 0.8f, 1) };
            int i = 0;
            foreach (Transform t in inv.transform) {
                var renderers = t.GetComponentsInChildren<Renderer>();
                Color c = colors[i % colors.Length];
                foreach (var r in renderers) {
                    foreach (var m in r.sharedMaterials) {
                        m.SetColor("_EmissionColor", c * 2f);
                        m.EnableKeyword("_EMISSION");
                    }
                }
                i++;
            }
        }

        private Material CreateLit(Color c, Texture2D n, float tile, bool emissive = false) {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = c;
            if (n) { mat.SetTexture("_BumpMap", n); mat.EnableKeyword("_NORMALMAP"); }
            if (emissive) { mat.SetColor("_EmissionColor", new Color(0.8f, 0.4f, 0) * 2f); mat.EnableKeyword("_EMISSION"); }
            mat.mainTextureScale = new Vector2(tile, tile);
            return mat;
        }

        private Material CreateLayeredFloor(Color c, Texture2D sN, Texture2D eN) {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = c;
            if (eN) { mat.SetTexture("_BumpMap", eN); mat.EnableKeyword("_NORMALMAP"); mat.SetFloat("_BumpScale", 0.35f); }
            if (sN) { mat.SetTexture("_DetailNormalMap", sN); mat.EnableKeyword("_DETAIL_MULX2"); mat.SetFloat("_DetailNormalMapScale", 1.2f); }
            mat.mainTextureScale = new Vector2(100, 100);
            return mat;
        }
    }
}
