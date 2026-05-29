using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;


namespace TheAlchemistsCrypt.UI
{
    public partial class MobileHUDButtons
    {


                private void SetupCanvas()
                {
                    var canvas = GetComponent<Canvas>();
                    if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
                    
                    var mainCam = Camera.main;
                    if (mainCam != null)
                    {
                        canvas.renderMode = RenderMode.ScreenSpaceCamera;
                        canvas.worldCamera = mainCam;
                        canvas.planeDistance = 5f; // Render close to camera but behind WeaponCamera
                    }
                    else
                    {
                        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    }
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

                    // Create Horror Overlay as first sibling (blurry fog vignette, not solid)
                    var horrorGo = new GameObject("HorrorOverlay", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    horrorGo.SetParent(root, false);
                    horrorGo.SetAsFirstSibling();
                    horrorGo.anchorMin = Vector2.zero; horrorGo.anchorMax = Vector2.one;
                    horrorGo.offsetMin = horrorGo.offsetMax = Vector2.zero;
                    var horrorImg = horrorGo.GetComponent<Image>();
                    // Load smog as sprite — if not yet reimported as Sprite type, create it from raw texture
                    var smogSprite = Resources.Load<Sprite>("Textures/Smog");
                    if (smogSprite == null)
                    {
                        // Fallback: load as Texture2D and create sprite at runtime
                        var smogTex = Resources.Load<Texture2D>("Textures/Smog");
                        if (smogTex != null)
                            smogSprite = Sprite.Create(smogTex,
                                new Rect(0, 0, smogTex.width, smogTex.height),
                                new Vector2(0.5f, 0.5f));
                    }
                    if (smogSprite != null)
                    {
                        horrorImg.sprite = smogSprite;
                        // Green fog vignette — alpha 0.42 is clearly visible but not oppressive
                        horrorImg.color = new Color(0.04f, 0.22f, 0.08f, 0.42f);
                        horrorImg.type = Image.Type.Simple;
                        horrorImg.preserveAspect = false;
                    }
                    else
                    {
                        // Fallback: Soft radial vignette — dark green only at screen edges (fog effect)
                        horrorImg.sprite = CreateProceduralGradientSprite(256, 256, new Color(0f, 0.12f, 0.03f, 0f), new Color(0f, 0.22f, 0.06f, 0.55f));
                        horrorImg.color = Color.white;
                    }
                    horrorImg.raycastTarget = false;

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
                    glowImage.color = new Color(0.0f, 0.9f, 0.4f, 0.5f); // translucent green glow
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

                    // ═══════════════════════════════════════════════════════
                    // HEALTH PANEL — Premium redesign
                    // Dark glassmorphism card | Glowing scarab-red heart icon
                    // Red→Orange→Gold gradient fill | Green HP% value
                    // ═══════════════════════════════════════════════════════
                    var healthPanel = new GameObject("CustomHealthPanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    healthPanel.SetParent(root, false);
                    healthPanel.anchorMin = healthPanel.anchorMax = new Vector2(0, 1);
                    healthPanel.pivot = new Vector2(0f, 1f);
                    healthPanel.anchoredPosition = new Vector2(14, -44);
                    healthPanel.sizeDelta = new Vector2(380, 54);

                    var hpPanelImg = healthPanel.GetComponent<Image>();
                    // Deep translucent dark panel with warm amber border
                    hpPanelImg.sprite = CreateGlassmorphismPanelSprite(380, 54, 
                        new Color(0.06f, 0.04f, 0.02f, 0.82f),   // Dark warm interior
                        new Color(0.92f, 0.62f, 0.15f, 0.85f),   // Amber border
                        2);
                    hpPanelImg.type = Image.Type.Simple;
                    hpPanelImg.enabled = false; // Disable panel background so it's a floating bar

                    // Icon — large glowing scarab-red heart
                    var hpIconGo = new GameObject("HealthIcon", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    hpIconGo.SetParent(healthPanel, false);
                    hpIconGo.anchorMin = hpIconGo.anchorMax = new Vector2(0f, 0.5f);
                    hpIconGo.pivot = new Vector2(0f, 0.5f);
                    hpIconGo.anchoredPosition = new Vector2(8, 0);
                    hpIconGo.sizeDelta = new Vector2(42, 42);
                    var hpIconImg = hpIconGo.GetComponent<Image>();
                    hpIconImg.sprite = healthIconSprite;
                    hpIconImg.color = new Color(1.0f, 0.22f, 0.22f, 1f); // Scarab crimson
                    hpIconImg.preserveAspect = true;

                    // "HP" label
                    var hpLblGo = new GameObject("HpLabel", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
                    hpLblGo.SetParent(healthPanel, false);
                    hpLblGo.anchorMin = hpLblGo.anchorMax = new Vector2(0f, 0.5f);
                    hpLblGo.pivot = new Vector2(0f, 0.5f);
                    hpLblGo.anchoredPosition = new Vector2(54, 0);
                    hpLblGo.sizeDelta = new Vector2(30, 30);
                    var hpLblTxt = hpLblGo.GetComponent<TextMeshProUGUI>();
                    hpLblTxt.font = GetTitleFont();
                    hpLblTxt.fontSize = 14;
                    hpLblTxt.fontStyle = FontStyles.Bold;
                    hpLblTxt.alignment = TextAlignmentOptions.Left;
                    hpLblTxt.color = new Color(0.95f, 0.62f, 0.15f, 1f); // Amber label
                    hpLblTxt.text = "HP";

                    // Hidden healthText (kept for compatibility)
                    var healthTxtGo = new GameObject("HealthText", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
                    healthTxtGo.SetParent(healthPanel, false);
                    healthTxtGo.sizeDelta = Vector2.zero;
                    healthText = healthTxtGo.GetComponent<TextMeshProUGUI>();
                    healthText.text = "";

                    // Bar background — narrower to fit label+value on same row
                    var hpBgBar = new GameObject("HpBarBg", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    hpBgBar.SetParent(healthPanel, false);
                    hpBgBar.anchorMin = hpBgBar.anchorMax = new Vector2(0f, 0.5f);
                    hpBgBar.pivot = new Vector2(0f, 0.5f);
                    hpBgBar.anchoredPosition = new Vector2(90, 0);
                    hpBgBar.sizeDelta = new Vector2(208, 22);
                    var hpBgImg = hpBgBar.GetComponent<Image>();
                    hpBgImg.sprite = CreateRoundedRectSprite(208, 22, new Color(0.08f, 0.05f, 0.02f, 0.9f), 4);

                    // Inner fill (red→orange→gold gradient)
                    var hpFillGo = new GameObject("HpFill", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    hpFillGo.SetParent(hpBgBar, false);
                    hpFillGo.anchorMin = Vector2.zero; hpFillGo.anchorMax = Vector2.one;
                    hpFillGo.offsetMin = new Vector2(2, 2); hpFillGo.offsetMax = new Vector2(-2, -2);
                    healthBarFill = hpFillGo.GetComponent<Image>();
                    healthBarFill.sprite = CreateHealthBarFillSprite(204, 18);
                    healthBarFill.type = Image.Type.Filled;
                    healthBarFill.fillMethod = Image.FillMethod.Horizontal;
                    healthBarFill.fillAmount = 1.0f;

                    // HP% value text — bold green, right of bar
                    var hpValGo = new GameObject("HpValueText", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
                    hpValGo.SetParent(healthPanel, false);
                    hpValGo.anchorMin = hpValGo.anchorMax = new Vector2(0f, 0.5f);
                    hpValGo.pivot = new Vector2(0f, 0.5f);
                    hpValGo.anchoredPosition = new Vector2(308, 0);
                    hpValGo.sizeDelta = new Vector2(65, 30);
                    healthValueText = hpValGo.GetComponent<TextMeshProUGUI>();
                    healthValueText.font = GetTitleFont();
                    healthValueText.fontSize = 16;
                    healthValueText.fontStyle = FontStyles.Bold;
                    healthValueText.alignment = TextAlignmentOptions.Right;
                    healthValueText.color = new Color(0.25f, 0.95f, 0.38f, 1f); // Vivid green
                    healthValueText.text = "100%";

                    // DOTween entrance slide-in
                    var hpPanelRect = healthPanel.GetComponent<RectTransform>();
                    hpPanelRect.anchoredPosition = new Vector2(-380, -44);
                    hpPanelRect.DOAnchorPosX(14, 0.55f).SetEase(DG.Tweening.Ease.OutBack).SetDelay(0.1f);

                    // ═══════════════════════════════════════════════════════
                    // AMMO PANEL — Premium redesign
                    // Matching dark card | Bullet icon | 2-row tick grid
                    // ═══════════════════════════════════════════════════════
                    var ammoPanel = new GameObject("CustomAmmoPanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    ammoPanel.SetParent(root, false);
                    ammoPanel.anchorMin = ammoPanel.anchorMax = new Vector2(0, 1);
                    ammoPanel.pivot = new Vector2(0f, 1f);
                    ammoPanel.anchoredPosition = new Vector2(14, -106);
                    ammoPanel.sizeDelta = new Vector2(380, 54);

                    var amPanelImg = ammoPanel.GetComponent<Image>();
                    amPanelImg.sprite = CreateGlassmorphismPanelSprite(380, 54,
                        new Color(0.04f, 0.04f, 0.06f, 0.82f),   // Slightly blue-tinted dark
                        new Color(0.92f, 0.62f, 0.15f, 0.85f),   // Same amber border
                        2);
                    amPanelImg.type = Image.Type.Simple;
                    amPanelImg.enabled = false; // Disable panel background so it's a floating bar

                    // Bullet/ammo icon
                    var amIconGo = new GameObject("AmmoIcon", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    amIconGo.SetParent(ammoPanel, false);
                    amIconGo.anchorMin = amIconGo.anchorMax = new Vector2(0f, 0.5f);
                    amIconGo.pivot = new Vector2(0f, 0.5f);
                    amIconGo.anchoredPosition = new Vector2(8, 0);
                    amIconGo.sizeDelta = new Vector2(42, 42);
                    ammoIconImage = amIconGo.GetComponent<Image>();
                    ammoIconImage.sprite = sulphurIconSprite;
                    ammoIconImage.color = new Color(0.95f, 0.72f, 0.12f, 1f); // Gold tint
                    ammoIconImage.preserveAspect = true;

                    // Mode label stub (hidden — value text replaces it)
                    var ammoTxtGo = new GameObject("AmmoText", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
                    ammoTxtGo.SetParent(ammoPanel, false);
                    ammoTxtGo.sizeDelta = Vector2.zero;
                    ammoText = ammoTxtGo.GetComponent<TextMeshProUGUI>();
                    ammoText.text = "";

                    // Mode value text ("AM" to match "HP") — sits left of bar
                    var ammoValGo = new GameObject("AmmoValueText", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
                    ammoValGo.SetParent(ammoPanel, false);
                    ammoValGo.anchorMin = ammoValGo.anchorMax = new Vector2(0f, 0.5f);
                    ammoValGo.pivot = new Vector2(0f, 0.5f);
                    ammoValGo.anchoredPosition = new Vector2(54, 0);
                    ammoValGo.sizeDelta = new Vector2(30, 30);
                    var ammoLblTxt = ammoValGo.GetComponent<TextMeshProUGUI>();
                    ammoLblTxt.font = GetTitleFont();
                    ammoLblTxt.fontSize = 14;
                    ammoLblTxt.fontStyle = FontStyles.Bold;
                    ammoLblTxt.alignment = TextAlignmentOptions.Left;
                    ammoLblTxt.color = new Color(0.95f, 0.62f, 0.15f, 1f);
                    ammoLblTxt.text = "AM";

                    // Bar background
                    var amBgBar = new GameObject("AmBarBg", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    amBgBar.SetParent(ammoPanel, false);
                    amBgBar.anchorMin = amBgBar.anchorMax = new Vector2(0f, 0.5f);
                    amBgBar.pivot = new Vector2(0f, 0.5f);
                    amBgBar.anchoredPosition = new Vector2(90, 0);
                    amBgBar.sizeDelta = new Vector2(208, 22);
                    var amBgImg = amBgBar.GetComponent<Image>();
                    amBgImg.sprite = CreateRoundedRectSprite(208, 22, new Color(0.04f, 0.04f, 0.06f, 0.9f), 4);

                    // Inner fill
                    var amFillGo = new GameObject("AmFill", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    amFillGo.SetParent(amBgBar, false);
                    amFillGo.anchorMin = Vector2.zero; amFillGo.anchorMax = Vector2.one;
                    amFillGo.offsetMin = new Vector2(2, 2); amFillGo.offsetMax = new Vector2(-2, -2);
                    ammoBarFill = amFillGo.GetComponent<Image>();
                    ammoBarFill.sprite = sulfurBarSprite;
                    ammoBarFill.type = Image.Type.Filled;
                    ammoBarFill.fillMethod = Image.FillMethod.Horizontal;
                    ammoBarFill.fillAmount = 1.0f;

                    // Ammo Count text value on the right
                    var ammoCountValGo = new GameObject("AmmoCountValueText", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
                    ammoCountValGo.SetParent(ammoPanel, false);
                    ammoCountValGo.anchorMin = ammoCountValGo.anchorMax = new Vector2(0f, 0.5f);
                    ammoCountValGo.pivot = new Vector2(0f, 0.5f);
                    ammoCountValGo.anchoredPosition = new Vector2(308, 0);
                    ammoCountValGo.sizeDelta = new Vector2(65, 30);
                    ammoValueText = ammoCountValGo.GetComponent<TextMeshProUGUI>();
                    ammoValueText.font = GetTitleFont();
                    ammoValueText.fontSize = 16;
                    ammoValueText.fontStyle = FontStyles.Bold;
                    ammoValueText.alignment = TextAlignmentOptions.Right;
                    ammoValueText.color = new Color(0.95f, 0.72f, 0.12f, 1f); // Gold/Amber matching element theme
                    ammoValueText.text = "30/30";

                    // ── Ammo tick grid (2 rows × 15 columns) ────────────────────────────
                    // Container for the tick grid
                    var ammoGridGo = new GameObject("AmmoGrid", typeof(RectTransform)).GetComponent<RectTransform>();
                    ammoGridGo.SetParent(ammoPanel, false);
                    ammoGridGo.anchorMin = ammoGridGo.anchorMax = new Vector2(0f, 0.5f);
                    ammoGridGo.pivot = new Vector2(0f, 0.5f);
                    ammoGridGo.anchoredPosition = new Vector2(106, 0);
                    ammoGridGo.sizeDelta = new Vector2(262, 42);

                    ammoTicks.Clear();
                    // 2 rows × 15 = 30 ticks, each a small diamond/pill shape
                    float tickW = 12f;   // wider tick
                    float tickH = 16f;   // shorter tick
                    float gapX = 5f;     // horizontal gap
                    float gapY = 5f;     // vertical gap between rows
                    float rowHeight = 42f;
                    // Row offsets: 2 rows centered vertically
                    float[] rowY = new float[] { rowHeight * 0.25f, rowHeight * 0.75f };

                    for (int row = 0; row < 2; row++)
                    {
                        for (int col = 0; col < 15; col++)
                        {
                            int idx = row * 15 + col;
                            var tickGo = new GameObject("Tick_" + idx, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                            tickGo.SetParent(ammoGridGo, false);
                            tickGo.anchorMin = tickGo.anchorMax = Vector2.zero;
                            tickGo.pivot = new Vector2(0f, 0f);
                            float xPos = col * (tickW + gapX);
                            float yPos = row == 0 ? (rowHeight * 0.5f + gapY * 0.5f) : gapY;
                            tickGo.anchoredPosition = new Vector2(xPos, yPos - tickH * 0.5f);
                            tickGo.sizeDelta = new Vector2(tickW, tickH);

                            var img = tickGo.GetComponent<Image>();
                            img.sprite = CreateDiamondTickSprite((int)tickW, (int)tickH);
                            img.color = new Color(1.0f, 0.82f, 0.12f, 0.95f); // Gold active
                            ammoTicks.Add(img);
                        }
                    }

                    // DOTween entrance slide-in (slight delay after HP bar)
                    var amPanelRect = ammoPanel.GetComponent<RectTransform>();
                    amPanelRect.anchoredPosition = new Vector2(-380, -106);
                    amPanelRect.DOAnchorPosX(14, 0.55f).SetEase(DG.Tweening.Ease.OutBack).SetDelay(0.22f);


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
                    
                    // Create GameplayBloodVignette at the bottom of the HUD hierarchy
                    var bloodGo = new GameObject("GameplayBloodVignette", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    bloodGo.SetParent(root, false);
                    bloodGo.anchorMin = Vector2.zero; bloodGo.anchorMax = Vector2.one;
                    bloodGo.offsetMin = bloodGo.offsetMax = Vector2.zero;
                    gameplayBloodVignette = bloodGo.GetComponent<Image>();
                    gameplayBloodVignette.sprite = CreateProceduralGradientSprite(256, 256, new Color(1f, 0f, 0f, 0f), new Color(1f, 0f, 0f, 0.7f));
                    gameplayBloodVignette.color = new Color(1f, 1f, 1f, 0f);
                    gameplayBloodVignette.raycastTarget = false;

                    // Setup additional animation overlays and UI widgets
                    SetupAnimationsUI(root);
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
