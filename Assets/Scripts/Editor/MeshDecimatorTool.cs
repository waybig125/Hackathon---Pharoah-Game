using UnityEngine;
using UnityEditor;
using UnityMeshSimplifier;

namespace TheAlchemistsCrypt.Editor
{
    public class MeshDecimatorTool : EditorWindow
    {
        [MenuItem("Egyptian/Low-Poly Decimator", false, 10)]
        public static void ShowWindow() => GetWindow<MeshDecimatorTool>("Low-Poly Decimator");

        [Range(0.05f, 1.0f)]
        private float quality = 0.3f; // 30% of original polygon count
        private bool processChildren = true;
        private bool saveMeshAsset = true;

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Select one or more GameObjects in the Hierarchy, set quality, then click Decimate.\n" +
                "Quality 1.0 = original. 0.3 = 30% of original polygon count.",
                MessageType.Info);

            quality = EditorGUILayout.Slider("Quality (polygon %)", quality, 0.05f, 1.0f);
            processChildren = EditorGUILayout.Toggle("Process Children", processChildren);
            saveMeshAsset = EditorGUILayout.Toggle("Save Mesh Asset", saveMeshAsset);

            EditorGUILayout.Space();
            if (GUILayout.Button("▶ DECIMATE SELECTED", GUILayout.Height(40)))
            {
                DecimateSelected();
            }
        }

        private void DecimateSelected()
        {
            var selected = Selection.gameObjects;
            if (selected.Length == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "Select at least one GameObject in the Hierarchy.", "OK");
                return;
            }

            int meshCount = 0;
            foreach (var go in selected)
            {
                var filters = processChildren
                    ? go.GetComponentsInChildren<MeshFilter>(true)
                    : go.GetComponents<MeshFilter>();

                foreach (var mf in filters)
                {
                    if (mf.sharedMesh == null) continue;

                    Mesh original = mf.sharedMesh;
                    int originalTris = original.triangles.Length / 3;

                    var simplifier = new MeshSimplifier();
                    simplifier.Initialize(original);
                    simplifier.SimplifyMesh(quality);

                    Mesh simplified = simplifier.ToMesh();
                    simplified.name = original.name + "_LP";

                    if (saveMeshAsset)
                    {
                        string dir = "Assets/GeneratedMeshes";
                        if (!System.IO.Directory.Exists(dir))
                            System.IO.Directory.CreateDirectory(dir);

                        string path = $"{dir}/{simplified.name}.asset";
                        AssetDatabase.CreateAsset(simplified, path);
                        simplified = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    }

                    mf.sharedMesh = simplified;
                    meshCount++;

                    Debug.Log($"[Decimator] {go.name}/{mf.name}: {originalTris} → {simplified.triangles.Length / 3} tris ({quality * 100:F0}%)");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Decimator] Done. Decimated {meshCount} meshes.");
        }
    }
}
