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

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            // Remove problematic mesh colliders that cause console spam
            MeshCollider mc = r.GetComponent<MeshCollider>();
            if (mc != null) DestroyImmediate(mc);

            Material[] mats = r.sharedMaterials;
            foreach (var mat in mats)
            {
                if (mat == null) continue;
                
                Shader standardLit = Shader.Find("Universal Render Pipeline/Lit");
                if (standardLit != null)
                {
                    mat.shader = standardLit;
                    mat.SetColor("_BaseColor", new Color(1.0f, 0.9f, 0.75f)); // Warm tint
                    mat.SetFloat("_Smoothness", 0.1f); // Matte stone
                    mat.SetFloat("_Metallic", 0.0f);
                }

                // Set the normal map
                if (normalMap != null)
                {
                    mat.SetTexture("_BumpMap", normalMap);
                    mat.EnableKeyword("_NORMALMAP");
                    mat.SetFloat("_BumpScale", 1.5f); // Make normal map more visible
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
