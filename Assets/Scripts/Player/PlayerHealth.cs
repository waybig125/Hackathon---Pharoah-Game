using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;

namespace TheAlchemistsCrypt.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        public float maxHealth = 100f;
        public float currentHealth;

        [Header("Effects")]
        private float damageAlpha = 0f;
        private UnityEngine.UI.Image damageOverlay;
        private bool isDead = false;

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        private void Start()
        {
            FindDamageOverlay();
        }

        private void FindDamageOverlay()
        {
            var overlay = GameObject.Find("MobileHUD_Root/DamageOverlay");
            if (overlay != null) damageOverlay = overlay.GetComponent<UnityEngine.UI.Image>();
        }

        public void TakeDamage(float amount)
        {
            if (isDead) return;

            currentHealth -= amount;
            if (currentHealth < 0) currentHealth = 0;
            
            // Trigger red flash on screen
            damageAlpha = 0.6f; 
            if (damageOverlay == null) FindDamageOverlay();

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void Update()
        {
            if (isDead) return;

            // Handle red flash fade out
            if (damageAlpha > 0)
            {
                damageAlpha -= Time.deltaTime * 1.5f;
                if (damageAlpha < 0) damageAlpha = 0;
                if (damageOverlay != null) 
                    damageOverlay.color = new Color(0.8f, 0.1f, 0.1f, damageAlpha);
            }
        }

        private void Die()
        {
            isDead = true;
            StartCoroutine(DeathSequence());
        }

        private IEnumerator DeathSequence()
        {
            Debug.Log("Player Died! Starting death sequence...");

            // 1. Disable character input & movement
            var character = GetComponent<InfimaGames.LowPolyShooterPack.Character>();
            if (character != null)
            {
                character.enabled = false;
            }

            // Disable MobileHUD_Root so it doesn't overlap
            var hud = GameObject.Find("MobileHUD_Root");
            if (hud != null) hud.SetActive(false);

            // 2. Cinematic Die Camera Motion (Tilt and Fall)
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Vector3 initialCamPos = mainCam.transform.localPosition;
                Quaternion initialCamRot = mainCam.transform.localRotation;
                float duration = 1.5f;
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    t = Mathf.SmoothStep(0f, 1f, t);
                    
                    // Fall down to 0.3f local height
                    mainCam.transform.localPosition = Vector3.Lerp(initialCamPos, new Vector3(initialCamPos.x, 0.3f, initialCamPos.z), t);
                    
                    // Tilt camera sideways by 75 degrees
                    mainCam.transform.localRotation = Quaternion.Slerp(initialCamRot, Quaternion.Euler(15, 0, 75), t);
                    
                    yield return null;
                }
            }

            // 3. Create a Gorgeous Spooky Death Screen Canvas
            var deathCanvasGo = new GameObject("DeathCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = deathCanvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            var scaler = deathCanvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Spooky dark vignette panel background
            var panelGo = new GameObject("VignettePanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            panelGo.SetParent(deathCanvasGo.transform, false);
            panelGo.anchorMin = Vector2.zero; panelGo.anchorMax = Vector2.one;
            panelGo.offsetMin = panelGo.offsetMax = Vector2.zero;
            
            var panelImg = panelGo.GetComponent<Image>();
            
            // Create a custom procedural dark vignette texture
            int texSize = 128;
            var deathTex = new Texture2D(texSize, texSize);
            for (int y = 0; y < texSize; y++)
            {
                for (int x = 0; x < texSize; x++)
                {
                    float u = (x - texSize * 0.5f) / (texSize * 0.5f);
                    float v = (y - texSize * 0.5f) / (texSize * 0.5f);
                    float dist = Mathf.Sqrt(u * u + v * v);
                    float t = Mathf.Clamp01(dist / 1.3f);
                    Color col = Color.Lerp(new Color(0.05f, 0.01f, 0.01f, 0.8f), new Color(0f, 0f, 0f, 0.98f), t);
                    deathTex.SetPixel(x, y, col);
                }
            }
            deathTex.Apply();
            panelImg.sprite = Sprite.Create(deathTex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f));
            panelImg.color = Color.white;

            // Spooky red mist title
            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            titleGo.SetParent(panelGo, false);
            titleGo.anchorMin = titleGo.anchorMax = new Vector2(0.5f, 0.5f);
            titleGo.anchoredPosition = new Vector2(0, 130);
            titleGo.sizeDelta = new Vector2(800, 120);

            var titleText = titleGo.GetComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 80;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(0.95f, 0.8f, 0.2f, 1f); // Spooky gold
            titleText.text = "YOU DIED";

            // Subtitle
            var subGo = new GameObject("Subtitle", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            subGo.SetParent(panelGo, false);
            subGo.anchorMin = subGo.anchorMax = new Vector2(0.5f, 0.5f);
            subGo.anchoredPosition = new Vector2(0, 50);
            subGo.sizeDelta = new Vector2(800, 50);

            var subText = subGo.GetComponent<Text>();
            subText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subText.fontSize = 24;
            subText.alignment = TextAnchor.MiddleCenter;
            subText.color = new Color(0.85f, 0.2f, 0.2f, 0.85f); // Crimson
            subText.text = "The Alchemical Crypt claims your essence...";

            // Resurrect Button
            var btnGo = new GameObject("RestartButton", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            btnGo.SetParent(panelGo, false);
            btnGo.anchorMin = btnGo.anchorMax = new Vector2(0.5f, 0.5f);
            btnGo.anchoredPosition = new Vector2(0, -90);
            btnGo.sizeDelta = new Vector2(340, 80);

            var btnImg = btnGo.GetComponent<Image>();
            
            // Procedural gold button texture
            var btnTex = new Texture2D(340, 80);
            for (int y = 0; y < 80; y++)
            {
                for (int x = 0; x < 340; x++)
                {
                    float borderX = Mathf.Min(x, 340 - x);
                    float borderY = Mathf.Min(y, 80 - y);
                    float borderDist = Mathf.Min(borderX, borderY);
                    
                    if (borderDist < 4)
                        btnTex.SetPixel(x, y, new Color(0.95f, 0.8f, 0.2f, 0.95f)); // Gold border
                    else
                        btnTex.SetPixel(x, y, new Color(0.06f, 0.02f, 0.02f, 0.85f)); // Deep dark red obsidian
                }
            }
            btnTex.Apply();
            btnImg.sprite = Sprite.Create(btnTex, new Rect(0, 0, 340, 80), new Vector2(0.5f, 0.5f));
            btnImg.color = Color.white;

            var btnTextGo = new GameObject("ButtonText", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            btnTextGo.SetParent(btnGo, false);
            btnTextGo.anchorMin = Vector2.zero; btnTextGo.anchorMax = Vector2.one;
            btnTextGo.offsetMin = btnTextGo.offsetMax = Vector2.zero;
            var btnText = btnTextGo.GetComponent<Text>();
            btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnText.fontSize = 24;
            btnText.fontStyle = FontStyle.Bold;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = new Color(0.95f, 0.8f, 0.2f, 0.95f); // Spooky gold
            btnText.text = "RESTART VOYAGE";

            // Click listener
            var buttonHelper = btnGo.gameObject.AddComponent<DeathButtonHelper>();
            buttonHelper.onDown = () =>
            {
                btnGo.localScale = new Vector3(0.95f, 0.95f, 1f);
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            };
            buttonHelper.onUp = () =>
            {
                btnGo.localScale = new Vector3(1f, 1f, 1f);
            };

            // Enable cursor so player can click restart on desktop/mobile
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public class DeathButtonHelper : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public System.Action onDown;
        public System.Action onUp;

        public void OnPointerDown(PointerEventData eventData)
        {
            onDown?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            onUp?.Invoke();
        }
    }
}
