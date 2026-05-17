# Programmatic Loopable Unity .anim Creation Guide

In Unity, animation clips imported from external assets (such as Mixamo FBX models) are **read-only sub-assets**. Attempting to modify their loop settings dynamically at runtime or in memory via `AnimationUtility.SetAnimationClipSettings` on the direct sub-asset will result in silent failure or temporary changes that reset upon entering Play Mode or reopening the editor.

This guide details the exact process and script logic to programmatically extract, duplicate, and serialize these sub-assets into native, fully loopable `.anim` files in the project's Assets folder.

---

## The Unity FBX Animation Clip Gotcha

When Unity imports an `.fbx` containing animations:
1. It encapsulates the animations as `AnimationClip` sub-assets inside the main `.fbx` file structure.
2. The sub-assets are **read-only**; they cannot be written to or modified directly on the file system without changing the FBX file itself.
3. If you query them via `AssetDatabase.LoadAllAssetsAtPath` and modify their loop properties using `AnimationUtility.SetAnimationClipSettings(clip, settings)`, Unity retains this change ONLY in transient editor memory. When the scene loads or compiles, this memory is cleared, reverting the clip to non-looping.

---

## The Foolproof Solution: Programmatic Duplication

To make animation clips permanently and reliably loop, we duplicate the read-only `AnimationClip` into a native `.anim` file on the disk, configure the copy to loop, and assign that copy to our Animator Controller states.

### Step-by-Step Implementation Logic

Here is the robust, editor-compliant C# helper function that handles this process automatically:

```csharp
using UnityEditor;
using UnityEngine;

public static class AnimationExtractor
{
    public static AnimationClip GetOrCreateLoopingClip(string fbxPath, string animName)
    {
        // 1. Load all sub-assets inside the FBX file
        var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        AnimationClip sourceClip = null;
        
        foreach (var a in assets)
        {
            if (a is AnimationClip)
            {
                var clip = (AnimationClip)a;
                // Exclude Unity internal preview clips
                if (clip.name.Contains("__preview__")) continue;
                
                sourceClip = clip;
                break; // Found the primary animation clip
            }
        }

        if (sourceClip == null)
        {
            Debug.LogError($"[AnimationExtractor] No AnimationClip found inside FBX at: {fbxPath}");
            return null;
        }

        // 2. Define the path where the native, writable .anim file will be saved
        string destPath = "Assets/Mummy_Assets/" + animName + "_loop.anim";
        
        // 3. Load existing or create a new AnimationClip asset
        AnimationClip destClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(destPath);
        if (destClip == null)
        {
            destClip = new AnimationClip();
            // Copy serializable properties from read-only clip to our new writable clip
            EditorUtility.CopySerialized(sourceClip, destClip);
            AssetDatabase.CreateAsset(destClip, destPath);
        }
        else
        {
            // Update existing copy to keep it in sync with the source FBX
            EditorUtility.CopySerialized(sourceClip, destClip);
        }

        // 4. Access and force loop settings on our serialized native .anim asset
        var settings = AnimationUtility.GetAnimationClipSettings(destClip);
        settings.loopTime = true;
        settings.loop = true;
        AnimationUtility.SetAnimationClipSettings(destClip, settings);

        // 5. Mark the native asset as dirty and force serialization on disk
        EditorUtility.SetDirty(destClip);
        AssetDatabase.SaveAssets();

        Debug.Log($"[AnimationExtractor] Successfully generated loopable native asset at: {destPath}");
        return destClip;
    }
}
```

### Why This Method is Bulletproof
- **Native Format**: `.anim` assets are native to Unity's engine and are fully writable and serializable.
- **CopySerialized**: `EditorUtility.CopySerialized` performs a deep binary copy of the animation data (keyframes, curves, events, bone bindings) without losing any precision or rigging data.
- **Persistent Loop State**: The `loopTime` and `loop` settings are permanently written to the `.meta` and binary/YAML files of the new `.anim` asset, ensuring they are preserved during build, scene reload, and system restarts.
