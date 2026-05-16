using UnityEngine;
using UnityEditor;
using Unity.AI.Navigation;
using System.Collections.Generic;

namespace TheAlchemistsCrypt.Editor
{
    public class StaticEgyptianCityGenerator : EditorWindow
    {
        [MenuItem("Tools/Generate Egyptian City (V4 - Final)")]
        public static void ShowWindow() => GetWindow<StaticEgyptianCityGenerator>("Egyptian City V4");

        private int seed = 888;
        private int gridSize = 12;
        private float houseDensity = 0.85f;
        private string rootName = "EgyptianCity_V4";
        
        private float defaultBlock = 24f;
        private float defaultStreet = 16f;

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("High-Fidelity V4: Recessed Windows, Mixed Street Widths, Correct Tints.", MessageType.Info);
            seed = EditorGUILayout.IntField("Seed", seed);
            gridSize = EditorGUILayout.IntSlider("Grid Size", gridSize, 5, 20);
            
            if (GUILayout.Button("▶ GENERATE FINAL V4 CITY", GUILayout.Height(50))) GenerateCity();
            if (GUILayout.Button("🗑 CLEANUP", GUILayout.Height(30))) Purge();
        }

        private void Purge()
        {
            var old = GameObject.Find(rootName);
            if (old != null) Undo.DestroyObjectImmediate(old);
            
            var playerCopy = GameObject.Find("Player_Copy");
            if (playerCopy != null) Undo.DestroyObjectImmediate(playerCopy);
        }

        private void GenerateCity()
        {
            Random.InitState(seed);
            Purge();

            var root = new GameObject(rootName);
            Undo.RegisterCreatedObjectUndo(root, "Gen City V4");
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

            Material wallMat = CreateLit(new Color(0.92f, 0.85f, 0.7f), wallN, 2.5f);
            Material floorMat = CreateLayeredFloor(new Color(0.88f, 0.82f, 0.72f), floorN1, floorN2);
            Material woodMat = CreateLit(new Color(0.28f, 0.18f, 0.1f), null, 1f); 
            Material recessMat = CreateLit(new Color(0.12f, 0.08f, 0.04f), null, 1f); // VERY Dark Brown

            SetupAtmosphere();

            float totalSize = gridSize * (defaultBlock + defaultStreet + 10f); // Accommodate wider streets
            
            // GROUND
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "DesertFloor";
            floor.transform.SetParent(root.transform);
            floor.transform.localScale = new Vector3(totalSize / 5f, 1f, totalSize / 5f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;
            floor.isStatic = true;

            // PYRAMIDS (High Visibility)
            CreatePyramid(root, new Vector3(-totalSize * 0.8f, -10, totalSize * 0.8f), 500f, wallMat, new Color(0.6f, 0.2f, 1f));
            CreatePyramid(root, new Vector3(totalSize * 0.9f, -10, -totalSize * 0.7f), 700f, wallMat, new Color(1f, 0.4f, 0f));

            float currentX = -totalSize * 0.4f;
            for (int x = 0; x < gridSize; x++) {
                float streetX = (x % 4 == 0) ? defaultStreet * 3.5f : defaultStreet; // WIDER STREETS EVERY 4 BLOCKS
                float currentZ = -totalSize * 0.4f;
                for (int z = 0; z < gridSize; z++) {
                    float streetZ = (z % 5 == 0) ? defaultStreet * 3.0f : defaultStreet;

                    var pos = new Vector3(currentX, 0, currentZ);
                    if (Vector3.Distance(pos, Vector3.zero) > 40f) {
                        int r = Random.Range(0, 100);
                        if (r < houseDensity * 100) {
                            PlaceDetailedHouse(root.transform, pos, wallMat, woodMat, recessMat, crate, barrel);
                        } else {
                            PlacePlaza(root.transform, pos, trees, pillar, crate, barrel);
                            if (r > 96 && zombie) SpawnZombie(zombie, root.transform, pos);
                        }
                    }
                    currentZ += defaultBlock + streetZ;
                }
                currentX += defaultBlock + streetX;
            }

            // NAVMESH
            var surface = root.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.BuildNavMesh();

            TintWeapons();
            StaticBatchingUtility.Combine(root);
        }

        private void SetupAtmosphere()
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.85f, 0.8f, 0.5f); // YELLOWISH TINT
            RenderSettings.fogDensity = 0.0035f;
            RenderSettings.fogStartDistance = 80f;
            RenderSettings.fogEndDistance = 1200f;
            RenderSettings.ambientLight = new Color(0.4f, 0.38f, 0.35f);
        }

        private void CreatePyramid(GameObject root, Vector3 pos, float size, Material mat, Color glow)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.transform.SetParent(root.transform);
            p.transform.position = pos + Vector3.up * (size * 0.3f);
            p.transform.localScale = new Vector3(size, size * 0.6f, size);
            p.transform.rotation = Quaternion.Euler(0, 45, 0);
            p.GetComponent<Renderer>().sharedMaterial = mat;
            
            var l = new GameObject("PyramidGlow").AddComponent<Light>();
            l.transform.SetParent(p.transform); l.transform.localPosition = Vector3.up * 0.8f;
            l.type = LightType.Point; l.color = glow; l.intensity = 800f; l.range = 2000f;
        }

        private void PlaceDetailedHouse(Transform parent, Vector3 pos, Material wall, Material wood, Material recess, GameObject crate, GameObject barrel)
        {
            var house = new GameObject("House");
            house.transform.SetParent(parent); house.transform.position = pos;
            house.isStatic = true;

            float h = Random.Range(15, 30);
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(house.transform);
            body.transform.localPosition = new Vector3(0, h/2, 0);
            body.transform.localScale = new Vector3(20, h, 20);
            body.GetComponent<Renderer>().sharedMaterial = wall;
            DestroyImmediate(body.GetComponent<Collider>());

            // DOOR (Wooden)
            var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.transform.SetParent(house.transform);
            door.transform.localPosition = new Vector3(0, 4, 10.1f);
            door.transform.localScale = new Vector3(6, 8, 0.5f);
            door.GetComponent<Renderer>().sharedMaterial = wood;

            // RECESSED WINDOWS
            AddRecessedWindows(body.transform, recess);

            // PROPS (Clustered Outside)
            if (Random.value > 0.4f) {
                Vector3 propArea = pos + new Vector3(14, 0, 8);
                if (crate) InstantiateProp(crate, propArea, house.transform);
                if (barrel && Random.value > 0.5f) InstantiateProp(barrel, propArea + Vector3.right * 2f, house.transform);
            }

            var bc = house.AddComponent<BoxCollider>();
            bc.center = new Vector3(0, h/2, 0); bc.size = new Vector3(22, h, 22);
        }

        private void AddRecessedWindows(Transform body, Material recess)
        {
            for (int i = 0; i < 4; i++) {
                float angle = i * 90f;
                var win = GameObject.CreatePrimitive(PrimitiveType.Cube);
                win.transform.SetParent(body);
                win.transform.localScale = new Vector3(0.2f, 0.25f, 0.15f);
                win.GetComponent<Renderer>().sharedMaterial = recess;
                DestroyImmediate(win.GetComponent<Collider>());

                float rad = angle * Mathf.Deg2Rad;
                // Move window INTO the house
                win.transform.localPosition = new Vector3(Mathf.Cos(rad) * 0.48f, 0.3f, Mathf.Sin(rad) * 0.48f);
                win.transform.localRotation = Quaternion.Euler(0, -angle + 90, 0);

                if (Random.value > 0.75f) {
                    var l = new GameObject("InteriorGlow").AddComponent<Light>();
                    l.transform.SetParent(win.transform); l.transform.localPosition = Vector3.zero;
                    l.type = LightType.Point; l.intensity = 2f; l.range = 8f;
                    l.color = (Random.value > 0.5f) ? new Color(1, 0.4f, 0) : new Color(0, 0.7f, 1);
                }
            }
        }

        private void InstantiateProp(GameObject prefab, Vector3 pos, Transform parent)
        {
            int count = Random.Range(1, 4);
            for (int i = 0; i < count; i++) {
                var p = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                p.transform.position = pos + new Vector3(i * 1.8f, 0, Random.Range(-1, 1));
                p.transform.localScale = Vector3.one * 0.28f;
                p.transform.rotation = Quaternion.Euler(-90, Random.Range(0, 360), 0);
                
                var rb = p.AddComponent<Rigidbody>();
                rb.mass = 50f; rb.linearDamping = 0.5f;
                if (p.GetComponent<Collider>() == null) p.AddComponent<MeshCollider>().convex = true;
            }
        }

        private void PlacePlaza(Transform parent, Vector3 pos, GameObject[] trees, GameObject pillar, GameObject crate, GameObject barrel)
        {
            var plaza = new GameObject("Plaza");
            plaza.transform.SetParent(parent); plaza.transform.position = pos;
            if (trees.Length > 0 && Random.value > 0.4f) {
                var t = (GameObject)PrefabUtility.InstantiatePrefab(trees[Random.Range(0, trees.Length)], plaza.transform);
                t.transform.localScale = Vector3.one * 7f;
                t.transform.rotation = Quaternion.Euler(-90, 0, 0);
            } else if (pillar) {
                var p = (GameObject)PrefabUtility.InstantiatePrefab(pillar, plaza.transform);
                p.transform.localScale = Vector3.one * 0.8f; p.transform.rotation = Quaternion.Euler(-90, 0, 0);
            }
        }

        private void SpawnZombie(GameObject prefab, Transform parent, Vector3 pos)
        {
            var z = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            z.transform.position = pos + Vector3.up; z.SetActive(true);
            if (z.GetComponent<TheAlchemistsCrypt.AI.ZombieAI>() == null)
                z.AddComponent<TheAlchemistsCrypt.AI.ZombieAI>();
        }

        private void TintWeapons()
        {
            var inv = GameObject.FindObjectOfType<InfimaGames.LowPolyShooterPack.Inventory>();
            if (inv == null) return;
            Color[] colors = { new Color(1, 0.4f, 0), Color.white, new Color(0, 0.8f, 1) };
            int i = 0;
            foreach (Transform t in inv.transform) {
                foreach (var r in t.GetComponentsInChildren<Renderer>()) {
                    foreach (var m in r.sharedMaterials) {
                        m.SetColor("_EmissionColor", colors[i % 3] * 3f);
                        m.EnableKeyword("_EMISSION");
                        m.color = colors[i % 3] * 0.5f; // BASE COLOR TINT
                    }
                }
                i++;
            }
        }

        private Material CreateLit(Color c, Texture2D n, float tile) {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = c;
            if (n) { mat.SetTexture("_BumpMap", n); mat.EnableKeyword("_NORMALMAP"); }
            mat.mainTextureScale = new Vector2(tile, tile);
            return mat;
        }

        private Material CreateLayeredFloor(Color c, Texture2D sN, Texture2D eN) {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = c;
            if (eN) { mat.SetTexture("_BumpMap", eN); mat.EnableKeyword("_NORMALMAP"); mat.SetFloat("_BumpScale", 0.4f); }
            if (sN) { mat.SetTexture("_DetailNormalMap", sN); mat.EnableKeyword("_DETAIL_MULX2"); mat.SetFloat("_DetailNormalMapScale", 1.4f); }
            mat.mainTextureScale = new Vector2(100, 100);
            return mat;
        }
    }
}
