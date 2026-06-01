using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using TMPro;

namespace TheAlchemistsCrypt.UI
{
    public partial class MobileHUDButtons
    {
         public void ShowDeathScreen()
                {
                    if (deathPanelInstance != null) return;
                    if (hudRootGo != null) hudRootGo.SetActive(false);

                    Time.timeScale = 0f;
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;

                    if (TheAlchemistsCrypt.Input.MobileInputManager.Instance)
                        TheAlchemistsCrypt.Input.MobileInputManager.Instance.enabled = false;

                    var deathCanvasGo = new GameObject("DeathCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                    var deathCanvas = deathCanvasGo.GetComponent<Canvas>();
                    deathCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    deathCanvas.sortingOrder = 1100;

                    var deathScaler = deathCanvasGo.GetComponent<CanvasScaler>();
                    deathScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    deathScaler.referenceResolution = new Vector2(1920, 1080);
                    deathScaler.matchWidthOrHeight = 1f;

                    deathPanelInstance = deathCanvasGo;

                    var deathPanelGo = new GameObject("DeathPanelOverlay", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    deathPanelGo.SetParent(deathCanvasGo.transform, false);
                    deathPanelGo.anchorMin = Vector2.zero; deathPanelGo.anchorMax = Vector2.one;
                    deathPanelGo.offsetMin = deathPanelGo.offsetMax = Vector2.zero;
                    
                    // Create a gorgeous radial gradient overlay (transparent center fading to dark crimson/black edges)
                    var bgImg = deathPanelGo.GetComponent<Image>();
                    bgImg.sprite = CreateProceduralGradientSprite(1920, 1080, new Color(0.28f, 0.04f, 0.04f, 0.0f), new Color(0.04f, 0.0f, 0.0f, 0.98f));
                    bgImg.color = new Color(1f, 1f, 1f, 0f); // Set alpha to 0 initially for fade-in

                    // Dangerous blood-red vignette overlay that pulses dynamically
                    var vignetteGo = new GameObject("BloodVignette", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    vignetteGo.SetParent(deathPanelGo, false);
                    vignetteGo.anchorMin = Vector2.zero; vignetteGo.anchorMax = Vector2.one;
                    vignetteGo.offsetMin = vignetteGo.offsetMax = Vector2.zero;
                    var vigImg = vignetteGo.GetComponent<Image>();
                    vigImg.sprite = CreateProceduralGradientSprite(1920, 1080, new Color(0.8f, 0.0f, 0.0f, 0.0f), new Color(0.4f, 0.0f, 0.0f, 0.95f));
                    vigImg.color = new Color(1f, 1f, 1f, 0f);

                    // Container for all content that will fade in smoothly (without background/modal card)
                    var contentContainerGo = new GameObject("DeathContent", typeof(RectTransform)).GetComponent<RectTransform>();
                    contentContainerGo.SetParent(deathPanelGo, false);
                    contentContainerGo.anchorMin = contentContainerGo.anchorMax = new Vector2(0.5f, 0.5f);
                    contentContainerGo.anchoredPosition = Vector2.zero;
                    contentContainerGo.sizeDelta = new Vector2(850, 640);
                    
                    var cardGroup = contentContainerGo.gameObject.AddComponent<CanvasGroup>();
                    cardGroup.alpha = 0f;

                    var titleGo = new GameObject("TitleText", typeof(RectTransform)).GetComponent<RectTransform>();
                    titleGo.SetParent(contentContainerGo, false);
                    titleGo.anchoredPosition = new Vector2(0, 100); titleGo.sizeDelta = new Vector2(900, 150);
                    var titleText = titleGo.gameObject.AddComponent<TextMeshProUGUI>();
                    titleText.font = GetTitleFont();
                    titleText.fontSize = 130; // Massive Impact Title
                    titleText.fontStyle = FontStyles.Bold;
                    titleText.alignment = TextAlignmentOptions.Center;
                    titleText.color = new Color(0.95f, 0.1f, 0.1f, 0.98f); // Warning Red
                    titleText.text = "YOU DIED";
                    titleText.outlineColor = new Color(0.2f, 0.0f, 0.0f, 0.8f);
                    titleText.outlineWidth = 0.25f;

                    // Action Buttons with micro-squeezing feedback in ButtonInputHelper
                    var btnRestart = CreateSettingsActionButton(contentContainerGo, "RESTART VOYAGE", new Vector2(-180, -80), new Vector2(320, 80), () => {
                        Time.timeScale = 1f;
                        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
                    }, new Color(0.8f, 0.1f, 0.1f, 0.20f));

                    var btnMenu = CreateSettingsActionButton(contentContainerGo, "MAIN MENU", new Vector2(180, -80), new Vector2(320, 80), () => {
                        Time.timeScale = 1f;
                        HasStartedGame = false;
                        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                    }, new Color(0.8f, 0.1f, 0.1f, 0.20f));

                    // Style buttons to be red with white text for premium legibility
                    var restartImg = btnRestart.GetComponent<Image>();
                    if (restartImg != null) restartImg.color = new Color(0.75f, 0.08f, 0.08f, 1.0f);
                    var restartTxt = btnRestart.GetComponentInChildren<TextMeshProUGUI>();
                    if (restartTxt != null) {
                        restartTxt.color = Color.white;
                        restartTxt.fontSize = 20;
                    }

                    var menuImg = btnMenu.GetComponent<Image>();
                    if (menuImg != null) menuImg.color = new Color(0.75f, 0.08f, 0.08f, 1.0f);
                    var menuTxt = btnMenu.GetComponentInChildren<TextMeshProUGUI>();
                    if (menuTxt != null) {
                        menuTxt.color = Color.white;
                        menuTxt.fontSize = 20;
                    }

                    // Play a scary voice line on death to make it feel dangerous!
                    string[] deathTaunts = { "Voice/vo_taunt_01", "Voice/vo_taunt_02", "Voice/vo_taunt_03", "Voice/vo_taunt_04", "Voice/vo_taunt_05", "Voice/vo_taunt_06", "Voice/vo_taunt_07", "Voice/vo_taunt_08" };
                    string randomDeathTaunt = deathTaunts[UnityEngine.Random.Range(0, deathTaunts.Length)];
                    TheAlchemistsCrypt.Gameplay.AudioManager.PlayVoiceLine(randomDeathTaunt, true, true);

                    SetLayerRecursively(deathCanvasGo, 5);

                    // Start the smooth unscaled fade-in animation
                    StartCoroutine(FadeInDeathScreen(bgImg, cardGroup, vigImg));
                }

         public void ShowOrbTooltip(string message)
                {
                    if (orbTooltipPanel == null)
                    {
                        var canvas = GetComponent<Canvas>();
                        if (canvas == null) return;
                        var root = canvas.GetComponent<RectTransform>();

                        // Sleek, completely transparent container for tooltips
                        var panelGo = new GameObject("OrbTooltipPanel", typeof(RectTransform)).GetComponent<RectTransform>();
                        panelGo.SetParent(root, false);
                        panelGo.anchorMin = panelGo.anchorMax = new Vector2(0.5f, 0f);
                        panelGo.pivot = new Vector2(0.5f, 0f);
                        panelGo.anchoredPosition = new Vector2(0, 140); // Shifted a little lower
                        panelGo.sizeDelta = new Vector2(600, 60);
                        orbTooltipPanel = panelGo.gameObject;

                        // Text
                        var txtGo = new GameObject("Text", typeof(RectTransform)).GetComponent<RectTransform>();
                        txtGo.SetParent(panelGo, false);
                        txtGo.anchorMin = Vector2.zero; txtGo.anchorMax = Vector2.one;
                        txtGo.offsetMin = new Vector2(15, 5); txtGo.offsetMax = new Vector2(-15, -5);
                        orbTooltipText = txtGo.gameObject.AddComponent<TextMeshProUGUI>();
                        orbTooltipText.font = GetRobustFont();
                        orbTooltipText.fontSize = 18;
                        orbTooltipText.fontStyle = FontStyles.Bold;
                        orbTooltipText.textWrappingMode = TextWrappingModes.Normal;
                        orbTooltipText.overflowMode = TextOverflowModes.Truncate;
                    }

                    orbTooltipPanel.SetActive(true);
                    orbTooltipText.text = message;

                    if (orbTooltipFadeRoutine != null) StopCoroutine(orbTooltipFadeRoutine);
                    orbTooltipFadeRoutine = StartCoroutine(OrbTooltipFadeOutSequence());
                }

         public void HideOrbTooltip()
                {
                    if (orbTooltipPanel != null) orbTooltipPanel.SetActive(false);
                    if (orbTooltipFadeRoutine != null) StopCoroutine(orbTooltipFadeRoutine);
                }

    }
}
