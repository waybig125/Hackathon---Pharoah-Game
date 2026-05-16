using UnityEngine;
using UnityEditor;
using Unity.AI.Navigation;
using System.Collections.Generic;

namespace TheAlchemistsCrypt.Editor
{
    public class StaticEgyptianCityGenerator : EditorWindow
    {
        [MenuItem("Tools/Generate Egyptian City (Static)")]
        public static void ShowWindow() =>
            GetWindow<StaticEgyptianCityGenerator>("Static Egyptian City");

        private int seed = 42;
        private int gridSize = 14;
        private float houseDensity = 0.85f;
        private bool bakeNavMesh = true;
        private string rootName = "EgyptianCity_Static";
        
        private float blockSize = 24f;
        private float streetWidth = 12f;

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Deterministic Mobile Egyptian Horror City Builder. Optimized for High-Fidelity.", MessageType.Info);

            seed = EditorGUILayout.IntField("Random Seed", seed);
            gridSize = EditorGUILayout.IntSlider("Grid Size", gridSize, 5, 25);
            houseDensity = EditorGUILayout.Slider("House Density", houseDensity, 0.1f, 1.0f);
            bakeNavMesh = EditorGUILayout.Toggle("Bake NavMesh", bakeNavMesh);
            
            if (GUILayout.Button("▶  Generate Optimized City", GUILayout.Height(44)))
                GenerateCity();

            if (GUILayout.Button("🗑  Cleanup City", GUILayout.Height(30)))
                Purge();
        }

        private void Purge()
        {
            var old = GameObject.Find(rootName);
            if (old != null) Undo.DestroyObjectImmediate(old);
            Debug.Log("City purged.");
        }

        private void GenerateCity()
        {
            Random.InitState(seed);
            Purge();

            var root = new GameObject(rootName);
            Undo.RegisterCreatedObjectUndo(root, "Generate City");
            root.isStatic = true; 
            root.AddComponent<TheAlchemistsCrypt.Environment.AmbientHorrorSFX>();

            // Load Assets
            var t1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/realistic_hd_date_palm_2178.glb");
            var t2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/realistic_hd_date_palm_378.glb");
            var t3 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/realistic_hd_date_palm_4778.glb");
            var pillar = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/egyptian_pillar_column.glb"); 
            var zombie = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TestZombie.prefab");
            var crate = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/crate.glb");
            var barrel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/barrel.glb");
            
            // Textures
            Texture2D wallN = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EgyptianAssets/desert_sand_normal.png");
            Texture2D floorN1 = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EgyptianAssets/desert_sand_normal.png");
            Texture2D floorN2 = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Textures/EgyptianNormalMap.png");
            
            Material wallMat = CreateURPLitMaterial(new Color(0.9f, 0.82f, 0.68f), wallN, 2f);
            Material floorMat = CreateLayeredFloorMaterial(new Color(0.85f, 0.78f, 0.65f), floorN1, floorN2);
            Material woodMat = CreateURPLitMaterial(new Color(0.38f, 0.25f, 0.12f), null, 1f);
            Material blackMat = CreateURPLitMaterial(Color.black, null, 1f);

            SetAtmosphere();

            float totalSize = gridSize * (blockSize + streetWidth);
            
            // Solid Floor
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "DesertFloor_Solid";
            floor.transform.SetParent(root.transform);
            floor.transform.localScale = new Vector3(totalSize / 5f, 1f, totalSize / 5f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;
            floor.isStatic = true;

            // Pyramids
            GenerateBetterPyramid(root, new Vector3(-totalSize * 1.6f, -15f, totalSize * 1.6f), 450f, wallMat, false);
            GenerateBetterPyramid(root, new Vector3(totalSize * 2f, -15f, -totalSize * 2.2f), 650f, wallMat, false);
            GenerateBetterPyramid(root, new Vector3(0, -15f, totalSize * 2.8f), 850f, wallMat, true);

            float start = -totalSize * 0.5f + blockSize * 0.5f;
            for (int x = 0; x < gridSize; x++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    float px = start + x * (blockSize + streetWidth);
                    float pz = start + z * (blockSize + streetWidth);
                    var pos = new Vector3(px, 0, pz);

                    if (Vector3.Distance(pos, Vector3.zero) < 60f) continue;

                    int pattern = (x * 7 + z * 3 + seed) % 10;
                    if (pattern > houseDensity * 10) {
                        PlaceOpenPlotFancy(root.transform, pos, t1, t2, t3, pillar, crate, barrel, pattern);
                        if (pattern == 9 && zombie) SpawnZombie(zombie, root.transform, pos);
                        continue;
                    }

                    PlaceComplexHouseDetailed(root.transform, pos, wallMat, woodMat, blackMat, crate, barrel, pattern);
                }
            }

            if (bakeNavMesh) {
                var surface = root.AddComponent<NavMeshSurface>();
                surface.collectObjects = CollectObjects.Children;
                surface.BuildNavMesh();
            }

            StaticBatchingUtility.Combine(root);
            Debug.Log("Optimized Egyptian City Generated.");
        }

        private static void SetAtmosphere()
        {
            Color fogCol = new Color(0.72f, 0.52f, 0.18f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = fogCol; RenderSettings.fogDensity = 0.01f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.38f, 0.32f);

            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include)) {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = fogCol;
            }
        }

        private static void GenerateBetterPyramid(GameObject root, Vector3 pos, float size, Material mat, bool horror)
        {
            GameObject pyr = new GameObject(horror ? "HorrorPyramid" : "Pyramid");
            pyr.transform.SetParent(root.transform);
            pyr.transform.position = pos;

            int steps = 6;
            float height = size * 0.8f;
            float stepH = height / steps;
            for (int i = 0; i < steps; i++) {
                float s = size * (1f - (float)i / steps);
                var step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.transform.SetParent(pyr.transform);
                step.transform.localPosition = new Vector3(0, i * stepH + stepH/2f, 0);
                step.transform.localScale = new Vector3(s, stepH, s);
                step.GetComponent<Renderer>().sharedMaterial = mat;
                DestroyImmediate(step.GetComponent<Collider>());
            }

            if (horror) {
                var l = new GameObject("PeakLight").AddComponent<Light>();
                l.transform.SetParent(pyr.transform);
                l.transform.localPosition = new Vector3(0, height + 15f, 0);
                l.type = LightType.Point; l.color = new Color(0.7f, 0.1f, 1f);
                l.intensity = 80f; l.range = 400f;
            }
        }

        private static void PlaceOpenPlotFancy(Transform parent, Vector3 pos, GameObject t1, GameObject t2, GameObject t3, GameObject pillar, GameObject crate, GameObject barrel, int pattern)
        {
            GameObject plot = new GameObject("OpenPlot");
            plot.transform.SetParent(parent); plot.transform.position = pos;

            if (pattern % 3 == 0 && pillar) {
                var p = (GameObject)PrefabUtility.InstantiatePrefab(pillar, plot.transform);
                p.transform.rotation = Quaternion.Euler(-90, pattern * 10, 0);
                p.transform.localScale = Vector3.one * 0.6f;
            } else {
                GameObject prefab = (pattern % 2 == 0) ? t1 : (pattern % 5 == 0 ? t3 : t2);
                if (prefab) {
                    var t = (GameObject)PrefabUtility.InstantiatePrefab(prefab, plot.transform);
                    t.transform.rotation = Quaternion.Euler(-90, pattern * 15, 0);
                    t.transform.localScale = Vector3.one * 5.5f;
                }
            }
            
            // Extra Props
            for (int i = 0; i < 3; i++) {
                if ((pattern + i) % 2 == 0 && crate) {
                    var c = (GameObject)PrefabUtility.InstantiatePrefab(crate, plot.transform);
                    c.transform.localPosition = new Vector3(Random.Range(-5, 5), 0, Random.Range(-5, 5));
                    c.transform.localScale = Vector3.one * 0.2f;
                }
            }
        }

        private static void PlaceComplexHouseDetailed(Transform parent, Vector3 pos, Material wall, Material wood, Material black, GameObject crate, GameObject barrel, int pattern)
        {
            var house = new GameObject("House_Complex");
            house.transform.SetParent(parent); house.transform.position = pos;
            house.isStatic = true;

            int mods = (pattern % 2) + 1;
            float h = 15f + (pattern % 3) * 3f;
            Color windowCol = (pattern % 3 == 0) ? new Color(1f, 0.4f, 0.1f) : // Sulfur Red/Orange
                             (pattern % 3 == 1) ? new Color(0.2f, 0.8f, 1f) : // Mercury Aqua
                             Color.white; // Salt White

            for (int i = 0; i < mods; i++) {
                var mod = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mod.transform.SetParent(house.transform);
                mod.transform.localPosition = new Vector3((i - (mods-1)*0.5f)*12f, h/2f, 0);
                mod.transform.localScale = new Vector3(20f, h, 20f);
                mod.GetComponent<Renderer>().sharedMaterial = wall;
                DestroyImmediate(mod.GetComponent<Collider>());
                AddWindowsAndGlows(mod.transform, black, windowCol);
            }

            if (pattern % 4 == 0) {
                var top = GameObject.CreatePrimitive(PrimitiveType.Cube);
                top.transform.SetParent(house.transform);
                top.transform.localPosition = new Vector3(0, h + 6f, 0);
                top.transform.localScale = new Vector3(14f, 12f, 14f);
                top.GetComponent<Renderer>().sharedMaterial = wall;
                DestroyImmediate(top.GetComponent<Collider>());
            }

            if (h > 20f) CreateLadder(house.transform, h, wood);

            // House Props
            if (crate) {
                var c = (GameObject)PrefabUtility.InstantiatePrefab(crate, house.transform);
                c.transform.localPosition = new Vector3(12f, 0.5f, 5f); c.transform.localScale = Vector3.one * 0.22f;
            }
            if (barrel) {
                var b = (GameObject)PrefabUtility.InstantiatePrefab(barrel, house.transform);
                b.transform.localPosition = new Vector3(-12f, 0.5f, -5f); b.transform.localScale = Vector3.one * 0.22f;
            }

            var bc = house.AddComponent<BoxCollider>();
            bc.center = new Vector3(0, h * 0.5f, 0); bc.size = new Vector3(30f, h * 2f, 30f);
        }

        private static void AddWindowsAndGlows(Transform mod, Material black, Color glow) {
            for (int i = 0; i < 4; i++) {
                var win = GameObject.CreatePrimitive(PrimitiveType.Cube);
                win.transform.SetParent(mod);
                win.transform.localScale = new Vector3(0.18f, 0.25f, 0.18f);
                win.GetComponent<Renderer>().sharedMaterial = black;
                DestroyImmediate(win.GetComponent<Collider>());
                float angle = i * 90f;
                win.transform.localPosition = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * 0.505f, 0.3f, Mathf.Sin(angle * Mathf.Deg2Rad) * 0.505f);

                var l = new GameObject("WinLight").AddComponent<Light>();
                l.transform.SetParent(win.transform); l.transform.localPosition = Vector3.zero;
                l.type = LightType.Point; l.color = glow; l.intensity = 2.5f; l.range = 7f;
            }
        }

        private static void CreateLadder(Transform parent, float h, Material mat) {
            var ladder = new GameObject("Ladder");
            ladder.transform.SetParent(parent); ladder.transform.localPosition = new Vector3(10.2f, 0, 4f);
            for (int i = 0; i < (int)h; i++) {
                var rung = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rung.transform.SetParent(ladder.transform);
                rung.transform.localPosition = new Vector3(0, i + 0.5f, 0);
                rung.transform.localScale = new Vector3(0.2f, 0.15f, 2.5f);
                rung.GetComponent<Renderer>().sharedMaterial = mat;
                DestroyImmediate(rung.GetComponent<Collider>());
            }
        }

        private static void SpawnZombie(GameObject prefab, Transform parent, Vector3 pos) {
            var z = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            z.transform.position = pos + Vector3.up; z.SetActive(true);
        }

        private static Material CreateURPLitMaterial(Color color, Texture2D normal, float tiling) {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Lit"));
            mat.color = color;
            if (normal) { mat.SetTexture("_BumpMap", normal); mat.EnableKeyword("_NORMALMAP"); }
            mat.mainTextureScale = new Vector2(tiling, tiling);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0f);
            return mat;
        }

        private static Material CreateLayeredFloorMaterial(Color color, Texture2D sandN, Texture2D egyptN) {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Lit"));
            mat.color = color;
            // Primary normal: Egyptian Hieroglyphics (Low strength)
            if (egyptN) { 
                mat.SetTexture("_BumpMap", egyptN); 
                mat.EnableKeyword("_NORMALMAP");
                mat.SetFloat("_BumpScale", 0.35f); // Submerged in sand
            }
            // Detail normal: Sand ripples
            if (sandN) {
                mat.SetTexture("_DetailNormalMap", sandN);
                mat.EnableKeyword("_DETAIL_MULX2");
                mat.SetFloat("_DetailNormalMapScale", 1.0f);
            }
            mat.mainTextureScale = new Vector2(150, 150);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0f);
            return mat;
        }
    }
}
