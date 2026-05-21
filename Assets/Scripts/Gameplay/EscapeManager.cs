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
                new Vector3(0f, 0f, 25f), 
                new Vector3(30f, 0f, 0f), 
                new Vector3(-30f, 0f, 0f)    
            };

            Vector3 chosenLoc = spawnLocations[Random.Range(0, spawnLocations.Length)];
            
            var terrain = Terrain.activeTerrain;
            if (terrain != null) chosenLoc.y = terrain.SampleHeight(chosenLoc) + 0.8f; // Lifted from 0.3f to 0.8f

            // Load custom model
            GameObject prefab = Resources.Load<GameObject>("papyrus");
            if (prefab != null)
            {
                keyObj = Instantiate(prefab, chosenLoc, Quaternion.identity);
                keyObj.transform.localScale = Vector3.one * 2.5f; // Slightly larger for visibility
            }
            else
            {
                keyObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder); 
                keyObj.transform.position = chosenLoc;
                keyObj.transform.localScale = new Vector3(0.5f, 0.8f, 0.5f);
                keyObj.transform.rotation = Quaternion.Euler(0, 0, 90f); 
            }
            keyObj.name = "AncientPapyrus";

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
            Vector3 spawnPos = new Vector3(0f, 0.5f, -340f); // Moved from -540f to -340f to match beach at -320f
            
            // Load custom boat model
            GameObject prefab = Resources.Load<GameObject>("boat");
            if (prefab != null)
            {
                boatObj = Instantiate(prefab, spawnPos, Quaternion.Euler(0, 180f, 0));
                boatObj.transform.localScale = Vector3.one * 5f;
            }
            else
            {
                boatObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                boatObj.transform.position = spawnPos;
                boatObj.transform.localScale = new Vector3(4f, 2f, 10f);
            }
            boatObj.name = "EscapeBoat";

            // Register with Minimap
            if (TheAlchemistsCrypt.UI.MinimapUI.Instance != null)
            {
                TheAlchemistsCrypt.UI.MinimapUI.Instance.RegisterDynamicStaticIcon(boatObj, new Vector2(24, 24), TheAlchemistsCrypt.UI.MinimapUI.Instance.boatSprite);
            }
            
            var col = boatObj.GetComponent<Collider>();
            if (col == null) col = boatObj.AddComponent<MeshCollider>();
            if (col is MeshCollider mc) mc.convex = true;
            col.isTrigger = true;
        }

        private void CreatePromptUI()
        {
            var canvasGo = new GameObject("EscapePromptCanvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            promptUiGo = new GameObject("PromptText", typeof(RectTransform), typeof(Text));
            promptUiGo.transform.SetParent(canvasGo.transform, false);
            var rt = promptUiGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, -150);
            rt.sizeDelta = new Vector2(800, 100);

            promptText = promptUiGo.GetComponent<Text>();
            promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            promptText.fontSize = 42;
            promptText.alignment = TextAnchor.MiddleCenter;
            promptText.color = Color.white;
            
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

            nearKey = !hasKey && distToKey < 5f;
            nearBoat = distToBoat < 8f;

            bool interactPressed = false;
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame) interactPressed = true;
            if (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame) interactPressed = true;

            if (nearKey)
            {
                promptUiGo.SetActive(true);
                promptText.text = "Tap or Press [E] to collect the Ancient Papyrus";
                
                if (interactPressed)
                {
                    hasKey = true;
                    if (keyObj != null) Destroy(keyObj);
                    promptUiGo.SetActive(true);
                    promptText.text = "ANCIENT PAPYRUS COLLECTED! FIND THE BOAT!";
                    TheAlchemistsCrypt.Gameplay.AudioManager.PlayVoiceLine("Voice/vo_taunt_01");
                }
            }
            else if (nearBoat)
            {
                promptUiGo.SetActive(true);
                if (hasKey)
                {
                    promptText.text = "Tap or Press [E] to Escape on the Boat!";
                    if (interactPressed)
                    {
                        WinGame();
                    }
                }
                else
                {
                    promptText.text = "You need the Ancient Papyrus to escape on the boat.";
                }
            }
            else
            {
                // Only hide if it's not the collected message
                if (!promptText.text.Contains("COLLECTED")) promptUiGo.SetActive(false);
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