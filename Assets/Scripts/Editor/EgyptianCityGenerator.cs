using UnityEngine;
using UnityEditor;

namespace TheAlchemistsCrypt.Editor
{
    /// <summary>
    /// Procedural Egyptian City Generator v6:
    /// - No mummies
    /// - Brighter, warmer atmosphere
    /// - Prominent enterable central temple (player spawns in front of it)
    /// - Both palm tree variants used randomly
    /// - Proper door frames instead of black cubes
    /// - Wider streets
    /// - No stray ghost walls
    /// </summary>
    public class EgyptianCityGenerator : EditorWindow
    {
        [MenuItem("Tools/Generate Egyptian City")]
        public static void ShowWindow() =>
            GetWindow<EgyptianCityGenerator>("Egyptian City Generator");

        private int   gridSize     = 12;
        private float blockSize    = 16f;
        private float streetWidth  = 22f;   // wide streets
        private float houseDensity = 0.52f;

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Generates a collision-safe Egyptian city.\n" +
                "Temple is placed at (0,0,0). Player should spawn at (0, 2, -60).",
                MessageType.Info);

            gridSize     = EditorGUILayout.IntField("Grid Size",    gridSize);
            blockSize    = EditorGUILayout.FloatField("Block Size", blockSize);
            streetWidth  = EditorGUILayout.FloatField("Street W",   streetWidth);
            houseDensity = EditorGUILayout.Slider("House Density",  houseDensity, 0.1f, 1f);

            if (GUILayout.Button("▶  Generate City", GUILayout.Height(44)))
                GenerateCity();
        }

        // ─────────────────────────────────────────────────────────────────────────

        private void GenerateCity()
        {
            // ── Clean old city ──
            var old = GameObject.Find("ProceduralEgyptianCity");
            if (old != null) Undo.DestroyObjectImmediate(old);

            var root = new GameObject("ProceduralEgyptianCity");
            Undo.RegisterCreatedObjectUndo(root, "Generate City");

            // ── Atmosphere ──
            SetAtmosphere();

            // ── Materials ──
            var wallMat  = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Materials/BrickOldSharp0108_5_S_1_URP.mat");
            var floorMat = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Materials/MarbleTiles0040_1_S_1_URP.mat");

            Material sandMat = MakeMat(new Color(0.82f, 0.70f, 0.45f)); // sandy
            Material woodMat = MakeMat(new Color(0.42f, 0.27f, 0.12f)); // wooden door
            Material darkMat = MakeMat(new Color(0.08f, 0.06f, 0.04f)); // dark window opening

            // ── Desert floor ──
            float totalSize = gridSize * (blockSize + streetWidth);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "DesertFloor";
            floor.transform.SetParent(root.transform);
            floor.transform.localScale = new Vector3(totalSize / 5f, 1f, totalSize / 5f);
            if (floorMat) floor.GetComponent<Renderer>().sharedMaterial = floorMat;

            // ── Load prefabs ──
            var tree1    = Load("Assets/EgyptianAssets/realistic_hd_date_palm_2178.glb");
            var tree2    = Load("Assets/EgyptianAssets/realistic_hd_date_palm_378.glb");
            var column   = Load("Assets/EgyptianAssets/egyptian_column.glb");
            var chamber  = Load("Assets/EgyptianAssets/egypt_chamber_for_ar__vr_games.glb");
            var crate    = Load("Assets/EgyptianAssets/crate.glb");
            var barrel   = Load("Assets/EgyptianAssets/barrel.glb");

            // ── Central Temple ──
            // Placed at world origin; player should spawn at (0, 2, -60)
            // The temple opens toward -Z so the player walks into it from the south.
            if (chamber)
            {
                var ch = Instantiate(chamber, root.transform);
                ch.name = "CentralTemple";
                ch.transform.position    = new Vector3(0, 0, 0);
                ch.transform.localScale  = Vector3.one * 300f;
                ch.transform.rotation    = Quaternion.Euler(-90, 180, 0); // face player spawn
                AddCollidersToMesh(ch, makeTriggerOnSmallFaces: true);    // keep entrance open
            }

            // ── Column ring around temple ──
            float templeRadius = 55f;
            for (int i = 0; i < 8 && column; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                var pos = new Vector3(Mathf.Sin(angle) * templeRadius, 0, Mathf.Cos(angle) * templeRadius);
                PlaceColumn(column, root.transform, pos, sandMat);
            }

            // ── City grid ──
            float start = -totalSize * 0.5f + blockSize * 0.5f;
            int   ci    = gridSize / 2;                     // center index

            for (int x = 0; x < gridSize; x++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    float px = start + x * (blockSize + streetWidth);
                    float pz = start + z * (blockSize + streetWidth);
                    var   pos = new Vector3(px, 0, pz);

                    // ── Clear zone around temple (4-block radius) ──
                    if (Mathf.Abs(x - ci) <= 3 && Mathf.Abs(z - ci) <= 3)
                        continue;

                    // ── Sparse open plots ──
                    if (Random.value > houseDensity)
                    {
                        PlaceOpenPlot(root.transform, pos, tree1, tree2, column, sandMat);
                        continue;
                    }

                    // ── House ──
                    PlaceHouse(root.transform, pos, wallMat, sandMat, woodMat, darkMat,
                               crate, barrel);
                }
            }

            // ── Background pyramids ──
            PlacePyramid(root, new Vector3(-totalSize * 1.8f, -15f,  totalSize * 1.8f), 350f, sandMat);
            PlacePyramid(root, new Vector3( totalSize * 2.0f, -15f, -totalSize * 2.2f), 500f, sandMat);

            Debug.Log("[EgyptianCity] v6 generated.");
        }

        // ─── Atmosphere ───────────────────────────────────────────────────────────

        private static void SetAtmosphere()
        {
            // Warm golden desert haze — visible at reasonable distance
            RenderSettings.fog        = true;
            RenderSettings.fogMode    = FogMode.ExponentialSquared;
            RenderSettings.fogColor   = new Color(0.65f, 0.55f, 0.35f, 1f); // warm sandy
            RenderSettings.fogDensity = 0.003f;                               // thin fog

            // Ambient
            RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.50f, 0.40f);    // warm ambient

            // Sun — point it higher so scene is brighter
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type == LightType.Directional)
                {
                    l.intensity         = 3.8f;
                    l.color             = new Color(1f, 0.92f, 0.75f);       // golden sun
                    l.shadows           = LightShadows.Soft;
                    l.transform.rotation = Quaternion.Euler(65f, -35f, 0f); // higher sun
                }
            }
        }

        // ─── Open plot (trees / columns / obelisks) ───────────────────────────────

        private static void PlaceOpenPlot(Transform parent, Vector3 pos,
            GameObject tree1, GameObject tree2, GameObject column, Material sandMat)
        {
            float r = Random.value;

            if (r < 0.35f)
            {
                // Palm tree — alternate variants, upright
                var prefab = (Random.value < 0.5f ? tree1 : tree2);
                if (prefab == null) return;
                var t = Instantiate(prefab, parent);
                t.transform.position    = pos + new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));
                t.transform.localScale  = Vector3.one * Random.Range(5f, 9f);
                t.transform.rotation    = Quaternion.Euler(-90, Random.Range(0, 360), 0);
                AddCollidersToMesh(t);
            }
            else if (r < 0.55f)
            {
                // Standing / fallen column
                if (column == null) return;
                PlaceColumn(column, parent, pos, sandMat);
            }
            else if (r < 0.70f)
            {
                // Obelisk (pure-primitive, no import rotation issues)
                MakeObelisk(parent, pos, sandMat);
            }
            // else: empty open plaza — intentional
        }

        private static void PlaceColumn(GameObject prefab, Transform parent, Vector3 pos, Material mat)
        {
            bool fallen = Random.value < 0.25f;
            var col = Instantiate(prefab, parent);
            col.transform.position   = pos + new Vector3(Random.Range(-2f, 2f), fallen ? 0.4f : 0, Random.Range(-2f, 2f));
            col.transform.rotation   = fallen
                ? Quaternion.Euler(0, Random.Range(0, 360), 90)
                : Quaternion.Euler(-90, Random.Range(0, 360), 0);
            col.transform.localScale = Vector3.one * Random.Range(0.6f, 1.0f);
            AddCollidersToMesh(col);
        }

        private static void MakeObelisk(Transform parent, Vector3 pos, Material mat)
        {
            var ob = new GameObject("Obelisk"); ob.transform.SetParent(parent);
            ob.transform.position = pos;

            var shaft = MakeCube(ob.transform, new Vector3(0, 6f, 0), new Vector3(2f, 12f, 2f), mat);
            shaft.name = "Shaft";
            var tip   = MakeCube(ob.transform, new Vector3(0, 13f, 0), new Vector3(1f, 2f, 1f), mat);
            tip.name  = "Pyramidion";
        }

        // ─── House ────────────────────────────────────────────────────────────────

        private static void PlaceHouse(Transform parent, Vector3 pos,
            Material wallMat, Material sandMat, Material woodMat, Material darkMat,
            GameObject crate, GameObject barrel)
        {
            float w = Random.Range(12f, 18f);
            float d = Random.Range(12f, 18f);
            float h = Random.Range(11f, 22f);

            // ── Main body ──
            var house = GameObject.CreatePrimitive(PrimitiveType.Cube);
            house.name = $"House";
            house.transform.SetParent(parent);
            house.transform.position   = new Vector3(pos.x, h * 0.5f, pos.z);
            house.transform.localScale = new Vector3(w, h, d);
            if (wallMat) house.GetComponent<Renderer>().sharedMaterial = wallMat;

            // ── Windows (max 2 per face to avoid overlaps) ──
            if (Random.value < 0.8f)
                AddWindows(house.transform, h, w, d, darkMat);

            // ── Door frame ──
            AddDoor(house.transform, h, w, d, woodMat, sandMat, darkMat);

            // ── Props ──
            if (Random.value < 0.25f && crate != null)
            {
                var c = Instantiate(crate, parent);
                c.transform.position   = pos + new Vector3(w * 0.5f + 1f, 0.3f, d * 0.35f);
                c.transform.localScale = Vector3.one * 0.18f;
                c.transform.rotation   = Quaternion.Euler(0, Random.Range(0, 360), 0);
                AddCollidersToMesh(c);
            }
            if (Random.value < 0.2f && barrel != null)
            {
                var b = Instantiate(barrel, parent);
                b.transform.position   = pos + new Vector3(-w * 0.5f - 1f, 0.4f, -d * 0.35f);
                b.transform.localScale = Vector3.one * 0.18f;
                b.transform.rotation   = Quaternion.Euler(0, Random.Range(0, 360), 0);
                AddCollidersToMesh(b);
            }
        }

        // ─── Windows ─────────────────────────────────────────────────────────────

        private static void AddWindows(Transform house, float h, float w, float d, Material darkMat)
        {
            // Each side gets up to 2 windows placed at fixed X-slots to avoid overlaps
            // Sides: +X (right), -X (left), +Z (front), -Z (back)
            AddWindowsOnSide(house, darkMat, true,  true,  h, w, d);  // +X
            AddWindowsOnSide(house, darkMat, true,  false, h, w, d);  // -X
            AddWindowsOnSide(house, darkMat, false, true,  h, w, d);  // +Z
            AddWindowsOnSide(house, darkMat, false, false, h, w, d);  // -Z
        }

        private static void AddWindowsOnSide(Transform house, Material darkMat,
            bool isXAxis, bool positive, float h, float w, float d)
        {
            if (Random.value > 0.6f) return;   // some sides have no windows

            int count = Random.Range(1, 3); // 1 or 2
            float span = isXAxis ? d : w;

            for (int i = 0; i < count; i++)
            {
                // Evenly space slots along the face
                float t = (i + 1f) / (count + 1f);
                float along = (t - 0.5f) * (span * 0.7f);  // keep away from edges

                float winW = 0.025f, winH = 0.12f, winD = 0.025f;
                Vector3 localPos;

                if (isXAxis)
                {
                    localPos = new Vector3(positive ? 0.51f : -0.51f, 0.2f, along / span);
                    winW = 0.025f; winH = 0.12f; winD = (d * 0.25f) / d;
                }
                else
                {
                    localPos = new Vector3(along / w, 0.2f, positive ? 0.51f : -0.51f);
                    winW = (w * 0.25f) / w; winH = 0.12f; winD = 0.025f;
                }

                var win = GameObject.CreatePrimitive(PrimitiveType.Cube);
                win.name = "Window";
                win.transform.SetParent(house);
                win.transform.localPosition = localPos;
                win.transform.localScale    = new Vector3(winW, winH, winD);
                win.GetComponent<Renderer>().sharedMaterial = darkMat;
                Object.DestroyImmediate(win.GetComponent<Collider>());

                // Occasional torch light from window
                if (Random.value < 0.35f)
                {
                    var lgo = new GameObject("Torch");
                    lgo.transform.SetParent(win.transform);
                    lgo.transform.localPosition = Vector3.zero;
                    var lt = lgo.AddComponent<Light>();
                    lt.type      = LightType.Point;
                    lt.color     = new Color(1f, 0.6f, 0.2f);
                    lt.intensity = 14f;
                    lt.range     = 14f;
                    lt.shadows   = LightShadows.None; // no shadow per-light for perf
                }
            }
        }

        // ─── Door ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Places a realistic door on the front face (+Z) of the house.
        /// Structure: sandstone frame (2 posts + lintel) + wooden door panel + dark interior
        /// </summary>
        private static void AddDoor(Transform house, float h, float w, float d,
            Material woodMat, Material sandMat, Material darkMat)
        {
            float doorH   = Mathf.Clamp(h * 0.35f, 2.5f, 5.5f);  // absolute door height
            float doorW   = Mathf.Clamp(w * 0.18f, 1.5f, 2.5f);  // absolute door width
            float frameT  = 0.25f;   // frame thickness (local)
            float faceZ   = 0.51f;   // just outside the front face in local coords

            // Normalise: local units = actual metres / building dimension
            float normDoorH = doorH / h;
            float normDoorW = doorW / w;
            float normDoorY = -0.5f + normDoorH * 0.5f;   // bottom of door sits on floor

            // ── Dark opening behind the door ──
            var opening = MakeCubePrim(house, new Vector3(0f, normDoorY, faceZ),
                new Vector3(normDoorW * 0.9f, normDoorH * 0.96f, 0.04f), darkMat);
            opening.name = "DoorOpening";
            Object.DestroyImmediate(opening.GetComponent<Collider>());

            // ── Door panel (wood) ──
            var panel = MakeCubePrim(house, new Vector3(0f, normDoorY, faceZ + 0.015f),
                new Vector3(normDoorW * 0.85f, normDoorH * 0.94f, 0.03f), woodMat);
            panel.name = "DoorPanel";
            Object.DestroyImmediate(panel.GetComponent<Collider>());

            // ── Door panel horizontal plank lines ──
            for (int i = 0; i < 3; i++)
            {
                float ty = -0.5f + (i + 1f) / 4f;
                var plank = MakeCubePrim(panel.transform,
                    new Vector3(0f, ty, 0.6f),
                    new Vector3(0.95f, 0.05f, 0.3f),
                    new Material(woodMat) { color = new Color(0.32f, 0.20f, 0.08f) });
                plank.name = "Plank";
                Object.DestroyImmediate(plank.GetComponent<Collider>());
            }

            // ── Door handle (small sphere) ──
            var handle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            handle.name = "DoorHandle";
            handle.transform.SetParent(panel.transform);
            handle.transform.localPosition = new Vector3(0.38f, -0.1f, 0.7f);
            handle.transform.localScale    = new Vector3(0.08f, 0.08f, 0.08f);
            handle.GetComponent<Renderer>().material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                { color = new Color(0.85f, 0.75f, 0.2f) }; // gold
            Object.DestroyImmediate(handle.GetComponent<Collider>());

            // ── Sandstone frame: left post, right post, lintel ──
            float postW = frameT / w;
            float postH = normDoorH + frameT / h * 0.5f;
            float lintelW = normDoorW + postW * 2f;

            MakeCubePrim(house, new Vector3(-(normDoorW * 0.5f + postW * 0.5f), normDoorY, faceZ + 0.025f),
                new Vector3(postW, postH, 0.06f), sandMat).name = "FrameLeft";
            MakeCubePrim(house, new Vector3(+(normDoorW * 0.5f + postW * 0.5f), normDoorY, faceZ + 0.025f),
                new Vector3(postW, postH, 0.06f), sandMat).name = "FrameRight";
            MakeCubePrim(house, new Vector3(0f, normDoorY + normDoorH * 0.5f + frameT / h * 0.5f, faceZ + 0.025f),
                new Vector3(lintelW, frameT / h, 0.06f), sandMat).name = "Lintel";
        }

        // ─── Pyramid ─────────────────────────────────────────────────────────────

        private static void PlacePyramid(GameObject root, Vector3 pos, float size, Material mat)
        {
            var py = new GameObject("Pyramid");
            py.transform.SetParent(root.transform);
            py.transform.position = pos;
            int steps = 18;
            float sh = size * 0.6f / steps;
            for (int i = 0; i < steps; i++)
            {
                float s = size * (1f - (float)i / steps);
                var step = MakeCube(py.transform,
                    new Vector3(0, i * sh + sh * 0.5f, 0),
                    new Vector3(s, sh * 1.05f, s), mat);
                step.name = $"Step_{i}";
            }
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private static GameObject MakeCube(Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            go.transform.localScale    = localScale;
            if (mat) go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        // Parent-space cube (local transform relative to parent)
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

        private static GameObject Load(string path) =>
            AssetDatabase.LoadAssetAtPath<GameObject>(path);

        private static GameObject Instantiate(GameObject prefab, Transform parent)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            return go;
        }

        /// <summary>
        /// Recursively adds MeshColliders to any MeshFilter children that lack a Collider.
        /// If makeTriggerOnSmallFaces is true, very small meshes get a trigger instead of solid.
        /// </summary>
        private static void AddCollidersToMesh(GameObject obj, bool makeTriggerOnSmallFaces = false)
        {
            foreach (var mf in obj.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.GetComponent<Collider>() != null) continue;
                if (mf.sharedMesh == null) continue;
                var mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                if (makeTriggerOnSmallFaces && mf.sharedMesh.bounds.size.magnitude < 1f)
                    mc.isTrigger = true;
            }
            // Ensure shadows
            foreach (var r in obj.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                r.receiveShadows    = true;
            }
        }
    }
}
