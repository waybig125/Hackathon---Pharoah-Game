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

        Shader egyptianShader = Shader.Find("Custom/AncientEgyptian");
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            // Remove problematic mesh colliders that cause console spam
            MeshCollider mc = r.GetComponent<MeshCollider>();
            if (mc != null) DestroyImmediate(mc);

            foreach (var mat in r.materials)
            {
                if (mat == null) continue;
                
                if (egyptianShader != null)
                {
                    mat.shader = egyptianShader;
                    mat.SetColor("_Warmth", new Color(1.0f, 0.9f, 0.75f)); // Warmer
                    mat.SetFloat("_Contrast", 1.2f);
                    mat.SetFloat("_SandAmount", 0.3f);
                    mat.SetFloat("_CrackScale", 25.0f);
                    mat.SetFloat("_CrackIntensity", 0.4f);
                }

                // Set the normal map
                if (normalMap != null)
                {
                    mat.SetTexture("_BumpMap", normalMap);
                    mat.EnableKeyword("_NORMALMAP");
                }
                
                // Set tiling
                mat.mainTextureScale = new Vector2(200, 200);
            }
        }

        // Add a box collider to the root if not exists for basic floor collision
        if (GetComponent<BoxCollider>() == null)
        {
            BoxCollider floor = gameObject.AddComponent<BoxCollider>();
            floor.size = new Vector3(2000, 1, 2000); // Massive floor
            floor.center = new Vector3(0, -0.5f, 0);
        }
    }
}
