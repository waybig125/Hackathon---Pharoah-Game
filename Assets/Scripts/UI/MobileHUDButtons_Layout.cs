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
                        // Green fog vignette — alpha 0.45 is clearly visible but not oppressive
                        horrorImg.color = new Color(0.2f, 0.55f, 0.25f, 0.45f);
                        horrorImg.type = Image.Type.Simple;
                        horrorImg.preserveAspect = false;
                    }
                    else
                    {
                        // Fallback: Soft radial vignette — dark green only at screen edges (fog effect)
                        horrorImg.sprite = CreateProceduralGradientSprite(256, 256, new Color(0f, 0.35f, 0.1f, 0f), new Color(0.1f, 0.55f, 0.2f, 0.55f));
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

                    // --- ACTION BUTTONS ---
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

                    Vector2 firePos = GetButtonPosition("FIRE", isLefty ? new Vector2(165, 165) : new Vector2(-165, 165));
                    Vector2 reloadPos = GetButtonPosition("RELOAD", isLefty ? new Vector2(390, 112) : new Vector2(-390, 112));
                    Vector2 swapPos = GetButtonPosition("SWAP", isLefty ? new Vector2(270, 465) : new Vector2(-270, 465));
                    Vector2 sprintPos = GetButtonPosition("SPRINT", isLefty ? new Vector2(487, 225) : new Vector2(-487, 225));
                    Vector2 focusPos = GetButtonPosition("FOCUS", isLefty ? new Vector2(337, 315) : new Vector2(-337, 315));
                    Vector2 jumpPos = GetButtonPosition("JUMP", isLefty ? new Vector2(112, 390) : new Vector2(-112, 390));

                    CreateButton(btnContainer, "FIRE", firePos, 285, fireIcon, () => SetFire(true), () => SetFire(false));
                    CreateButton(btnContainer, "RELOAD", reloadPos, 150, reloadIcon, () => Reload());
                    CreateButton(btnContainer, "SWAP", swapPos, 150, swapIcon, () => Swap());
                    CreateSprintButton(btnContainer, sprintPos, 150);
                    CreateButton(btnContainer, "FOCUS", focusPos, 150, focusIcon, () => SetAiming(true), () => SetAiming(false));
                    CreateButton(btnContainer, "JUMP", jumpPos, 165, jumpIcon, () => SetJump(true), () => SetJump(false));

                    HideDebugLabels();

                    // ═══════════════════════════════════════════════════════
                    // HEALTH PANEL — Premium redesign
                    // Red→Orange→Gold gradient fill | Green HP% value
                    // ═══════════════════════════════════════════════════════
                    var healthPanel = new GameObject("CustomHealthPanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    healthPanel.SetParent(root, false);
                    healthPanel.anchorMin = healthPanel.anchorMax = new Vector2(0, 1);
                    healthPanel.pivot = new Vector2(0f, 1f);
                    healthPanel.anchoredPosition = new Vector2(14, -44);
                    healthPanel.sizeDelta = new Vector2(440, 54);

                    var hpPanelImg = healthPanel.GetComponent<Image>();
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
                    hpIconImg.color = new Color(1.0f, 0.22f, 0.22f, 1f); 
                    hpIconImg.preserveAspect = true;

                    // "HP" label
                    var hpLblGo = new GameObject("HpLabel", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
                    hpLblGo.SetParent(healthPanel, false);
                    hpLblGo.anchorMin = hpLblGo.anchorMax = new Vector2(0f, 0.5f);
                    hpLblGo.pivot = new Vector2(0f, 0.5f);
                    hpLblGo.anchoredPosition = new Vector2(54, 0);
                    hpLblGo.sizeDelta = new Vector2(36, 34);
                    var hpLblTxt = hpLblGo.GetComponent<TextMeshProUGUI>();
                    hpLblTxt.font = GetTitleFont();
                    hpLblTxt.fontSize = 15;
                    hpLblTxt.fontStyle = FontStyles.Bold;
                    hpLblTxt.alignment = TextAlignmentOptions.Center;
                    hpLblTxt.color = new Color(1.0f, 0.8f, 0.2f, 1f); 
                    hpLblTxt.outlineColor = new Color32(0, 0, 0, 230);
                    hpLblTxt.outlineWidth = 0.25f;
                    hpLblTxt.text = " HP";

                    // Hidden healthText (kept for compatibility)
                    var healthTxtGo = new GameObject("HealthText", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
                    healthTxtGo.SetParent(healthPanel, false);
                    healthTxtGo.sizeDelta = Vector2.zero;
                    healthText = healthTxtGo.GetComponent<TextMeshProUGUI>();
                    healthText.text = "";

                    // Bar background
                    var hpBgBar = new GameObject("HpBarBg", typeof(RectTransform), typeof(Image), typeof(Mask)).GetComponent<RectTransform>();
                    hpBgBar.SetParent(healthPanel, false);
                    hpBgBar.anchorMin = hpBgBar.anchorMax = new Vector2(0f, 0.5f);
                    hpBgBar.pivot = new Vector2(0f, 0.5f);
                    hpBgBar.anchoredPosition = new Vector2(105, 0);
                    hpBgBar.sizeDelta = new Vector2(220, 24);
                    var hpBgImg = hpBgBar.GetComponent<Image>();
                    hpBgImg.sprite = CreateRoundedRectSprite(220, 24, Color.white, 12);
                    hpBgImg.color = new Color(0.02f, 0.08f, 0.04f, 1f);
                    var hpMask = hpBgBar.GetComponent<Mask>();
                    hpMask.showMaskGraphic = true;

                    // Inner fill
                    var hpFillGo = new GameObject("HpFill", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    hpFillGo.SetParent(hpBgBar, false);
                    hpFillGo.anchorMin = new Vector2(0f, 0f); hpFillGo.anchorMax = new Vector2(1f, 1f);
                    hpFillGo.offsetMin = new Vector2(0f, 0f); hpFillGo.offsetMax = new Vector2(0f, 0f);
                    healthBarFill = hpFillGo.GetComponent<Image>();
                    healthBarFill.sprite = CreateRoundedRectSprite(24, 24, Color.white, 12);
                    healthBarFill.color = new Color(0.0f, 0.9f, 0.3f, 0.85f);
                    healthBarFill.type = Image.Type.Sliced;

                    // HP% value text
                    var hpValGo = new GameObject("HpValueText", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
                    hpValGo.SetParent(healthPanel, false);
                    hpValGo.anchorMin = hpValGo.anchorMax = new Vector2(0f, 0.5f);
                    hpValGo.pivot = new Vector2(0f, 0.5f);
                    hpValGo.anchoredPosition = new Vector2(345, 0);
                    hpValGo.sizeDelta = new Vector2(75, 34);
                    healthValueText = hpValGo.GetComponent<TextMeshProUGUI>();
                    healthValueText.font = GetTitleFont();
                    healthValueText.fontSize = 15;
                    healthValueText.fontStyle = FontStyles.Bold;
                    healthValueText.alignment = TextAlignmentOptions.Right;
                    healthValueText.color = new Color(0.5f, 1.0f, 0.5f, 1f);
                    healthValueText.outlineColor = new Color32(0, 0, 0, 220);
                    healthValueText.outlineWidth = 0.22f;
                    healthValueText.text = "100%";

                    // DOTween entrance slide-in
                    var hpPanelRect = healthPanel.GetComponent<RectTransform>();
                    hpPanelRect.anchoredPosition = new Vector2(-440, -44);
                    hpPanelRect.DOAnchorPosX(14, 0.55f).SetEase(DG.Tweening.Ease.OutBack).SetDelay(0.1f);

                    // ═══════════════════════════════════════════════════════
                    // AMMO PANEL
                    // ═══════════════════════════════════════════════════════
                    var ammoPanel = new GameObject("CustomAmmoPanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    ammoPanel.SetParent(root, false);
                    ammoPanel.anchorMin = ammoPanel.anchorMax = new Vector2(0, 1);
                    ammoPanel.pivot = new Vector2(0f, 1f);
                    ammoPanel.anchoredPosition = new Vector2(14, -106);
                    ammoPanel.sizeDelta = new Vector2(440, 54);

                    var amPanelImg = ammoPanel.GetComponent<Image>();
                    amPanelImg.enabled = false;

                    // Bullet/ammo icon
                    var amIconGo = new GameObject("AmmoIcon", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    amIconGo.SetParent(ammoPanel, false);
                    amIconGo.anchorMin = amIconGo.anchorMax = new Vector2(0f, 0.5f);
                    amIconGo.pivot = new Vector2(0f, 0.5f);
                    amIconGo.anchoredPosition = new Vector2(8, 0);
                    amIconGo.sizeDelta = new Vector2(42, 42);
                    ammoIconImage = amIconGo.GetComponent<Image>();
                    ammoIconImage.sprite = sulphurIconSprite;
                    ammoIconImage.color = new Color(0.95f, 0.72f, 0.12f, 1f); 
                    ammoIconImage.preserveAspect = true;

                    var amTxtGo = new GameObject("AmmoText", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
                    amTxtGo.SetParent(ammoPanel, false);
                    amTxtGo.sizeDelta = Vector2.zero;
                    ammoText = amTxtGo.GetComponent<TextMeshProUGUI>();
                    ammoText.text = "";

                    // AM label
                    var ammoValGo = new GameObject("AmmoValueText", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
                    ammoValGo.SetParent(ammoPanel, false);
                    ammoValGo.anchorMin = ammoValGo.anchorMax = new Vector2(0f, 0.5f);
                    ammoValGo.pivot = new Vector2(0f, 0.5f);
                    ammoValGo.anchoredPosition = new Vector2(54, 0);
                    ammoValGo.sizeDelta = new Vector2(36, 34);
                    var ammoLblTxt = ammoValGo.GetComponent<TextMeshProUGUI>();
                    ammoLblTxt.font = GetTitleFont();
                    ammoLblTxt.fontSize = 15;
                    ammoLblTxt.fontStyle = FontStyles.Bold;
                    ammoLblTxt.alignment = TextAlignmentOptions.Center;
                    ammoLblTxt.color = new Color(1.0f, 0.8f, 0.2f, 1f);
                    ammoLblTxt.outlineColor = new Color32(0, 0, 0, 230);
                    ammoLblTxt.outlineWidth = 0.25f;
                    ammoLblTxt.text = " AM ";

                    // Bar background
                    var amBgBar = new GameObject("AmmoBarBg", typeof(RectTransform), typeof(Image), typeof(Mask)).GetComponent<RectTransform>();
                    amBgBar.SetParent(ammoPanel, false);
                    amBgBar.anchorMin = amBgBar.anchorMax = new Vector2(0f, 0.5f);
                    amBgBar.pivot = new Vector2(0f, 0.5f);
                    amBgBar.anchoredPosition = new Vector2(105, 0);
                    amBgBar.sizeDelta = new Vector2(220, 24);
                    var amBgImg = amBgBar.GetComponent<Image>();
                    amBgImg.sprite = CreateRoundedRectSprite(220, 24, Color.white, 12);
                    amBgImg.color = new Color(0.02f, 0.08f, 0.04f, 1f);
                    var amMask = amBgBar.GetComponent<Mask>();
                    amMask.showMaskGraphic = true;

                    // Inner fill
                    var amFillGo = new GameObject("AmmoFill", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    amFillGo.SetParent(amBgBar, false);
                    amFillGo.anchorMin = Vector2.zero; amFillGo.anchorMax = Vector2.one;
                    amFillGo.offsetMin = amFillGo.offsetMax = Vector2.zero;
                    ammoBarFill = amFillGo.GetComponent<Image>();
                    ammoBarFill.sprite = CreateRoundedRectSprite(24, 24, Color.white, 12);
                    ammoBarFill.type = Image.Type.Filled;
                    ammoBarFill.fillMethod = Image.FillMethod.Horizontal;
                    ammoBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
                    ammoBarFill.color = new Color(0.95f, 0.55f, 0.05f, 0.85f);

                    // Ammo Count text value on the right
                    var ammoCountValGo = new GameObject("AmmoCountValueText", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
                    ammoCountValGo.SetParent(ammoPanel, false);
                    ammoCountValGo.anchorMin = ammoCountValGo.anchorMax = new Vector2(0f, 0.5f);
                    ammoCountValGo.pivot = new Vector2(0f, 0.5f);
                    ammoCountValGo.anchoredPosition = new Vector2(345, 0);
                    ammoCountValGo.sizeDelta = new Vector2(75, 34);
                    ammoValueText = ammoCountValGo.GetComponent<TextMeshProUGUI>();
                    ammoValueText.font = GetTitleFont();
                    ammoValueText.fontSize = 15;
                    ammoValueText.fontStyle = FontStyles.Bold;
                    ammoValueText.alignment = TextAlignmentOptions.Right;
                    ammoValueText.color = new Color(1.0f, 0.85f, 0.3f, 1f);
                    ammoValueText.outlineColor = new Color32(0, 0, 0, 220);
                    ammoValueText.outlineWidth = 0.22f;
                    ammoValueText.text = "30/30";

                    // --- ELEMENT LABEL (Below Ammo) ---
                    var elementTxtGo = new GameObject("ElementLabel", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
                    elementTxtGo.SetParent(ammoPanel, false);
                    elementTxtGo.anchorMin = new Vector2(0.5f, 0f);
                    elementTxtGo.anchorMax = new Vector2(0.5f, 0f);
                    elementTxtGo.pivot = new Vector2(0.5f, 1f);
                    elementTxtGo.anchoredPosition = new Vector2(0, -6f);
                    elementTxtGo.sizeDelta = new Vector2(300, 25);
                    elementText = elementTxtGo.GetComponent<TextMeshProUGUI>();
                    elementText.font = GetTitleFont();
                    elementText.fontSize = 14;
                    elementText.fontStyle = FontStyles.Bold;
                    elementText.color = new Color(0.95f, 0.55f, 0.05f, 0.9f);
                    elementText.text = "ACTIVE ELEMENT: SULPHUR";
                    elementText.alignment = TextAlignmentOptions.Center;
                    elementText.textWrappingMode = TextWrappingModes.NoWrap;

                    // ═══════════════════════════════════════════════════════
                    // KILLS PANEL
                    // ═══════════════════════════════════════════════════════
                    var killsPanel = new GameObject("CustomKillsPanel", typeof(RectTransform)).GetComponent<RectTransform>();
                    killsPanel.SetParent(root, false);
                    killsPanel.anchorMin = killsPanel.anchorMax = new Vector2(0, 1);
                    killsPanel.pivot = new Vector2(0f, 1f);
                    killsPanel.anchoredPosition = new Vector2(14, -188);
                    killsPanel.sizeDelta = new Vector2(200, 36);

                    var killsTxtGo = new GameObject("KillsValueText", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
                    killsTxtGo.SetParent(killsPanel, false);
                    killsTxtGo.anchorMin = Vector2.zero;
                    killsTxtGo.anchorMax = Vector2.one;
                    killsTxtGo.offsetMin = new Vector2(12, 0);
                    killsTxtGo.offsetMax = new Vector2(-12, 0);
                    killsTxtGo.pivot = new Vector2(0.5f, 0.5f);

                    killsText = killsTxtGo.GetComponent<TextMeshProUGUI>();
                    killsText.font = GetTitleFont();
                    killsText.fontSize = 18;
                    killsText.fontStyle = FontStyles.Bold;
                    killsText.alignment = TextAlignmentOptions.Left;
                    killsText.color = new Color(1.0f, 0.82f, 0.2f, 1f);
                    killsText.outlineColor = new Color32(0, 0, 0, 255);
                    killsText.outlineWidth = 0.35f;
                    killsText.text = "KILLS: 0/20";

                    // DOTween entrance slide-in
                    var amPanelRect = ammoPanel.GetComponent<RectTransform>();
                    amPanelRect.anchoredPosition = new Vector2(-440, -106);
                    amPanelRect.DOAnchorPosX(14, 0.55f).SetEase(DG.Tweening.Ease.OutBack).SetDelay(0.22f);

                    var killsPanelRect = killsPanel.GetComponent<RectTransform>();
                    killsPanelRect.anchoredPosition = new Vector2(-200, -188);
                    killsPanelRect.DOAnchorPosX(14, 0.55f).SetEase(DG.Tweening.Ease.OutBack).SetDelay(0.3f);


                    // --- SETTINGS BUTTON ---
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

                    // --- RETICLE ---
                    var reticleGo = new GameObject("TargetingReticle", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    reticleGo.SetParent(root, false);
                    reticleGo.anchorMin = reticleGo.anchorMax = new Vector2(0.5f, 0.5f);
                    reticleGo.anchoredPosition = Vector2.zero;
                    reticleGo.sizeDelta = new Vector2(80, 80);
                    var reticleImg = reticleGo.GetComponent<Image>();
                    reticleImg.sprite = CreateTargetingReticleSprite(128);
                    reticleImg.raycastTarget = false;

                    new GameObject("MinimapCanvasContainer", typeof(RectTransform), typeof(MinimapUI)).transform.SetParent(transform, false);

                    // --- GUIDE ARROW ---
                    guideArrowSprite = CreateProceduralArrowSprite(128);
                    var guideContainer = new GameObject("HUD_GuideContainer", typeof(RectTransform), typeof(CanvasGroup));
                    guideContainer.transform.SetParent(root, false);
                    var containerRect = guideContainer.GetComponent<RectTransform>();
                    containerRect.anchorMin = containerRect.anchorMax = new Vector2(0.5f, 1.0f);
                    containerRect.anchoredPosition = new Vector2(0f, -110f);
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
                    bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
                    bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;
                    guideArrowImage = bgGo.GetComponent<Image>();
                    guideArrowImage.sprite = guideArrowSprite;
                    guideArrowImage.color = new Color(0.2f, 1f, 0.4f, 0.85f);
                    guideArrowImage.preserveAspect = true;

                    var outGo = new GameObject("HUD_GuideOutline", typeof(RectTransform), typeof(Image));
                    outGo.transform.SetParent(arrowGo.transform, false);
                    var outRect = outGo.GetComponent<RectTransform>();
                    outRect.anchorMin = Vector2.zero; outRect.anchorMax = Vector2.one;
                    outRect.offsetMin = outRect.offsetMax = new Vector2(-4, -4);
                    guideArrowOutlineImage = outGo.GetComponent<Image>();
                    guideArrowOutlineImage.sprite = guideArrowSprite;
                    guideArrowOutlineImage.color = new Color(1f, 0.8f, 0.2f, 0.4f);
                    guideArrowOutlineImage.preserveAspect = true;
                    outGo.transform.SetAsFirstSibling();

                    var txtGo = new GameObject("HUD_GuideText", typeof(RectTransform), typeof(TextMeshProUGUI));
                    txtGo.transform.SetParent(guideContainer.transform, false);
                    var txtRect = txtGo.GetComponent<RectTransform>();
                    txtRect.anchorMin = new Vector2(0f, 0f); txtRect.anchorMax = new Vector2(1f, 0.3f);
                    txtRect.offsetMin = txtRect.offsetMax = Vector2.zero;
                    guideArrowText = txtGo.GetComponent<TextMeshProUGUI>();
                    guideArrowText.font = GetTitleFont();
                    guideArrowText.fontSize = 18;
                    guideArrowText.alignment = TextAlignmentOptions.Center;
                    guideArrowText.color = Color.white;
                    guideArrowText.text = "LOCATE ANCIENT PAPYRUS";

                    var sprintIndicatorGo = new GameObject("SprintIndicator", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    sprintIndicatorGo.SetParent(root, false);
                    sprintIndicatorGo.anchorMin = sprintIndicatorGo.anchorMax = new Vector2(0.5f, 0.15f);
                    sprintIndicatorGo.anchoredPosition = new Vector2(0, 0);
                    sprintIndicatorGo.sizeDelta = new Vector2(60, 60);
                    sprintIndicatorImg = sprintIndicatorGo.GetComponent<Image>();
                    sprintIndicatorImg.sprite = sprintIcon;
                    sprintIndicatorImg.color = new Color(1f, 1f, 1f, 0f);
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
                    var iImg = iconGo.GetComponent<Image>();
                    iImg.sprite = iconSprite;
                    // 80% opacity when idle, 100% when active/pressed
                    iImg.color = new Color(1f, 1f, 1f, 0.8f);
                    iImg.raycastTarget = false;
                    iImg.preserveAspect = true; 

                    var helper = go.gameObject.AddComponent<ButtonInputHelper>();
                    helper.isDraggable = true;
                    helper.glowObject = shadowGo.gameObject;
                    helper.onDown = () => { go.localScale = new Vector3(0.9f, 0.9f, 1f); iImg.color = new Color(1f, 1f, 1f, 1f); onDown?.Invoke(); };
                    helper.onUp = () => { go.localScale = new Vector3(1f, 1f, 1f); iImg.color = new Color(1f, 1f, 1f, 0.8f); onUp?.Invoke(); };
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
                    sprintButtonIconImg = iconGo.GetComponent<Image>(); sprintButtonIconImg.sprite = sprintIcon; sprintButtonIconImg.raycastTarget = false;
                    sprintButtonIconImg.color = new Color(1f, 1f, 1f, 0.8f); // 80% idle opacity
                    sprintButtonIconImg.preserveAspect = true; 

                    var helper = go.gameObject.AddComponent<ButtonInputHelper>();
                    helper.isDraggable = true;
                    helper.onDown = () => { sprintToggleState = !sprintToggleState; UpdateSprintVisuals(); SetSprint(sprintToggleState); };

                    UpdateSprintVisuals();
                    }

    }
}
