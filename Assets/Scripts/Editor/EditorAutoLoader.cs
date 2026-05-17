using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class EditorAutoLoader
{
    static EditorAutoLoader()
    {
        // Delay call to ensure the Editor is fully initialized
        EditorApplication.delayCall += Initialize;
    }

    private static void Initialize()
    {
        // 1. Optimize texture icons to be uncompressed, no mipmaps, and bilinear for ultra-crisp resolution
        OptimizeThemedIcons();

        // 2. Ensure MainGame.unity is scene 0 in build settings
        EnsureMainGameInBuildSettings();

        // 3. Load MainGame scene if empty scene active
        var currentScene = EditorSceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(currentScene.path) || currentScene.name == "SampleScene" || currentScene.name == "Empty" || string.IsNullOrEmpty(currentScene.name))
        {
            Debug.Log("[EditorAutoLoader] Automatically loading the main scene: Assets/Scenes/MainGame.unity");
            EditorSceneManager.OpenScene("Assets/Scenes/MainGame.unity", OpenSceneMode.Single);
        }
    }

    private static void EnsureMainGameInBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes;
        bool mainGameExists = false;
        int mainGameIndex = -1;
        for (int i = 0; i < scenes.Length; i++)
        {
            if (scenes[i].path.Contains("MainGame.unity"))
            {
                mainGameExists = true;
                mainGameIndex = i;
                break;
            }
        }

        if (!mainGameExists || mainGameIndex != 0)
        {
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            list.Add(new EditorBuildSettingsScene("Assets/Scenes/MainGame.unity", true));
            for (int i = 0; i < scenes.Length; i++)
            {
                if (!scenes[i].path.Contains("MainGame.unity"))
                {
                    list.Add(scenes[i]);
                }
            }
            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log("[EditorAutoLoader] Ensured MainGame.unity is the active first scene in Build Settings.");
        }
    }

    private static void OptimizeThemedIcons()
    {
        string[] folders = new string[] { "Assets/Resources/egypt_themed_icons", "Assets/Resources/egyptian_items" };
        bool anyChanged = false;

        foreach (string folder in folders)
        {
            if (!System.IO.Directory.Exists(folder)) continue;

            string[] guids = AssetDatabase.FindAssets("t:Texture", new[] { folder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    bool changed = false;
                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        changed = true;
                    }
                    if (importer.spriteImportMode != SpriteImportMode.Single)
                    {
                        importer.spriteImportMode = SpriteImportMode.Single;
                        changed = true;
                    }
                    if (importer.mipmapEnabled)
                    {
                        importer.mipmapEnabled = false;
                        changed = true;
                    }
                    if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                    {
                        importer.textureCompression = TextureImporterCompression.Uncompressed;
                        changed = true;
                    }
                    if (importer.filterMode != FilterMode.Bilinear)
                    {
                        importer.filterMode = FilterMode.Bilinear;
                        changed = true;
                    }
                    if (importer.maxTextureSize != 2048)
                    {
                        importer.maxTextureSize = 2048;
                        changed = true;
                    }
                    if (changed)
                    {
                        importer.SaveAndReimport();
                        anyChanged = true;
                    }
                }
            }
        }

        if (anyChanged)
        {
            Debug.Log("[TextureOptimizer] Successfully optimized all Egyptian theme icons and items to Uncompressed UI Sprites!");
        }
    }
}
