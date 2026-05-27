using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class SpriteImporter
{
    static SpriteImporter()
    {
        EditorApplication.delayCall += ImportSprites;
    }

    private static void ImportSprites()
    {
        string[] paths = new string[]
        {
            "Assets/Resources/egypt_themed_icons_generated/joystick_ring.png",
            "Assets/Resources/egypt_themed_icons_generated/joystick_knob.png"
        };

        bool importedAny = false;
        foreach (var path in paths)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
                importedAny = true;
                Debug.Log($"[SpriteImporter] Converted {path} to Sprite.");
            }
        }
    }
}
