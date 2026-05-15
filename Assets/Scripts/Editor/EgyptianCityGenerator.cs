using UnityEngine;
using UnityEditor;

namespace TheAlchemistsCrypt.Editor
{
    /// <summary>
    /// Procedural Egyptian City Generator v8:
    /// - Temple moved very close to player spawn (-40m from spawn)
    /// - Massive 70m clear zone around temple to ensure visibility
    /// - 3 Tree types distributed 33/33/33
    /// - Fog: Vibrant Green + 0.5x Gray mix
    /// - Ground: "Skin/Sandy" color, tiling fixed
    /// - Top-down Directional Light for perfect color clarity
    /// - Props strictly outside house bounds
    /// </summary>
    public class EgyptianCityGenerator : EditorWindow
    {
        [MenuItem("Tools/Generate Egyptian City")]
        public static void ShowWindow() =>
            GetWindow<EgyptianCityGenerator>("Egyptian City Generator");

        private int   gridSize     = 14;
        private float blockSize    = 16f;
        private float streetWidth  = 22f;
        private float houseDensity = 0.50f;

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Temple is at (0,0,-20). Player spawns at (0,2,-60) facing the temple.\n" +
                "Clears 70m radius around temple.",
                MessageType.Info);

            gridSize     = EditorGUILayout.IntField("Grid Size",    gridSize);
            blockSize    = EditorGUILayout.FloatField("Block Size", blockSize);
            streetWidth  = EditorGUILayout.FloatField("Street W",   streetWidth);
            houseDensity = EditorGUILayout.Slider("House Density",  houseDensity, 0.1f, 1f);

            if (GUILayout.Button("▶  Generate City", GUILayout.Height(44)))
                GenerateCity();
        }

        private void GenerateCity()
        {
            var old = GameObject.Find("ProceduralEgyptianCity");
            if (old != null) Undo.DestroyObjectImmediate(old);

            var root = new GameObject("ProceduralEgyptianCity");
            Undo.RegisterCreatedObjectUndo(root, "Generate City");

            SetAtmosphere();

            var wallMat  = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BrickOldSharp0108_5_S_1_URP.mat");
            var floorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/MarbleTiles0040_1_S_1_URP.mat");

            // Warm skin/sandy color
            Material sandMat = MakeMat(new Color(0.92f, 0.85f, 0.72f)); 
            Material woodMat = MakeMat(new Color(0.38f, 0.25f, 0.10f));
            Material darkMat = MakeMat(new Color(0.05f, 0.04f, 0.03f));

            float totalSize = gridSize * (blockSize + streetWidth);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "DesertFloor";
            floor.transform.SetParent(root.transform);
            floor.transform.localScale = new Vector3(totalSize / 5f, 1f, totalSize / 5f);
            var floorRenderer = floor.GetComponent<Renderer>();
            
            // Apply skin/sandy color to ground
            if (floorMat) 
            {
                var inst = new Material(floorMat);
                inst.color = new Color(0.95f, 0.88f, 0.75f); // Skin/Sandy tint
                inst.mainTextureScale = new Vector2(100, 100); // More tiling = less blurry
                floorRenderer.sharedMaterial = inst;
            }
            else
            {
                floorRenderer.sharedMaterial = sandMat;
            }

            var tree1 = Load("Assets/EgyptianAssets/realistic_hd_date_palm_2178.glb");
            var tree2 = Load("Assets/EgyptianAssets/realistic_hd_date_palm_378.glb");
            var tree3 = Load("Assets/EgyptianAssets/realistic_hd_date_palm_4778.glb");
            var column = Load("Assets/EgyptianAssets/egyptian_column.glb");
            var chamber = Load("Assets/EgyptianAssets/egypt_chamber_for_ar__vr_games.glb");
            var crate = Load("Assets/EgyptianAssets/crate.glb");
            var barrel = Load("Assets/EgyptianAssets/barrel.glb");

            // Temple at -20 (Spawn is at -60, so 40m in front)
            if (chamber)
            {
                var ch = Instantiate(chamber, root.transform);
                ch.name = "CentralTemple";
                ch.transform.position    = new Vector3(0, 0, -20);
                ch.transform.localScale  = Vector3.one * 380f;
                ch.transform.rotation    = Quaternion.Euler(-90, 180, 0); 
                AddCollidersToMesh(ch, makeTriggerOnSmallFaces: true);
            }

            float start = -totalSize * 0.5f + blockSize * 0.5f;
            for (int x = 0; x < gridSize; x++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    float px = start + x * (blockSize + streetWidth);
                    float pz = start + z * (blockSize + streetWidth);
                    var pos = new Vector3(px, 0, pz);

                    // HUGE Clear zone (70m) around temple center (-20)
                    if (Vector3.Distance(pos, new Vector3(0, 0, -20)) < 70f)
                        continue;

                    if (Random.value > houseDensity)
                    {
                        PlaceOpenPlot(root.transform, pos, tree1, tree2, tree3, column, sandMat);
                        continue;
                    }

                    PlaceHouse(root.transform, pos, wallMat, sandMat, woodMat, darkMat, crate, barrel);
                }
            }

            // Pyramids further back
            PlacePyramid(root, new Vector3(-totalSize * 1.5f, -10f,  totalSize * 1.5f), 400f, sandMat);
            PlacePyramid(root, new Vector3( totalSize * 1.8f, -10f, -totalSize * 2.0f), 600f, sandMat);
        }

        private static void SetAtmosphere()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            // Vibrant Green + 0.5x Gray
            RenderSettings.fogColor = new Color(0.35f, 0.65f, 0.40f, 1f); 
            RenderSettings.fogDensity = 0.0012f; // Even lower for clarity

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            // MUCH Brighter, warmer ambient light
            RenderSettings.ambientLight = new Color(0.85f, 0.82f, 0.75f); 

            // Existing Sun
            Light sun = null;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional && l.name != "TopDownClarityLight") { sun = l; break; }

            if (sun != null)
            {
                sun.intensity = 3.0f; // Boost sun
                sun.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
            }

            // TOP-DOWN CLARITY LIGHT
            var topLightObj = GameObject.Find("TopDownClarityLight");
            if (topLightObj != null) Object.DestroyImmediate(topLightObj);
            topLightObj = new GameObject("TopDownClarityLight");
            var topLight = topLightObj.AddComponent<Light>();
            topLight.type = LightType.Directional;
            topLight.intensity = 3.5f; // MASSIVE boost for side-lighting/fill
            topLight.color = new Color(1f, 0.98f, 0.9f); // Slightly warm white
            topLight.transform.rotation = Quaternion.Euler(85f, 10f, 0f); // Slightly off-center for better shading
            topLight.shadows = LightShadows.None;
        }

        private static void PlaceOpenPlot(Transform parent, Vector3 pos,
            GameObject tree1, GameObject tree2, GameObject tree3, GameObject column, Material sandMat)
        {
            float r = Random.value;
            if (r < 0.45f)
            {
                // All 3 trees used evenly
                float tr = Random.value;
                var prefab = (tr < 0.33f ? tree1 : (tr < 0.66f ? tree2 : tree3));
                if (prefab == null) prefab = tree1; 
                
                var t = Instantiate(prefab, parent);
                t.transform.position = pos + new Vector3(Random.Range(-4f, 4f), 0, Random.Range(-4f, 4f));
                t.transform.localScale = Vector3.one * Random.Range(7f, 12f);
                t.transform.rotation = Quaternion.Euler(-90, Random.Range(0, 360), 0);
                AddCollidersToMesh(t);
            }
            else if (r < 0.65f && column != null)
            {
                var col = Instantiate(column, parent);
                bool fallen = Random.value < 0.3f;
                col.transform.position = pos + new Vector3(Random.Range(-2f, 2f), fallen ? 0.4f : 0, Random.Range(-2f, 2f));
                col.transform.rotation = fallen ? Quaternion.Euler(0, Random.Range(0, 360), 90) : Quaternion.Euler(-90, Random.Range(0, 360), 0);
                col.transform.localScale = Vector3.one * Random.Range(0.9f, 1.3f);
                AddCollidersToMesh(col);
            }
        }

        private static void PlaceHouse(Transform parent, Vector3 pos,
            Material wallMat, Material sandMat, Material woodMat, Material darkMat, GameObject crate, GameObject barrel)
        {
            float w = Random.Range(14f, 19f);
            float d = Random.Range(14f, 19f);
            float h = Random.Range(14f, 24f);

            var house = GameObject.CreatePrimitive(PrimitiveType.Cube);
            house.name = "House";
            house.transform.SetParent(parent);
            house.transform.position = new Vector3(pos.x, h * 0.5f, pos.z);
            house.transform.localScale = new Vector3(w, h, d);
            if (wallMat) house.GetComponent<Renderer>().sharedMaterial = wallMat;

            AddWindows(house.transform, h, w, d, darkMat);
            AddDoor(house.transform, h, w, d, woodMat, sandMat, darkMat);

            // Props Guaranteed Outside
            if (Random.value < 0.35f && crate != null)
            {
                var c = Instantiate(crate, parent);
                float offsetX = (w * 0.5f + 3.0f) * (Random.value > 0.5f ? 1f : -1f);
                c.transform.position = pos + new Vector3(offsetX, 0.5f, Random.Range(-d*0.3f, d*0.3f));
                c.transform.localScale = Vector3.one * 0.22f;
                c.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                AddCollidersToMesh(c);
            }
            if (Random.value < 0.3f && barrel != null)
            {
                var b = Instantiate(barrel, parent);
                float offsetZ = (d * 0.5f + 3.0f) * (Random.value > 0.5f ? 1f : -1f);
                b.transform.position = pos + new Vector3(Random.Range(-w*0.3f, w*0.3f), 0.6f, offsetZ);
                b.transform.localScale = Vector3.one * 0.22f;
                b.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                AddCollidersToMesh(b);
            }
        }

        private static void AddWindows(Transform house, float h, float w, float d, Material darkMat)
        {
            AddWindowsOnSide(house, darkMat, true, true, h, w, d);
            AddWindowsOnSide(house, darkMat, true, false, h, w, d);
            AddWindowsOnSide(house, darkMat, false, true, h, w, d);
            AddWindowsOnSide(house, darkMat, false, false, h, w, d);
        }

        private static void AddWindowsOnSide(Transform house, Material darkMat, bool isXAxis, bool positive, float h, float w, float d)
        {
            if (Random.value > 0.4f) return;
            int count = Random.Range(1, 3);
            float span = isXAxis ? d : w;
            for (int i = 0; i < count; i++)
            {
                float t = (i + 1f) / (count + 1f);
                float along = (t - 0.5f) * (span * 0.7f);
                float winW = 0.03f, winH = 0.15f, winD = 0.03f;
                Vector3 localPos;
                if (isXAxis) {
                    localPos = new Vector3(positive ? 0.51f : -0.51f, 0.25f, along / span);
                    winD = (d * 0.25f) / d;
                } else {
                    localPos = new Vector3(along / w, 0.25f, positive ? 0.51f : -0.51f);
                    winW = (w * 0.25f) / w;
                }
                var win = GameObject.CreatePrimitive(PrimitiveType.Cube);
                win.transform.SetParent(house);
                win.transform.localPosition = localPos;
                win.transform.localScale = new Vector3(winW, winH, winD);
                win.GetComponent<Renderer>().sharedMaterial = darkMat;
                Object.DestroyImmediate(win.GetComponent<Collider>());
            }
        }

        private static void AddDoor(Transform house, float h, float w, float d, Material woodMat, Material sandMat, Material darkMat)
        {
            float doorH = 5f, doorW = 2.5f;
            float normDoorH = doorH / h;
            float normDoorW = doorW / w;
            float normDoorY = -0.5f + normDoorH * 0.5f;
            var panel = MakeCubePrim(house, new Vector3(0f, normDoorY, 0.51f), new Vector3(normDoorW, normDoorH, 0.05f), woodMat);
            Object.DestroyImmediate(panel.GetComponent<Collider>());
        }

        private static void PlacePyramid(GameObject root, Vector3 pos, float size, Material mat)
        {
            var py = new GameObject("Pyramid");
            py.transform.SetParent(root.transform);
            py.transform.position = pos;
            int steps = 20;
            float sh = size * 0.6f / steps;
            for (int i = 0; i < steps; i++) {
                float s = size * (1f - (float)i / steps);
                MakeCube(py.transform, new Vector3(0, i * sh + sh * 0.5f, 0), new Vector3(s, sh, s), mat);
            }
        }

        private static GameObject MakeCube(Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            go.transform.localScale    = localScale;
            if (mat) go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private static GameObject MakeCubePrim(Transform parent, Vector3 lPos, Vector3 lScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(parent);
            go.transform.localPosition = lPos;
            go.transform.localScale    = lScale;
            if (mat) go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private static Material MakeMat(Color color)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;
            return mat;
        }

        private static GameObject Load(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);

        private static GameObject Instantiate(GameObject prefab, Transform parent) => (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);

        private static void AddCollidersToMesh(GameObject obj, bool makeTriggerOnSmallFaces = false)
        {
            foreach (var mf in obj.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.GetComponent<Collider>() != null) continue;
                if (mf.sharedMesh == null) continue;
                var mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                if (makeTriggerOnSmallFaces && mf.sharedMesh.bounds.size.magnitude < 15f) mc.isTrigger = true;
            }
            foreach (var r in obj.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; r.receiveShadows = true; }
        }
    }
}
