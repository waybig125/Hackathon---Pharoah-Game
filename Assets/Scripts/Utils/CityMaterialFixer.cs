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

        Shader egyptianShader = Shader.Find("Custom/AncientEgyptian");
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            foreach (var mat in r.materials)
            {
                if (mat == null) continue;
                
                if (egyptianShader != null)
                {
                    mat.shader = egyptianShader;
                    mat.SetColor("_Warmth", new Color(1.0f, 0.85f, 0.7f));
                    mat.SetFloat("_Contrast", 1.3f);
                    mat.SetFloat("_SandAmount", 0.4f);
                    mat.SetFloat("_CrackScale", 15.0f);
                    mat.SetFloat("_CrackIntensity", 0.6f);
                }

                // Set the normal map
                mat.SetTexture("_BumpMap", normalMap);
                mat.EnableKeyword("_NORMALMAP");
                
                // Set tiling
                mat.mainTextureScale = new Vector2(200, 200);
            }
        }
    }
}
