using UnityEngine;
using UnityEditor;
using Unity.AI.Navigation;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using UnityMeshSimplifier;

namespace TheAlchemistsCrypt.Editor
{
    public partial class StaticEgyptianCityGenerator : EditorWindow
    {
        [MenuItem("Egyptian/Generate & Setup City", false, 1)]
        public static void QuickRegen() {
            var g = CreateInstance<StaticEgyptianCityGenerator>();
            g.Purge(); 
            g.GeneratePolishedCity();
        }

        [MenuItem("Egyptian/Open Generator Window", false, 2)]
        public static void ShowWindow() => GetWindow<StaticEgyptianCityGenerator>("Egyptian City V5.2");

        private int seed = 999;
        private int gridSize = 8; 
        private string rootName = "EgyptianCity_V5_Final";
        private static Dictionary<string, Material> convertedMaterialsCache = new Dictionary<string, Material>();

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("V5.2 AESTHETIC OVERHAUL: stack floors, terra-cotta walls, purple shadows, gradient sky disc, emissive artifacts.", MessageType.Info);
            seed = EditorGUILayout.IntField("Seed", seed);
            gridSize = EditorGUILayout.IntField("Grid Size", gridSize);

            if (GUILayout.Button("▶ GENERATE & SETUP CITY GAME", GUILayout.Height(40))) {
                Purge();
                GeneratePolishedCity();
            }
            
            EditorGUILayout.Space();
            if (GUILayout.Button("🗑 CLEANUP", GUILayout.Height(30))) Purge();
        }

        private void Purge()
        {
            // 1. Destroy procedural city root if it exists
            var cityRoot = GameObject.Find(rootName);
            if (cityRoot != null) DestroyImmediate(cityRoot);

            // Destroy all existing terrains in the scene to prevent duplicate floors stacking up
            var terrains = GameObject.FindObjectsByType<Terrain>(FindObjectsInactive.Include);
            foreach (var t in terrains)
            {
                if (t != null) DestroyImmediate(t.gameObject);
            }

            // 2. Loop through root GameObjects and destroy any non-essential ones
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var r in roots)
            {
                if (r == null) continue;
                string lowerName = r.name.ToLower().Replace(" ", "");

                // Keep Player, cameras, lights, EventSystem, GameController, and TestRoot
                if (lowerName.Contains("player") || lowerName.Contains("camera") || lowerName.Contains("controller") || 
                    lowerName.Contains("eventsystem") || lowerName.Contains("light") || 
                    lowerName.Contains("sun") || lowerName.Contains("sky") || r.name == "TestRoot")
                {
                    continue;
                }

                DestroyImmediate(r);
            }

            // 3. Keep fallback keyword check for childless / hidden / empty name GameObjects
            var all = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in all) {
                if (go == null) continue;
                
                // Clear any empty / unnamed remnants immediately
                if (string.IsNullOrEmpty(go.name) || string.IsNullOrEmpty(go.name.Trim()))
                {
                    DestroyImmediate(go);
                    continue;
                }

                string lowerName = go.name.ToLower().Replace(" ", ""); 
                if (lowerName.Contains("egyptiancity") || lowerName.Contains("desertfloor") || lowerName.Contains("floorground") ||
                    lowerName.Contains("groundplane") || lowerName.Contains("desertterrain") || lowerName.Contains("terrainfloor") ||
                    lowerName.Contains("player_copy") || lowerName.Contains("mobilehud") || lowerName.Contains("p_lpsp_ui_canvas") || 
                    lowerName.StartsWith("mummy") || lowerName.Contains("windowlight") || lowerName.Contains("crater") ||
                    lowerName.Contains("plaza") || lowerName.Contains("house") || lowerName.Contains("pyramid") ||
                    lowerName.Contains("sphinx") || lowerName.Contains("mastaba") || lowerName.Contains("temple") ||
                    lowerName.Contains("stall") || lowerName.Contains("obelisk") ||
                    lowerName.Contains("seazone") || lowerName.Contains("beachzone") || lowerName.Contains("coastlinebarrier") ||
                    lowerName.Contains("globalvolume") || lowerName.Contains("reflectionprobe") ||
                    lowerName.Contains("claritylight") || lowerName.Contains("environmentvolume") ||
                    lowerName.Contains("audiomanager") || lowerName.Contains("hivemindmanager") || lowerName.Contains("mummyspawner") ||
                    lowerName.Contains("escapemanager") || lowerName.Contains("dynamicprops")) 
                {
                    DestroyImmediate(go);
                }
            }
            
            // Clean up unreferenced procedural meshes from editor memory to prevent scene file bloating
            EditorUtility.UnloadUnusedAssetsImmediate();
        }

        public void GeneratePolishedCity()
        {
            convertedMaterialsCache.Clear();
            GenerateSkyCloudNormalMap();

            Random.InitState(seed);
            Purge();

            List<Vector3> occupiedPositions = new List<Vector3>();

            var root = new GameObject(rootName);
            root.isStatic = true;

            var trees = new GameObject[] {
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/EgyptianAssets/realistic_hd_date_palm_2178.glb"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/EgyptianAssets/realistic_hd_date_palm_378.glb")
            };
            var crate = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/EgyptianAssets/crate.glb");
            var barrel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/EgyptianAssets/barrel.glb");
            var columnPillarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/EgyptianAssets/egyptian_pillar_column.glb");
            var columnStandardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/EgyptianAssets/egyptian_column.glb");
            System.Func<GameObject> GetRandomColumn = () => (columnPillarPrefab != null && columnStandardPrefab != null) ? (Random.value > 0.5f ? columnPillarPrefab : columnStandardPrefab) : (columnPillarPrefab ?? columnStandardPrefab);

            // Load new assets for Phase 2 integration
            var arabicHousePrefabs = new List<GameObject>();
            string[] housePaths = {
                "Assets/Resources/more_items_for_map/arabic_house_4.glb",
                "Assets/Resources/more_items_for_map/arabic_house_5.glb",
                "Assets/Resources/more_items_for_map/egyptian_house.glb",
                "Assets/Resources/more_items_for_map/lay_house.glb",
                "Assets/Resources/more_items_for_map/medieval_stone_arab_house.glb"
            };
            foreach (var path in housePaths) {
                var h = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (h != null) arabicHousePrefabs.Add(h);
            }

            var stallPrefabs = new List<GameObject>();
            string[] stallPaths = {
                "Assets/Resources/more_items_for_map/low_poly_market_stall_pack.glb",
                "Assets/Resources/more_items_for_map/medieval_stall.glb",
                "Assets/Resources/more_items_for_map/vietnamese_meat_market_stall.glb"
            };
            foreach (var path in stallPaths) {
                var s = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (s != null) stallPrefabs.Add(s);
            }

            var sphinxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/more_items_for_map/the_great_sphinx_of_giza_-_egypt.glb");
            var mastabaPrefab = (GameObject)null; // Disabled false_door_chamber_mastaba_of_qar_giza as requested
            var templeComplexPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/more_items_for_map/egyptian_temple_complex_game_asset.glb");
            var templesPackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/more_items_for_map/egyptian_temples.glb");
            var obeliskNewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/more_items_for_map/stylized_egypt_obelisk.glb");
            var lighthousePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/more_items_for_map/light_house_-_egypt_game_ready_lowpoly.glb");
            var bigHousePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/more_items_for_map/big_egypt_house.glb");
            var layHousePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/more_items_for_map/lay_house.glb");
            var doorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/more_items_for_map/egyptian_door.glb");
            
            // Create warm golden sandstone gradient texture for houses
            string houseTexPath = "Assets/Art/EgyptianAssets/HouseGradientTex.png";
            if (System.IO.File.Exists(houseTexPath)) {
                System.IO.File.Delete(houseTexPath);
                System.IO.File.Delete(houseTexPath + ".meta");
                AssetDatabase.Refresh();
            }
            Texture2D houseTex = null;
            if (houseTex == null) {
                if (!System.IO.Directory.Exists("Assets/Art/EgyptianAssets")) System.IO.Directory.CreateDirectory("Assets/Art/EgyptianAssets");
                int texSize = 512;
                houseTex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, true);
                houseTex.wrapMode = TextureWrapMode.Clamp;
                houseTex.filterMode = FilterMode.Bilinear;
                Color bottomColor = new Color(0.75f, 0.55f, 0.47f); // Desaturated warm sand bottom
                Color topColor = new Color(0.86f, 0.62f, 0.50f);    // Desaturated warm sand top
                Color[] pixels = new Color[texSize * texSize];
                for (int y = 0; y < texSize; y++) {
                    float t = (float)y / (texSize - 1);
                    Color rowColor = Color.Lerp(bottomColor, topColor, t);
                    for (int x = 0; x < texSize; x++) pixels[y * texSize + x] = rowColor;
                }
                houseTex.SetPixels(pixels);
                houseTex.Apply(true);
                byte[] bytes = houseTex.EncodeToPNG();
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, "Art/EgyptianAssets/HouseGradientTex.png"), bytes);
                AssetDatabase.Refresh();
                houseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(houseTexPath);
            }

            // ── AESTHETIC PALETTE (Golden Sandstone Desert) ──
            string normalMapPath = "Assets/Resources/Textures/EgyptianNormalMap.png";
            var importer = AssetImporter.GetAtPath(normalMapPath) as TextureImporter;
            if (importer != null)
            {
                bool changed = false;
                if (importer.textureType != TextureImporterType.NormalMap)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    changed = true;
                }
                if (!importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = true;
                    changed = true;
                }
                if (changed)
                {
                    importer.SaveAndReimport();
                    AssetDatabase.Refresh();
                }
            }

            Material wallMat = GetOrCreateMaterial("M_City_Wall", new Color(1.0f, 0.88f, 0.72f), 0.05f, 0.0f, Color.clear, false); // Sun-bleached warm sandstone
            if (houseTex != null)
            {
                wallMat.SetTexture("_BaseMap", houseTex);
                wallMat.SetTextureScale("_BaseMap", new Vector2(12, 12));
            }

            Texture2D normalMapTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalMapPath);
            if (normalMapTex != null)
            {
                wallMat.SetTexture("_BumpMap", normalMapTex);
                wallMat.SetTextureScale("_BumpMap", new Vector2(12, 12));
                wallMat.EnableKeyword("_NORMALMAP");
                wallMat.SetFloat("_BumpScale", 2.0f);
            }
            wallMat.enableInstancing = true;
            EditorUtility.SetDirty(wallMat);

            Material woodMat = GetOrCreateMaterial("M_City_Wood", new Color(0.20f, 0.12f, 0.08f), 0.0f, 0.0f, Color.clear, false);
            woodMat.enableInstancing = true;
            EditorUtility.SetDirty(woodMat);

            Material floorMat = GetOrCreateMaterial("M_City_Floor", new Color(0.95f, 0.85f, 0.70f), 0.10f, 0.0f, new Color(1.0f, 0.95f, 0.8f) * 0.02f, true);
            floorMat.enableInstancing = true;
            EditorUtility.SetDirty(floorMat);

            Material litWindowMat = GetOrCreateMaterial("M_City_LitWindow", new Color(1f, 0.95f, 0.8f), 0.10f, 0.0f, new Color(1.0f, 0.75f, 0.25f) * 15.0f, true);
            litWindowMat.enableInstancing = true;
            EditorUtility.SetDirty(litWindowMat);

            Material darkWindowMat = GetOrCreateMaterial("M_City_DarkWindow", new Color(0.15f, 0.1f, 0.15f), 0.0f, 0.0f, Color.clear, false);
            darkWindowMat.enableInstancing = true;
            EditorUtility.SetDirty(darkWindowMat);

            SetupEnvironment(root);
            SetupManagers(root);
            
            var probeGo = new GameObject("GlobalReflectionProbe");
            probeGo.transform.SetParent(root.transform);
            var probe = probeGo.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Baked;
            probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.renderDynamicObjects = true;
            probe.size = new Vector3(2000f, 500f, 2000f);
            probe.importance = 1;

            try {
                string exrPath = "Assets/Art/Materials/GlobalReflectionProbe.exr";
                if (!System.IO.Directory.Exists("Assets/Art/Materials")) System.IO.Directory.CreateDirectory("Assets/Art/Materials");
#if UNITY_EDITOR
                UnityEditor.Lightmapping.BakeReflectionProbe(probe, exrPath);
                Debug.Log($"[CityGen] Baked GlobalReflectionProbe to {exrPath}");
#endif
            } catch (System.Exception e) {
                Debug.LogWarning($"[CityGen] Failed to bake GlobalReflectionProbe: {e.Message}");
            }

            TerrainData terrainData = new TerrainData();
            terrainData.heightmapResolution = 513;
            terrainData.size = new Vector3(3000f, 10f, 3000f); // Massive extended desert

            int resolution = terrainData.heightmapResolution;
            float[,] heights = new float[resolution, resolution];
            for (int i = 0; i < resolution; i++) {
                for (int j = 0; j < resolution; j++) {
                    float tx = (float)i / (resolution - 1); // Z-axis normalized
                    float ty = (float)j / (resolution - 1); // X-axis normalized
                    
                    float dune1 = Mathf.PerlinNoise(tx * 3f, ty * 3f) * 0.6f;
                    float dune2 = Mathf.PerlinNoise(tx * 8f + 10f, ty * 8f + 10f) * 0.12f;
                    float baseDune = dune1 + dune2;

                    float wx = ty * 1000f - 500f; // World X
                    float wz = tx * 1000f - 500f; // World Z

                    // Flatten entire city grid (X: [-260, 260], Z: [-80, 260])
                    float cityFlatten = 1f;
                    float margin = 50f;
                    float minCityX = -260f;
                    float maxCityX = 260f;
                    float minCityZ = -80f;
                    float maxCityZ = 260f;

                    if (wx >= minCityX && wx <= maxCityX && wz >= minCityZ && wz <= maxCityZ) {
                        cityFlatten = 0f;
                    } else {
                        float dx = 0f;
                        if (wx < minCityX) dx = minCityX - wx;
                        else if (wx > maxCityX) dx = wx - maxCityX;

                        float dz = 0f;
                        if (wz < minCityZ) dz = minCityZ - wz;
                        else if (wz > maxCityZ) dz = wz - maxCityZ;

                        float distToCity = Mathf.Max(dx, dz);
                        cityFlatten = Mathf.Clamp01(distToCity / margin);
                    }

                    heights[i, j] = baseDune * cityFlatten;

                    // SEA & COASTLINE FLATTENING
                    if (wz < -80f) {
                        if (wz <= -100f) {
                            heights[i, j] = 0f;
                        } else {
                            float shoreFactor = Mathf.SmoothStep(0f, 1f, (wz - (-100f)) / 20f);
                            heights[i, j] *= shoreFactor;
                        }
                    }
                }
            }
            terrainData.SetHeights(0, 0, heights);

            string layerPath = "Assets/Art/EgyptianAssets/DesertSandLayer_V2.terrainlayer";
            TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
            if (layer == null) {
                layer = new TerrainLayer();
                if (!System.IO.Directory.Exists("Assets/Art/EgyptianAssets")) System.IO.Directory.CreateDirectory("Assets/Art/EgyptianAssets");
                AssetDatabase.CreateAsset(layer, layerPath);
            }

            // Always recreate the desert sand texture with rich multi-freq noise for a realistic look
            string sandTexPath = "Assets/Art/EgyptianAssets/SandTexGradient_1024.png";
            if (System.IO.File.Exists(sandTexPath)) {
                System.IO.File.Delete(sandTexPath);
                System.IO.File.Delete(sandTexPath + ".meta");
                AssetDatabase.Refresh();
            }
            Texture2D sandTex = null;
            {
                int texSize = 1024;
                sandTex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, true);
                sandTex.wrapMode = TextureWrapMode.Repeat;
                sandTex.filterMode = FilterMode.Trilinear;

                // Egyptian desert floor palette
                Color duneCrest   = new Color(0.96f, 0.82f, 0.58f); // Bright dune crown
                Color sandBase    = new Color(0.82f, 0.66f, 0.42f); // Main sand colour
                Color shadowTrough= new Color(0.65f, 0.51f, 0.32f); // Inter-dune shadow
                Color stoneDark   = new Color(0.48f, 0.38f, 0.27f); // Dark rocky fleck

                Color[] pixels = new Color[texSize * texSize];
                for (int y = 0; y < texSize; y++) {
                    float fy = (float)y / (texSize - 1);
                    for (int x = 0; x < texSize; x++) {
                        float fx = (float)x / (texSize - 1);

                        // Large-scale dune shape (low freq)
                        float dune = Mathf.PerlinNoise(fx * 2.5f + 0.3f, fy * 2.5f + 0.7f);

                        // Medium ripple (wind-driven sand lines)
                        float ripple = Mathf.PerlinNoise(fx * 9f + 5.1f, fy * 9f + 3.3f) * 0.35f;

                        // Fine grain / high-frequency noise
                        float grain = Mathf.PerlinNoise(fx * 28f + 11f, fy * 28f + 17f) * 0.12f;

                        // Rare dark stone fleck (large-period sparse)
                        float fleck = Mathf.PerlinNoise(fx * 55f + 22f, fy * 55f + 44f);
                        bool isFleck = (fleck < 0.06f); // ~6% coverage

                        float combined = Mathf.Clamp01(dune * 0.55f + ripple + grain);

                        Color col;
                        if (isFleck) {
                            col = Color.Lerp(stoneDark, shadowTrough, combined);
                        } else {
                            // Blend from shadow trough through base to dune crest
                            if (combined < 0.45f)
                                col = Color.Lerp(shadowTrough, sandBase, combined / 0.45f);
                            else
                                col = Color.Lerp(sandBase, duneCrest, (combined - 0.45f) / 0.55f);
                        }
                        pixels[y * texSize + x] = col;
                    }
                }
                sandTex.SetPixels(pixels);
                sandTex.Apply(true);
                byte[] pngBytes = sandTex.EncodeToPNG();
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, "Art/EgyptianAssets/SandTexGradient_1024.png"), pngBytes);
                AssetDatabase.Refresh();
                sandTex = AssetDatabase.LoadAssetAtPath<Texture2D>(sandTexPath);
            }

            layer.diffuseTexture = sandTex;
            layer.tileSize = new Vector2(12f, 12f); // 12m tiling: sand detail visible from ground level
            layer.specular = new Color(0.02f, 0.015f, 0.01f, 0f);
            layer.smoothness = 0.04f;
            EditorUtility.SetDirty(layer);
            AssetDatabase.SaveAssets();
            terrainData.terrainLayers = new TerrainLayer[] { layer };

            // ── PRIMARY TERRAIN: Unity Terrain as the solid visual & collision floor ────────────
            // Always create a full 1000x1000 Unity Terrain underneath to guarantee ZERO
            // fall-through.
            var unityTerrainGo = Terrain.CreateTerrainGameObject(terrainData);
            unityTerrainGo.name = "TerrainFloor";
            unityTerrainGo.transform.SetParent(root.transform);
            unityTerrainGo.transform.position = new Vector3(-1500f, -0.15f, -1000f);
            unityTerrainGo.isStatic = true;
            var unityTerrainComp = unityTerrainGo.GetComponent<Terrain>();
            if (unityTerrainComp != null) {
                unityTerrainComp.enabled = true; // Enabled visual rendering as it is now our main floor
                unityTerrainComp.basemapDistance = 2000f;
                unityTerrainComp.drawInstanced = true;
                // Apply sand texture layer to the Unity Terrain
                unityTerrainComp.terrainData = terrainData;
            }

            GameObject terrainGo = unityTerrainGo;

            float spacing = 20f; // Decreased spacing to bring houses closer together for a denser residential feel
            float halfSpan = (gridSize * spacing) / 2f;
            var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Enemy-AI/Prefabs/TestZombie.prefab");

            for (int x = 0; x < gridSize; x++) {
                for (int z = 0; z < gridSize; z++) {
                    float posX = -halfSpan + (x * spacing) + (spacing / 2f);
                    float posZ = -halfSpan + (z * spacing) + (spacing / 2f);
                    if (posZ < -60f) continue; // Sea area

                    Vector3 pos = new Vector3(posX, 0f, posZ);
                    pos.y = GetTerrainHeight(pos);

                    // --- ZONING LOGIC ---
                    bool isMarketZone = (z == 3 || z == 4) && (x == 3 || x == 4);
                    bool isAncientDistrict = (z >= 6);

                    if (isAncientDistrict) {
                        // Skip grid placement in the Ancient District; landmarks are placed manually
                        continue;
                    }

                    if (isMarketZone) {
                        PlacePlaza(root.transform, pos, trees, GetRandomColumn(), floorMat);
                        occupiedPositions.Add(pos);
                        
                        // Populate market with multiple stalls
                        int numStalls = Random.Range(3, 7);
                        for (int i = 0; i < numStalls; i++) {
                            if (stallPrefabs.Count > 0) {
                                Vector3 sOffset = new Vector3(Random.Range(-25f, 25f), 0f, Random.Range(-25f, 25f));
                                Vector3 sPos = pos + sOffset;
                                sPos.y = GetTerrainHeight(sPos);
                                
                                // Make stalls face the plaza center (pos)
                                Vector3 lookDir = (new Vector3(pos.x, sPos.y, pos.z) - sPos).normalized;
                                float yaw = Mathf.Atan2(lookDir.x, lookDir.z) * Mathf.Rad2Deg;
                                if (Random.value < 0.3f) yaw = Random.Range(0f, 360f); // Add organic variety

                                PlaceIntegratedAsset(root.transform, sPos, stallPrefabs[Random.Range(0, stallPrefabs.Count)], 1.0f, true, false, 0f, yaw, false);
                            }
                        }
                    } 
                    else {
                        // Residential Zone (rows z = 3, 4, 5 outside the market)
                        float roll = Random.value;
                        if (roll < 0.05f) {
                             BuildAlchemistTomb(root.transform, pos, wallMat);
                             occupiedPositions.Add(pos);
                        } 
                        else if (roll < 0.85f) {
                            // Street alignment: even rows (z == 4) face North (180), odd rows (z == 3, 5) face South (0)
                            float targetAngleY = (z % 2 == 0) ? 180f : 0f;

                            // Weighted split: 75% Arabic House, 25% Procedural
                            if (Random.value < 0.75f && arabicHousePrefabs.Count > 0) {
                                var housePrefab = arabicHousePrefabs[Random.Range(0, arabicHousePrefabs.Count)];
                                var houseObj = PlaceIntegratedAsset(root.transform, pos, housePrefab, 1.0f, true, false, 0f, targetAngleY, false);
                                occupiedPositions.Add(pos);

                                // Add ladder to roof occasionally (e.g. 50% chance)
                                if (Random.value < 0.50f && houseObj != null) {
                                    float houseHeight = 12f;
                                    var mf = houseObj.GetComponentInChildren<MeshFilter>(true);
                                    if (mf != null && mf.sharedMesh != null) {
                                        houseHeight = mf.sharedMesh.bounds.size.y * houseObj.transform.localScale.y;
                                    }
                                    Vector3 ladderBasePos = pos - Quaternion.Euler(0f, targetAngleY, 0f) * Vector3.forward * 13.5f;
                                    BuildProceduralLadderRamp(root.transform, ladderBasePos, houseHeight, targetAngleY);
                                }
                                

                            } else {
                                BuildHouse(root.transform, pos, wallMat, woodMat, litWindowMat, darkWindowMat, crate, barrel, floorMat, targetAngleY);
                                occupiedPositions.Add(pos);
                            }
                        } 
                        else {
                            PlacePlaza(root.transform, pos, trees, GetRandomColumn(), floorMat);
                            occupiedPositions.Add(pos);
                            // Occasional obelisk
                            if (Random.value < 0.5f) {
                                Vector3 obPos = pos + new Vector3(Random.Range(-10f, 10f), 0f, Random.Range(-10f, 10f));
                                obPos.y = GetTerrainHeight(obPos);
                                if (Random.value < 0.5f && obeliskNewPrefab != null) {
                                    PlaceIntegratedAsset(root.transform, obPos, obeliskNewPrefab, 1.0f, true, false, 0f, Random.Range(0f, 360f), true);
                                } else {
                                    BuildProceduralObelisk(root.transform, obPos, wallMat, Random.value < 0.3f);
                                }
                            }
                        }
                    }

                    // Enemies
                    if (enemyPrefab != null && Random.value < 0.08f) {
                        var e = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab, root.transform);
                        e.transform.position = pos + Vector3.up * 0.5f;
                        var zai = e.GetComponent<TheAlchemistsCrypt.AI.ZombieAI>() ?? e.AddComponent<TheAlchemistsCrypt.AI.ZombieAI>();
                        zai.maxHealth = 10f; zai.currentHealth = 10f;
                    }
                }
            }

            // Cleanup
            CleanupOverlappingColumns(root);

            // Add dynamic distance-based culling for mobile optimization
            // root.AddComponent<TheAlchemistsCrypt.Utils.DistanceCuller>(); //disabled due to weird results: do not delete comment

            CreateProceduralPyramid(root, new Vector3(-450f, 0f, 400f), 150f, 95f, wallMat, new Color(1f, 0.85f, 0.4f));
            CreateProceduralPyramid(root, new Vector3(450f, 0f, 400f), 160f, 100f, wallMat, new Color(1f, 0.5f, 0.2f)); 
            CreateProceduralPyramid(root, new Vector3(450f, 0f, 120f), 140f, 85f, wallMat, new Color(1f, 0.82f, 0.45f));
            CreateProceduralPyramid(root, new Vector3(-450f, 0f, 120f), 170f, 110f, wallMat, new Color(1f, 0.7f, 0.3f)); 
            
            // Add obelisks in the empty space between the front and back pyramids
            for (float x = -300f; x <= 300f; x += 150f) {
                Vector3 obPos = new Vector3(x, 0f, 260f); // Middle area
                obPos.y = GetTerrainHeight(obPos);
                BuildProceduralObelisk(root.transform, obPos, wallMat, true, false);
            } 

            SpawnDesertBrokenPillars(root, wallMat);
            SpawnPalmTreeOasis(root, trees);
            SpawnLowPolyEnvironmentObjects(root);
            SpawnDesertMedicinePickups(root);

            // Spawn shoreline lighthouse specifically
            if (lighthousePrefab != null) {
                Vector3 lighthousePos = new Vector3(140f, 0f, -55f);
                lighthousePos.y = GetTerrainHeight(lighthousePos);
                var lighthouse = PlaceIntegratedAsset(root.transform, lighthousePos, lighthousePrefab, 1.6f, true, false, 0f, 180f, false);
                if (lighthouse != null) {
                    lighthouse.name = "ShorelineLighthouse";
                }
                occupiedPositions.Add(lighthousePos);
            }

            // Spawn player house (lay_house.glb) explicitly so player starts inside it
            if (layHousePrefab != null) {
                Vector3 spawnHousePos = new Vector3(0f, 0f, 60f); // Near central plaza
                spawnHousePos.y = GetTerrainHeight(spawnHousePos);
                var spawnHouse = PlaceIntegratedAsset(root.transform, spawnHousePos, layHousePrefab, 1.0f, true, true, 0f, 180f, false);
                if (spawnHouse != null) {
                    spawnHouse.name = "LayHouse_Spawn";
                }
                occupiedPositions.Add(spawnHousePos);
            }

            // Spawn extra obelisks and columns in the city (some fallen/broken/shattered) to fill empty spaces
            int extraObelisks = 55;
            for (int i = 0; i < extraObelisks; i++) {
                float rx = Random.Range(-240f, 240f);
                float rz = Random.Range(-40f, 240f);
                Vector3 pos = new Vector3(rx, 0f, rz);
                
                bool tooClose = false;
                foreach (var occ in occupiedPositions) {
                    if (Vector3.Distance(pos, occ) < 15f) {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                pos.y = GetTerrainHeight(pos);
                if (pos.y > 0.5f) {
                    BuildProceduralObelisk(root.transform, pos, wallMat, Random.value < 0.5f, Random.value < 0.4f);
                    occupiedPositions.Add(pos);
                }
            }

            // Spawn extra crates and barrels
            int extraCrates = 200;
            var cratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/EgyptianAssets/crate.glb");
            var barrelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/EgyptianAssets/barrel.glb");
            if (cratePrefab != null && barrelPrefab != null) {
                for (int i = 0; i < extraCrates; i++) {
                    float rx = Random.Range(-240f, 240f);
                    float rz = Random.Range(-40f, 240f);
                    Vector3 pos = new Vector3(rx, 0f, rz);
                    
                    bool tooClose = false;
                    foreach (var occ in occupiedPositions) {
                        if (Vector3.Distance(pos, occ) < 4f) { // Crates can be closer to buildings
                            tooClose = true;
                            break;
                        }
                    }
                    if (tooClose) continue;

                    pos.y = GetTerrainHeight(pos);
                    if (pos.y > 0.5f) {
                        GameObject prefabToUse = (Random.value < 0.5f) ? cratePrefab : barrelPrefab;
                        PlaceIntegratedAsset(root.transform, pos, prefabToUse, 1.0f, true, false, 0f, Random.Range(0f, 360f), false);
                    }
                }
            }

            int extraFallenCols = 45;
            for (int i = 0; i < extraFallenCols; i++) {
                float rx = Random.Range(-220f, 220f);
                float rz = Random.Range(-45f, 220f);
                Vector3 pos = new Vector3(rx, 0f, rz);
                
                bool tooClose = false;
                foreach (var occ in occupiedPositions) {
                    if (Vector3.Distance(pos, occ) < 12f) {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                pos.y = GetTerrainHeight(pos);
                var colPrefab = GetRandomColumn();
                if (pos.y > 0.5f && colPrefab != null) {
                    SpawnFallenColumn(root.transform, pos, colPrefab);
                    occupiedPositions.Add(pos);
                }
            }

            // --- CENTRAL PLAZA & DOOR MONUMENT ---
            Vector3 centerPlazaPos = new Vector3(0f, 0f, 30f);
            centerPlazaPos.y = GetTerrainHeight(centerPlazaPos);
            PlacePlaza(root.transform, centerPlazaPos, trees, GetRandomColumn(), floorMat);
            occupiedPositions.Add(centerPlazaPos);

            if (doorPrefab != null) {
                var centerDoor = PlaceIntegratedAsset(root.transform, centerPlazaPos, doorPrefab, 1.5f, true, false, 0f, 0f, false);
                if (centerDoor != null) {
                    centerDoor.name = "CityCenterDoor";
                }
            }

            // --- STRATEGIC LANDMARK PLACEMENT ---
            if (sphinxPrefab != null) {
                // North-East outlier in Ancient District, moved closer to the city center for better visibility
                Vector3 sphinxPos = new Vector3(150f, 0f, 160f);
                var sphinx = PlaceIntegratedAsset(root.transform, sphinxPos, sphinxPrefab, 1.0f, true, false, 0f, 220f, false);
                if (sphinx != null) {
                    sphinx.name = "GreatSphinx";
                }
                occupiedPositions.Add(sphinxPos);
            }

            if (mastabaPrefab != null) {
                // Center of the Ancient District
                Vector3 mastabaPos = new Vector3(0f, 0f, 210f);
                var mastaba = PlaceIntegratedAsset(root.transform, mastabaPos, mastabaPrefab, 0.84f, true, true, -2.2f, 0f, false);
                if (mastaba != null) {
                    mastaba.name = "MastabaOfQar";
                    
                    // Add Door frame/slab at the entrance (Z = 205)
                    // Temporarily disabled egyptian_door as requested
                    /*
                    if (doorPrefab != null) {
                        Vector3 doorPos = new Vector3(0f, 0f, 205f);
                        var door = PlaceIntegratedAsset(mastaba.transform, doorPos, doorPrefab, 1.0f, true, false, 0f, 0f, false);
                        if (door != null) door.name = "MastabaDoor";
                    }
                    */
                }
                occupiedPositions.Add(mastabaPos);
            }

            if (templeComplexPrefab != null) {
                // West edge of the Ancient District
                Vector3 templePos = new Vector3(-220f, 0f, 210f);
                var tObj = PlaceIntegratedAsset(root.transform, templePos, templeComplexPrefab, 1.0f, true, true, 0f, 90f, false);
                if (tObj != null) {
                    tObj.name = "TempleComplex";
                    // Palm tree inside
                    Vector3 treeInsidePos = templePos + new Vector3(5f, 0f, 5f);
                    PlaceIntegratedAsset(tObj.transform, treeInsidePos, trees[Random.Range(0, trees.Length)], 1.0f, true, false, -1.8f, Random.Range(0f, 360f), true);
                }
                occupiedPositions.Add(templePos);
            }

            if (templesPackPrefab != null) {
                // Interspersed smaller temples in Ancient District
                Vector3[] extraTemplePositions = { new Vector3(-110f, 0f, 150f), new Vector3(110f, 0f, 150f) };
                float[] extraTempleYaws = { 45f, -45f };
                for (int i = 0; i < extraTemplePositions.Length; i++) {
                    PlaceIntegratedAsset(root.transform, extraTemplePositions[i], templesPackPrefab, 1.0f, true, true, 0f, extraTempleYaws[i], false);
                    occupiedPositions.Add(extraTemplePositions[i]);
                }
            }

            // Fill empty city spaces with palm trees
            SpawnCityPalmTrees(root, trees, occupiedPositions);

            CreateSeaAndCoastline(root);
            CreateWorldBounds(root);
            FixPlayerAndWeapons();
            SetupMummyAnimations();

            // Build NavMesh surface at the end to include all generated city items correctly
            var surface = root.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.RenderMeshes;
            surface.BuildNavMesh();

            // Combine all static meshes under the city root to minimize draw calls and maximize mobile FPS!
            StaticBatchingUtility.Combine(root);

            // ── Post-generation performance setup ──────────────────────────────────────────
            // Marks all city children as OccluderStatic/OccludeeStatic/BatchingStatic so they
            // participate in static batching and are ready for occlusion culling baking.
            // Also converts any remaining Standard-shader materials to URP Lit (fixes SRP Batcher)
            // and applies the common sandstone albedo and normal map textures.
            // NOTE: Occlusion culling BAKE is intentionally NOT done here so city iteration
            // stays fast. Use Egyptian → 🔥 Bake Occlusion Culling when ready for final testing.
            URPSRPBatcherFixer.FixMaterialsNoDialog();
            // ─────────────────────────────────────────────────────────────────────────────

            var activeScene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            
            // Clean up any remaining unused assets before saving to minimize scene file size
            EditorUtility.UnloadUnusedAssetsImmediate();
            
            EditorSceneManager.SaveScene(activeScene);
            
            StaticOcclusionCulling.Clear();
            
            Debug.Log("Polished Egyptian City V5.2 generated! Static flags + SRP materials fixed. Run 'Egyptian → 🔥 Bake Occlusion Culling' before final testing.");
        }

        private GameObject PlaceIntegratedAsset(Transform parent, Vector3 pos, GameObject prefab, float scaleMultiplier, bool decimate, bool enterable = false, float yOffset = 0f, float targetAngleY = 0f, bool useRandomRotationY = false)
        {
            if (prefab == null) return null;

            // Hotfix: Prevent spawning trees in the sea or shallows (height < 1.1f or Z < -70f)
            string pName = prefab.name.ToLower();
            if (pName.Contains("palm") || pName.Contains("tree"))
            {
                if (pos.z < -70f || GetTerrainHeight(pos) < 1.1f)
                {
                    return null; // Prevent spawning trees in/near the water
                }
            }

            var obj = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            obj.isStatic = true;
            if (pName.Contains("temple") || pName.Contains("mastaba"))
            {
                RemoveFloorsFromLandmarks(obj);
            }

            // Step 1: Calculate local bounds in root space using pure local hierarchy math
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

            // Step 2: Determine target size and calculate scale
            float finalScale = scaleMultiplier;
            if (hasBounds) {
                float targetSize = -1f;
                bool scaleByHeight = false;
                string name = prefab.name.ToLower();
                
                if (name.Contains("house")) targetSize = 25f;
                else if (name.Contains("sphinx")) targetSize = 85f;
                else if (name.Contains("mastaba")) targetSize = 35f; // Spacious for interior navigation
                else if (name.Contains("temple_complex")) targetSize = 70f;
                else if (name.Contains("egyptian_temples")) targetSize = 120f; 
                else if (name.Contains("obelisk")) targetSize = 16f;
                else if (name.Contains("medieval_stall")) {
                    targetSize = 2.5f; // Restore to original standard size
                    scaleByHeight = true;
                }
                else if (name.Contains("vietnamese_meat_market_stall")) {
                    targetSize = 3.3f; // 1.5x the current size (2.2 * 1.5)
                    scaleByHeight = true;
                }
                else if (name.Contains("low_poly_market_stall_pack")) {
                    targetSize = 14.4f; // Set to 3x the previous size (4.8f * 3) as requested
                    scaleByHeight = true;
                }
                else if (name.Contains("stall")) {
                    targetSize = 2.4f; // Restore fallback to standard size
                    scaleByHeight = true;
                }
                else if (name.Contains("palm")) targetSize = 18f;
                else if (name.Contains("farmer")) targetSize = 25f; // Reclassified as house
                else if (name.Contains("door")) targetSize = 4.5f;

                if (targetSize > 0) {
                    float dimensionToScale = scaleByHeight ? localBounds.size.y : Mathf.Max(localBounds.size.x, localBounds.size.y, localBounds.size.z);
                    if (dimensionToScale > 0) {
                        finalScale = (targetSize / dimensionToScale) * scaleMultiplier;
                    }
                    if (name.Contains("stall")) {
                        Debug.Log($"[StallScale] Name: {prefab.name}, TargetSize: {targetSize}, DimensionToScale(Y): {dimensionToScale}, FinalScale: {finalScale}, scaleMultiplier: {scaleMultiplier}");
                    }
                }
            }
            obj.transform.localScale = Vector3.one * finalScale;

            // Step 3: Intelligent GLB-aware rotation (keep native X/Z, rotate around Y)
            float nativeRotX = prefab.transform.rotation.eulerAngles.x;
            float nativeRotZ = prefab.transform.rotation.eulerAngles.z;
            float yaw = useRandomRotationY ? Random.Range(0f, 360f) : targetAngleY;
            Quaternion targetRotation = Quaternion.Euler(nativeRotX, yaw, nativeRotZ);
            obj.transform.rotation = targetRotation;

            // Step 4: Precise ground alignment and horizontal centering using local math
            if (hasBounds) {
                float terrainY = GetTerrainHeight(pos);
                float adjustedYOffset = yOffset;
                if (prefab.name.ToLower().Contains("house")) {
                    adjustedYOffset -= 1.4f;
                }
                float targetBottomY = terrainY + adjustedYOffset;
                
                // Find world bottom Y after scaling and rotation using simulated TRS
                float worldMinY = float.MaxValue;
                Vector3[] corners = GetBoundsCorners(localBounds);
                Matrix4x4 testTRS = Matrix4x4.TRS(pos, targetRotation, obj.transform.localScale);
                
                foreach (var corner in corners) {
                    Vector3 worldCorner = testTRS.MultiplyPoint3x4(corner);
                    if (worldCorner.y < worldMinY) worldMinY = worldCorner.y;
                }
                
                float deltaY = targetBottomY - worldMinY;
                Vector3 worldCenter = testTRS.MultiplyPoint3x4(localBounds.center);
                
                obj.transform.position = new Vector3(
                    pos.x - (worldCenter.x - pos.x),
                    pos.y + deltaY,
                    pos.z - (worldCenter.z - pos.z)
                );
            } else {
                obj.transform.position = pos + new Vector3(0f, yOffset, 0f);
            }

            // Step 5: Optimization and Collision
            // Keep 80% decimation for trees (date palm), disable for all other models as requested to preserve quality and prevent offsets.
            if (decimate) {
                if (pName.Contains("palm") || pName.Contains("tree")) {
                    DecimateRecursively(obj, 0.8f);
                    // PERFORMANCE: Add LOD group to cull high-poly palm trees at distance.
                    // Date palms are 33-38 MB GLBs with very high vertex counts.
                    // LOD0: full mesh up to 30m, LOD1: decimated mesh up to 60m, Cull beyond.
                    // AddLODGroupToPalmTree(obj); // DISABLED to fix trees hiding when close
                }
            }

            if (enterable) {
                foreach (var mf in obj.GetComponentsInChildren<MeshFilter>(true)) {
                    if (mf.sharedMesh == null) continue;
                    var collider = mf.gameObject.GetComponent<MeshCollider>();
                    if (collider == null) {
                        collider = mf.gameObject.AddComponent<MeshCollider>();
                    }
                    collider.sharedMesh = mf.sharedMesh;
                    collider.convex = false;
                }
            } else {
                var colliders = obj.GetComponentsInChildren<Collider>(true);
                if (colliders.Length == 0) {
                    var filters = obj.GetComponentsInChildren<MeshFilter>(true);
                    if (filters.Length > 0) {
                        foreach (var filterObj in filters) {
                            if (filterObj.sharedMesh == null) continue;
                            
                            string filterName = filterObj.gameObject.name.ToLower();
                            if (filterName.Contains("meat") || filterName.Contains("food") || 
                                filterName.Contains("utensil") || filterName.Contains("plate") || 
                                filterName.Contains("cup") || filterName.Contains("detail") || 
                                filterName.Contains("prop") || filterName.Contains("casing") ||
                                filterName.Contains("barrel") || filterName.Contains("crate")) 
                            {
                                continue;
                            }

                            // PERFORMANCE: Use MeshCollider for complex structures like houses to support roof walking, otherwise BoxCollider
                            if (pName.Contains("house") || filterName.Contains("house") || pName.Contains("building")) {
                                var mc = filterObj.gameObject.GetComponent<MeshCollider>();
                                if (mc == null) {
                                    mc = filterObj.gameObject.AddComponent<MeshCollider>();
                                }
                                mc.sharedMesh = filterObj.sharedMesh;
                                mc.convex = false;
                            } else {
                                var bc = filterObj.gameObject.GetComponent<BoxCollider>();
                                if (bc == null) {
                                    bc = filterObj.gameObject.AddComponent<BoxCollider>();
                                }
                            }
                        }
                    } else {
                        obj.AddComponent<BoxCollider>();
                    }
                }
            }

            // Hotfix: Remove MeshRenderer from any children named "COLLIDER" or whose material is named "COLLIDER"
            // so they don't render as solid colored boxes and don't get combined by StaticBatchingUtility.Combine.
            var childRenderers = obj.GetComponentsInChildren<Renderer>(true);
            foreach (var r in childRenderers)
            {
                if (r == null) continue;
                bool isCollider = r.name.Contains("COLLIDER", System.StringComparison.OrdinalIgnoreCase) ||
                                  r.name.Contains("Collider", System.StringComparison.OrdinalIgnoreCase);
                if (!isCollider && r.sharedMaterials != null)
                {
                    foreach (var m in r.sharedMaterials)
                    {
                        if (m != null && (m.name.Contains("COLLIDER", System.StringComparison.OrdinalIgnoreCase) ||
                                            m.name.Contains("Collider", System.StringComparison.OrdinalIgnoreCase)))
                        {
                            isCollider = true;
                            break;
                        }
                    }
                }

                if (isCollider)
                {
                    UnityEngine.Object.DestroyImmediate(r);
                }
            }

            return obj;
        }

        private void ApplySandstoneTintToAllBuildings(GameObject rootObj)
        {
            Renderer[] renderers = rootObj.GetComponentsInChildren<Renderer>(true);
            Color sandstoneTint = new Color(0.88f, 0.74f, 0.52f);
            
            foreach (var r in renderers)
            {
                if (r == null || r is ParticleSystemRenderer || r is TrailRenderer) continue;
                
                // Skip terrain, player, enemies, UI
                string nameLower = r.gameObject.name.ToLower();
                Transform curr = r.transform;
                bool skip = false;
                while (curr != null)
                {
                    string pName = curr.gameObject.name.ToLower();
                    if (pName.Contains("player") || pName.Contains("enemy") || pName.Contains("zombie") || 
                        pName.Contains("mummy") || pName.Contains("terrain") || pName.Contains("sea") || 
                        pName.Contains("water") || pName.Contains("canvas") || pName.Contains("hud"))
                    {
                        skip = true;
                        break;
                    }
                    curr = curr.parent;
                }
                if (skip) continue;
                
                Material[] mats = r.sharedMaterials;
                Material[] newMats = new Material[mats.Length];
                bool anyChanged = false;
                
                for (int i = 0; i < mats.Length; i++)
                {
                    Material mat = mats[i];
                    if (mat == null) continue;
                    
                    string matName = mat.name.ToLower();
                    // Identify building/stone materials
                    bool isStoneAsset =
                        matName.Contains("house") || matName.Contains("building") ||
                        matName.Contains("temple") || matName.Contains("tomb") ||
                        matName.Contains("sphinx") || matName.Contains("mastaba") ||
                        matName.Contains("ruin")   || matName.Contains("fort");
                        
                    if (matName.Contains("column") || matName.Contains("pillar") || 
                        matName.Contains("ladder") || matName.Contains("door") || 
                        matName.Contains("gate") || matName.Contains("stall") || 
                        matName.Contains("obelisk") || matName.Contains("prop"))
                    {
                        isStoneAsset = false;
                    }
                        
                    if (isStoneAsset)
                    {
                        Material instMat = new Material(mat);
                        Color albedo = instMat.HasProperty("_BaseColor") ? instMat.GetColor("_BaseColor") : Color.white;
                        Color tintedAlbedo = Color.Lerp(albedo, new Color(sandstoneTint.r, sandstoneTint.g, sandstoneTint.b, albedo.a), 0.35f);
                        if (instMat.HasProperty("_BaseColor"))
                        {
                            instMat.SetColor("_BaseColor", tintedAlbedo);
                        }
                        else if (instMat.HasProperty("_Color"))
                        {
                            instMat.SetColor("_Color", tintedAlbedo);
                        }
                        newMats[i] = instMat;
                        anyChanged = true;
                    }
                    else
                    {
                        newMats[i] = mat;
                    }
                }
                
                if (anyChanged)
                {
                    r.sharedMaterials = newMats;
                }
            }
        }

        /// <summary>
        /// Adds a 3-level LOD group to palm trees for significant GPU vertex throughput savings.
        ///
        /// LOD0 (0 – 30m):  Full original mesh (high-poly)
        /// LOD1 (30 – 60m): Same rendered meshes but rendered at reduced screen-space coverage
        ///                   (Unity automatically reduces overdraw at distance)
        /// Cull (60m+):     Renderer disabled entirely — palm trees beyond 60m are invisible at
        ///                   typical mobile resolution and camera FOV anyway.
        ///
        /// Expected FPS gain: 5–12 FPS depending on how many palms are visible at once.
        /// </summary>
        
    }
}
