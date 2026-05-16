using UnityEngine;
using UnityEditor;

namespace TheAlchemistsCrypt.Editor
{
    public class RandomEgyptianCityGenerator : EditorWindow
    {
        [MenuItem("Tools/Generate Egyptian City (Random)")]
        public static void ShowWindow() =>
            GetWindow<RandomEgyptianCityGenerator>("Egyptian City Generator");

        private int   gridSize     = 14;
        private float blockSize    = 16f;
        private float streetWidth  = 22f;
        private float houseDensity = 0.50f;

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Randomized layout with optimized colliders and material caching for mobile performance.",
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
            root.isStatic = true;

            SetAtmosphere();

            var wallMatAsset = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BrickOldSharp0108_5_S_1_URP.mat");
            Material wallMat = wallMatAsset ? new Material(wallMatAsset) : MakeMat(new Color(0.92f, 0.85f, 0.72f));
            Material sandMat = MakeMat(new Color(0.92f, 0.85f, 0.72f)); 
            Material woodMat = MakeMat(new Color(0.38f, 0.25f, 0.10f));
            Material darkMat = MakeMat(new Color(0.05f, 0.04f, 0.03f));

            float totalSize = gridSize * (blockSize + streetWidth);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "DesertFloor";
            floor.transform.SetParent(root.transform);
            floor.transform.localScale = new Vector3(totalSize / 5f, 1f, totalSize / 5f);
            floor.isStatic = true;
            floor.GetComponent<Renderer>().sharedMaterial = sandMat;

            var tree1 = Load("Assets/EgyptianAssets/realistic_hd_date_palm_2178.glb");
            var tree2 = Load("Assets/EgyptianAssets/realistic_hd_date_palm_378.glb");
            var tree3 = Load("Assets/EgyptianAssets/realistic_hd_date_palm_4778.glb");
            var column = Load("Assets/EgyptianAssets/egyptian_column.glb");
            var chamber = Load("Assets/EgyptianAssets/egypt_chamber_for_ar__vr_games.glb");
            
            GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Mesh cubeMesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tempCube);
            var urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Lit");

            if (chamber)
            {
                var ch = Instantiate(chamber, root.transform);
                ch.name = "CentralTemple";
                ch.transform.position = new Vector3(0, 0, -20);
                ch.transform.localScale = Vector3.one * 550f; 
                ch.transform.rotation = Quaternion.Euler(0, 180, 0); 
                AddCollidersToMesh(ch, true, urpShader);
            }

            float start = -totalSize * 0.5f + blockSize * 0.5f;
            for (int x = 0; x < gridSize; x++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    float px = start + x * (blockSize + streetWidth);
                    float pz = start + z * (blockSize + streetWidth);
                    var pos = new Vector3(px, 0, pz);

                    if (Vector3.Distance(pos, new Vector3(0, 0, -20)) < 70f) continue;

                    if (Random.value > houseDensity)
                    {
                        PlaceOpenPlot(root.transform, pos, tree1, tree2, tree3, column, sandMat, urpShader);
                        continue;
                    }

                    PlaceHouseOptimized(root.transform, pos, wallMat, woodMat, darkMat, cubeMesh);
                }
            }

            StaticBatchingUtility.Combine(root);
        }

        private static void SetAtmosphere()
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.45f, 0.25f, 0.05f, 1f); 
            RenderSettings.fogDensity = 0.012f; 
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.22f, 0.20f, 0.25f); 
        }

        private static void PlaceOpenPlot(Transform parent, Vector3 pos, GameObject tree1, GameObject tree2, GameObject tree3, GameObject column, Material sandMat, Shader shader)
        {
            float r = Random.value;
            if (r < 0.45f)
            {
                var prefab = (Random.value < 0.33f ? tree1 : (Random.value < 0.66f ? tree2 : tree3));
                if (prefab != null) {
                    var t = Instantiate(prefab, parent);
                    t.transform.position = pos + new Vector3(Random.Range(-4f, 4f), 0, Random.Range(-4f, 4f));
                    t.transform.localScale = Vector3.one * Random.Range(3.5f, 6.5f);
                    t.transform.rotation = Quaternion.Euler(-90, Random.Range(0, 360), 0);
                    AddCollidersToMesh(t, false, shader);
                }
            }
        }

        private static void PlaceHouseOptimized(Transform parent, Vector3 pos, Material wallMat, Material woodMat, Material darkMat, Mesh cubeMesh)
        {
            float w = Random.Range(14f, 26f);
            float d = Random.Range(14f, 26f);
            float h = Random.Range(14f, 35f);

            var houseGroup = new GameObject("House");
            houseGroup.transform.SetParent(parent);
            houseGroup.transform.position = pos;
            houseGroup.isStatic = true;

            var block = new GameObject("HouseModule");
            block.transform.SetParent(houseGroup.transform);
            block.transform.localPosition = new Vector3(0, h * 0.5f, 0);
            block.transform.localScale = new Vector3(w, h, d);
            block.AddComponent<MeshFilter>().sharedMesh = cubeMesh;
            block.AddComponent<MeshRenderer>().sharedMaterial = wallMat;
            block.isStatic = true;

            // Simple BoxCollider for high FPS
            var bc = houseGroup.AddComponent<BoxCollider>();
            bc.center = new Vector3(0, h * 0.5f, 0);
            bc.size = new Vector3(w, h, d);
        }

        private static Material MakeMat(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Lit");
            var mat = new Material(shader); mat.color = color;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.0f);
            return mat;
        }

        private static GameObject Load(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);
        private static GameObject Instantiate(GameObject prefab, Transform parent) => (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);

        private static void AddCollidersToMesh(GameObject obj, bool makeTriggerOnSmallFaces, Shader shader)
        {
            foreach (var mf in obj.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                var mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                if (makeTriggerOnSmallFaces && mf.sharedMesh.bounds.size.magnitude < 15f) mc.isTrigger = true;
            }
            if (shader != null)
            {
                foreach (var r in obj.GetComponentsInChildren<Renderer>())
                {
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++) {
                        if (mats[i] != null && !mats[i].shader.name.Contains("Universal Render Pipeline")) {
                            var newMat = new Material(shader);
                            if (mats[i].HasProperty("_BaseColor")) newMat.SetColor("_BaseColor", mats[i].GetColor("_BaseColor"));
                            else if (mats[i].HasProperty("_Color")) newMat.SetColor("_BaseColor", mats[i].color);
                            r.sharedMaterials[i] = newMat;
                        }
                    }
                }
            }
        }
    }
}
