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
        private void SetupMummyAnimations()
                {
                    string[] fbxPaths = {
                        "Assets/Art/Mummy_Assets/base.fbx",
                        "Assets/Art/Mummy_Assets/base_basic_pbr.fbx",
                        "Assets/Art/Mummy_Assets/base_basic_shaded.fbx",
                        "Assets/Art/Mummy_Assets/mummy_base.fbx",
                        "Assets/Art/Mummy_Assets/mummy_idle.fbx",
                        "Assets/Art/Mummy_Assets/new_Walking.fbx",
                        "Assets/Art/Mummy_Assets/mummy_attack.fbx",
                        "Assets/Art/Mummy_Assets/mummy_death.fbx",
                        "Assets/Resources/Pharaoh/base_basic_shaded (3).fbx"
                    };
                    foreach (var p in fbxPaths) {
                        ConfigureFbxToHumanoid(p);
                    }

                    string controllerPath = "Assets/Art/Mummy_Assets/MummyTestController.controller";
                    var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
                    if (controller == null) {
                        controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                    }

                    // Ensure parameters exist
                    bool hasSpeed = false;
                    bool hasAttack = false;
                    foreach (var p in controller.parameters) {
                        if (p.name == "Speed") hasSpeed = true;
                        if (p.name == "Attack") hasAttack = true;
                    }
                    if (!hasSpeed) controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
                    if (!hasAttack) controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

                    var layer = controller.layers[0];
                    var rootStateMachine = layer.stateMachine;

                    if (!System.IO.Directory.Exists("Assets/Art/Mummy_Assets")) {
                        System.IO.Directory.CreateDirectory("Assets/Art/Mummy_Assets");
                    }

                    var idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Art/Mummy_Assets/mummy_idle.fbx");
                    var walkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Art/Mummy_Assets/new_Walking.fbx");
                    var attackClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Art/Mummy_Assets/mummy_attack.fbx");

                    var idleState = GetOrAddState(rootStateMachine, "Idle", idleClip);
                    var walkState = GetOrAddState(rootStateMachine, "Walk", walkClip);
                    var attackState = GetOrAddState(rootStateMachine, "Attack", attackClip);

                    if (idleState.transitions.Length == 0) {
                        var t = idleState.AddTransition(walkState);
                        t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.1f, "Speed");
                        t.hasExitTime = false; t.duration = 0.25f;
                    }
                    if (walkState.transitions.Length == 0) {
                        var t = walkState.AddTransition(idleState);
                        t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.1f, "Speed");
                        t.hasExitTime = false; t.duration = 0.25f;
                    }
                    
                    bool attackTransExists = false;
                    foreach(var t in rootStateMachine.anyStateTransitions) if(t.destinationState == attackState) attackTransExists = true;
                    if(!attackTransExists) {
                        var t = rootStateMachine.AddAnyStateTransition(attackState);
                        t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "Attack");
                        t.duration = 0.1f;
                    }

                    Debug.Log("Mummy & Pharaoh Animator Setup with Transitions!");

                    // Apply 80% Mesh Decimation to Mummies for Mobile Performance
                    // var mummies = GameObject.FindObjectsByType<TheAlchemistsCrypt.AI.ZombieAI>(FindObjectsInactive.Include);
                    // foreach (var m in mummies) {
                    //     DecimateMesh(m.gameObject, 0.8f);
                    // }

                    // Also decimate the prefabs in Resources to ensure spawned ones are optimized
                    // string[] resourcePrefabs = { "Assets/Resources/Mummy_Dynamic_Prefab.prefab", "Assets/Resources/Pharaoh_Prefab.prefab" };
                    // foreach (var path in resourcePrefabs) {
                    //     var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    //     if (prefab != null) {
                    //         DecimateMesh(prefab, 0.8f);
                    //         EditorUtility.SetDirty(prefab);
                    //     }
                    // }
                }

        private UnityEditor.Animations.AnimatorState GetOrAddState(UnityEditor.Animations.AnimatorStateMachine sm, string name, AnimationClip clip) {
                    foreach (var s in sm.states) if (s.state.name == name) {
                        s.state.motion = clip;
                        return s.state;
                    }
                    var newState = sm.AddState(name);
                    newState.motion = clip;
                    return newState;
                }

        private void ConfigureFbxToHumanoid(string path)
                {
                    var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                    if (importer != null) {
                        // Skip if already configured to avoid redundant re-imports and Rig Error spam
                        if (importer.animationType == ModelImporterAnimationType.Human && 
                            importer.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel)
                        {
                            return;
                        }

                        bool dirty = false;
                        if (importer.animationType != ModelImporterAnimationType.Human) {
                            importer.animationType = ModelImporterAnimationType.Human; 
                            dirty = true;
                        }
                        if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel) {
                            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel; 
                            dirty = true;
                        }
                        
                        if (dirty) {
                            try {
                                Debug.Log($"[CityGen] Reimporting {path} as Humanoid...");
                                importer.SaveAndReimport();
                            } catch (System.Exception ex) {
                                Debug.LogError($"[CityGen] Failed to configure {path} as Humanoid: {ex.Message}");
                            }
                        }
                    }
                }

        private void SetupManagers(GameObject root)
                {
                    var am = new GameObject("AudioManager");
                    am.transform.SetParent(root.transform);
                    am.AddComponent<TheAlchemistsCrypt.Gameplay.AudioManager>();

                    var hm = new GameObject("HiveMindManager");
                    hm.transform.SetParent(root.transform);
                    hm.AddComponent<TheAlchemistsCrypt.AI.HiveMindManager>();

                    var ms = new GameObject("MummySpawner");
                    ms.transform.SetParent(root.transform);
                    ms.AddComponent<TheAlchemistsCrypt.AI.MummySpawner>();

                    var em = new GameObject("EscapeManager");
                    em.transform.SetParent(root.transform);
                    em.AddComponent<TheAlchemistsCrypt.Gameplay.EscapeManager>();
                    
                    Debug.Log("[CityGen] AudioManager, HiveMindManager, MummySpawner, and EscapeManager injected into scene.");
                }

        private void FixPlayerAndWeapons()
                {
                    var p = GameObject.Find("Player");
                    if (p == null)
                    {
                        var character = GameObject.FindAnyObjectByType<InfimaGames.LowPolyShooterPack.Character>();
                        if (character != null) p = character.gameObject;
                    }

                    if (p == null)
                    {
                        Debug.LogWarning("[CityGen] No Player found — spawning P_LPSP_FP_CH prefab automatically.");
                        string fpCharPath = "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Prefabs/P_LPSP_FP_CH.prefab";
                        GameObject fpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fpCharPath);
                        if (fpPrefab != null)
                        {
                            p = PrefabUtility.InstantiatePrefab(fpPrefab) as GameObject;
                            p.name = "Player";
                        }
                        else
                        {
                            Debug.LogError("[CityGen] P_LPSP_FP_CH prefab not found — cannot spawn player!");
                            return;
                        }
                    }

                    p.tag = "Player";
                    
                    // Spawn player OUTSIDE the LayHouse_Spawn, in front of its doorway
                    // The house is at Z=60 facing south (180° yaw) — doorway opens toward Z<60
                    var spawnHouse = GameObject.Find("LayHouse_Spawn");
                    if (spawnHouse != null) {
                        // Place player just outside the front door opening, facing the house
                        float groundY = GetTerrainHeight(new Vector3(0f, 0f, 47f)) + 1.3f;
                        p.transform.position = new Vector3(0f, groundY, 47f);
                        p.transform.rotation = Quaternion.Euler(0f, 0f, 0f); // Face north (toward house door)
                        Debug.Log("[CityGen] Player spawned outside lay_house.glb doorway.");
                    } else {
                        p.transform.position = new Vector3(0f, GetTerrainHeight(new Vector3(0f, 0f, 47f)) + 1.3f, 47f);
                        p.transform.rotation = Quaternion.identity;
                    }

                    // Ensure AlchemicalFocus is attached to the player GameObject
                    var focus = p.GetComponent<TheAlchemistsCrypt.Weapons.AlchemicalFocus>();
                    if (focus == null)
                    {
                        focus = p.AddComponent<TheAlchemistsCrypt.Weapons.AlchemicalFocus>();
                        Debug.Log("[CityGen] Attached AlchemicalFocus component to Player.");
                    }

                    Camera mainCam = p.GetComponentInChildren<Camera>(true);
                    if (mainCam == null) mainCam = Camera.main;
                    if (mainCam == null) mainCam = GameObject.FindAnyObjectByType<Camera>();

                    if (mainCam != null)
                    {
                        mainCam.gameObject.SetActive(true);
                        mainCam.enabled = true;
                        mainCam.tag = "MainCamera";
                        mainCam.targetDisplay = 0;
                        try { mainCam.farClipPlane = Mathf.Max(mainCam.farClipPlane, 4000f); } catch { }

                        // Exclude the player character body layer (9) from the world camera
                        // The weapon camera in LPSP renders arms/hands separately on layer 9
                        // Rendering it on the main camera causes the skin-coloured body mesh to bleed
                        // into the FPS view and cover the HUD buttons at the bottom of the screen
                        mainCam.cullingMask &= ~(1 << 9);
                    }
                    else
                    {
                        var camGo = new GameObject("Main Camera");
                        camGo.tag = "MainCamera";
                        camGo.transform.SetParent(p.transform);
                        camGo.transform.localPosition = new Vector3(0f, 1.7f, 0f);
                        var cam = camGo.AddComponent<Camera>();
                        cam.targetDisplay = 0;
                        cam.nearClipPlane = 0.01f;
                        cam.farClipPlane = 2000f;
                        cam.fieldOfView = 70f;
                        camGo.AddComponent<AudioListener>();
                    }

                    // Find all Inventory components under the player and clean up duplicates
                    var inventories = p.GetComponentsInChildren<InfimaGames.LowPolyShooterPack.Inventory>(true);
                    InfimaGames.LowPolyShooterPack.Inventory mainInv = null;
                    if (inventories != null && inventories.Length > 0)
                    {
                        mainInv = inventories[0];
                        // Destroy duplicates
                        for (int i = 1; i < inventories.Length; i++)
                        {
                            if (inventories[i] != null && inventories[i].gameObject != null)
                            {
                                Debug.LogWarning("[CityGen] Destroying duplicate inventory: " + inventories[i].gameObject.name);
                                DestroyImmediate(inventories[i].gameObject);
                            }
                        }
                    }

                    // If we don't have an inventory at all, spawn one
                    if (mainInv == null)
                    {
                        string invPath = "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Prefabs/Weapons/P_LPSP_Inventory.prefab";
                        GameObject invPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(invPath);
                        if (invPrefab != null)
                        {
                            var invGo = PrefabUtility.InstantiatePrefab(invPrefab) as GameObject;
                            invGo.name = "Inventory";
                            invGo.transform.SetParent(p.transform, false);
                            invGo.transform.localPosition = Vector3.zero;
                            mainInv = invGo.GetComponent<InfimaGames.LowPolyShooterPack.Inventory>();
                        }
                    }

                    if (mainInv == null)
                    {
                        Debug.LogError("[CityGen] Failed to resolve Player inventory!");
                        return;
                    }

                    // Clear old weapons from inventory transform
                    for (int i = mainInv.transform.childCount - 1; i >= 0; i--)
                    {
                        DestroyImmediate(mainInv.transform.GetChild(i).gameObject);
                    }

                    // Populate the inventory with the three elemental weapon prefabs
                    string[] wPrefabs = {
                        "Assets/Prefabs/WEP_Sulfur.prefab",
                        "Assets/Prefabs/WEP_Mercury.prefab",
                        "Assets/Prefabs/WEP_Salt.prefab"
                    };

                    foreach (var wpPath in wPrefabs)
                    {
                        GameObject wpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(wpPath);
                        if (wpPrefab != null)
                        {
                            var wpObj = PrefabUtility.InstantiatePrefab(wpPrefab, mainInv.transform) as GameObject;
                            wpObj.name = System.IO.Path.GetFileNameWithoutExtension(wpPath);
                            Debug.Log("[CityGen] Instantiated element weapon prefab: " + wpObj.name);
                        }
                        else
                        {
                            Debug.LogError("[CityGen] Failed to load element weapon prefab: " + wpPath);
                        }
                    }

                    // Bake the alchemical weapon prefabs directly to avoid scene instance material leaks/serialization errors
                    UpdateWeaponPrefabMaterials();
                }

        private void SpawnDesertBrokenPillars(GameObject root, Material stoneMat)
                {
                    var folder = new GameObject("DesertBrokenPillars");
                    folder.transform.SetParent(root.transform);

                    int spawnedCount = 0;
                    int attempts = 0;
                    while (spawnedCount < 50 && attempts < 500)
                    {
                        attempts++;
                        float rx = Random.Range(-480f, 480f);
                        float rz = Random.Range(-480f, 480f);
                        Vector3 pos = new Vector3(rx, 0f, rz);
                        
                        if (pos.magnitude > 150f && rz >= -75f)
                        {
                            pos.y = GetTerrainHeight(pos);
                            if (pos.y < 0.5f) continue;

                            var pillar = new GameObject("DesertBrokenPillar");
                            pillar.transform.SetParent(folder.transform);
                            pillar.transform.position = pos;
                            pillar.isStatic = true;

                            float height = Random.Range(4f, 8f);
                            float baseWidth = Random.Range(1.8f, 2.5f);
                            float topWidth = baseWidth * Random.Range(0.6f, 0.85f);

                            int segments = Random.Range(3, 6);
                            float segHeight = height / segments;

                            for (int i = 0; i < segments; i++)
                            {
                                float currentWidth = Mathf.Lerp(baseWidth, topWidth, (float)i / (segments - 1));
                                var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                seg.transform.SetParent(pillar.transform);
                                seg.transform.localPosition = new Vector3(0f, (i * segHeight) + (segHeight / 2f), 0f);
                                seg.transform.localScale = new Vector3(currentWidth, segHeight, currentWidth);
                                seg.GetComponent<Renderer>().sharedMaterial = stoneMat;
                                seg.isStatic = true;
                            }

                            var col = pillar.AddComponent<BoxCollider>();
                            col.center = new Vector3(0f, height / 2f, 0f);
                            col.size = new Vector3(baseWidth, height, baseWidth);

                            Vector3 normal = GetTerrainNormal(pos);
                            Quaternion normalRot = Quaternion.FromToRotation(Vector3.up, normal);
                            Quaternion randomTilt = Quaternion.Euler(Random.Range(-20f, 20f), Random.Range(0f, 360f), Random.Range(-20f, 20f));
                            pillar.transform.rotation = normalRot * randomTilt;

                            spawnedCount++;
                        }
                    }
                }

        private void SpawnPalmTreeOasis(GameObject root, GameObject[] treePrefabs)
                {
                    if (treePrefabs == null || treePrefabs.Length == 0 || treePrefabs[0] == null) return;

                    var folder = new GameObject("PalmTreeOasis");
                    folder.transform.SetParent(root.transform);

                    int spawnedCount = 0;
                    int attempts = 0;
                    while (spawnedCount < 40 && attempts < 400)
                    {
                        attempts++;
                        float rx = Random.Range(-450f, 450f);
                        float rz = Random.Range(-70f, -40f); // Constrain to dry beach sand zone
                        Vector3 pos = new Vector3(rx, 0f, rz);
                        pos.y = GetTerrainHeight(pos);

                        // Make sure it doesn't spawn in water, shoreline shallows, or extremely high up
                        if (pos.z < -70f || pos.y < 1.1f || pos.y > 6.0f) continue;

                        var prefab = treePrefabs[Random.Range(0, treePrefabs.Length)];
                        if (prefab == null) continue;

                        // Use the centralized placement helper to ensure correct GLB rotation (-90 on X) and scaling
                        PlaceIntegratedAsset(folder.transform, pos, prefab, Random.Range(0.8f, 1.4f), true, false, -1.8f);

                        spawnedCount++;
                    }
                }

        private void SpawnCityPalmTrees(GameObject root, GameObject[] treePrefabs, List<Vector3> occupiedPositions)
                {
                    if (treePrefabs == null || treePrefabs.Length == 0 || treePrefabs[0] == null) return;

                    var folder = new GameObject("City_Vegetation_Safe"); // Renamed to bypass 'tree' removal
                    folder.transform.SetParent(root.transform);

                    int spawnedCount = 0;
                    int attempts = 0;
                    Vector3 playerSpawn = new Vector3(16f, 0f, 48f);

                    while (spawnedCount < 100 && attempts < 1000)
                    {
                        attempts++;
                        float rx = Random.Range(-240f, 240f);
                        float rz = Random.Range(-90f, 240f); // Relaxed Z range
                        Vector3 pos = new Vector3(rx, 0f, rz);

                        // Ensure it's at least 18 units away from player spawn
                        if (Vector3.Distance(new Vector3(rx, 0f, rz), playerSpawn) < 18f) continue;

                        // Ensure it's at least 18 units away from occupiedPositions
                        bool tooClose = false;
                        foreach (var occupied in occupiedPositions)
                        {
                            if (Vector3.Distance(new Vector3(rx, 0f, rz), new Vector3(occupied.x, 0f, occupied.z)) < 18f)
                            {
                                tooClose = true;
                                break;
                            }
                        }
                        if (tooClose) continue;

                        pos.y = GetTerrainHeight(pos);

                        // Relaxed height check (allow on lower dunes)
                        if (pos.z < -95f || pos.y < 0.2f || pos.y > 8.0f) continue;

                        var prefab = treePrefabs[Random.Range(0, treePrefabs.Length)];
                        if (prefab == null) continue;

                        // Use the centralized placement helper (yOffset = -1.8f to sink root)
                        PlaceIntegratedAsset(folder.transform, pos, prefab, Random.Range(0.8f, 1.4f), true, false, -1.8f);

                        spawnedCount++;
                    }
                    Debug.Log($"[CityGen] Spawned {spawnedCount} city vegetation units after {attempts} attempts.");
                }

        private void SpawnDesertMedicinePickups(GameObject root)
        {
            var folder = new GameObject("DesertMedicinePickups");
            folder.transform.SetParent(root.transform);

            int spawnedCount = 0;
            int attempts = 0;
            while (spawnedCount < 60 && attempts < 800)
            {
                attempts++;
                float rx = Random.Range(-480f, 480f);
                float rz = Random.Range(-480f, 480f);
                Vector3 pos = new Vector3(rx, 0f, rz);

                if (pos.magnitude > 150f && rz >= -75f)
                {
                    pos.y = GetTerrainHeight(pos) + 0.5f;
                    if (pos.y < 0.5f) continue;

                    var medGo = new GameObject("DesertMedicinePickup");
                    medGo.transform.SetParent(folder.transform);
                    medGo.transform.position = pos;
                    var pickup = medGo.AddComponent<TheAlchemistsCrypt.Gameplay.MedicinePickup>();
                    pickup.healAmount = 25f;

                    spawnedCount++;
                }
            }
            Debug.Log($"[CityGen] Spawned {spawnedCount} desert medicine/health pickups after {attempts} attempts.");
        }
    }
}
