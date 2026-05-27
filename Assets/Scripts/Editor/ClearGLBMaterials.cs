using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace TheAlchemistsCrypt.Editor
{
    public static class ClearGLBMaterials
    {
        [MenuItem("Egyptian/Reset and Fix GLB Materials", false, 20)]
        public static void ResetAndFix()
        {
            string[] guids = AssetDatabase.FindAssets("t:GameObject");
            var processedPaths = new HashSet<string>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (processedPaths.Contains(path)) continue;
                processedPaths.Add(path);

                if (path.EndsWith(".glb", System.StringComparison.OrdinalIgnoreCase) || 
                    path.EndsWith(".gltf", System.StringComparison.OrdinalIgnoreCase))
                {
        var importer = UnityEditor.AssetImporter.GetAtPath(path);
        if (importer != null)
        {
            var extMap = importer.GetExternalObjectMap();
            foreach (var kvp in extMap)
            {
                importer.RemoveRemap(kvp.Key);
            }
            UnityEditor.AssetDatabase.WriteImportSettingsIfDirty(path);
            UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceUpdate);
        }
                }
            }
            Debug.Log("Cleared all GLB remaps.");
            URPSRPBatcherFixer.FixMaterialsNoDialog();
            Debug.Log("Regenerated all materials.");
        }
    }
}
