using UnityEngine;
using UnityEngine.UI;

namespace TheAlchemistsCrypt.Gameplay
{
    public class EscapeManager : MonoBehaviour
    {
        public static EscapeManager Instance;
        
        [Header("State")]
        public bool hasKey = false;
        public bool hasEscaped = false;

        private GameObject keyObj;
        private GameObject boatObj;

        private GameObject promptUiGo;
        private Text promptText;

        private bool nearKey = false;
        private bool nearBoat = false;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            SpawnKey();
            SpawnBoat();
            CreatePromptUI();
        }

        private void SpawnKey()
        {
            Vector3[] spawnLocations = new Vector3[]
            {
                new Vector3(250f, 0f, 350f), 
                new Vector3(-280f, 0f, 250f), 
                new Vector3(80f, 0f, 450f)    
            };

            Vector3 chosenLoc = spawnLocations[Random.Range(0, spawnLocations.Length)];
            
            var terrain = Terrain.activeTerrain;
            if (terrain != null) chosenLoc.y = terrain.SampleHeight(chosenLoc) + 0.8f;

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

            var lightGo = new GameObject("PapyrusLight", typeof(Light));
            lightGo.transform.SetParent(keyObj.transform);
            lightGo.transform.localPosition = Vector3.up * 0.5f;
            var l = lightGo.GetComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.9f, 0.5f);
            l.intensity = 5f;
            l.range = 8f;

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
            // Moved to Z=-104f (slightly past the Z=-100f CoastlineBarrier) and scaled to 0.18f
            Vector3 spawnPos = new Vector3(0f, 1.2f, -104f);
            
            GameObject prefab = Resources.Load<GameObject>("boat");
            if (prefab != null)
            {
                // Applied -90 on X (standard GLB/Blender fix) and 90 on Y to align "straight" with the coastline
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
            
            // Set all colliders in the boat hierarchy to solid so that characters cannot enter
            foreach (var c in boatObj.GetComponentsInChildren<Collider>(true))
            {
                c.isTrigger = false;
            }
        }

        private void CreatePromptUI()
        {
            var canvasGo = new GameObject("EscapePromptCanvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;

            promptUiGo = new GameObject("PromptText", typeof(RectTransform), typeof(Text));
            promptUiGo.transform.SetParent(canvasGo.transform, false);
            var rt = promptUiGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 150);
            rt.sizeDelta = new Vector2(1000, 100);

            promptText = promptUiGo.GetComponent<Text>();
            promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            promptText.fontSize = 42;
            promptText.fontStyle = FontStyle.Bold;
            promptText.alignment = TextAnchor.MiddleCenter;
            promptText.color = Color.white;
            
            var outline = promptUiGo.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, -2);

            promptUiGo.SetActive(false);
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
                promptUiGo.SetActive(true);
                if (distToKey < 3.5f)
                {
                    hasKey = true;
                    if (keyObj != null) Destroy(keyObj);
                    promptText.text = "ANCIENT PAPYRUS COLLECTED! RUN TO THE BOAT!";
                    TheAlchemistsCrypt.Gameplay.AudioManager.PlayVoiceLine("Voice/vo_taunt_01");
                    TheAlchemistsCrypt.Gameplay.AudioManager.PlaySFX("sfx/sfx_pickup", false, 0.8f);
                }
                else
                {
                    promptText.text = "ANCIENT PAPYRUS DETECTED! MOVE CLOSER TO AUTO-PICKUP";
                }
            }
            else if (nearBoat)
            {
                promptUiGo.SetActive(true);
                if (hasKey)
                { 
                    promptText.text = "YOU REACHED THE BOAT! TAP OR PRESS [E] TO BOARD AND ESCAPE!";
                    if (interactPressed)
                    {
                        WinGame();
                    }
                }
                else
                {
                    promptText.text = "THE ANCIENT BOAT. YOU NEED THE PAPYRUS TO DEPART.";
                }
            }
            else
            {
                if (promptText.text.Contains("COLLECTED")) { if (distToBoat > 40f && distToKey > 40f) promptUiGo.SetActive(false); }
                else promptUiGo.SetActive(false);
            }
        }

        private void WinGame()
        {
            hasEscaped = true;
            promptUiGo.SetActive(false);
            
            // Try to use the HUD's ShowVictoryScreen if it exists, otherwise do it here
            if (TheAlchemistsCrypt.UI.MobileHUDButtons.Instance != null)
            {
                TheAlchemistsCrypt.UI.MobileHUDButtons.Instance.ShowVictoryScreen();
            }
        }
    }

    public class Floater : MonoBehaviour
    {
        private float startY;
        private void Start() { startY = transform.position.y; }
        private void Update() {
            transform.position = new Vector3(transform.position.x, startY + Mathf.Sin(Time.time * 2f) * 0.5f, transform.position.z);
            transform.Rotate(0, 45f * Time.deltaTime, 0);
        }
    }
}