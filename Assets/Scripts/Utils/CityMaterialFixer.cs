using UnityEngine;

public class CityMaterialFixer : MonoBehaviour
{
    public Texture normalMap;

    void Start()
    {
        FixMaterials();
        FixWindowsAndLights();
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
            if (r.gameObject.name.ToLower().Contains("floor") || r.gameObject.name.ToLower().Contains("ground"))
                continue;

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

    [ContextMenu("Fix Windows and Lights")]
    public void FixWindowsAndLights()
    {
        // 1. Destroy all Light components on "WindowLight" objects
        int lightsRemoved = 0;
        Light[] lights = GetComponentsInChildren<Light>(true);
        foreach (var l in lights)
        {
            if (l.gameObject.name.Equals("WindowLight"))
            {
                if (Application.isPlaying)
                    Destroy(l);
                else
                    DestroyImmediate(l);
                lightsRemoved++;
            }
        }
        Debug.Log($"[CityMaterialFixer] Removed {lightsRemoved} real-time window lights.");

        // 2. Adjust window offsets to eliminate Z-fighting
        int windowsAdjusted = 0;
        Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
        foreach (var t in allTransforms)
        {
            if (t == null) continue;
            // Check scale to identify window cubes (approx 3.6f, 2.6f, 0.3f)
            Vector3 scale = t.localScale;
            if (Mathf.Approximately(scale.x, 3.6f) && Mathf.Approximately(scale.y, 2.6f) && Mathf.Approximately(scale.z, 0.3f))
            {
                Vector3 localPos = t.localPosition;
                // Identify which axis the window is offset on
                if (Mathf.Abs(localPos.x) < 0.01f) // Z-axis window
                {
                    float dist = Mathf.Abs(localPos.z);
                    float baseValue = Mathf.Round(dist);
                    float currentOffset = dist - baseValue;
                    if (currentOffset > 0.10f && currentOffset < 0.16f)
                    {
                        t.localPosition = new Vector3(localPos.x, localPos.y, Mathf.Sign(localPos.z) * (baseValue + 0.18f));
                        windowsAdjusted++;
                    }
                }
                else if (Mathf.Abs(localPos.z) < 0.01f) // X-axis window
                {
                    float dist = Mathf.Abs(localPos.x);
                    float baseValue = Mathf.Round(dist);
                    float currentOffset = dist - baseValue;
                    if (currentOffset > 0.10f && currentOffset < 0.16f)
                    {
                        t.localPosition = new Vector3(Mathf.Sign(localPos.x) * (baseValue + 0.18f), localPos.y, localPos.z);
                        windowsAdjusted++;
                    }
                }
            }
        }
        Debug.Log($"[CityMaterialFixer] Adjusted {windowsAdjusted} window offsets for Z-fighting.");
    }
}
