using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcessSetup : EditorWindow
{
    [MenuItem("Tools/Enhance Post Processing")]
    public static void Enhance()
    {
        var volumes = Object.FindObjectsByType<Volume>(FindObjectsInactive.Include);
        Volume globalVolume = null;
        
        foreach (var v in volumes)
        {
            if (v.isGlobal)
            {
                globalVolume = v;
                break;
            }
        }
        
        if (globalVolume == null)
        {
            var go = new GameObject("Global Post-Processing Volume");
            globalVolume = go.AddComponent<Volume>();
            globalVolume.isGlobal = true;
        }

        VolumeProfile profile = globalVolume.profile;
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "Enhanced_URP_Profile";
            AssetDatabase.CreateAsset(profile, "Assets/Settings/Enhanced_URP_Profile.asset");
            globalVolume.profile = profile;
        }

        // Bloom
        if (!profile.TryGet<Bloom>(out var bloom)) bloom = profile.Add<Bloom>(false);
        bloom.active = true;
        bloom.intensity.Override(1.5f);
        bloom.threshold.Override(0.9f);
        bloom.scatter.Override(0.7f);
        bloom.tint.Override(new Color(1f, 0.9f, 0.8f)); // Warm golden tint

        // Chromatic Aberration
        if (!profile.TryGet<ChromaticAberration>(out var chromatic)) chromatic = profile.Add<ChromaticAberration>(false);
        chromatic.active = true;
        chromatic.intensity.Override(0.15f);

        // Color Adjustments
        if (!profile.TryGet<ColorAdjustments>(out var colorAdjust)) colorAdjust = profile.Add<ColorAdjustments>(false);
        colorAdjust.active = true;
        colorAdjust.postExposure.Override(0.1f);
        colorAdjust.contrast.Override(15f);
        colorAdjust.saturation.Override(5f);

        // Vignette
        if (!profile.TryGet<Vignette>(out var vignette)) vignette = profile.Add<Vignette>(false);
        vignette.active = true;
        vignette.intensity.Override(0.35f);
        vignette.smoothness.Override(0.4f);

        // Film Grain
        if (!profile.TryGet<FilmGrain>(out var grain)) grain = profile.Add<FilmGrain>(false);
        grain.active = true;
        grain.type.Override(FilmGrainLookup.Medium1);
        grain.intensity.Override(0.4f);
        
        EditorUtility.SetDirty(profile);
        EditorUtility.SetDirty(globalVolume.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(globalVolume.gameObject.scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[PostProcessSetup] Successfully enhanced URP Post-Processing Volume.");
    }
}
