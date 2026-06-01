using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text;

public static class VerifyMummyModelEditor
{
    [MenuItem("Tools/Verify Mummy Model")]
    public static void Run()
    {
        StringBuilder sb = new StringBuilder();
        string[] files = { "Assets/Art/Mummy_Assets/mummy_base.fbx", "Assets/Mummy/base_basic_shaded.fbx" };
        foreach (string path in files)
        {
            sb.AppendLine("--------------------------------------------------");
            sb.AppendLine("FILE: " + path);
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null) {
                sb.AppendLine("Model not found at " + path);
                continue;
            }

            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null) {
                sb.AppendLine($"Animation Type: {importer.animationType}, Avatar Setup: {importer.avatarSetup}, Global Scale: {importer.globalScale}");
            }

            var renderers = model.GetComponentsInChildren<Renderer>(true);
            sb.AppendLine($"Total Renderers: {renderers.Length}");
            foreach (var r in renderers) {
                if (r is SkinnedMeshRenderer smr) {
                    sb.AppendLine($"SkinnedMeshRenderer: {smr.gameObject.name}, Bounds: {smr.localBounds.size}, Verts: {(smr.sharedMesh != null ? smr.sharedMesh.vertexCount.ToString() : "NULL")}");
                } else if (r is MeshRenderer mr) {
                    var mf = r.GetComponent<MeshFilter>();
                    sb.AppendLine($"MeshRenderer: {mr.gameObject.name}, Verts: {(mf != null && mf.sharedMesh != null ? mf.sharedMesh.vertexCount.ToString() : "NULL")}");
                }
            }
            
            var animator = model.GetComponent<Animator>();
            if (animator != null) {
                sb.AppendLine($"Animator Avatar: {(animator.avatar != null ? animator.avatar.name : "NULL")}");
            } else {
                sb.AppendLine("No Animator found on root.");
            }
        }
        
        File.WriteAllText("Mummy_Validation.txt", sb.ToString());
        Debug.Log("Validation saved to Mummy_Validation.txt");
    }
}
