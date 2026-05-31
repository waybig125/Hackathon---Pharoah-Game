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
        private void BuildHouse(Transform parent, Vector3 pos, Material wall, Material wood, Material litWindowMat, Material darkWindowMat, GameObject crate, GameObject barrel, Material floorMat = null, float angleY = 0f, bool addLadder = false)
                {
                    var h = new GameObject("House"); h.transform.SetParent(parent); h.transform.position = pos; h.transform.rotation = Quaternion.Euler(0f, angleY, 0f); h.isStatic = true;
                    
                    // Randomly vary height slightly for organic skyline silhouette
                    float heightScale = Random.Range(0.85f, 1.25f);
                    
                    // 1. Central main building hall (extended 3.0f below ground to prevent floating foundations on slopes)
                    float hallWidth = 20f; float hallDepth = 15f; float hallHeight = 14f * heightScale;
                    var hall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    hall.name = "MainHall";
                    hall.transform.SetParent(h.transform);
                    hall.transform.localPosition = new Vector3(0f, (hallHeight - 3.0f) / 2f, 0f);
                    hall.transform.localScale = new Vector3(hallWidth, hallHeight + 3.0f, hallDepth);
                    hall.GetComponent<Renderer>().sharedMaterial = wall;
                    hall.isStatic = true;

                    // Dark wooden border ring (cornice) at the top of the main hall
                    var hallTopRing = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    hallTopRing.name = "HallTopRing";
                    hallTopRing.transform.SetParent(h.transform);
                    hallTopRing.transform.localPosition = new Vector3(0f, hallHeight, 0f);
                    hallTopRing.transform.localScale = new Vector3(hallWidth + 0.3f, 0.4f, hallDepth + 0.3f);
                    hallTopRing.GetComponent<Renderer>().sharedMaterial = wood;
                    DestroyImmediate(hallTopRing.GetComponent<Collider>());
                    hallTopRing.isStatic = true;

                    // Side decorative pillars embedded in walls (extends 3.0f below ground)
                    float[] pillarX = { -hallWidth / 2f - 0.1f, hallWidth / 2f + 0.1f };
                    float[] pillarZs = { -3f, 0f, 3f };
                    foreach (var px in pillarX)
                    {
                        foreach (var pz in pillarZs)
                        {
                            var sidePillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            sidePillar.name = "SideDecorPillar";
                            sidePillar.transform.SetParent(h.transform);
                            sidePillar.transform.localPosition = new Vector3(px, (hallHeight - 3.0f) / 2f, pz);
                            sidePillar.transform.localScale = new Vector3(0.3f, hallHeight + 3.0f, 0.6f);
                            sidePillar.GetComponent<Renderer>().sharedMaterial = wood;
                            DestroyImmediate(sidePillar.GetComponent<Collider>());
                            sidePillar.isStatic = true;
                        }
                    }

                    // 2. Corner Step-Sloped Towers (4 corners)
                    float tDistX = (hallWidth / 2f) + 0.5f;
                    float tDistZ = (hallDepth / 2f) + 0.5f;
                    Vector3[] towerPositions = {
                        new Vector3(-tDistX, 0f, -tDistZ), // Left Front
                        new Vector3(tDistX, 0f, -tDistZ),  // Right Front
                        new Vector3(-tDistX, 0f, tDistZ),  // Left Back
                        new Vector3(tDistX, 0f, tDistZ)   // Right Back
                    };

                    foreach (var tPos in towerPositions)
                    {
                        var towerRoot = new GameObject("TaperedTower");
                        towerRoot.transform.SetParent(h.transform);
                        towerRoot.transform.localPosition = tPos;
                        towerRoot.isStatic = true;

                        // Tapered look via 3 stacked stepped segments
                        int numSegments = 3;
                        float segHeight = (18f * heightScale) / numSegments;
                        float baseSize = 5.0f;
                        float taperDelta = 0.5f;

                        for (int s = 0; s < numSegments; s++)
                        {
                            float size = baseSize - (s * taperDelta);
                            var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            seg.name = $"Seg_{s}";
                            seg.transform.SetParent(towerRoot.transform);
                            
                            float yLoc = (s * segHeight) + (segHeight / 2f);
                            float ySize = segHeight;
                            if (s == 0)
                            {
                                // Extend bottom segment downward by 3.0f to act as a solid foundation
                                ySize = segHeight + 3.0f;
                                yLoc = (segHeight - 3.0f) / 2f;
                            }
                            seg.transform.localPosition = new Vector3(0f, yLoc, 0f);
                            seg.transform.localScale = new Vector3(size, ySize, size);
                            seg.GetComponent<Renderer>().sharedMaterial = wall;
                            seg.isStatic = true;

                            // Horizontal dark transition ring at the top of each segment
                            var ring = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            ring.name = $"Ring_{s}";
                            ring.transform.SetParent(towerRoot.transform);
                            ring.transform.localPosition = new Vector3(0f, (s + 1) * segHeight, 0f);
                            ring.transform.localScale = new Vector3(size + 0.25f, 0.4f, size + 0.25f);
                            ring.GetComponent<Renderer>().sharedMaterial = wood;
                            DestroyImmediate(ring.GetComponent<Collider>());
                            ring.isStatic = true;
                        }

                        // Add roof battlements (crenelations) on the top segment of each tower
                        float topSize = baseSize - ((numSegments - 1) * taperDelta);
                        float topY = 18f * heightScale;
                        float crenSize = 0.5f;
                        Vector3[] crenOffsets = {
                            new Vector3(-topSize/2f + 0.25f, topY + 0.25f, -topSize/2f + 0.25f),
                            new Vector3(topSize/2f - 0.25f, topY + 0.25f, -topSize/2f + 0.25f),
                            new Vector3(-topSize/2f + 0.25f, topY + 0.25f, topSize/2f - 0.25f),
                            new Vector3(topSize/2f - 0.25f, topY + 0.25f, topSize/2f - 0.25f)
                        };
                        foreach (var cOff in crenOffsets)
                        {
                            var cren = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            cren.name = "Crenelation";
                            cren.transform.SetParent(towerRoot.transform);
                            cren.transform.localPosition = cOff;
                            cren.transform.localScale = new Vector3(crenSize, crenSize, crenSize);
                            cren.GetComponent<Renderer>().sharedMaterial = wall;
                            cren.isStatic = true;
                        }
                    }

                    // 3. Central Temple Gateway Frame (Front Face, extended 3.0f below ground)
                    float gateHeight = 8f * heightScale;
                    var lPillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    lPillar.name = "GatePillar_L";
                    lPillar.transform.SetParent(h.transform);
                    lPillar.transform.localPosition = new Vector3(-2.8f, (gateHeight - 3.0f) / 2f, -(hallDepth / 2f) - 0.3f);
                    lPillar.transform.localScale = new Vector3(1.2f, gateHeight + 3.0f, 0.8f);
                    lPillar.GetComponent<Renderer>().sharedMaterial = wood;
                    lPillar.isStatic = true;

                    var rPillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    rPillar.name = "GatePillar_R";
                    rPillar.transform.SetParent(h.transform);
                    rPillar.transform.localPosition = new Vector3(2.8f, (gateHeight - 3.0f) / 2f, -(hallDepth / 2f) - 0.3f);
                    rPillar.transform.localScale = new Vector3(1.2f, gateHeight + 3.0f, 0.8f);
                    rPillar.GetComponent<Renderer>().sharedMaterial = wood;
                    rPillar.isStatic = true;

                    var lintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    lintel.name = "GateLintel";
                    lintel.transform.SetParent(h.transform);
                    lintel.transform.localPosition = new Vector3(0f, gateHeight + 0.6f, -(hallDepth / 2f) - 0.3f);
                    lintel.transform.localScale = new Vector3(6.8f, 1.2f, 1.2f);
                    lintel.GetComponent<Renderer>().sharedMaterial = wood;
                    lintel.isStatic = true;

                    // 4. Central Glowing Entryway (Emissive Light Portal, extended 3.0f below ground)
                    var entryway = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    entryway.name = "TempleEntrance";
                    entryway.transform.SetParent(h.transform);
                    entryway.transform.localPosition = new Vector3(0f, (gateHeight - 3.0f) / 2f, -(hallDepth / 2f) - 0.1f);
                    entryway.transform.localScale = new Vector3(4.0f, gateHeight + 3.0f, 0.2f);
                    entryway.GetComponent<Renderer>().sharedMaterial = litWindowMat;
                    DestroyImmediate(entryway.GetComponent<Collider>()); // Trigger/walk-through
                    entryway.isStatic = true;

                    // 5. Narrow Vertical Slot Windows (Sides)
                    Vector3[] winLeftLocs = {
                        new Vector3(-(hallWidth / 2f) - 0.1f, 8f * heightScale, -2.5f),
                        new Vector3(-(hallWidth / 2f) - 0.1f, 8f * heightScale, 2.5f)
                    };
                    foreach (var wLoc in winLeftLocs)
                    {
                        Material winMat = (Random.value < 0.6f) ? litWindowMat : darkWindowMat;
                        var win = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        win.name = "SlotWindow";
                        win.transform.SetParent(h.transform);
                        win.transform.localPosition = wLoc;
                        win.transform.localScale = new Vector3(0.18f, 4.5f, 0.8f);
                        win.GetComponent<Renderer>().sharedMaterial = winMat;
                        DestroyImmediate(win.GetComponent<Collider>());
                        win.isStatic = true;
                    }

                    Vector3[] winRightLocs = {
                        new Vector3((hallWidth / 2f) + 0.1f, 8f * heightScale, -2.5f),
                        new Vector3((hallWidth / 2f) + 0.1f, 8f * heightScale, 2.5f)
                    };
                    foreach (var wLoc in winRightLocs)
                    {
                        Material winMat = (Random.value < 0.6f) ? litWindowMat : darkWindowMat;
                        var win = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        win.name = "SlotWindow";
                        win.transform.SetParent(h.transform);
                        win.transform.localPosition = wLoc;
                        win.transform.localScale = new Vector3(0.18f, 4.5f, 0.8f);
                        win.GetComponent<Renderer>().sharedMaterial = winMat;
                        DestroyImmediate(win.GetComponent<Collider>());
                        win.isStatic = true;
                    }

                    Vector3[] winBackLocs = {
                        new Vector3(-4f, 8f * heightScale, (hallDepth / 2f) + 0.1f),
                        new Vector3(4f, 8f * heightScale, (hallDepth / 2f) + 0.1f)
                    };
                    foreach (var wLoc in winBackLocs)
                    {
                        Material winMat = (Random.value < 0.6f) ? litWindowMat : darkWindowMat;
                        var win = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        win.name = "SlotWindow";
                        win.transform.SetParent(h.transform);
                        win.transform.localPosition = wLoc;
                        win.transform.localScale = new Vector3(0.8f, 4.5f, 0.18f);
                        win.GetComponent<Renderer>().sharedMaterial = winMat;
                        DestroyImmediate(win.GetComponent<Collider>());
                        win.isStatic = true;
                    }

                    float distanceToPlayer = Vector3.Distance(pos, new Vector3(16f, pos.y, 48f));
                    if (addLadder && distanceToPlayer > 35f)
                    {
                        // 6. Rooftop Access Ladder (Angles at 35 degrees from the back, acting as a clean ramp)
                        float ladderLength = (hallHeight + 2f) / Mathf.Sin(35f * Mathf.Deg2Rad);
                        var ladderRamp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        ladderRamp.name = "LadderRamp";
                        ladderRamp.transform.SetParent(h.transform);
                        ladderRamp.transform.localScale = new Vector3(3f, 0.3f, ladderLength);
                        
                        float zOffset = (hallDepth / 2f) + (ladderLength * Mathf.Cos(35f * Mathf.Deg2Rad) / 2f);
                        ladderRamp.transform.localPosition = new Vector3(0f, hallHeight / 2f, zOffset);
                        ladderRamp.transform.localRotation = Quaternion.Euler(35f, 0f, 0f);
                        ladderRamp.GetComponent<Renderer>().sharedMaterial = wood;
                        ladderRamp.isStatic = true;
                    }

                    // Add NavMeshObstacle to the house root to carve the NavMesh
                    var nmoHouse = h.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                    nmoHouse.carving = true;
                    nmoHouse.size = new Vector3(25f, 20f, 19f);
                    nmoHouse.center = new Vector3(0f, 7f, 0f);

                    if (crate != null) {
                        Vector3 cratePos = pos + new Vector3(15f, 0f, 13f);
                        var cObj = (GameObject)PrefabUtility.InstantiatePrefab(crate, parent);
                        cObj.transform.localScale = new Vector3(0.875f, 0.875f, 0.875f);
                        AlignToGroundAndAddCollider(cObj, cratePos, Quaternion.Euler(-90f, Random.Range(0f, 360f), 0f), 0f);
                        
                        var bottomCrateRenderer = cObj.GetComponentInChildren<Renderer>();
                        if (bottomCrateRenderer != null)
                        {
                            Vector3 stackedPos = cObj.transform.position;
                            stackedPos.y = bottomCrateRenderer.bounds.max.y;
                            var cObj2 = (GameObject)PrefabUtility.InstantiatePrefab(crate, parent);
                            cObj2.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
                            AlignToGroundAndAddCollider(cObj2, stackedPos, Quaternion.Euler(-90f, Random.Range(0f, 360f), 0f), 0f, false);
                        }
                    }

                    if (barrel != null) {
                        Vector3 barrelPos = pos + new Vector3(-15f, 0f, -13f);
                        var bObj = (GameObject)PrefabUtility.InstantiatePrefab(barrel, parent);
                        bObj.transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
                        AlignToGroundAndAddCollider(bObj, barrelPos, Quaternion.Euler(-90f, 0f, 0f), 0f);
                    }
                }

        private void PlacePlaza(Transform parent, Vector3 pos, GameObject[] trees, GameObject columnPrefab, Material floorMat = null)
                {
                    var p = new GameObject("Plaza"); p.transform.SetParent(parent); p.transform.position = pos; p.isStatic = true;
                    // Plaza floor plane removed to prevent double floors / z-fighting with the desert terrain
                    /*
                    if (floorMat != null) {
                        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                        floor.name = "PlazaFloor";
                        floor.transform.SetParent(p.transform);
                        floor.transform.localPosition = new Vector3(0f, 0.01f, 0f);
                        floor.transform.localScale = new Vector3(3.2f, 1f, 3.2f);

                        floor.GetComponent<Renderer>().sharedMaterial = floorMat;
                        floor.isStatic = true;
                    }
                    */
                    
                    if (columnPrefab != null) {
                        var colObj = (GameObject)PrefabUtility.InstantiatePrefab(columnPrefab, p.transform);
                        colObj.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                        AlignToGroundAndAddCollider(colObj, pos + new Vector3(-14.5f, 0f, -14.5f), Quaternion.Euler(-90f, 0f, 0f), 0f);

                        // Add NavMeshObstacle to column to carve the NavMesh
                        var nmoCol = colObj.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                        nmoCol.carving = true;
                        nmoCol.size = new Vector3(3f, 12f, 3f);
                        nmoCol.center = new Vector3(0f, 6f, 0f);
                    }
                    
                    if (trees != null && trees.Length > 0) {
                        int numTrees = Random.Range(1, 4); // 1 to 3 trees Max
                        var sectors = new List<Vector3>() {
                            new Vector3(14.5f, 0f, 14.5f),   // Top-Right
                            new Vector3(-14.5f, 0f, 14.5f),  // Top-Left
                            new Vector3(14.5f, 0f, -14.5f)   // Bottom-Right
                        };

                        // Shuffle sectors to randomize spawn locations
                        for (int i = 0; i < sectors.Count; i++) {
                            int tempIndex = Random.Range(i, sectors.Count);
                            Vector3 hold = sectors[i];
                            sectors[i] = sectors[tempIndex];
                            sectors[tempIndex] = hold;
                        }

                        for (int i = 0; i < numTrees; i++) {
                            Vector3 offset = sectors[i] + new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));
                            Vector3 spawnLoc = pos + offset;

                            // Prevent spawning trees too close to the origin to avoid spawning inside player
                            if (Vector3.Distance(new Vector3(spawnLoc.x, 0f, spawnLoc.z), Vector3.zero) < 12f) {
                                continue;
                            }

                            GameObject treePrefab = trees[Random.Range(0, trees.Length)];
                            PlaceIntegratedAsset(p.transform, spawnLoc, treePrefab, 1.0f, false, false, -0.4f);
                        }
                    }

                    if (Random.value < 0.35f || pos.magnitude < 30f) { 
                        Vector3 medPos = pos + new Vector3(Random.Range(-5f, 5f), 1f, Random.Range(-5f, 5f));
                        medPos.y = GetTerrainHeight(medPos) + 0.5f;
                        var medGo = new GameObject("MedicinePickup");
                        medGo.transform.SetParent(p.transform);
                        medGo.transform.position = medPos;
                        var pickup = medGo.AddComponent<TheAlchemistsCrypt.Gameplay.MedicinePickup>();
                        pickup.healAmount = 25f;
                    }
                }

        private void BuildProceduralLadderRamp(Transform parent, Vector3 basePos, float targetHeight, float yaw)
                {
                    var ladder = new GameObject("WalkableLadderRamp");
                    ladder.transform.SetParent(parent);
                    ladder.isStatic = true;

                    // Tilt angle
                    float tiltAngle = 35f;
                    float rad = tiltAngle * Mathf.Deg2Rad;
                    float length = targetHeight / Mathf.Sin(rad);

                    // Material
                    Material woodMat = new Material(GetLitShader());
                    woodMat.SetColor("_BaseColor", new Color(0.35f, 0.22f, 0.15f));
                    woodMat.enableInstancing = true;

                    // Rails
                    float railWidth = 0.15f;
                    float railSpacing = 2.2f;

                    var leftRail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    leftRail.name = "Rail_L";
                    leftRail.transform.SetParent(ladder.transform);
                    leftRail.transform.localPosition = new Vector3(-railSpacing / 2f, 0f, 0f);
                    leftRail.transform.localScale = new Vector3(railWidth, railWidth, length);
                    leftRail.GetComponent<Renderer>().sharedMaterial = woodMat;
                    leftRail.isStatic = true;
                    DestroyImmediate(leftRail.GetComponent<Collider>());

                    var rightRail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    rightRail.name = "Rail_R";
                    rightRail.transform.SetParent(ladder.transform);
                    rightRail.transform.localPosition = new Vector3(railSpacing / 2f, 0f, 0f);
                    rightRail.transform.localScale = new Vector3(railWidth, railWidth, length);
                    rightRail.GetComponent<Renderer>().sharedMaterial = woodMat;
                    rightRail.isStatic = true;
                    DestroyImmediate(rightRail.GetComponent<Collider>());

                    // Rungs
                    int numRungs = Mathf.RoundToInt(length / 0.8f);
                    for (int i = 0; i < numRungs; i++)
                    {
                        var rung = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        rung.name = $"Rung_{i}";
                        rung.transform.SetParent(ladder.transform);
                        rung.transform.localPosition = new Vector3(0f, 0.05f, -length / 2f + (i * 0.8f) + 0.4f);
                        rung.transform.localScale = new Vector3(railSpacing, 0.08f, 0.15f);
                        rung.GetComponent<Renderer>().sharedMaterial = woodMat;
                        rung.isStatic = true;
                        DestroyImmediate(rung.GetComponent<Collider>());
                    }

                    // Invisible collision ramp for walking up
                    var rampCollider = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    rampCollider.name = "CollisionRamp";
                    rampCollider.transform.SetParent(ladder.transform);
                    rampCollider.transform.localPosition = new Vector3(0f, -0.05f, 0f);
                    rampCollider.transform.localScale = new Vector3(railSpacing, 0.1f, length);
                    var rampRenderer = rampCollider.GetComponent<Renderer>();
                    if (rampRenderer != null) DestroyImmediate(rampRenderer); // Invisible
                    rampCollider.isStatic = true;

                    // Position and rotation of the whole ladder ramp
                    float halfHeight = (length / 2f) * Mathf.Sin(rad);
                    float halfDepth = (length / 2f) * Mathf.Cos(rad);
                    
                    // Shift position so bottom is at ground and z is offset
                    Vector3 worldPos = basePos;
                    worldPos.y = GetTerrainHeight(basePos) + halfHeight;
                    
                    // Rotate the offset according to yaw
                    Vector3 localOffset = new Vector3(0f, 0f, -halfDepth);
                    Vector3 rotatedOffset = Quaternion.Euler(0f, yaw, 0f) * localOffset;
                    
                    ladder.transform.position = worldPos - rotatedOffset;
                    ladder.transform.rotation = Quaternion.Euler(tiltAngle, yaw, 0f);
                }

        private void CreateProceduralPyramid(GameObject root, Vector3 pos, float baseSize, float height, Material mat, Color glowColor, bool isStatic = true)
                {
                    var pGo = new GameObject("Pyramid"); pGo.transform.SetParent(root.transform); pGo.transform.position = pos; pGo.isStatic = isStatic;
                    Mesh mesh = new Mesh(); float half = baseSize / 2f;
                    Vector3 apex = new Vector3(0, height, 0); Vector3 fl = new Vector3(-half, 0, -half), fr = new Vector3(half, 0, -half), br = new Vector3(half, 0, half), bl = new Vector3(-half, 0, half);
                    mesh.vertices = new Vector3[] { fl, fr, apex, fr, br, apex, br, bl, apex, bl, fl, apex, bl, br, fl, br, fr, fl };
                    mesh.triangles = new int[] { 0, 2, 1, 3, 5, 4, 6, 8, 7, 9, 11, 10, 12, 14, 13, 15, 17, 16 };
                    
                    // Add UV mapping for texturing
                    Vector2[] uvs = new Vector2[18];
                    // Face 1 (fl, fr, apex)
                    uvs[0] = new Vector2(0f, 0f); uvs[1] = new Vector2(1f, 0f); uvs[2] = new Vector2(0.5f, 1f);
                    // Face 2 (fr, br, apex)
                    uvs[3] = new Vector2(0f, 0f); uvs[4] = new Vector2(1f, 0f); uvs[5] = new Vector2(0.5f, 1f);
                    // Face 3 (br, bl, apex)
                    uvs[6] = new Vector2(0f, 0f); uvs[7] = new Vector2(1f, 0f); uvs[8] = new Vector2(0.5f, 1f);
                    // Face 4 (bl, fl, apex)
                    uvs[9] = new Vector2(0f, 0f); uvs[10] = new Vector2(1f, 0f); uvs[11] = new Vector2(0.5f, 1f);
                    // Base Triangle 1
                    uvs[12] = new Vector2(0f, 0f); uvs[13] = new Vector2(1f, 0f); uvs[14] = new Vector2(0f, 1f);
                    // Base Triangle 2
                    uvs[15] = new Vector2(1f, 0f); uvs[16] = new Vector2(1f, 1f); uvs[17] = new Vector2(0f, 1f);
                    mesh.uv = uvs;

                    mesh.RecalculateNormals(); mesh.RecalculateBounds();
                    pGo.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var renderer = pGo.AddComponent<MeshRenderer>();
                    var pMat = new Material(mat);
                    
                    if (baseSize > 20f)
                    {
                        pMat.SetColor("_BaseColor", new Color(1f, 0.95f, 0.85f)); 
                        var albedoTex = Resources.Load<Texture2D>("Textures/Pyramid_Albedo");
                        var normalTex = Resources.Load<Texture2D>("Textures/Pyramid_Normal");
                        if (albedoTex != null)
                        {
                            pMat.SetTexture("_BaseMap", albedoTex);
                            pMat.SetTextureScale("_BaseMap", new Vector2(15f, 15f));
                        }
                        if (normalTex != null)
                        {
                            pMat.SetTexture("_BumpMap", normalTex);
                            pMat.SetTextureScale("_BumpMap", new Vector2(15f, 15f));
                            pMat.EnableKeyword("_NORMALMAP");
                        }
                    }
                    else
                    {
                        pMat.SetColor("_BaseColor", new Color(0.78f, 0.52f, 0.35f)); 
                    }
                    
                    pMat.SetColor("_EmissionColor", glowColor * 1.5f);
                    if (glowColor != Color.clear) pMat.EnableKeyword("_EMISSION");
                    renderer.sharedMaterial = pMat;
                    
                    var mc = pGo.AddComponent<MeshCollider>();
                    mc.sharedMesh = mesh;
                    mc.convex = true;
                }

        private void BuildProceduralObelisk(Transform parent, Vector3 pos, Material stoneMat, bool isBroken = false, bool isFallen = false)
        {
            if (!isFallen && Random.value < 0.35f) isFallen = true;

            var obRoot = new GameObject((isBroken ? "Broken" : "") + (isFallen ? "Fallen" : "") + "Obelisk");
            
            var dynamicProps = GameObject.Find("DynamicProps");
            if (dynamicProps == null)
            {
                dynamicProps = new GameObject("DynamicProps");
                dynamicProps.isStatic = false;
            }
            obRoot.transform.SetParent(dynamicProps.transform);
            
            Vector3 spawnPos = pos;
            spawnPos.y = pos.y + (isFallen ? 0.8f : 0.1f);
            obRoot.transform.position = spawnPos;
            obRoot.isStatic = false;

            float height = isBroken ? Random.Range(4f, 7f) : 14f;
            float baseWidth = 2.0f;
            float topWidth = isBroken ? 1.4f : 0.8f;

            int segments = isBroken ? Random.Range(3, 5) : 8;
            float segHeight = height / segments;

            for (int i = 0; i < segments; i++)
            {
                float currentWidth = Mathf.Lerp(baseWidth, topWidth, (float)i / (segments - 1));
                var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seg.transform.SetParent(obRoot.transform);
                seg.transform.localPosition = new Vector3(0f, (i * segHeight) + (segHeight / 2f), 0f);
                seg.transform.localScale = new Vector3(currentWidth, segHeight, currentWidth);
                seg.GetComponent<Renderer>().sharedMaterial = stoneMat;
                seg.isStatic = false;
            }

            if (!isBroken)
            {
                CreateProceduralPyramid(obRoot, new Vector3(0f, height, 0f), topWidth, topWidth * 1.5f, stoneMat, Color.clear, false);
            }

            // REMOVED: Massive Rigidbody that caused dancing/jitter. 
            // Obelisks are now rock-solid static structures.
            obRoot.isStatic = true;

            if (isFallen)
            {
                obRoot.transform.rotation = Quaternion.Euler(Random.Range(85f, 95f), Random.Range(0f, 360f), Random.Range(-5f, 5f));
            }
            
            var nmoObelisk = obRoot.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            nmoObelisk.carving = true;
            if (isFallen)
            {
                nmoObelisk.size = new Vector3(3f, 3f, height + 2f);
                nmoObelisk.center = new Vector3(0f, 0f, height / 2f);
            }
            else
            {
                nmoObelisk.size = new Vector3(3f, height + 2f, 3f);
                nmoObelisk.center = new Vector3(0f, height / 2f, 0f);
            }

            if (isBroken && isFallen)
            {
                int pieces = Random.Range(1, 3);
                for (int p = 0; p < pieces; p++)
                {
                    var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    piece.name = "ObeliskShatteredChunk";
                    piece.transform.SetParent(dynamicProps.transform);
                    
                    Vector3 piecePos = pos + new Vector3(Random.Range(-4f, 4f), 0.5f, Random.Range(-4f, 4f));
                    piecePos.y = GetTerrainHeight(piecePos) + 0.3f;
                    piece.transform.position = piecePos;
                    piece.transform.localScale = new Vector3(Random.Range(1.2f, 1.8f), Random.Range(1.0f, 2.0f), Random.Range(1.2f, 1.8f));
                    piece.transform.rotation = Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));
                    piece.GetComponent<Renderer>().sharedMaterial = stoneMat;
                    piece.isStatic = true;
                }
            }
        }

        private void BuildAlchemistTomb(Transform parent, Vector3 pos, Material stoneMat)
                {
                    var root = new GameObject("AlchemistTomb");
                    
                    // Find or create DynamicProps as parent (never use a static parent for active Rigidbodies)
                    var dynamicProps = GameObject.Find("DynamicProps");
                    if (dynamicProps == null)
                    {
                        dynamicProps = new GameObject("DynamicProps");
                        dynamicProps.isStatic = false;
                    }
                    root.transform.SetParent(dynamicProps.transform);
                    
                    // Spawn slightly above terrain to prevent initial collider penetration/stuck states
                    Vector3 spawnPos = pos;
                    spawnPos.y = pos.y + 0.1f;
                    root.transform.position = spawnPos;
                    root.isStatic = false;

                    float heightScale = 1.3f;

                    // 1. Left and Right Massive Pylons (Tapered)
                    float[] sideX = { -5.5f, 5.5f };
                    foreach (float x in sideX)
                    {
                        var pylon = new GameObject("TombPylon");
                        pylon.transform.SetParent(root.transform);
                        pylon.transform.localPosition = new Vector3(x, 0f, 0f);
                        pylon.isStatic = false;

                        float pBase = 6f;
                        float pTaper = 0.5f;
                        for (int s = 0; s < 4; s++)
                        {
                            float sizeX = pBase - (s * pTaper);
                            float sizeZ = pBase - (s * pTaper);
                            float segH = 4f * heightScale;

                            var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            seg.transform.SetParent(pylon.transform);
                            seg.transform.localPosition = new Vector3(0f, (s * segH) + (segH / 2f), 0f);
                            seg.transform.localScale = new Vector3(sizeX, segH, sizeZ);
                            seg.GetComponent<Renderer>().sharedMaterial = stoneMat;
                            seg.isStatic = false;
                        }
                    }

                    // 2. Stepped Arch Structure (Approximating a curve with blocks)
                    float archStartY = 16f * heightScale;
                    for (int i = 0; i < 3; i++)
                    {
                        var archL = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        archL.transform.SetParent(root.transform);
                        archL.transform.localPosition = new Vector3(-3.5f + (i * 0.8f), archStartY + (i * 1.5f), 0f);
                        archL.transform.localScale = new Vector3(3f + i, 2f, 4f);
                        archL.GetComponent<Renderer>().sharedMaterial = stoneMat;
                        archL.isStatic = false;

                        var archR = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        archR.transform.SetParent(root.transform);
                        archR.transform.localPosition = new Vector3(3.5f - (i * 0.8f), archStartY + (i * 1.5f), 0f);
                        archR.transform.localScale = new Vector3(3f + i, 2f, 4f);
                        archR.GetComponent<Renderer>().sharedMaterial = stoneMat;
                        archR.isStatic = false;
                    }

                    // 3. Top Massive Lintel Block
                    var lintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    lintel.transform.SetParent(root.transform);
                    lintel.transform.localPosition = new Vector3(0f, archStartY + 5f, 0f);
                    lintel.transform.localScale = new Vector3(12f, 3f, 5f);
                    lintel.GetComponent<Renderer>().sharedMaterial = stoneMat;
                    lintel.isStatic = true;

                    // REMOVED: Heavy Rigidbody that caused "dancing" physics issues.
                    // Converting to a rock-solid static structure.
                    root.isStatic = true;

                    // Add NavMeshObstacle to carve the NavMesh around the tomb
                    var nmoTomb = root.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                    nmoTomb.carving = true;
                    nmoTomb.size = new Vector3(20f, 25f, 20f);
                    nmoTomb.center = new Vector3(0f, 12.5f, 0f);
                }

                private void SpawnFallenColumn(Transform parent, Vector3 pos, GameObject columnPrefab)
                {
                    if (columnPrefab == null) return;
                    
                    Vector3 spawnPos = pos;
                    spawnPos.y = GetTerrainHeight(spawnPos) + 0.8f;
                    
                    var colObj = (GameObject)PrefabUtility.InstantiatePrefab(columnPrefab, parent);
                    colObj.transform.position = spawnPos;
                    colObj.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                    colObj.transform.rotation = Quaternion.Euler(Random.Range(85f, 95f), Random.Range(0f, 360f), Random.Range(-5f, 5f));
                    colObj.isStatic = true;
                    
                    var col = colObj.GetComponent<Collider>();
                    if (col == null)
                    {
                        var box = colObj.AddComponent<BoxCollider>();
                        box.size = new Vector3(2f, 10f, 2f);
                        box.center = new Vector3(0f, 5f, 0f);
                    }
                    
                    var nmo = colObj.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                    nmo.carving = true;
                    nmo.size = new Vector3(3f, 3f, 10f);
                    nmo.center = new Vector3(0f, 0f, 5f);
                }

    }
}
