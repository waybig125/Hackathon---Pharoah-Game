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
        [SerializeField] private string targetSceneName = "SampleScene"; // Main game scene

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
            bgGo.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.05f, 1f);

            // Title
            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(canvasGo.transform, false);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0, 100);
            titleRect.sizeDelta = new Vector2(800, 100);
            var titleTxt = titleGo.GetComponent<TextMeshProUGUI>();
            titleTxt.text = "THE PHARAOH'S VAULT";
            titleTxt.fontSize = 64;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = new Color(0.95f, 0.8f, 0.2f, 1f);

            // Progress Bar Background
            var pbBgGo = new GameObject("ProgressBarBg", typeof(RectTransform), typeof(Image));
            pbBgGo.transform.SetParent(canvasGo.transform, false);
            var pbBgRect = pbBgGo.GetComponent<RectTransform>();
            pbBgRect.anchorMin = pbBgRect.anchorMax = new Vector2(0.5f, 0.5f);
            pbBgRect.anchoredPosition = new Vector2(0, -100);
            pbBgRect.sizeDelta = new Vector2(600, 20);
            pbBgGo.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // Progress Bar Fill
            var pbFillGo = new GameObject("ProgressBarFill", typeof(RectTransform), typeof(Image));
            pbFillGo.transform.SetParent(pbBgGo.transform, false);
            var pbFillRect = pbFillGo.GetComponent<RectTransform>();
            pbFillRect.anchorMin = new Vector2(0f, 0f);
            pbFillRect.anchorMax = new Vector2(0f, 1f); // Starts empty
            pbFillRect.offsetMin = pbFillRect.offsetMax = Vector2.zero;
            progressBar = pbFillGo.GetComponent<Image>();
            progressBar.color = new Color(0.95f, 0.8f, 0.2f, 1f);

            // Loading Text
            loadingUiGo = new GameObject("LoadingText", typeof(RectTransform), typeof(TextMeshProUGUI));
            loadingUiGo.transform.SetParent(canvasGo.transform, false);
            var loadingRect = loadingUiGo.GetComponent<RectTransform>();
            loadingRect.anchorMin = loadingRect.anchorMax = new Vector2(0.5f, 0.5f);
            loadingRect.anchoredPosition = new Vector2(0, -140);
            loadingRect.sizeDelta = new Vector2(400, 50);
            loadingText = loadingUiGo.GetComponent<TextMeshProUGUI>();
            loadingText.text = "Loading Vault Environment... 0%";
            loadingText.fontSize = 24;
            loadingText.alignment = TextAlignmentOptions.Center;
            loadingText.color = Color.white;
        }

        private IEnumerator LoadMainGameAsync()
        {
            yield return new WaitForSeconds(0.5f); // Brief delay for visuals to appear

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
            
            // Allow scene to activate immediately upon load
            asyncLoad.allowSceneActivation = true;

            while (!asyncLoad.isDone)
            {
                // asyncLoad.progress stops at 0.9 if allowSceneActivation is false, but we set it true
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                
                if (progressBar != null)
                {
                    var rect = progressBar.GetComponent<RectTransform>();
                    rect.anchorMax = new Vector2(progress, 1f);
                }
                
                if (loadingText != null)
                {
                    loadingText.text = $"Loading Vault Environment... {Mathf.RoundToInt(progress * 100)}%";
                }
                
                yield return null;
            }
        }
    }
}
