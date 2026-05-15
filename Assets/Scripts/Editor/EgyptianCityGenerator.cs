using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace TheAlchemistsCrypt.Editor
{
    public class EgyptianCityGenerator : EditorWindow
    {
        [MenuItem("Tools/Generate Egyptian City")]
        public static void ShowWindow()
        {
            GetWindow<EgyptianCityGenerator>("City Generator");
        }

        private int gridSize = 10;
        private float blockSize = 15f;
        private float streetWidth = 6f;

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Generates a collision-safe, modular Egyptian City.", MessageType.Info);
            gridSize = EditorGUILayout.IntField("Grid Size", gridSize);
            blockSize = EditorGUILayout.FloatField("Block Size", blockSize);
            streetWidth = EditorGUILayout.FloatField("Street Width", streetWidth);

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

            cityRoot = new GameObject("ProceduralEgyptianCity");
            Undo.RegisterCreatedObjectUndo(cityRoot, "Generate City");

            // Load Materials
            Material floorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/MarbleTiles0040_1_S_1_URP.mat");
            Material wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BrickOldSharp0108_5_S_1_URP.mat");

            if (floorMat == null || wallMat == null) {
                Debug.LogWarning("Materials not found! Please check paths.");
            }

            // Floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "DesertFloor";
            floor.transform.SetParent(cityRoot.transform);
            float totalSize = gridSize * (blockSize + streetWidth);
            floor.transform.localScale = new Vector3(totalSize / 5f, 1, totalSize / 5f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;

            // Generate Blocks
            float startX = -totalSize / 2f + blockSize / 2f;
            float startZ = -totalSize / 2f + blockSize / 2f;

            for (int x = 0; x < gridSize; x++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    // Leave some empty spots for squares
                    if (Random.value < 0.15f) continue;

                    float px = startX + x * (blockSize + streetWidth);
                    float pz = startZ + z * (blockSize + streetWidth);

                    float height = Random.Range(6f, 12f);
                    
                    GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    block.name = $"House_{x}_{z}";
                    block.transform.SetParent(cityRoot.transform);
                    block.transform.position = new Vector3(px, height / 2f, pz);
                    block.transform.localScale = new Vector3(blockSize, height, blockSize);
                    block.GetComponent<Renderer>().sharedMaterial = wallMat;
                    
                    // Add some decorative pillars occasionally
                    if (Random.value < 0.3f)
                    {
                        GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        pillar.name = "Pillar";
                        pillar.transform.SetParent(block.transform);
                        pillar.transform.localPosition = new Vector3(0.5f, 0.5f, 0.5f);
                        pillar.transform.localScale = new Vector3(0.1f, 1.2f, 0.1f);
                        pillar.GetComponent<Renderer>().sharedMaterial = wallMat;
                    }
                }
            }

            // Generate Pyramids in the distance
            GeneratePyramid(cityRoot, new Vector3(-totalSize, 0, totalSize), 100f, 60f, wallMat);
            GeneratePyramid(cityRoot, new Vector3(totalSize, 0, -totalSize * 1.2f), 150f, 80f, wallMat);

            Debug.Log("Procedural Egyptian City generated successfully.");
        }

        private void GeneratePyramid(GameObject root, Vector3 pos, float size, float height, Material mat)
        {
            GameObject pyrRoot = new GameObject("Pyramid");
            pyrRoot.transform.SetParent(root.transform);
            pyrRoot.transform.position = pos;

            int steps = 10;
            float stepHeight = height / steps;
            
            for(int i=0; i<steps; i++)
            {
                float currentSize = size * (1f - (float)i / steps);
                GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.name = $"Step_{i}";
                step.transform.SetParent(pyrRoot.transform);
                step.transform.localPosition = new Vector3(0, i * stepHeight + stepHeight/2f, 0);
                step.transform.localScale = new Vector3(currentSize, stepHeight, currentSize);
                step.GetComponent<Renderer>().sharedMaterial = mat;
            }
        }
    }
}
