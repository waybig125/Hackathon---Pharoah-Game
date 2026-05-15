using UnityEngine;
using UnityEditor;

namespace TheAlchemistsCrypt.Editor
{
    public class EgyptianCityGenerator : EditorWindow
    {
        [MenuItem("Tools/Generate Egyptian City")]
        public static void ShowWindow()
        {
            GetWindow<EgyptianCityGenerator>("City Generator");
        }

        private int gridSize = 12;
        private float blockSize = 15f;
        private float streetWidth = 14f;
        private float houseDensity = 0.55f;

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Generates a collision-safe, modular Egyptian City with Advanced Assets.", MessageType.Info);
            gridSize = EditorGUILayout.IntField("Grid Size", gridSize);
            blockSize = EditorGUILayout.FloatField("Block Size", blockSize);
            streetWidth = EditorGUILayout.FloatField("Street Width", streetWidth);
            houseDensity = EditorGUILayout.Slider("House Density", houseDensity, 0.1f, 1f);

            if (GUILayout.Button("Generate City", GUILayout.Height(40)))
            {
                GenerateCity();
            }
        }

        private void GenerateCity()
        {
            GameObject cityRoot = GameObject.Find("ProceduralEgyptianCity");
            if (cityRoot != null)
            {
                Undo.DestroyObjectImmediate(cityRoot);
            }
            
            GameObject oldTerrain = GameObject.Find("DesertTerrain");
            if (oldTerrain != null)
            {
                Undo.DestroyObjectImmediate(oldTerrain);
            }

            cityRoot = new GameObject("ProceduralEgyptianCity");
            Undo.RegisterCreatedObjectUndo(cityRoot, "Generate City");

            // Setup Sun
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            bool hasSun = false;
            foreach(var l in lights) {
                if (l.type == LightType.Directional) hasSun = true;
            }
            if (!hasSun) {
                GameObject sunObj = new GameObject("Directional Light");
                sunObj.transform.SetParent(cityRoot.transform);
                Light sun = sunObj.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.shadows = LightShadows.Soft;
                sun.color = new Color(1f, 0.85f, 0.7f);
                sun.intensity = 1.2f;
                sunObj.transform.rotation = Quaternion.Euler(30f, -45f, 0f);
            }

            // Load Materials
            Material floorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/MarbleTiles0040_1_S_1_URP.mat");
            Material wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BrickOldSharp0108_5_S_1_URP.mat");
            Material blackMat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = Color.black };

            // Load Prefabs
            GameObject tree1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/realistic_hd_date_palm_2178.glb");
            GameObject tree2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/realistic_hd_date_palm_378.glb");
            GameObject columnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/egyptian_column.glb");
            GameObject chamberPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/egypt_chamber_for_ar__vr_games.glb");
            GameObject cratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/crate.glb");
            GameObject barrelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/barrel.glb");
            GameObject mummyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianAssets/ancient_egyptian_mummy_scan.glb");

            // Floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "DesertFloor";
            floor.transform.SetParent(cityRoot.transform);
            float totalSize = gridSize * (blockSize + streetWidth);
            floor.transform.localScale = new Vector3(totalSize / 5f, 1, totalSize / 5f);
            if (floorMat != null) floor.GetComponent<Renderer>().sharedMaterial = floorMat;

            // Generate Blocks
            float startX = -totalSize / 2f + blockSize / 2f;
            float startZ = -totalSize / 2f + blockSize / 2f;
            
            int centerIdx = gridSize / 2;

            for (int x = 0; x < gridSize; x++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    float px = startX + x * (blockSize + streetWidth);
                    float pz = startZ + z * (blockSize + streetWidth);
                    Vector3 pos = new Vector3(px, 0, pz);

                    // Central Chamber
                    if (x == centerIdx && z == centerIdx)
                    {
                        if (chamberPrefab != null)
                        {
                            GameObject chamber = (GameObject)PrefabUtility.InstantiatePrefab(chamberPrefab, cityRoot.transform);
                            chamber.transform.position = pos;
                            chamber.transform.localScale = Vector3.one * 1.5f;
                            EnsureColliders(chamber);
                        }
                        continue;
                    }

                    // Leave empty spots based on density
                    if (Random.value > houseDensity)
                    {
                        // Open space! Put trees, obelisks or columns
                        float r = Random.value;
                        if (r < 0.3f && tree1 != null)
                        {
                            GameObject t = (GameObject)PrefabUtility.InstantiatePrefab(Random.value < 0.5f ? tree1 : tree2, cityRoot.transform);
                            t.transform.position = pos;
                            t.transform.localScale = Vector3.one * Random.Range(3f, 5f);
                            t.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                            EnsureColliders(t);
                        }
                        else if (r < 0.5f)
                        {
                            // Create Obelisk from primitives
                            GameObject obelisk = new GameObject("Obelisk");
                            obelisk.transform.SetParent(cityRoot.transform);
                            obelisk.transform.position = pos;
                            
                            GameObject basePart = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            basePart.transform.SetParent(obelisk.transform);
                            basePart.transform.localPosition = new Vector3(0, 5, 0);
                            basePart.transform.localScale = new Vector3(3, 10, 3);
                            basePart.GetComponent<Renderer>().sharedMaterial = wallMat;
                            
                            GameObject topPart = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            topPart.transform.SetParent(obelisk.transform);
                            topPart.transform.localPosition = new Vector3(0, 11, 0);
                            topPart.transform.localScale = new Vector3(1, 2, 1); // Pyramidion
                            topPart.GetComponent<Renderer>().sharedMaterial = wallMat;
                        }
                        continue;
                    }

                    // Build House
                    float height = Random.Range(8f, 16f);
                    float widthOffset = Random.Range(-2f, 2f);
                    float depthOffset = Random.Range(-2f, 2f);
                    
                    GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    block.name = $"House_{x}_{z}";
                    block.transform.SetParent(cityRoot.transform);
                    block.transform.position = new Vector3(px, height / 2f, pz);
                    block.transform.localScale = new Vector3(blockSize + widthOffset, height, blockSize + depthOffset);
                    if (wallMat != null) block.GetComponent<Renderer>().sharedMaterial = wallMat;
                    
                    // Add Windows
                    for (int i = 0; i < 3; i++)
                    {
                        if (Random.value < 0.7f)
                        {
                            GameObject window = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            window.name = "Window";
                            window.transform.SetParent(block.transform);
                            float side = (Random.value < 0.5f) ? 0.51f : -0.51f;
                            window.transform.localPosition = new Vector3(side, Random.Range(0.1f, 0.4f), Random.Range(-0.3f, 0.3f));
                            window.transform.localScale = new Vector3(0.02f, 0.15f, 0.1f);
                            window.GetComponent<Renderer>().sharedMaterial = blackMat;
                            DestroyImmediate(window.GetComponent<Collider>());
                        }
                    }

                    // Add Doorway
                    GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    door.name = "Doorway";
                    door.transform.SetParent(block.transform);
                    door.transform.localPosition = new Vector3(0f, -0.5f + 1.5f/height, 0.51f);
                    door.transform.localScale = new Vector3(0.2f, 3f/height, 0.02f);
                    door.GetComponent<Renderer>().sharedMaterial = blackMat;
                    DestroyImmediate(door.GetComponent<Collider>());
                    
                    // Add Torch
                    if (Random.value < 0.4f)
                    {
                        GameObject torch = new GameObject("Torch");
                        torch.transform.SetParent(block.transform);
                        torch.transform.localPosition = new Vector3(0.52f, 0.1f, 0.2f);
                        
                        GameObject stick = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        stick.transform.SetParent(torch.transform);
                        stick.transform.localPosition = Vector3.zero;
                        stick.transform.localScale = new Vector3(0.01f, 0.1f, 0.01f);
                        stick.GetComponent<Renderer>().sharedMaterial = blackMat;
                        
                        GameObject lightObj = new GameObject("TorchLight");
                        lightObj.transform.SetParent(torch.transform);
                        lightObj.transform.localPosition = new Vector3(0, 0.1f, 0);
                        Light l = lightObj.AddComponent<Light>();
                        l.type = LightType.Point;
                        l.color = new Color(1f, 0.5f, 0.1f);
                        l.intensity = 5f;
                        l.range = 10f;
                    }

                    // Assets around the house
                    if (columnPrefab != null && Random.value < 0.3f)
                    {
                        GameObject col = (GameObject)PrefabUtility.InstantiatePrefab(columnPrefab, cityRoot.transform);
                        bool isFallen = Random.value < 0.3f;
                        col.transform.position = pos + new Vector3(blockSize/2f + 2f, isFallen ? 0.5f : 0f, 0);
                        col.transform.rotation = isFallen ? Quaternion.Euler(0, Random.Range(0, 360), 90) : Quaternion.identity;
                        col.transform.localScale = Vector3.one * 0.5f;
                        EnsureColliders(col);
                    }
                    
                    if (cratePrefab != null && Random.value < 0.2f)
                    {
                        GameObject crate = (GameObject)PrefabUtility.InstantiatePrefab(cratePrefab, cityRoot.transform);
                        crate.transform.position = pos + new Vector3(-blockSize/2f - 1f, 0, -blockSize/2f - 1f);
                        crate.transform.localScale = Vector3.one * 0.15f;
                        EnsureColliders(crate);
                    }
                    
                    if (barrelPrefab != null && Random.value < 0.2f)
                    {
                        GameObject barrel = (GameObject)PrefabUtility.InstantiatePrefab(barrelPrefab, cityRoot.transform);
                        barrel.transform.position = pos + new Vector3(blockSize/2f + 1f, 0, blockSize/2f + 1f);
                        barrel.transform.localScale = Vector3.one * 0.15f;
                        EnsureColliders(barrel);
                    }
                }
            }

            // Pyramids (Massive, far away)
            GeneratePyramid(cityRoot, new Vector3(-totalSize * 2.5f, -20f, totalSize * 2.5f), 400f, 250f, wallMat);
            GeneratePyramid(cityRoot, new Vector3(totalSize * 2.5f, -20f, -totalSize * 3f), 600f, 400f, wallMat);

            Debug.Log("Procedural Egyptian City generated successfully.");
        }

        private void EnsureColliders(GameObject obj)
        {
            if (obj.GetComponentInChildren<Collider>() == null)
            {
                MeshFilter[] filters = obj.GetComponentsInChildren<MeshFilter>();
                foreach (var filter in filters)
                {
                    if (filter.gameObject.GetComponent<Collider>() == null)
                        filter.gameObject.AddComponent<MeshCollider>();
                }
            }
        }

        private void GeneratePyramid(GameObject root, Vector3 pos, float size, float height, Material mat)
        {
            GameObject pyrRoot = new GameObject("Pyramid");
            pyrRoot.transform.SetParent(root.transform);
            pyrRoot.transform.position = pos;

            int steps = 20;
            float stepHeight = height / steps;
            
            for(int i=0; i<steps; i++)
            {
                float currentSize = size * (1f - (float)i / steps);
                GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.name = $"Step_{i}";
                step.transform.SetParent(pyrRoot.transform);
                step.transform.localPosition = new Vector3(0, i * stepHeight + stepHeight/2f, 0);
                step.transform.localScale = new Vector3(currentSize, stepHeight, currentSize);
                if (mat != null) step.GetComponent<Renderer>().sharedMaterial = mat;
            }
        }
    }
}
