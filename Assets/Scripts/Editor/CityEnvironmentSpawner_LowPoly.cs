using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace TheAlchemistsCrypt.Editor
{
    public partial class StaticEgyptianCityGenerator
    {
        private void SpawnLowPolyEnvironmentObjects(GameObject root)
        {
            // 1. Load Prefabs
            var mountingPrefabs = new List<GameObject>();
            for (int i = 1; i <= 3; i++) {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/LowPoly Environment Pack/Prefabs/Mounting_{i}.prefab");
                if (p != null) mountingPrefabs.Add(p);
            }

            var rockPrefabs = new List<GameObject>();
            for (int i = 1; i <= 6; i++) {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/LowPoly Environment Pack/Prefabs/Rock_{i}.prefab");
                if (p != null) rockPrefabs.Add(p);
            }
            var stonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LowPoly Environment Pack/Prefabs/Stone_1.prefab");
            if (stonePrefab != null) rockPrefabs.Add(stonePrefab);

            var plantPrefabs = new List<GameObject>();
            for (int i = 1; i <= 2; i++) {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/LowPoly Environment Pack/Prefabs/Grass_{i}.prefab");
                if (p != null) plantPrefabs.Add(p);
            }

            if (mountingPrefabs.Count == 0 && rockPrefabs.Count == 0 && plantPrefabs.Count == 0) {
                Debug.LogWarning("[CityGen] No Low-Poly Environment Pack prefabs found to spawn.");
                return;
            }

            // Create folders
            var envFolder = new GameObject("LowPolyEnvironment");
            envFolder.transform.SetParent(root.transform);
            envFolder.isStatic = true;

            // Mountains have been removed to avoid disturbing city generation.
            var rockFolder = new GameObject("Rocks");
            rockFolder.transform.SetParent(envFolder.transform);
            rockFolder.isStatic = true;

            var vegetationFolder = new GameObject("Vegetation");
            vegetationFolder.transform.SetParent(envFolder.transform);
            vegetationFolder.isStatic = true;

            // 3. Scatter Rocks (near mountains and outer spaces)
            if (rockPrefabs.Count > 0)
            {
                int rockCount = 50;
                for (int i = 0; i < rockCount; i++)
                {
                    float rx = 0f;
                    float rz = 0f;
                    // Pick a border zone
                    int zone = Random.Range(0, 3);
                    if (zone == 0) { // North
                        rx = Random.Range(-450f, 450f);
                        rz = Random.Range(270f, 450f);
                    } else if (zone == 1) { // West
                        rx = Random.Range(-450f, -280f);
                        rz = Random.Range(-70f, 300f);
                    } else { // East
                        rx = Random.Range(280f, 450f);
                        rz = Random.Range(-70f, 300f);
                    }

                    Vector3 pos = new Vector3(rx, 0f, rz);
                    var prefab = rockPrefabs[Random.Range(0, rockPrefabs.Count)];
                    PlaceIntegratedAsset(rockFolder.transform, pos, prefab, Random.Range(1.5f, 4.5f), false, false, -0.2f, Random.Range(0f, 360f), true);
                }
            }

            // 4. Scatter low-poly vegetation in city spaces and borders
            if (plantPrefabs.Count > 0)
            {
                Physics.SyncTransforms(); // Sync physics transforms so OverlapSphere works reliably in Editor
                int vegCount = 650;
                int attempts = 0;
                int spawned = 0;
                while (spawned < vegCount && attempts < 4000)
                {
                    attempts++;
                    float rx = Random.Range(-350f, 350f);
                    float rz = Random.Range(-60f, 350f);
                    Vector3 pos = new Vector3(rx, 0f, rz);
                    pos.y = GetTerrainHeight(pos);

                    // Don't spawn in water or extremely high
                    if (pos.z < -70f || pos.y < 0.8f || pos.y > 10f) continue;

                    // Ensure it is not too close to the player spawn point (0f, 0f, 60f)
                    if (Vector3.Distance(pos, new Vector3(0f, pos.y, 60f)) < 12f) continue;

                    // Prevent spawning grass under/inside houses, temples, mastabas, columns, obelisks, etc.
                    bool overlapsStructure = false;
                    Collider[] colliders = Physics.OverlapSphere(pos, 3.5f);
                    foreach (var c in colliders)
                    {
                        if (c == null || c.gameObject == null) continue;
                        string nameLower = c.gameObject.name.ToLower();
                        if (nameLower.Contains("house") || nameLower.Contains("temple") || 
                            nameLower.Contains("mastaba") || nameLower.Contains("obelisk") || 
                            nameLower.Contains("column") || nameLower.Contains("pyramid") || 
                            nameLower.Contains("stall") || nameLower.Contains("wall") || 
                            nameLower.Contains("ladder") || nameLower.Contains("building"))
                        {
                            overlapsStructure = true;
                            break;
                        }
                    }
                    if (overlapsStructure) continue;

                    var prefab = plantPrefabs[Random.Range(0, plantPrefabs.Count)];
                    float scale = Random.Range(0.8f, 2.2f);
                    if (prefab.name.ToLower().Contains("tree")) scale = Random.Range(1.2f, 2.8f);
                    else if (prefab.name.ToLower().Contains("grass")) scale = Random.Range(0.6f, 1.2f);

                    PlaceIntegratedAsset(vegetationFolder.transform, pos, prefab, scale, false, false, 0f, Random.Range(0f, 360f), true);
                    spawned++;
                }
            }

            // Convert all shaders in the LowPoly folder to URP Lit to prevent any potential magenta issue
            var meshRenderers = envFolder.GetComponentsInChildren<MeshRenderer>(true);
            var urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader != null)
            {
                foreach (var mr in meshRenderers)
                {
                    var mats = mr.sharedMaterials;
                    bool changed = false;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        if (mats[i] != null && !mats[i].shader.name.Contains("Universal Render Pipeline"))
                        {
                            if (mr.gameObject.name.ToLower().Contains("palm") || mr.gameObject.name.ToLower().Contains("tree")) continue;
                            
                            Material newM = new Material(urpShader);
                            newM.name = mats[i].name + "_URP";
                            Color origCol = mats[i].HasProperty("_Color") ? mats[i].color : 
                                           (mats[i].HasProperty("_BaseColor") ? mats[i].GetColor("_BaseColor") : Color.white);
                            newM.SetColor("_BaseColor", origCol);
                            if (mats[i].HasProperty("_MainTex") && mats[i].mainTexture != null) newM.SetTexture("_BaseMap", mats[i].mainTexture);
                            else if (mats[i].HasProperty("_BaseMap") && mats[i].GetTexture("_BaseMap") != null) newM.SetTexture("_BaseMap", mats[i].GetTexture("_BaseMap"));
                            newM.SetFloat("_Smoothness", 0.05f);
                            mats[i] = newM;
                            changed = true;
                        }
                    }
                    if (changed) mr.sharedMaterials = mats;
                }
            }

            Debug.Log($"[CityGen] Successfully spawned Low-Poly Environment Range.");
        }
    }
}
