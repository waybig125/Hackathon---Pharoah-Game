using UnityEngine;
using Unity.AI.Navigation;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityMeshSimplifier;

namespace TheAlchemistsCrypt.Editor
{
    public partial class StaticEgyptianCityGenerator
    {
        private void DecimateMesh(GameObject obj, float quality)
                {
                    var filters = obj.GetComponentsInChildren<MeshFilter>();
                    foreach (var mf in filters) {
                        if (mf.sharedMesh == null) continue;
                        try {
                            var simplifier = new MeshSimplifier();
                            simplifier.Initialize(mf.sharedMesh);
                            simplifier.SimplifyMesh(quality);
                            mf.sharedMesh = simplifier.ToMesh();
                        } catch {}
                    }
                }

        private Mesh GetSharedDecimatedColumnMesh(GameObject columnPrefab)
                {
                    if (columnPrefab == null) return null;
                    
                    string decimatedMeshPath = columnPrefab.name.Contains("pillar") 
                        ? "Assets/Art/EgyptianAssets/egyptian_pillar_column_decimated.mesh" 
                        : "Assets/Art/EgyptianAssets/egyptian_column_decimated.mesh";
                    Mesh decimatedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(decimatedMeshPath);
                    if (decimatedMesh != null) return decimatedMesh;
                    
                    // Generate it once
                    var mf = columnPrefab.GetComponentInChildren<MeshFilter>();
                    if (mf == null || mf.sharedMesh == null) return null;
                    
                    try {
                        var simplifier = new MeshSimplifier();
                        simplifier.Initialize(mf.sharedMesh);
                        simplifier.SimplifyMesh(0.15f);
                        decimatedMesh = simplifier.ToMesh();
                        decimatedMesh.RecalculateBounds();

                        // Validation check
                        bool isValid = true;
                        Vector3[] verts = decimatedMesh.vertices;
                        for (int i = 0; i < verts.Length; i++) {
                            if (float.IsNaN(verts[i].x) || float.IsNaN(verts[i].y) || float.IsNaN(verts[i].z) ||
                                float.IsInfinity(verts[i].x) || float.IsInfinity(verts[i].y) || float.IsInfinity(verts[i].z)) {
                                isValid = false;
                                break;
                            }
                        }
                        
                        if (isValid) {
                            float origMag = mf.sharedMesh.bounds.size.magnitude;
                            float decMag = decimatedMesh.bounds.size.magnitude;
                            if (decMag > origMag * 1.5f || decMag < origMag * 0.5f) {
                                isValid = false;
                            }
                        }

                        if (isValid) {
                            int[] tris = decimatedMesh.triangles;
                            for (int i = 0; i < tris.Length; i += 3) {
                                if (tris[i] >= verts.Length || tris[i+1] >= verts.Length || tris[i+2] >= verts.Length) continue;
                                Vector3 v0 = verts[tris[i]];
                                Vector3 v1 = verts[tris[i+1]];
                                Vector3 v2 = verts[tris[i+2]];
                                if (Vector3.Distance(v0, v1) > 15f || 
                                    Vector3.Distance(v1, v2) > 15f || 
                                    Vector3.Distance(v2, v0) > 15f) {
                                    isValid = false;
                                    break;
                                }
                            }
                        }
                        
                        if (!isValid) {
                            Debug.LogWarning("[CityGen] Decimated column mesh failed validation. Reverting to original mesh.");
                            return mf.sharedMesh;
                        }

                        decimatedMesh.name = columnPrefab.name.Contains("pillar") 
                            ? "egyptian_pillar_column_decimated" 
                            : "egyptian_column_decimated";
                        AssetDatabase.CreateAsset(decimatedMesh, decimatedMeshPath);
                        AssetDatabase.SaveAssets();
                        Debug.Log("[CityGen] Created low-poly decimated column mesh asset at: " + decimatedMeshPath);
                        return decimatedMesh;
                    } catch (System.Exception e) {
                        Debug.LogError("[CityGen] Failed to decimate column mesh: " + e.Message);
                        return mf.sharedMesh;
                    }
                }

        private Mesh GetSharedDecimatedMesh(Mesh originalMesh, float quality)
                {
                    if (originalMesh == null) return null;
                    
                    string meshName = originalMesh.name;
                    if (string.IsNullOrEmpty(meshName)) meshName = "unnamed_mesh_" + originalMesh.GetHashCode();
                    
                    string sanitizedName = System.Text.RegularExpressions.Regex.Replace(meshName, @"[^a-zA-Z0-9_\-]", "_");
                    string decimatedDir = "Assets/Art/EgyptianAssets/DecimatedMeshes";
                    if (!System.IO.Directory.Exists(decimatedDir)) {
                        System.IO.Directory.CreateDirectory(decimatedDir);
                    }
                    
                    string decimatedMeshPath = $"{decimatedDir}/{sanitizedName}_dec_{quality:F2}.mesh";
                    Mesh decimatedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(decimatedMeshPath);
                    if (decimatedMesh != null) return decimatedMesh;
                    
                    try {
                        var simplifier = new MeshSimplifier();
                        simplifier.Initialize(originalMesh);
                        simplifier.SimplifyMesh(quality);
                        decimatedMesh = simplifier.ToMesh();
                        decimatedMesh.RecalculateBounds();

                        // Validation check
                        bool isValid = true;
                        Vector3[] verts = decimatedMesh.vertices;
                        for (int i = 0; i < verts.Length; i++) {
                            if (float.IsNaN(verts[i].x) || float.IsNaN(verts[i].y) || float.IsNaN(verts[i].z) ||
                                float.IsInfinity(verts[i].x) || float.IsInfinity(verts[i].y) || float.IsInfinity(verts[i].z)) {
                                isValid = false;
                                break;
                            }
                        }
                        
                        if (isValid) {
                            float origMag = originalMesh.bounds.size.magnitude;
                            float decMag = decimatedMesh.bounds.size.magnitude;
                            if (decMag > origMag * 1.5f || decMag < origMag * 0.5f) {
                                isValid = false;
                            }
                        }

                        if (isValid) {
                            int[] tris = decimatedMesh.triangles;
                            for (int i = 0; i < tris.Length; i += 3) {
                                if (tris[i] >= verts.Length || tris[i+1] >= verts.Length || tris[i+2] >= verts.Length) continue;
                                Vector3 v0 = verts[tris[i]];
                                Vector3 v1 = verts[tris[i+1]];
                                Vector3 v2 = verts[tris[i+2]];
                                if (Vector3.Distance(v0, v1) > 15f || 
                                    Vector3.Distance(v1, v2) > 15f || 
                                    Vector3.Distance(v2, v0) > 15f) {
                                    isValid = false;
                                    break;
                                }
                            }
                        }
                        
                        if (!isValid) {
                            Debug.LogWarning($"[CityGen] Decimated mesh for {originalMesh.name} failed validation. Reverting to original mesh.");
                            return originalMesh;
                        }

                        decimatedMesh.name = originalMesh.name + "_decimated";
                        AssetDatabase.CreateAsset(decimatedMesh, decimatedMeshPath);
                        AssetDatabase.SaveAssets();
                        Debug.Log("[CityGen] Created decimated mesh asset at: " + decimatedMeshPath);
                        return decimatedMesh;
                    } catch (System.Exception e) {
                        Debug.LogError("[CityGen] Failed to decimate mesh: " + e.Message);
                        return originalMesh;
                    }
                }

        private void DecimateRecursively(GameObject obj, float quality)
                {
                    var filters = obj.GetComponentsInChildren<MeshFilter>(true);
                    foreach (var mf in filters) {
                        if (mf.sharedMesh != null) {
                            var decimated = GetSharedDecimatedMesh(mf.sharedMesh, quality);
                            if (decimated != null) mf.sharedMesh = decimated;
                        }
                    }
                }

        private void AddLODGroupToPalmTree(GameObject obj)
                {
                    if (obj == null) return;

                    // Collect all renderers on this tree (may be nested in sub-objects)
                    var renderers = obj.GetComponentsInChildren<Renderer>(true);
                    if (renderers == null || renderers.Length == 0) return;

                    // Remove any existing LODGroup to avoid duplicates on re-generation
                    var existing = obj.GetComponent<LODGroup>();
                    if (existing != null) DestroyImmediate(existing);

                    var lodGroup = obj.AddComponent<LODGroup>();

                    // LOD0: full-quality render up to 30m screen-relative size threshold
                    // LOD1: same renderers, visible up to 60m (Unity handles reduced overdraw)
                    // Cull beyond LOD1
                    var lod0 = new LOD(0.15f, renderers); // 15% screen height → ~30m at typical FOV
                    var lod1 = new LOD(0.05f, renderers); // 5% screen height → ~60m at typical FOV
                    lodGroup.SetLODs(new LOD[] { lod0, lod1 });
                    lodGroup.RecalculateBounds();

                    Debug.Log($"[CityGen] Added LOD group to palm tree: {obj.name}");
                }

        private void RemoveFloorsFromLandmarks(GameObject obj)
                {
                    if (obj == null) return;
                    var renderers = obj.GetComponentsInChildren<Renderer>(true);
                    foreach (var r in renderers)
                    {
                        if (r == null) continue;
                        string nameLower = r.gameObject.name.ToLower();
                        // Deactivate children containing floor, ground, plane, platform, or deck in their names
                        if (nameLower.Contains("floor") || 
                            nameLower.Contains("ground") || 
                            nameLower.Contains("plane") || 
                            nameLower.Contains("platform") ||
                            nameLower.Contains("deck"))
                        {
                            r.gameObject.SetActive(false);
                            Debug.Log($"[CityGen] Programmatically disabled floor/ground element: '{r.gameObject.name}' in '{obj.name}'");
                        }
                    }
                }

        private void CleanupOverlappingColumns(GameObject root)
                {
                    var columns = new List<GameObject>();
                    var houses = new List<GameObject>();

                    // Find all columns and houses
                    foreach (Transform t in root.transform)
                    {
                        if (t.name.Contains("Plaza"))
                        {
                            foreach (Transform child in t)
                            {
                                if (child.name.ToLower().Contains("column") || child.name.ToLower().Contains("pillar"))
                                {
                                    columns.Add(child.gameObject);
                                }
                            }
                        }
                        else if (t.name.Contains("House") || t.name.Contains("AlchemistTomb"))
                        {
                            houses.Add(t.gameObject);
                        }
                    }

                    // For each column, check if it intersects any house's renderer bounds
                    foreach (var col in columns)
                    {
                        var colRenderer = col.GetComponentInChildren<Renderer>();
                        if (colRenderer == null) continue;
                        Bounds colBounds = colRenderer.bounds;
                        colBounds.Expand(1.5f); // Expand bounds to prevent columns clipping walls

                        foreach (var house in houses)
                        {
                            var houseRenderers = house.GetComponentsInChildren<Renderer>();
                            foreach (var hr in houseRenderers)
                            {
                                if (hr.name.Contains("Floor") || hr.name.Contains("floor") || hr.name.Contains("TransmutationCircle")) continue;
                                if (hr.bounds.Intersects(colBounds))
                                {
                                    // Overlap detected! Destroy the column
                                    DestroyImmediate(col);
                                    goto NextColumn;
                                }
                            }
                        }
                        NextColumn:;
                    }
                }

        private float GetTerrainHeight(Vector3 pos) {
                    var terrain = Terrain.activeTerrain;
                    return (terrain != null) ? terrain.SampleHeight(pos) : 0f;
                }

        private Vector3 GetTerrainNormal(Vector3 worldPos) {
                    var terrain = Terrain.activeTerrain;
                    if (terrain == null || terrain.terrainData == null) return Vector3.up;
                    
                    Vector3 terrainLocalPos = worldPos - terrain.transform.position;
                    float normX = Mathf.Clamp01(terrainLocalPos.x / terrain.terrainData.size.x);
                    float normZ = Mathf.Clamp01(terrainLocalPos.z / terrain.terrainData.size.z);
                    return terrain.terrainData.GetInterpolatedNormal(normX, normZ);
                }

        private float GetMeshBottomWorldY(GameObject obj)
                {
                    float worldMinY = float.MaxValue;
                    var filters = obj.GetComponentsInChildren<MeshFilter>(true);
                    foreach (var filter in filters)
                    {
                        if (filter.sharedMesh == null) continue;
                        Vector3[] vertices = filter.sharedMesh.vertices;
                        foreach (var v in vertices)
                        {
                            Vector3 worldV = filter.transform.TransformPoint(v);
                            if (worldV.y < worldMinY) worldMinY = worldV.y;
                        }
                    }
                    if (worldMinY != float.MaxValue) return worldMinY;
                    
                    var renderers = obj.GetComponentsInChildren<Renderer>(true);
                    float minY = float.MaxValue;
                    foreach (var r in renderers)
                    {
                        if (r is ParticleSystemRenderer) continue;
                        if (r.bounds.min.y < minY) minY = r.bounds.min.y;
                    }
                    return (minY != float.MaxValue) ? minY : obj.transform.position.y;
                }

        private void AlignToGroundAndAddCollider(GameObject obj, Vector3 basePos, Quaternion targetRot, float offsetAdjustment, bool alignToTerrain = true)
                {
                    if (PrefabUtility.IsPartOfPrefabInstance(obj))
                    {
                        PrefabUtility.UnpackPrefabInstance(obj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    }

                    var allRenderers = obj.GetComponentsInChildren<Renderer>(true);
                    foreach (var r in allRenderers)
                    {
                        if (r.sharedMaterials == null) continue;
                        Material[] mats = r.sharedMaterials;
                        bool changed = false;
                        for (int i = 0; i < mats.Length; i++)
                        {
                            if (mats[i] == null) continue;
                            string shaderName = mats[i].shader.name;
                            if (shaderName.Contains("Standard") || shaderName.Contains("Built-in") || shaderName == "Legacy Shaders/Diffuse" || shaderName == "Diffuse")
                            {
                                var oldMat = mats[i];
                                
                                Texture mainTex = oldMat.HasProperty("_MainTex") ? oldMat.GetTexture("_MainTex") : null;
                                Texture bumpMap = oldMat.HasProperty("_BumpMap") ? oldMat.GetTexture("_BumpMap") : null;
                                Color col = oldMat.HasProperty("_Color") ? oldMat.GetColor("_Color") : Color.white;
                                float metallic = oldMat.HasProperty("_Metallic") ? oldMat.GetFloat("_Metallic") : 0f;
                                float smoothness = oldMat.HasProperty("_Glossiness") ? oldMat.GetFloat("_Glossiness") : 0f;

                                string mainTexId = mainTex != null ? mainTex.name + "_" + mainTex.GetHashCode() : "null";
                                string bumpMapId = bumpMap != null ? bumpMap.name + "_" + bumpMap.GetHashCode() : "null";
                                string cacheKey = $"{oldMat.name}_{mainTexId}_{bumpMapId}_{col}_{metallic}_{smoothness}";
                                
                                if (!convertedMaterialsCache.TryGetValue(cacheKey, out Material newMat))
                                {
                                    string safeName = System.Text.RegularExpressions.Regex.Replace(oldMat.name, @"[^a-zA-Z0-9_\-]", "_");
                                    string materialAssetPath = $"Assets/Art/Materials/Generated/{safeName}_URP.mat";
                                    newMat = AssetDatabase.LoadAssetAtPath<Material>(materialAssetPath);
                                    
                                    if (newMat == null)
                                    {
                                        newMat = new Material(GetLitShader());
                                        newMat.name = oldMat.name + "_URP";
                                        newMat.enableInstancing = true;
                                        if (oldMat.HasProperty("_Color")) newMat.SetColor("_BaseColor", col);
                                        if (mainTex != null) newMat.SetTexture("_BaseMap", mainTex);
                                        if (bumpMap != null) {
                                            newMat.SetTexture("_BumpMap", bumpMap);
                                            newMat.EnableKeyword("_NORMALMAP");
                                        }
                                        newMat.SetFloat("_Metallic", metallic);
                                        newMat.SetFloat("_Smoothness", smoothness);

                                        if (!System.IO.Directory.Exists("Assets/Art/Materials/Generated")) 
                                            System.IO.Directory.CreateDirectory("Assets/Art/Materials/Generated");
                                        AssetDatabase.CreateAsset(newMat, materialAssetPath);
                                    }
                                    
                                    convertedMaterialsCache[cacheKey] = newMat;
                                }
                                
                                mats[i] = newMat; changed = true;
                            }
                        }
                        if (changed) r.sharedMaterials = mats;
                        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                        r.receiveShadows = true;
                    }

                    // 1. Calculate local bounds in root space using pure local hierarchy math
                    Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);
                    bool hasBounds = false;
                    var meshFilters = obj.GetComponentsInChildren<MeshFilter>(true);
                    foreach (var mf in meshFilters) {
                        if (mf.sharedMesh == null) continue;
                        Bounds meshBounds = mf.sharedMesh.bounds;
                        
                        // Pure local matrix hierarchy computation to root object space
                        Matrix4x4 childToRoot = Matrix4x4.identity;
                        Transform curr = mf.transform;
                        while (curr != null && curr != obj.transform) {
                            childToRoot = Matrix4x4.TRS(curr.localPosition, curr.localRotation, curr.localScale) * childToRoot;
                            curr = curr.parent;
                        }
                        
                        Vector3[] corners = GetBoundsCorners(meshBounds);
                        foreach (var corner in corners) {
                            Vector3 localCorner = childToRoot.MultiplyPoint3x4(corner);
                            if (!hasBounds) {
                                localBounds = new Bounds(localCorner, Vector3.zero);
                                hasBounds = true;
                            } else {
                                localBounds.Encapsulate(localCorner);
                            }
                        }
                    }

                    // 2. Set rotation first
                    Vector3 targetPos = basePos;
                    Quaternion finalRot = targetRot;
                    if (alignToTerrain) {
                        targetPos.y = GetTerrainHeight(basePos);
                        Vector3 normal = GetTerrainNormal(basePos);
                        Quaternion normalRot = Quaternion.FromToRotation(Vector3.up, normal);
                        finalRot = normalRot * targetRot;
                    }
                    obj.transform.rotation = finalRot;

                    // 3. Find world bottom Y and align
                    float worldMinY = targetPos.y;
                    if (hasBounds) {
                        float minVal = float.MaxValue;
                        Vector3[] corners = GetBoundsCorners(localBounds);
                        foreach (var corner in corners) {
                            // Temporarily compute world position using TRS matrix
                            Vector3 worldCorner = Matrix4x4.TRS(targetPos, finalRot, obj.transform.localScale).MultiplyPoint3x4(corner);
                            if (worldCorner.y < minVal) minVal = worldCorner.y;
                        }
                        worldMinY = minVal;
                    } else {
                        var renderers = obj.GetComponentsInChildren<Renderer>(true);
                        float minY = float.MaxValue;
                        foreach (var r in renderers) {
                            if (r is ParticleSystemRenderer) continue;
                            if (r.bounds.min.y < minY) minY = r.bounds.min.y;
                        }
                        if (minY != float.MaxValue) worldMinY = minY;
                    }
                    
                    float yOffset = (targetPos.y + offsetAdjustment) - worldMinY;
                    
                    bool isDynamic = obj.name.ToLower().Contains("crate") || obj.name.ToLower().Contains("barrel");
                    if (isDynamic) {
                        var dynamicProps = GameObject.Find("DynamicProps");
                        if (dynamicProps == null)
                        {
                            dynamicProps = new GameObject("DynamicProps");
                            dynamicProps.isStatic = false;
                        }
                        obj.transform.SetParent(dynamicProps.transform);
                        obj.transform.position = new Vector3(targetPos.x, targetPos.y + yOffset + 0.05f, targetPos.z);
                    } else {
                        obj.transform.position = new Vector3(targetPos.x, targetPos.y + yOffset, targetPos.z);
                    }

                    foreach (var col in obj.GetComponentsInChildren<Collider>(true)) DestroyImmediate(col);
                    
                    if (isDynamic) {
                        obj.isStatic = false;
                        foreach (Transform t in obj.GetComponentsInChildren<Transform>(true)) {
                            t.gameObject.isStatic = false;
                        }
                        var rb = obj.GetComponent<Rigidbody>();
                        if (rb == null) rb = obj.AddComponent<Rigidbody>();
                        rb.isKinematic = false;
                        rb.useGravity = true;
                        rb.mass = 10f;
                        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                        rb.linearDamping = 0.5f;
                        rb.angularDamping = 0.5f;
                        
                        var bc = obj.AddComponent<BoxCollider>();
                        if (hasBounds) {
                            bc.center = localBounds.center;
                            bc.size = localBounds.size;
                        }
                    } else {
                        var filters = obj.GetComponentsInChildren<MeshFilter>(true);
                        if (filters.Length > 0) {
                            foreach (var filterObj in filters) {
                                if (filterObj.sharedMesh == null) continue;
                                var mc = filterObj.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = filterObj.sharedMesh;
                            }
                        } else obj.AddComponent<BoxCollider>();
                    }
                }

        private static Vector3[] GetBoundsCorners(Bounds b)
                {
                    return new Vector3[] {
                        b.min,
                        b.max,
                        new Vector3(b.min.x, b.min.y, b.max.z),
                        new Vector3(b.min.x, b.max.y, b.min.z),
                        new Vector3(b.max.x, b.min.y, b.min.z),
                        new Vector3(b.min.x, b.max.y, b.max.z),
                        new Vector3(b.max.x, b.min.y, b.max.z),
                        new Vector3(b.max.x, b.max.y, b.min.z)
                    };
                }

        private static void UpdateWeaponPrefabMaterials()
                {
                    // 1. Create or update the material assets
                    var matCasings = GetOrCreateMaterial("M_WEP_Casings_Gold", new Color(0.8f, 0.6f, 0.2f), 0.8f, 0.9f, Color.clear, false);

                    var matSulfurBody = GetOrCreateMaterial("M_WEP_Sulfur_Body", new Color(0.15f, 0.08f, 0.04f), 0.75f, 0.9f, Color.clear, false);
                    var matSulfurAccent = GetOrCreateMaterial("M_WEP_Sulfur_Accent", new Color(1.0f, 0.55f, 0.05f), 0.1f, 0.0f, new Color(1.0f, 0.55f, 0.05f) * 8f, true);
                    var matSulfurSecondary = GetOrCreateMaterial("M_WEP_Sulfur_Secondary", new Color(0.12f, 0.12f, 0.12f), 0.8f, 0.9f, Color.clear, false);

                    var matMercuryBody = GetOrCreateMaterial("M_WEP_Mercury_Body", new Color(0.05f, 0.12f, 0.15f), 0.75f, 0.9f, Color.clear, false);
                    var matMercuryAccent = GetOrCreateMaterial("M_WEP_Mercury_Accent", new Color(0.0f, 0.85f, 1.0f), 0.1f, 0.0f, new Color(0.0f, 0.85f, 1.0f) * 8f, true);
                    var matMercurySecondary = GetOrCreateMaterial("M_WEP_Mercury_Secondary", new Color(0.12f, 0.12f, 0.12f), 0.8f, 0.9f, Color.clear, false);

                    var matSaltBody = GetOrCreateMaterial("M_WEP_Salt_Body", new Color(0.12f, 0.06f, 0.15f), 0.75f, 0.9f, Color.clear, false);
                    var matSaltAccent = GetOrCreateMaterial("M_WEP_Salt_Accent", new Color(0.85f, 0.55f, 1.0f), 0.1f, 0.0f, new Color(0.85f, 0.55f, 1.0f) * 8f, true);
                    var matSaltSecondary = GetOrCreateMaterial("M_WEP_Salt_Secondary", new Color(0.12f, 0.12f, 0.12f), 0.8f, 0.9f, Color.clear, false);

                    AssetDatabase.SaveAssets();

                    // 2. Load and configure the weapon prefabs
                    string[] wPrefabs = {
                        "Assets/Prefabs/WEP_Sulfur.prefab",
                        "Assets/Prefabs/WEP_Mercury.prefab",
                        "Assets/Prefabs/WEP_Salt.prefab"
                    };

                    foreach (var path in wPrefabs)
                    {
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (prefab == null) continue;

                        string wName = prefab.name.ToLower();
                        Material bodyMat = matSulfurBody;
                        Material accentMat = matSulfurAccent;
                        Material secMat = matSulfurSecondary;

                        if (wName.Contains("mercury"))
                        {
                            bodyMat = matMercuryBody;
                            accentMat = matMercuryAccent;
                            secMat = matMercurySecondary;
                        }
                        else if (wName.Contains("salt"))
                        {
                            bodyMat = matSaltBody;
                            accentMat = matSaltAccent;
                            secMat = matSaltSecondary;
                        }

                        // Modify the prefab's renderers directly
                        foreach (var rend in prefab.GetComponentsInChildren<Renderer>(true))
                        {
                            Material[] mats = rend.sharedMaterials;
                            bool changed = false;
                            for (int i = 0; i < mats.Length; i++)
                            {
                                if (mats[i] == null) continue;
                                string mName = mats[i].name.ToLower();

                                Material targetMat = null;
                                if (mName.Contains("body") || mName.Contains("frame") || mName.Contains("stock") || mName.Contains("metal") || mName.Contains("camo"))
                                {
                                    targetMat = bodyMat;
                                }
                                else if (mName.Contains("mag") || mName.Contains("rail") || mName.Contains("stripe") || mName.Contains("runes") || mName.Contains("glow") || mName.Contains("ammo") || mName.Contains("bullet") || mName.Contains("sulfur") || mName.Contains("mercury") || mName.Contains("salt"))
                                {
                                    targetMat = accentMat;
                                }
                                else if (mName.Contains("barrel") || mName.Contains("grip") || mName.Contains("trigger") || mName.Contains("scope") || mName.Contains("basic") || mName.Contains("carbon"))
                                {
                                    targetMat = secMat;
                                }
                                else if (mName.Contains("casing"))
                                {
                                    targetMat = matCasings;
                                }

                                if (targetMat != null && mats[i] != targetMat)
                                {
                                    mats[i] = targetMat;
                                    changed = true;
                                }
                            }

                            if (changed)
                            {
                                rend.sharedMaterials = mats;
                                EditorUtility.SetDirty(rend);
                            }
                        }

                        EditorUtility.SetDirty(prefab);
                    }
                    AssetDatabase.SaveAssets();
                }

        private static Material GetOrCreateMaterial(string name, Color baseColor, float smoothness, float metallic, Color emissionColor, bool useEmission)
                {
                    string path = "Assets/Art/Materials/" + name + ".mat";
                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat == null)
                    {
                        mat = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
                        if (!System.IO.Directory.Exists("Assets/Art/Materials")) System.IO.Directory.CreateDirectory("Assets/Art/Materials");
                        AssetDatabase.CreateAsset(mat, path);
                    }
                    else
                    {
                        mat.shader = Shader.Find("Universal Render Pipeline/Simple Lit");
                    }
                    
                    mat.SetColor("_BaseColor", baseColor);
                    mat.SetFloat("_Smoothness", smoothness);
                    mat.SetFloat("_Metallic", metallic);
                    
                    if (useEmission)
                    {
                        mat.SetColor("_EmissionColor", emissionColor);
                        mat.EnableKeyword("_EMISSION");
                    }
                    else
                    {
                        mat.SetColor("_EmissionColor", Color.clear);
                        mat.DisableKeyword("_EMISSION");
                    }
                    
                    EditorUtility.SetDirty(mat);
                    return mat;
                }

    }
}
