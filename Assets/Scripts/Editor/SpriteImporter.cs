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
            "Assets/Resources/egypt_themed_icons/joystick_outer.png",
            "Assets/Resources/egypt_themed_icons/joystick_knob.png",
            "Assets/Resources/egypt_themed_icons/fire.png",
            "Assets/Resources/egypt_themed_icons/reload_ammo.png",
            "Assets/Resources/egypt_themed_icons/swap_weapon.png",
            "Assets/Resources/egypt_themed_icons/sprint.png",
            "Assets/Resources/egypt_themed_icons/jump.png",
            "Assets/Resources/egypt_themed_icons/focus_icon.png",
            "Assets/Resources/Textures/Smog.png"
        };

        foreach (var path in paths)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
                Debug.Log($"[SpriteImporter] Converted {path} to Sprite.");
            }
        }
    }
}
