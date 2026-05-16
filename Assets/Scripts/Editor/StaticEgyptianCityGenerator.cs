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
        private float houseDensity = 0.8f;
        private bool bakeNavMesh = true;
        private string rootName = "EgyptianCity_Static";
        
        private float blockSize = 24f;
        private float streetWidth = 12f;

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Deterministic Mobile-Optimized Egyptian Horror City Builder.", MessageType.Info);

            seed = EditorGUILayout.IntField("Random Seed", seed);
            gridSize = EditorGUILayout.IntSlider("Grid Size", gridSize, 5, 25);
            houseDensity = EditorGUILayout.Slider("House Density", houseDensity, 0.1f, 1.0f);
            bakeNavMesh = EditorGUILayout.Toggle("Bake NavMesh", bakeNavMesh);
            
            if (GUILayout.Button("▶  Generate Deterministic City", GUILayout.Height(44)))
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
            var tree1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/realistic_hd_date_palm_2178.glb");
            var tree2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/realistic_hd_date_palm_378.glb");
            var tree3 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/realistic_hd_date_palm_4778.glb");
            var pillar = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/egyptian_pillar_column.glb"); 
            var zombiePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TestZombie.prefab");
            
            // Textures & Materials
            Texture2D wallNormal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EgyptianAssets/desert_sand_normal.png");
            Texture2D floorNormal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Textures/EgyptianNormalMap.png");
            
            Material wallMat = CreateURPLitMaterial(new Color(0.9f, 0.8f, 0.65f), wallNormal, 2f);
            Material floorMat = CreateURPLitMaterial(new Color(0.85f, 0.75f, 0.6f), floorNormal, 150f);
            Material woodMat = CreateURPLitMaterial(new Color(0.4f, 0.25f, 0.15f), null, 1f);
            Material blackMat = CreateURPLitMaterial(Color.black, null, 1f);

            SetAtmosphere();

            float totalSize = gridSize * (blockSize + streetWidth);
            
            // Floor
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "DesertFloor_Solid";
            floor.transform.SetParent(root.transform);
            floor.transform.localScale = new Vector3(totalSize / 5f, 1f, totalSize / 5f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;
            floor.isStatic = true;

            // Pyramid Backgrounds
            GenerateBetterPyramid(root, new Vector3(-totalSize * 1.5f, -10f, totalSize * 1.5f), 400f, wallMat, false);
            GenerateBetterPyramid(root, new Vector3(totalSize * 1.8f, -10f, -totalSize * 2.0f), 600f, wallMat, false);
            GenerateBetterPyramid(root, new Vector3(0, -10f, totalSize * 2.5f), 800f, wallMat, true); // The Horror Pyramid

            float start = -totalSize * 0.5f + blockSize * 0.5f;
            for (int x = 0; x < gridSize; x++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    float px = start + x * (blockSize + streetWidth);
                    float pz = start + z * (blockSize + streetWidth);
                    var pos = new Vector3(px, 0, pz);

                    // Central Plaza
                    if (Vector3.Distance(pos, Vector3.zero) < 60f) {
                        if (x == gridSize/2 && z == gridSize/2 && zombiePrefab) {
                            SpawnZombie(zombiePrefab, root.transform, pos);
                        }
                        continue;
                    }

                    int pattern = (x * 7 + z * 3) % 10;
                    if (pattern > houseDensity * 10) {
                        PlaceOpenPlotDeterministic(root.transform, pos, tree1, tree2, tree3, pillar, pattern);
                        if (pattern == 9 && zombiePrefab) SpawnZombie(zombiePrefab, root.transform, pos);
                        continue;
                    }

                    PlaceComplexHouseDeterministic(root.transform, pos, wallMat, woodMat, blackMat, pattern);
                }
            }

            if (bakeNavMesh) {
                var surface = root.AddComponent<NavMeshSurface>();
                surface.collectObjects = CollectObjects.Children;
                surface.BuildNavMesh();
            }

            StaticBatchingUtility.Combine(root);
            Debug.Log("Deterministic Egyptian City Generated Successfully.");
        }

        private static void SetAtmosphere()
        {
            Color fogCol = new Color(0.7f, 0.5f, 0.1f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = fogCol; 
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.skybox = null; // Forces flat/procedural look for mobile
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.4f, 0.35f, 0.3f);

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

            // Simplified massive steps for mobile
            int steps = 5;
            float height = size * 0.75f;
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
                GameObject lightObj = new GameObject("HorrorPeak");
                lightObj.transform.SetParent(pyr.transform);
                lightObj.transform.localPosition = new Vector3(0, height + 10f, 0);
                var l = lightObj.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(0.8f, 0.1f, 0.9f); // Purple glow
                l.intensity = 50f;
                l.range = 300f;
            }
        }

        private static void PlaceOpenPlotDeterministic(Transform parent, Vector3 pos, GameObject t1, GameObject t2, GameObject t3, GameObject pillar, int pattern)
        {
            if (pattern % 3 == 0 && pillar) {
                var p = (GameObject)PrefabUtility.InstantiatePrefab(pillar, parent);
                p.transform.position = pos;
                p.transform.localScale = Vector3.one * 0.5f;
                FixRotations(p);
            } else {
                GameObject prefab = (pattern % 2 == 0) ? t1 : (pattern % 5 == 0 ? t3 : t2);
                if (prefab) {
                    var t = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                    t.transform.position = pos;
                    t.transform.localScale = Vector3.one * 5f;
                    FixRotations(t);
                }
            }
        }

        private static void FixRotations(GameObject go) {
            // Palms and Pillars often come in sideways from GLB
            go.transform.rotation = Quaternion.Euler(-90, Random.Range(0, 360), 0);
        }

        private static void PlaceComplexHouseDeterministic(Transform parent, Vector3 pos, Material wall, Material wood, Material black, int pattern)
        {
            var house = new GameObject("House_Module");
            house.transform.SetParent(parent);
            house.transform.position = pos;
            house.isStatic = true;

            int modules = (pattern % 3) + 1;
            float h = 14f + (pattern % 5) * 2f;
            
            for (int i = 0; i < modules; i++) {
                var mod = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mod.transform.SetParent(house.transform);
                float offset = (i - (modules-1)*0.5f) * 10f;
                mod.transform.localPosition = new Vector3(offset, h/2f, 0);
                mod.transform.localScale = new Vector3(18f, h, 18f);
                mod.GetComponent<Renderer>().sharedMaterial = wall;
                DestroyImmediate(mod.GetComponent<Collider>());
                
                AddWindowsAndLights(mod.transform, black);
            }

            // 50% Second Story
            if (pattern % 2 == 0) {
                var top = GameObject.CreatePrimitive(PrimitiveType.Cube);
                top.transform.SetParent(house.transform);
                top.transform.localPosition = new Vector3(0, h + 5f, 0);
                top.transform.localScale = new Vector3(12f, 10f, 12f);
                top.GetComponent<Renderer>().sharedMaterial = wall;
                DestroyImmediate(top.GetComponent<Collider>());
            }

            // Horror Glow
            if (pattern == 5) {
                GameObject glow = new GameObject("HorrorGlow");
                glow.transform.SetParent(house.transform);
                glow.transform.localPosition = new Vector3(0, 2f, 0);
                var l = glow.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(0.2f, 0.9f, 0.1f);
                l.intensity = 5f; l.range = 15f;
            }

            // Ladder
            if (h > 20f && pattern % 2 != 0) {
                CreateProceduralLadder(house.transform, h, wood);
            }

            var bc = house.AddComponent<BoxCollider>();
            bc.center = new Vector3(0, h*0.5f, 0);
            bc.size = new Vector3(25f, h * 1.5f, 25f);
        }

        private static void AddWindowsAndLights(Transform mod, Material black) {
            for (int i = 0; i < 4; i++) {
                var win = GameObject.CreatePrimitive(PrimitiveType.Cube);
                win.transform.SetParent(mod);
                win.transform.localScale = new Vector3(0.15f, 0.2f, 0.15f);
                win.GetComponent<Renderer>().sharedMaterial = black;
                DestroyImmediate(win.GetComponent<Collider>());
                float angle = i * 90f;
                win.transform.localPosition = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * 0.505f, 0.2f, Mathf.Sin(angle * Mathf.Deg2Rad) * 0.505f);

                // Window Light
                GameObject lightObj = new GameObject("WindowLight");
                lightObj.transform.SetParent(win.transform);
                lightObj.transform.localPosition = Vector3.zero;
                var l = lightObj.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, 0.45f, 0.1f);
                l.intensity = 2f; l.range = 5f;
                l.shadows = LightShadows.None; // Mobile optimization
            }
        }

        private static void CreateProceduralLadder(Transform parent, float height, Material mat) {
            GameObject ladder = new GameObject("Ladder");
            ladder.transform.SetParent(parent);
            ladder.transform.localPosition = new Vector3(9.1f, 0, 0);
            
            for (int i = 0; i < (int)height; i++) {
                var rung = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rung.transform.SetParent(ladder.transform);
                rung.transform.localPosition = new Vector3(0, i + 0.5f, 0);
                rung.transform.localScale = new Vector3(0.2f, 0.1f, 2f);
                rung.GetComponent<Renderer>().sharedMaterial = mat;
                DestroyImmediate(rung.GetComponent<Collider>());
            }
        }

        private static void SpawnZombie(GameObject prefab, Transform parent, Vector3 pos) {
            var z = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            z.transform.position = pos;
            z.SetActive(true);
        }

        private static Material CreateURPLitMaterial(Color color, Texture2D normal, float tiling) {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Lit"));
            mat.color = color;
            if (normal != null) {
                mat.SetTexture("_BumpMap", normal);
                mat.EnableKeyword("_NORMALMAP");
            }
            mat.mainTextureScale = new Vector2(tiling, tiling);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0f);
            return mat;
        }

        private static void PurgeMissing(GameObject go) {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            foreach (Transform t in go.transform) PurgeMissing(t.gameObject);
        }
    }
}
