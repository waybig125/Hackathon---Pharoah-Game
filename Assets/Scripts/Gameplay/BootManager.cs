using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace TheAlchemistsCrypt.Gameplay
{
    public class BootManager : MonoBehaviour
    {
        [Header("Scene Settings")]
        [SerializeField] private string targetSceneName = "MainGame"; // Main game scene

        [Header("UI References")]
        private GameObject loadingUiGo;
        private TextMeshProUGUI loadingText;
        private Image progressBar;

        private void Start()
        {
            SetupBootUI();
            StartCoroutine(LoadMainGameAsync());
        }

        private void SetupBootUI()
        {
            var canvasGo = new GameObject("BootCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            
            // Add a simple camera to satisfy Unity's rendering pipeline and avoid "No cameras rendering" warnings
            var camGo = new GameObject("BootCamera", typeof(Camera));
            var cam = camGo.GetComponent<Camera>();
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.05f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.orthographic = true;
            
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Background
            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(canvasGo.transform, false);
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;
            var bgImg = bgGo.GetComponent<Image>();
            bgImg.color = Color.white;
            
            // Load the generated background sprite
            var bgTex = Resources.Load<Texture2D>("egyptian_items/BootBackground");
            if (bgTex != null)
            {
                bgImg.sprite = Sprite.Create(bgTex, new Rect(0, 0, bgTex.width, bgTex.height), new Vector2(0.5f, 0.5f));
            }
            else
            {
                bgImg.color = new Color(0.05f, 0.05f, 0.05f, 1f);
            }

            // Progress Bar Background
            var pbBgGo = new GameObject("ProgressBarBg", typeof(RectTransform), typeof(Image), typeof(Mask));
            pbBgGo.transform.SetParent(canvasGo.transform, false);
            var pbBgRect = pbBgGo.GetComponent<RectTransform>();
            pbBgRect.anchorMin = pbBgRect.anchorMax = new Vector2(0.5f, 0.5f);
            
            // Align slightly better
            pbBgRect.anchoredPosition = new Vector2(0f, -65f);
            pbBgRect.sizeDelta = new Vector2(730f, 44f);
            
            var pbBgImg = pbBgGo.GetComponent<Image>();
            pbBgImg.sprite = CreateRoundedRectSprite(730, 44, 22f); // Fully rounded capsule
            pbBgImg.color = new Color(0.02f, 0.08f, 0.04f, 1f); // Solid dark green for better masking

            var mask = pbBgGo.GetComponent<Mask>();
            mask.showMaskGraphic = true;

            // Progress Bar Fill
            var pbFillGo = new GameObject("ProgressBarFill", typeof(RectTransform), typeof(Image));
            pbFillGo.transform.SetParent(pbBgGo.transform, false);
            var pbFillRect = pbFillGo.GetComponent<RectTransform>();
            pbFillRect.anchorMin = new Vector2(0f, 0f);
            pbFillRect.anchorMax = new Vector2(0f, 1f); // Starts empty
            pbFillRect.offsetMin = pbFillRect.offsetMax = Vector2.zero;
            progressBar = pbFillGo.GetComponent<Image>();
            progressBar.sprite = CreateRoundedRectSprite(44, 44, 22f); // Sliced circle for perfect capsule stretch
            progressBar.type = Image.Type.Sliced;
            progressBar.color = new Color(0.0f, 0.9f, 0.3f, 0.85f); // Bright premium chemical green matching HUD

            // Lightning overlay
            var lightningGo = new GameObject("LightningOverlay", typeof(RectTransform), typeof(Image));
            lightningGo.transform.SetParent(canvasGo.transform, false);
            var lRect = lightningGo.GetComponent<RectTransform>();
            lRect.anchorMin = Vector2.zero; lRect.anchorMax = Vector2.one;
            lRect.offsetMin = lRect.offsetMax = Vector2.zero;
            var lImg = lightningGo.GetComponent<Image>();
            lImg.color = new Color(1f, 1f, 1f, 0f);
            lImg.raycastTarget = false;

            StartCoroutine(LightningFlashesRoutine(lImg));

            // Start bubble animation
            StartBubblesEffect(pbBgGo.transform, new Vector2(730f, 44f));
        }

        private Sprite CreateRoundedRectSprite(int width, int height, float cornerRadius)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] colors = new Color[width * height];
            
            float cx1 = cornerRadius;
            float cy1 = cornerRadius;
            float cx2 = width - cornerRadius;
            float cy2 = height - cornerRadius;
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool inside = true;
                    if (x < cx1 && y < cy1) // Bottom-left corner
                    {
                        float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx1, cy1));
                        if (d > cornerRadius) inside = false;
                    }
                    else if (x > cx2 && y < cy1) // Bottom-right corner
                    {
                        float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx2, cy1));
                        if (d > cornerRadius) inside = false;
                    }
                    else if (x < cx1 && y > cy2) // Top-left corner
                    {
                        float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx1, cy2));
                        if (d > cornerRadius) inside = false;
                    }
                    else if (x > cx2 && y > cy2) // Top-right corner
                    {
                        float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx2, cy2));
                        if (d > cornerRadius) inside = false;
                    }
                    
                    colors[y * width + x] = inside ? Color.white : Color.clear;
                }
            }
            
            tex.SetPixels(colors);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(cornerRadius, cornerRadius, cornerRadius, cornerRadius));
        }

        private Sprite CreateCircleSprite(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] colors = new Color[size * size];
            float radius = size / 2f;
            float cx = size / 2f;
            float cy = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    if (d < radius - 1f)
                    {
                        float ratio = d / radius;
                        if (ratio > 0.6f)
                            colors[y * size + x] = new Color(1f, 1f, 1f, 1f);
                        else
                            colors[y * size + x] = new Color(1f, 1f, 1f, 0.35f);
                    }
                    else if (d < radius)
                    {
                        colors[y * size + x] = new Color(1f, 1f, 1f, (radius - d));
                    }
                    else
                    {
                        colors[y * size + x] = Color.clear;
                    }
                }
            }
            tex.SetPixels(colors);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private void StartBubblesEffect(Transform parent, Vector2 barSize)
        {
            StartCoroutine(GenerateBubbles(parent, barSize));
        }

        private IEnumerator GenerateBubbles(Transform parent, Vector2 barSize)
        {
            var bubbleSprite = CreateCircleSprite(16);
            while (parent != null)
            {
                float progress = (progressBar != null) ? progressBar.rectTransform.anchorMax.x : 0f;
                if (progress > 0.02f)
                {
                    var bubbleGo = new GameObject("Bubble", typeof(RectTransform), typeof(Image));
                    bubbleGo.transform.SetParent(parent, false);
                    var rect = bubbleGo.GetComponent<RectTransform>();
                    
                    float fillWidth = progress * barSize.x;
                    // Position X relative to the filled portion of the bar
                    float randomX = Random.Range(-barSize.x / 2f, -barSize.x / 2f + fillWidth);
                    rect.anchoredPosition = new Vector2(randomX, -barSize.y / 2f);
                    
                    float bubbleSize = Random.Range(10f, 22f);
                    rect.sizeDelta = new Vector2(bubbleSize, bubbleSize);
                    
                    var img = bubbleGo.GetComponent<Image>();
                    img.sprite = bubbleSprite;
                    img.color = new Color(0.7f, 1f, 0.8f, Random.Range(0.6f, 0.9f));
                    
                    StartCoroutine(AnimateBubble(rect, img, barSize.y));
                }
                
                yield return new WaitForSeconds(Random.Range(0.04f, 0.12f));
            }
        }

        private IEnumerator AnimateBubble(RectTransform bubbleRect, Image bubbleImg, float barHeight)
        {
            float duration = Random.Range(0.8f, 1.4f);
            float elapsed = 0f;
            Vector2 startPos = bubbleRect.anchoredPosition;
            float endY = barHeight / 2f + 5f;
            float driftWidth = Random.Range(-12f, 12f);
            float driftSpeed = Random.Range(1.5f, 4f);
            
            while (elapsed < duration && bubbleRect != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                float currentY = Mathf.Lerp(startPos.y, endY, t);
                float currentX = startPos.x + Mathf.Sin(t * Mathf.PI * driftSpeed) * driftWidth * t;
                
                bubbleRect.anchoredPosition = new Vector2(currentX, currentY);
                
                if (bubbleImg != null)
                {
                    Color c = bubbleImg.color;
                    c.a = Mathf.Lerp(c.a, 0f, t);
                    bubbleImg.color = c;
                }
                
                yield return null;
            }
            
            if (bubbleRect != null)
            {
                Destroy(bubbleRect.gameObject);
            }
        }

        private IEnumerator LoadMainGameAsync()
        {
            yield return new WaitForSeconds(0.5f); // Brief delay for visuals to appear

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
            
            // Do not allow immediate activation so we can animate the bar
            asyncLoad.allowSceneActivation = false;

            float displayProgress = 0f;

            while (!asyncLoad.isDone)
            {
                // asyncLoad.progress stops at 0.9 if allowSceneActivation is false
                float targetProgress = asyncLoad.progress / 0.9f;
                displayProgress = Mathf.MoveTowards(displayProgress, targetProgress, Time.deltaTime * 0.4f); // ~2.5s to fill
                
                if (progressBar != null)
                {
                    var rect = progressBar.GetComponent<RectTransform>();
                    rect.anchorMax = new Vector2(displayProgress, 1f);
                }

                if (displayProgress >= 0.99f && asyncLoad.progress >= 0.9f)
                {
                    asyncLoad.allowSceneActivation = true;
                }
                
                yield return null;
            }
        }

        private IEnumerator LightningFlashesRoutine(Image img)
        {
            while (img != null)
            {
                yield return new WaitForSeconds(Random.Range(2.5f, 6.5f));
                if (img == null) break;

                float flashIntensity = Random.Range(0.4f, 0.75f);
                img.color = new Color(1f, 0.95f, 0.85f, flashIntensity);

                float elapsed = 0f;
                float duration = Random.Range(0.08f, 0.15f);
                while (elapsed < duration && img != null)
                {
                    elapsed += Time.deltaTime;
                    img.color = new Color(1f, 0.95f, 0.85f, Mathf.Lerp(flashIntensity, 0f, elapsed / duration));
                    yield return null;
                }

                if (img != null && Random.value < 0.6f)
                {
                    yield return new WaitForSeconds(Random.Range(0.05f, 0.12f));
                    if (img == null) break;

                    flashIntensity = Random.Range(0.2f, 0.45f);
                    img.color = new Color(1f, 0.95f, 0.85f, flashIntensity);

                    elapsed = 0f;
                    duration = Random.Range(0.12f, 0.25f);
                    while (elapsed < duration && img != null)
                    {
                        elapsed += Time.deltaTime;
                        img.color = new Color(1f, 0.95f, 0.85f, Mathf.Lerp(flashIntensity, 0f, elapsed / duration));
                        yield return null;
                    }
                }

                if (img != null) img.color = new Color(1f, 1f, 1f, 0f);
            }
        }
    }
}
