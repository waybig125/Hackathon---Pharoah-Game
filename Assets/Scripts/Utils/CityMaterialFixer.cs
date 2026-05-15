using UnityEngine;

public class CityMaterialFixer : MonoBehaviour
{
    public Texture normalMap;

    void Start()
    {
        FixMaterials();
    }

    [ContextMenu("Fix Materials")]
    public void FixMaterials()
    {
        if (normalMap == null)
        {
            normalMap = Resources.Load<Texture>("Textures/EgyptianNormalMap");
        }

        if (normalMap == null) return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            // Use sharedMaterials to avoid leaking material instances in editor
            foreach (var mat in r.sharedMaterials)
            {
                if (mat == null) continue;

                // Only set the normal map - don't change the shader or colors
                // The original city materials already have the correct URP shader assigned
                if (mat.HasProperty("_BumpMap"))
                {
                    mat.SetTexture("_BumpMap", normalMap);
                    mat.EnableKeyword("_NORMALMAP");
                }
                if (mat.HasProperty("_BumpScale"))
                    mat.SetFloat("_BumpScale", 1.0f);

                // Reduce tiling to fix stretching (was 200 which caused blurring)
                mat.mainTextureScale = new Vector2(10, 10);
            }
        }
    }
}
