using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace TheAlchemistsCrypt.Gameplay
{
    public class EscapeManager : MonoBehaviour
    {
        public static EscapeManager Instance;
        
        [Header("State")]
        public bool hasKey = false;
        public bool hasEscaped = false;
        
        [Header("UI References")]
        private GameObject promptUiGo;
        private Text promptText;
        private GameObject victoryUiGo;
        private GameObject deathUiGo;

        public GameObject keyObj;
        public GameObject boatObj;
        private bool nearKey = false;
        private bool nearBoat = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            SpawnKey();
            SpawnBoat();
            SetupUI();
        }

        private void SetupUI()
        {
            var canvas = GameObject.Find("P_LPSP_UI_Canvas");
            if (canvas == null) canvas = GameObject.Find("Canvas");
            if (canvas == null) return;

            promptUiGo = new GameObject("ProximityPrompt");
            promptUiGo.transform.SetParent(canvas.transform, false);
            var rect = promptUiGo.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, 100f);
            rect.sizeDelta = new Vector2(600, 50);

            promptText = promptUiGo.AddComponent<Text>();
            promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            promptText.fontSize = 24;
            promptText.alignment = TextAnchor.MiddleCenter;
            promptText.color = Color.white;
            var outline = promptUiGo.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, -2);
            promptUiGo.SetActive(false);
        }

        private void SpawnKey()
        {
            Vector3 chosenLoc;
            var plazas = new List<GameObject>();
            var allGo = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in allGo) if (go.name.Contains("Plaza")) plazas.Add(go);

            if (plazas.Count > 0)
            {
                var targetPlaza = plazas[Random.Range(0, plazas.Count)];
                chosenLoc = targetPlaza.transform.position + new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f));
                Debug.Log($"[EscapeManager] Spawning Papyrus at random Plaza: {targetPlaza.name} at {chosenLoc}");
            }
            else
            {
                Vector3[] spawnLocations = new Vector3[]
                {
                    new Vector3(250f, 0f, 350f), 
                    new Vector3(-280f, 0f, 250f), 
                    new Vector3(80f, 0f, 450f)    
                };
                chosenLoc = spawnLocations[Random.Range(0, spawnLocations.Length)];
                Debug.Log($"[EscapeManager] No Plazas found, spawning Papyrus at fallback location: {chosenLoc}");
            }
            
            var terrain = Terrain.activeTerrain;
            if (terrain != null) chosenLoc.y = terrain.SampleHeight(chosenLoc) + 0.8f;

            // Load custom model
            GameObject prefab = Resources.Load<GameObject>("papyrus");
            if (prefab != null)
            {
                keyObj = Instantiate(prefab, chosenLoc, Quaternion.identity);
                keyObj.transform.localScale = Vector3.one * 0.4f; 
            }
            else
            {
                keyObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder); 
                keyObj.transform.position = chosenLoc;
                keyObj.transform.localScale = new Vector3(0.2f, 0.4f, 0.2f);
                keyObj.transform.rotation = Quaternion.Euler(0, 0, 90f); 
            }
            keyObj.name = "AncientPapyrus";
            keyObj.SetActive(true);

            // Add a point light to the papyrus so it glows and is visible
            var lightGo = new GameObject("PapyrusLight", typeof(Light));
            lightGo.transform.SetParent(keyObj.transform);
            lightGo.transform.localPosition = Vector3.up * 0.5f;
            var l = lightGo.GetComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.9f, 0.5f);
            l.intensity = 5f;
            l.range = 8f;

            // Register with Minimap
            if (TheAlchemistsCrypt.UI.MinimapUI.Instance != null)
            {
                TheAlchemistsCrypt.UI.MinimapUI.Instance.RegisterDynamicStaticIcon(keyObj, new Vector2(16, 16), TheAlchemistsCrypt.UI.MinimapUI.Instance.keySprite);
            }
            
            var col = keyObj.GetComponent<Collider>();
            if (col == null) col = keyObj.AddComponent<MeshCollider>();
            if (col is MeshCollider mc) mc.convex = true;
            col.isTrigger = true;
            keyObj.AddComponent<Floater>();
        }

        private void SpawnBoat()
        {
            Vector3 spawnPos = new Vector3(0f, 1.2f, -106f);
            
            GameObject prefab = Resources.Load<GameObject>("boat");
            if (prefab != null)
            {
                boatObj = Instantiate(prefab, spawnPos, Quaternion.Euler(-90f, 90f, 0f));
                boatObj.transform.localScale = Vector3.one * 0.18f; 
            }
            else
            {
                boatObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                boatObj.transform.position = spawnPos;
                boatObj.transform.localScale = new Vector3(2f, 1f, 4f);
            }
            boatObj.name = "EscapeBoat";
            boatObj.SetActive(true);

            var boatLightGo = new GameObject("BoatBeacon", typeof(Light));
            boatLightGo.transform.SetParent(boatObj.transform);
            boatLightGo.transform.localPosition = Vector3.up * 3f;
            var bl = boatLightGo.GetComponent<Light>();
            bl.type = LightType.Point;
            bl.color = Color.blue;
            bl.intensity = 15f;
            bl.range = 25f;

            if (TheAlchemistsCrypt.UI.MinimapUI.Instance != null)
            {
                TheAlchemistsCrypt.UI.MinimapUI.Instance.RegisterDynamicStaticIcon(boatObj, new Vector2(24, 24), TheAlchemistsCrypt.UI.MinimapUI.Instance.boatSprite);
            }
            
            var col = boatObj.GetComponent<Collider>();
            if (col == null) col = boatObj.AddComponent<MeshCollider>();
            if (col is MeshCollider mc) mc.convex = true;
            col.isTrigger = true;
        }

        private void Update()
        {
            if (hasEscaped) return;
            if (!TheAlchemistsCrypt.UI.MobileHUDButtons.HasStartedGame) return;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            float distToKey = keyObj != null ? Vector3.Distance(player.transform.position, keyObj.transform.position) : 9999f;
            float distToBoat = boatObj != null ? Vector3.Distance(player.transform.position, boatObj.transform.position) : 9999f;

            nearKey = !hasKey && distToKey < 15f; 
            nearBoat = distToBoat < 15f;

            bool interactPressed = false;
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame) interactPressed = true;
            if (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame) interactPressed = true;

            if (nearKey)
            {
                if (promptUiGo != null) promptUiGo.SetActive(true);
                if (distToKey < 3.5f)
                {
                    hasKey = true;
                    if (keyObj != null) Destroy(keyObj);
                    if (promptText != null) promptText.text = "ANCIENT PAPYRUS COLLECTED! RUN TO THE BOAT!";
                    TheAlchemistsCrypt.Gameplay.AudioManager.PlayVoiceLine("Voice/vo_taunt_01");
                    TheAlchemistsCrypt.Gameplay.AudioManager.PlaySFX("sfx/sfx_pickup", false, 0.8f);
                }
                else
                {
                    if (promptText != null) promptText.text = "ANCIENT PAPYRUS DETECTED! MOVE CLOSER TO AUTO-PICKUP";
                }
            }
            else if (nearBoat)
            {
                if (promptUiGo != null) promptUiGo.SetActive(true);
                if (hasKey)
                { 
                    if (promptText != null) promptText.text = "YOU REACHED THE BOAT! TAP OR PRESS [E] TO BOARD AND ESCAPE!";
                    if (interactPressed)
                    {
                        WinGame();
                    }
                }
                else
                {
                    if (promptText != null) promptText.text = "THE ANCIENT BOAT. YOU NEED THE PAPYRUS TO DEPART.";
                }
            }
            else
            {
                if (promptUiGo != null && promptText != null)
                {
                    if (promptText.text.Contains("COLLECTED")) { if (distToBoat > 40f && distToKey > 40f) promptUiGo.SetActive(false); }
                    else promptUiGo.SetActive(false);
                }
            }
        }

        private void WinGame()
        {
            hasEscaped = true;
            if (TheAlchemistsCrypt.UI.MobileHUDButtons.Instance != null)
                TheAlchemistsCrypt.UI.MobileHUDButtons.Instance.ShowVictoryScreen();
        }
    }

    public class Floater : MonoBehaviour {
        private float startY;
        private void Start() { startY = transform.position.y; }
        private void Update() {
            transform.position = new Vector3(transform.position.x, startY + Mathf.Sin(Time.time * 2f) * 0.5f, transform.position.z);
            transform.Rotate(0, 45f * Time.deltaTime, 0);
        }
    }
}
