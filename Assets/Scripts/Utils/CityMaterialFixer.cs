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
            foreach (var mat in r.materials) // Use .materials to create instances if needed, or .sharedMaterials
            {
                if (mat == null) continue;
                
                // Set the normal map
                mat.SetTexture("_BumpMap", normalMap);
                mat.EnableKeyword("_NORMALMAP");
                
                // Ensure tiling is high enough (increased to 200 to reduce stretching)
                mat.mainTextureScale = new Vector2(200, 200);
                
                // Adjust strength if possible
                if (mat.HasProperty("_BumpScale"))
                    mat.SetFloat("_BumpScale", 1.0f);
            }
        }
    }
}
