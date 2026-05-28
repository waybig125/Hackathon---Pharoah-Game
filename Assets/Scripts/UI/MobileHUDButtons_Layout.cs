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


                private void SetupCanvas()
                {
                    var canvas = GetComponent<Canvas>();
                    if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 999;
                    
                    var scaler = GetComponent<CanvasScaler>();
                    if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    scaler.matchWidthOrHeight = 1.0f; // Force match height for consistent mobile look

                    if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

                    var eventSystem = UnityEngine.EventSystems.EventSystem.current;
                    if (eventSystem == null) eventSystem = GameObject.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
                    GameObject eventSystemGo;
                    if (eventSystem == null)
                    {
                        eventSystemGo = new GameObject("EventSystem");
                        eventSystem = eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    }
                    else
                    {
                        eventSystemGo = eventSystem.gameObject;
                    }
                    
                    var legacyModule = eventSystemGo.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                    if (legacyModule != null)
                    {
                        if (Application.isPlaying) Destroy(legacyModule);
                        else DestroyImmediate(legacyModule);
                    }
                    
                    var modernModule = eventSystemGo.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                    if (modernModule == null)
                    {
                        modernModule = eventSystemGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                        modernModule.AssignDefaultActions();
                    }
                }



                public void BuildHUD()
                {
                    foreach (Transform t in transform) Destroy(t.gameObject);

                    var root = new GameObject("HUD_Root", typeof(RectTransform)).GetComponent<RectTransform>();
                    root.SetParent(transform, false);
                    root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
                    root.offsetMin = root.offsetMax = Vector2.zero;
                    hudRootGo = root.gameObject;

                    var lookZone = new GameObject("LookZone", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    lookZone.SetParent(root, false);
                    lookZone.anchorMin = new Vector2(0.4f, 0f); lookZone.anchorMax = Vector2.one;
                    lookZone.offsetMin = lookZone.offsetMax = Vector2.zero;
                    lookZone.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);
                    lookZone.gameObject.AddComponent<LookSwipeZone>();

                    var moveZone = new GameObject("MoveZone", typeof(RectTransform)).GetComponent<RectTransform>();
                    moveZone.SetParent(root, false);
                    moveZone.anchorMin = Vector2.zero; moveZone.anchorMax = new Vector2(0.4f, 1f);
                    moveZone.offsetMin = moveZone.offsetMax = Vector2.zero;

                    // --- MASSIVE JOYSTICK (2.5x original scale) ---
                    var joystickBg = new GameObject("NativeJoystick_Bg", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    joystickBg.SetParent(moveZone, false);
                    joystickBg.anchorMin = joystickBg.anchorMax = new Vector2(0.4f, 0.4f); 
                    joystickBg.anchoredPosition = Vector2.zero;
                    joystickBg.sizeDelta = new Vector2(550, 550); 

                    var bgImage = joystickBg.GetComponent<Image>();
                    bgImage.color = Color.white;
                    if (joystickRingSprite != null) bgImage.sprite = joystickRingSprite;

                    var joystickHandle = new GameObject("HandleTarget", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    joystickHandle.SetParent(joystickBg, false);
                    joystickHandle.anchoredPosition = Vector2.zero;
                    joystickHandle.sizeDelta = new Vector2(550, 550); 

                    var targetImage = joystickHandle.GetComponent<Image>();
                    targetImage.color = new Color(0, 0, 0, 0); targetImage.raycastTarget = true;

                    var knobVisual = new GameObject("KnobVisual", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    knobVisual.SetParent(joystickHandle, false);
                    knobVisual.anchoredPosition = Vector2.zero;
                    knobVisual.sizeDelta = new Vector2(200, 200); 

                    // Add glow behind the knobVisual
                    var knobGlow = new GameObject("KnobGlow", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    knobGlow.SetParent(knobVisual, false);
                    knobGlow.anchoredPosition = Vector2.zero;
                    knobGlow.sizeDelta = new Vector2(260, 260); // 1.3x scaling
                    knobGlow.transform.SetAsFirstSibling();
                    var glowImage = knobGlow.GetComponent<Image>();
                    glowImage.color = new Color(1f, 0.6f, 0.1f, 0.5f); // translucent warm orange/gold glow
                    glowImage.raycastTarget = false;
                    if (joystickKnobSprite != null) glowImage.sprite = joystickKnobSprite;

                    var visualImage = knobVisual.GetComponent<Image>();
                    visualImage.color = Color.white; visualImage.raycastTarget = false;
                    if (joystickKnobSprite != null) visualImage.sprite = joystickKnobSprite;

                    var dragHandler = joystickHandle.gameObject.AddComponent<JoystickDragHandler>();
                    dragHandler.backgroundRing = joystickBg;
                    dragHandler.knobVisual = knobVisual;
                    dragHandler.movementRange = 180f;

                    // --- ACTION BUTTONS (Circular translucent gold themed, identical to fd582c0) ---
                    string currentPreset = PlayerPrefs.GetString("HUD_Preset", "DEFAULT");
                    bool isLefty = (currentPreset == "LEFTY");

                    var btnContainer = new GameObject("ButtonContainer", typeof(RectTransform)).GetComponent<RectTransform>();
                    btnContainer.SetParent(root, false);
                    if (isLefty)
                    {
                        btnContainer.anchorMin = btnContainer.anchorMax = new Vector2(0, 0);
                        btnContainer.anchoredPosition = new Vector2(50, 50);
                    }
                    else
                    {
                        btnContainer.anchorMin = btnContainer.anchorMax = new Vector2(1, 0);
                        btnContainer.anchoredPosition = new Vector2(-50, 50);
                    }

                    Vector2 firePos = GetButtonPosition("FIRE", isLefty ? new Vector2(220, 220) : new Vector2(-220, 220));
                    Vector2 reloadPos = GetButtonPosition("RELOAD", isLefty ? new Vector2(520, 150) : new Vector2(-520, 150));
                    Vector2 swapPos = GetButtonPosition("SWAP", isLefty ? new Vector2(360, 620) : new Vector2(-360, 620));
                    Vector2 sprintPos = GetButtonPosition("SPRINT", isLefty ? new Vector2(650, 300) : new Vector2(-650, 300));
                    Vector2 focusPos = GetButtonPosition("FOCUS", isLefty ? new Vector2(450, 420) : new Vector2(-450, 420));
                    Vector2 jumpPos = GetButtonPosition("JUMP", isLefty ? new Vector2(150, 520) : new Vector2(-150, 520));

                    CreateButton(btnContainer, "FIRE", firePos, 380, fireIcon, () => SetFire(true), () => SetFire(false));
                    CreateButton(btnContainer, "RELOAD", reloadPos, 200, reloadIcon, () => Reload());
                    CreateButton(btnContainer, "SWAP", swapPos, 200, swapIcon, () => Swap());
                    CreateSprintButton(btnContainer, sprintPos, 200);
                    CreateButton(btnContainer, "FOCUS", focusPos, 200, focusIcon, () => SetAiming(true), () => SetAiming(false));
                    CreateButton(btnContainer, "JUMP", jumpPos, 220, jumpIcon, () => SetJump(true), () => SetJump(false));

                    HideDebugLabels();

                    // --- REFINED HEALTH PANEL ---
                    var healthPanel = new GameObject("CustomHealthPanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    healthPanel.SetParent(root, false);
                    healthPanel.anchorMin = healthPanel.anchorMax = new Vector2(0, 1);
                    healthPanel.pivot = new Vector2(0f, 1f);
                    healthPanel.anchoredPosition = new Vector2(50, -50);
                    healthPanel.sizeDelta = new Vector2(550, 85);
                    var hpPanelImg = healthPanel.GetComponent<Image>();
                    hpPanelImg.sprite = null;
                    hpPanelImg.color = Color.clear; // Fully borderless/transparent background
                    
                    var hpIconGo = new GameObject("HealthIcon", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    hpIconGo.SetParent(healthPanel, false);
                    hpIconGo.anchorMin = hpIconGo.anchorMax = new Vector2(0f, 0.5f);
                    hpIconGo.pivot = new Vector2(0f, 0.5f);
                    hpIconGo.anchoredPosition = new Vector2(15, 0);
                    hpIconGo.sizeDelta = new Vector2(70, 70);
                    var hpIconImg = hpIconGo.GetComponent<Image>();
                    hpIconImg.sprite = healthIconSprite;
                    hpIconImg.preserveAspect = true;

                    var healthTxtGo = new GameObject("HealthText", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
                    healthTxtGo.SetParent(healthPanel, false);
                    healthTxtGo.sizeDelta = Vector2.zero;
                    healthText = healthTxtGo.GetComponent<TextMeshProUGUI>();
                    healthText.text = "";

                    var hpBgBar = new GameObject("HpBarBg", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    hpBgBar.SetParent(healthPanel, false);
                    hpBgBar.anchorMin = hpBgBar.anchorMax = new Vector2(0f, 0.5f);
                    hpBgBar.pivot = new Vector2(0f, 0.5f);
                    hpBgBar.anchoredPosition = new Vector2(100, 0);
                    hpBgBar.sizeDelta = new Vector2(295, 30);
                    hpBgBar.GetComponent<Image>().sprite = CreateFramedBarSprite(295, 30, new Color(0.95f, 0.8f, 0.2f, 0.9f), new Color(0.04f, 0.04f, 0.04f, 0.8f), 2);

                    var hpFillGo = new GameObject("HpFill", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    hpFillGo.SetParent(hpBgBar, false);
                    hpFillGo.anchorMin = Vector2.zero; hpFillGo.anchorMax = Vector2.one;
                    // Pad the fill by 3 pixels to fit inside the 2px gold border cleanly
                    hpFillGo.offsetMin = new Vector2(3, 3); hpFillGo.offsetMax = new Vector2(-3, -3);
                    healthBarFill = hpFillGo.GetComponent<Image>();
                    healthBarFill.sprite = CreateHealthBarFillSprite(289, 24);
                    healthBarFill.type = Image.Type.Filled;
                    healthBarFill.fillMethod = Image.FillMethod.Horizontal;
                    healthBarFill.fillAmount = 1.0f;

                    // Value text on the right side of the health bar
                    var hpValGo = new GameObject("HpValueText", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
                    hpValGo.SetParent(healthPanel, false);
                    hpValGo.anchorMin = hpValGo.anchorMax = new Vector2(0f, 0.5f);
                    hpValGo.pivot = new Vector2(0f, 0.5f);
                    hpValGo.anchoredPosition = new Vector2(410, 0);
                    hpValGo.sizeDelta = new Vector2(120, 35);
                    healthValueText = hpValGo.GetComponent<TextMeshProUGUI>();
                    healthValueText.font = GetTitleFont();
                    healthValueText.fontSize = 22;
                    healthValueText.fontStyle = FontStyles.Bold;
                    healthValueText.alignment = TextAlignmentOptions.Left;
                    healthValueText.color = new Color(0.1f, 0.9f, 0.3f, 0.95f); // Elegant vibrant green
                    healthValueText.text = "100%";

                    // --- REFINED AMMO PANEL ---
                    var ammoPanel = new GameObject("CustomAmmoPanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    ammoPanel.SetParent(root, false);
                    ammoPanel.anchorMin = ammoPanel.anchorMax = new Vector2(0, 1);
                    ammoPanel.pivot = new Vector2(0f, 1f);
                    ammoPanel.anchoredPosition = new Vector2(50, -135);
                    ammoPanel.sizeDelta = new Vector2(550, 85);
                    var amPanelImg = ammoPanel.GetComponent<Image>();
                    amPanelImg.sprite = null;
                    amPanelImg.color = Color.clear; // Fully borderless/transparent background
                    
                    var amIconGo = new GameObject("AmmoIcon", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    amIconGo.SetParent(ammoPanel, false);
                    amIconGo.anchorMin = amIconGo.anchorMax = new Vector2(0f, 0.5f);
                    amIconGo.pivot = new Vector2(0f, 0.5f);
                    amIconGo.anchoredPosition = new Vector2(15, 0);
                    amIconGo.sizeDelta = new Vector2(70, 70);
                    ammoIconImage = amIconGo.GetComponent<Image>();
                    ammoIconImage.sprite = sulphurIconSprite;
                    ammoIconImage.preserveAspect = true;

                    var ammoTxtGo = new GameObject("AmmoText", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
                    ammoTxtGo.SetParent(ammoPanel, false);
                    ammoTxtGo.sizeDelta = Vector2.zero;
                    ammoText = ammoTxtGo.GetComponent<TextMeshProUGUI>();
                    ammoText.text = "";

                    var ammoGridGo = new GameObject("AmmoGrid", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    ammoGridGo.SetParent(ammoPanel, false);
                    ammoGridGo.anchorMin = ammoGridGo.anchorMax = new Vector2(0f, 0.5f);
                    ammoGridGo.pivot = new Vector2(0f, 0.5f);
                    ammoGridGo.anchoredPosition = new Vector2(100, 0);
                    ammoGridGo.sizeDelta = new Vector2(295, 30);
                    ammoGridGo.GetComponent<Image>().sprite = CreateFramedBarSprite(295, 30, new Color(0.95f, 0.8f, 0.2f, 0.9f), new Color(0.04f, 0.04f, 0.04f, 0.8f), 2);

                    ammoTicks.Clear();
                    float tickWidth = 6f;
                    float tickHeight = 22f; // Sized down to 22px to fit inside the 2px border cleanly with padding
                    float spacing = 4f;
                    for (int i = 0; i < 30; i++)
                    {
                        var tickGo = new GameObject("Tick_" + i, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                        tickGo.SetParent(ammoGridGo, false);
                        tickGo.anchorMin = tickGo.anchorMax = new Vector2(0f, 0.5f);
                        tickGo.pivot = new Vector2(0f, 0.5f);
                        tickGo.anchoredPosition = new Vector2(i * (tickWidth + spacing) + tickWidth * 0.5f + 4f, 0); // Offset x starting pos to account for left border
                        tickGo.sizeDelta = new Vector2(tickWidth, tickHeight);

                        var img = tickGo.GetComponent<Image>();
                        img.sprite = CreateSolidBarSprite((int)tickWidth, (int)tickHeight, new Color(1.0f, 0.82f, 0.12f, 0.95f)); // Gold ticks
                        ammoTicks.Add(img);
                    }

                    // Value text on the right side of the ammo bar
                    var ammoValGo = new GameObject("AmmoValueText", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
                    ammoValGo.SetParent(ammoPanel, false);
                    ammoValGo.anchorMin = ammoValGo.anchorMax = new Vector2(0f, 0.5f);
                    ammoValGo.pivot = new Vector2(0f, 0.5f);
                    ammoValGo.anchoredPosition = new Vector2(410, 0);
                    ammoValGo.sizeDelta = new Vector2(120, 35);
                    ammoValueText = ammoValGo.GetComponent<TextMeshProUGUI>();
                    ammoValueText.font = GetTitleFont();
                    ammoValueText.fontSize = 22;
                    ammoValueText.fontStyle = FontStyles.Bold;
                    ammoValueText.alignment = TextAlignmentOptions.Left;
                    ammoValueText.color = new Color(0.95f, 0.55f, 0.05f, 0.95f); // Matching gold sulphur initially
                    ammoValueText.text = "SULPHUR";

                    // --- SETTINGS BUTTON (Always uses the beautiful procedural medallion gear) ---
                    var settingsBtnGo = new GameObject("SettingsButton", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    settingsBtnGo.SetParent(root, false);
                    settingsBtnGo.anchorMin = settingsBtnGo.anchorMax = new Vector2(1, 1);
                    settingsBtnGo.pivot = new Vector2(1, 1);
                    settingsBtnGo.anchoredPosition = new Vector2(-320, -70);
                    settingsBtnGo.sizeDelta = new Vector2(80, 80);
                    var settingsImg = settingsBtnGo.GetComponent<Image>();
                    settingsImg.sprite = CreateSettingsMedallionSprite(80, 80);
                    
                    var sHelper = settingsBtnGo.gameObject.AddComponent<ButtonInputHelper>();
                    sHelper.onClick = () => OpenSettingsModal(root);

                    // --- TARGETING RETICLE ---
                    var reticleGo = new GameObject("TargetingReticle", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    reticleGo.SetParent(root, false);
                    reticleGo.anchorMin = reticleGo.anchorMax = new Vector2(0.5f, 0.5f);
                    reticleGo.anchoredPosition = Vector2.zero;
                    reticleGo.sizeDelta = new Vector2(80, 80);
                    var reticleImg = reticleGo.GetComponent<Image>();
                    reticleImg.sprite = CreateTargetingReticleSprite(128);
                    reticleImg.raycastTarget = false;

                    new GameObject("MinimapCanvasContainer", typeof(RectTransform), typeof(MinimapUI)).transform.SetParent(transform, false);

                    // --- GUIDE ARROW & TARGET INDICATOR ---
                    guideArrowSprite = CreateProceduralArrowSprite(128);
                    var guideContainer = new GameObject("HUD_GuideContainer", typeof(RectTransform), typeof(CanvasGroup));
                    guideContainer.transform.SetParent(root, false);
                    var containerRect = guideContainer.GetComponent<RectTransform>();
                    containerRect.anchorMin = containerRect.anchorMax = new Vector2(0.5f, 1.0f);
                    containerRect.anchoredPosition = new Vector2(0f, -110f); // Top center
                    containerRect.sizeDelta = new Vector2(300, 150);
                    guideArrowCanvasGroup = guideContainer.GetComponent<CanvasGroup>();
                    guideArrowCanvasGroup.alpha = 0f;

                    var arrowGo = new GameObject("HUD_GuideArrow", typeof(RectTransform));
                    arrowGo.transform.SetParent(guideContainer.transform, false);
                    guideArrowRect = arrowGo.GetComponent<RectTransform>();
                    guideArrowRect.anchoredPosition = new Vector2(0f, 25f);
                    guideArrowRect.sizeDelta = new Vector2(90f, 90f);

                    var bgGo = new GameObject("HUD_GuideBg", typeof(RectTransform), typeof(Image));
                    bgGo.transform.SetParent(arrowGo.transform, false);
                    var bgRect = bgGo.GetComponent<RectTransform>();
                    bgRect.anchorMin = bgRect.anchorMax = new Vector2(0.5f, 0.5f);
                    bgRect.anchoredPosition = Vector2.zero;
                    bgRect.sizeDelta = new Vector2(90f, 90f);
                    var bgImg = bgGo.GetComponent<Image>();
                    bgImg.sprite = CreateSolidCircleSprite(128, new Color(0f, 0f, 0f, 0.5f));
                    bgImg.raycastTarget = false;

                    var outlineGo = new GameObject("HUD_GuideOutline", typeof(RectTransform), typeof(Image));
                    outlineGo.transform.SetParent(arrowGo.transform, false);
                    var outlineRect = outlineGo.GetComponent<RectTransform>();
                    outlineRect.anchorMin = outlineRect.anchorMax = new Vector2(0.5f, 0.5f);
                    outlineRect.anchoredPosition = Vector2.zero;
                    outlineRect.sizeDelta = new Vector2(90f, 90f);
                    guideArrowOutlineImage = outlineGo.GetComponent<Image>();
                    guideArrowOutlineImage.sprite = CreateProceduralRingSprite(128);
                    guideArrowOutlineImage.raycastTarget = false;

                    var chevronGo = new GameObject("HUD_Chevron", typeof(RectTransform), typeof(Image));
                    chevronGo.transform.SetParent(arrowGo.transform, false);
                    var chevronRect = chevronGo.GetComponent<RectTransform>();
                    chevronRect.anchorMin = chevronRect.anchorMax = new Vector2(0.5f, 0.5f);
                    chevronRect.anchoredPosition = Vector2.zero;
                    chevronRect.sizeDelta = new Vector2(90f, 90f);
                    guideArrowImage = chevronGo.GetComponent<Image>();
                    guideArrowImage.sprite = guideArrowSprite;
                    guideArrowImage.raycastTarget = false;

                    var guideTxtGo = new GameObject("HUD_GuideText", typeof(RectTransform), typeof(TextMeshProUGUI));
                    guideTxtGo.transform.SetParent(guideContainer.transform, false);
                    var gTxtRect = guideTxtGo.GetComponent<RectTransform>();
                    gTxtRect.anchorMin = gTxtRect.anchorMax = new Vector2(0.5f, 0.5f);
                    gTxtRect.anchoredPosition = new Vector2(0, -50);
                    gTxtRect.sizeDelta = new Vector2(250, 45);
                    guideArrowText = guideTxtGo.GetComponent<TextMeshProUGUI>();
                    guideArrowText.font = GetTitleFont();
                    guideArrowText.fontSize = 17;
                    guideArrowText.fontStyle = FontStyles.Bold;
                    guideArrowText.alignment = TextAlignmentOptions.Center;
                    guideArrowText.color = Color.white;
                    guideArrowText.outlineColor = new Color(0, 0, 0, 0.5f);
                    guideArrowText.outlineWidth = 0.2f;
                    guideArrowText.raycastTarget = false;
                    }


                private void CreateBlockButton(Transform parent, string label, Vector2 pos, Vector2 size, Sprite iconSprite, System.Action onDown, System.Action onUp = null)
                {
                    var go = new GameObject(label, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    go.SetParent(parent, false);
                    go.anchorMin = go.anchorMax = new Vector2(1f, 0f);
                    go.pivot = new Vector2(1f, 0f);
                    go.anchoredPosition = pos;
                    go.sizeDelta = size;

                    var img = go.GetComponent<Image>();
                    img.sprite = charcoalSprite;
                    img.raycastTarget = true;

                    var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    iconGo.SetParent(go, false);
                    var iconImg = iconGo.GetComponent<Image>();
                    iconImg.sprite = iconSprite;
                    iconImg.color = new Color(0.95f, 0.8f, 0.2f, 0.95f);
                    iconImg.preserveAspect = true;
                    iconImg.raycastTarget = false;

                    var txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
                    txtGo.SetParent(go, false);

                    var txt = txtGo.GetComponent<TextMeshProUGUI>();
                    txt.font = GetRobustFont();
                    txt.fontStyle = FontStyles.Bold;
                    txt.color = new Color(0.95f, 0.8f, 0.2f, 0.95f); // Alchemical gold!
                    txt.text = label;
                    txt.raycastTarget = false;

                    if (size.y > 100) // Large FIRE button
                    {
                        iconGo.anchorMin = iconGo.anchorMax = new Vector2(0.5f, 0.5f);
                        iconGo.anchoredPosition = new Vector2(0, 25);
                        iconGo.sizeDelta = new Vector2(85, 85);

                        txtGo.anchorMin = txtGo.anchorMax = new Vector2(0.5f, 0.5f);
                        txtGo.anchoredPosition = new Vector2(0, -45);
                        txtGo.sizeDelta = new Vector2(220, 40);
                        txt.alignment = TextAlignmentOptions.Center;
                        txt.fontSize = 28;
                    }
                    else // Smaller utility buttons
                    {
                        iconGo.anchorMin = iconGo.anchorMax = new Vector2(0f, 0.5f);
                        iconGo.pivot = new Vector2(0f, 0.5f);
                        iconGo.anchoredPosition = new Vector2(20, 0);
                        iconGo.sizeDelta = new Vector2(40, 40);

                        txtGo.anchorMin = Vector2.zero;
                        txtGo.anchorMax = Vector2.one;
                        txtGo.offsetMin = new Vector2(70, 0);
                        txtGo.offsetMax = Vector2.zero;
                        txt.alignment = TextAlignmentOptions.Left;
                        txt.fontSize = 20;
                    }

                    var helper = go.gameObject.AddComponent<ButtonInputHelper>();
                    helper.isDraggable = true;
                    helper.onDown = () => {
                        go.localScale = new Vector3(0.95f, 0.95f, 1f);
                        txt.color = new Color(0.8f, 0.65f, 0.1f, 0.95f);
                        if (iconImg != null) iconImg.color = new Color(0.8f, 0.65f, 0.1f, 0.95f);
                        onDown?.Invoke();
                    };
                    helper.onUp = () => {
                        go.localScale = new Vector3(1f, 1f, 1f);
                        txt.color = new Color(0.95f, 0.8f, 0.2f, 0.95f);
                        if (iconImg != null) iconImg.color = new Color(0.95f, 0.8f, 0.2f, 0.95f);
                        onUp?.Invoke();
                    };
                }



                private void CreateSprintBlockButton(Transform parent, Vector2 pos, Vector2 size, Sprite iconSprite)
                {
                    var go = new GameObject("SPRINT", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    go.SetParent(parent, false);
                    go.anchorMin = go.anchorMax = new Vector2(1f, 0f);
                    go.pivot = new Vector2(1f, 0f);
                    go.anchoredPosition = pos;
                    go.sizeDelta = size;

                    var img = go.GetComponent<Image>();
                    img.sprite = charcoalSprite;
                    img.raycastTarget = true;

                    var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    iconGo.SetParent(go, false);
                    iconGo.anchorMin = iconGo.anchorMax = new Vector2(0f, 0.5f);
                    iconGo.pivot = new Vector2(0f, 0.5f);
                    iconGo.anchoredPosition = new Vector2(20, 0);
                    iconGo.sizeDelta = new Vector2(40, 40);
                    var iconImg = iconGo.GetComponent<Image>();
                    iconImg.sprite = iconSprite;
                    iconImg.color = new Color(0.95f, 0.8f, 0.2f, 0.95f);
                    iconImg.preserveAspect = true;
                    iconImg.raycastTarget = false;

                    var txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
                    txtGo.SetParent(go, false);
                    txtGo.anchorMin = Vector2.zero;
                    txtGo.anchorMax = Vector2.one;
                    txtGo.offsetMin = new Vector2(70, 0);
                    txtGo.offsetMax = Vector2.zero;

                    sprintButtonText = txtGo.GetComponent<TextMeshProUGUI>();
                    sprintButtonText.font = GetRobustFont();
                    sprintButtonText.fontSize = 20;
                    sprintButtonText.fontStyle = FontStyles.Bold;
                    sprintButtonText.alignment = TextAlignmentOptions.Left;
                    sprintButtonText.color = new Color(0.95f, 0.8f, 0.2f, 0.95f);
                    sprintButtonText.text = "SPRINT: OFF";

                    var helper = go.gameObject.AddComponent<ButtonInputHelper>();
                    helper.isDraggable = true;
                    helper.onDown = () => {
                        sprintToggleState = !sprintToggleState;
                        sprintButtonText.text = sprintToggleState ? "SPRINT: ON" : "SPRINT: OFF";
                        go.localScale = sprintToggleState ? new Vector3(0.97f, 0.97f, 1f) : new Vector3(1f, 1f, 1f);
                        sprintButtonText.color = sprintToggleState ? new Color(1f, 0.95f, 0.6f, 0.95f) : new Color(0.95f, 0.8f, 0.2f, 0.95f);
                        if (iconImg != null) iconImg.color = sprintToggleState ? new Color(1f, 0.95f, 0.6f, 0.95f) : new Color(0.95f, 0.8f, 0.2f, 0.95f);
                        SetSprint(sprintToggleState);
                    };
                }



                private void CreateButton(Transform parent, string label, Vector2 pos, float diameter, Sprite iconSprite, System.Action onDown, System.Action onUp = null)
                {
                    var go = new GameObject(label, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    go.SetParent(parent, false); go.anchoredPosition = pos; go.sizeDelta = new Vector2(diameter, diameter);
                    var img = go.GetComponent<Image>(); img.color = new Color(0, 0, 0, 0); img.raycastTarget = true;

                    var shadowGo = new GameObject("Shadow", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    shadowGo.SetParent(go, false); shadowGo.anchorMin = Vector2.zero; shadowGo.anchorMax = Vector2.one; shadowGo.offsetMin = shadowGo.offsetMax = Vector2.zero;
                    var shadowImg = shadowGo.GetComponent<Image>(); shadowImg.sprite = goldGradientSprite; shadowImg.raycastTarget = false;
                    shadowGo.gameObject.SetActive(false);

                    var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    iconGo.SetParent(go, false); iconGo.anchorMin = Vector2.zero; iconGo.anchorMax = Vector2.one; iconGo.offsetMin = iconGo.offsetMax = Vector2.zero;
                    var iImg = iconGo.GetComponent<Image>(); iImg.sprite = iconSprite; iImg.color = Color.white; iImg.raycastTarget = false;
                    iImg.preserveAspect = true; 

                    var helper = go.gameObject.AddComponent<ButtonInputHelper>();
                    helper.isDraggable = true;
                    helper.glowObject = shadowGo.gameObject;
                    helper.onDown = () => { go.localScale = new Vector3(0.9f, 0.9f, 1f); iImg.color = new Color(0.8f, 0.8f, 0.8f, 1f); onDown?.Invoke(); };
                    helper.onUp = () => { go.localScale = new Vector3(1f, 1f, 1f); iImg.color = Color.white; onUp?.Invoke(); };
                }



                private void CreateSprintButton(Transform parent, Vector2 pos, float diameter)
                {
                    var go = new GameObject("SPRINT", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    go.SetParent(parent, false); go.anchoredPosition = pos; go.sizeDelta = new Vector2(diameter, diameter);
                    var img = go.GetComponent<Image>(); img.color = new Color(0, 0, 0, 0); img.raycastTarget = true;

                    var shadowGo = new GameObject("Shadow", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    shadowGo.SetParent(go, false); shadowGo.anchorMin = Vector2.zero; shadowGo.anchorMax = Vector2.one; shadowGo.offsetMin = shadowGo.offsetMax = Vector2.zero;
                    sprintShadowImage = shadowGo.GetComponent<Image>(); sprintShadowImage.sprite = goldGradientSprite; sprintShadowImage.raycastTarget = false;
                    shadowGo.gameObject.SetActive(false);

                    var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    iconGo.SetParent(go, false); iconGo.anchorMin = Vector2.zero; iconGo.anchorMax = Vector2.one; iconGo.offsetMin = iconGo.offsetMax = Vector2.zero;
                    sprintIconImage = iconGo.GetComponent<Image>(); sprintIconImage.sprite = sprintIcon; sprintIconImage.raycastTarget = false;
                    sprintIconImage.preserveAspect = true;

                    var helper = go.gameObject.AddComponent<ButtonInputHelper>();
                    helper.isDraggable = true;
                    helper.onDown = () => { sprintToggleState = !sprintToggleState; UpdateSprintVisuals(); SetSprint(sprintToggleState); };

                    UpdateSprintVisuals();
                }

    }
}
