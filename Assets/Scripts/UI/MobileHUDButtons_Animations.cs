using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;
using Coffee.UIExtensions;

namespace TheAlchemistsCrypt.UI
{
    public partial class MobileHUDButtons : MonoBehaviour
    {
        private Image healthCatchUpFill;
        private Image healthIconFill;
        private Image healthIconBg;
        private GameObject threatMeterGo;
        private Image threatMeterFill;
        private GameObject threatCompassGo;
        private Image threatEyeGlyphImg;
        private GameObject scrollUiGo;
        private RectTransform scrollUiRect;
        private Image scrollUiIcon;
        private Coffee.UIExtensions.UIParticle scrollUiParticles;
        private Image damageOverlayImg;
        private int lastAmmoCount = -1;
        private bool lastHasScroll = false;
        private float lastThreatVal = 0f;
        private Tween catchUpTween;

        private void SetupAnimationsUI(RectTransform root)
        {
            // 1. Setup Visceral Catch-up Health Bar
            var hpBgBarGo = GameObject.Find("MobileHUD_Root/HealthPanel/HpBarBg");
            if (hpBgBarGo != null)
            {
                var catchUpGo = new GameObject("HpCatchUpFill", typeof(RectTransform), typeof(Image));
                catchUpGo.transform.SetParent(hpBgBarGo.transform, false);
                var catchUpRect = catchUpGo.GetComponent<RectTransform>();
                catchUpRect.anchorMin = Vector2.zero;
                catchUpRect.anchorMax = Vector2.one;
                catchUpRect.offsetMin = new Vector2(2, 2);
                catchUpRect.offsetMax = new Vector2(-2, -2);
                
                healthCatchUpFill = catchUpGo.GetComponent<Image>();
                healthCatchUpFill.sprite = CreateHealthBarFillSprite(204, 18);
                healthCatchUpFill.type = Image.Type.Filled;
                healthCatchUpFill.fillMethod = Image.FillMethod.Horizontal;
                healthCatchUpFill.fillAmount = 1.0f;
                healthCatchUpFill.color = new Color(0.8f, 0.1f, 0.15f, 0.85f); // Deep visceral crimson red
                
                if (healthBarFill != null)
                {
                    healthBarFill.transform.SetAsLastSibling();
                    healthBarFill.color = Color.white; // Use the red->orange->gold gradient directly
                }
            }

            // 2. Setup Threat Meter UI
            threatMeterGo = new GameObject("ThreatMeter", typeof(RectTransform), typeof(Image));
            threatMeterGo.transform.SetParent(root, false);
            var tmRect = threatMeterGo.GetComponent<RectTransform>();
            tmRect.anchorMin = tmRect.anchorMax = new Vector2(0, 1);
            tmRect.pivot = new Vector2(0f, 1f);
            tmRect.anchoredPosition = new Vector2(60, -235);
            tmRect.sizeDelta = new Vector2(85, 85);
            
            var tmBg = threatMeterGo.GetComponent<Image>();
            tmBg.sprite = CreateCircularSandstoneMedallionSprite(128);
            tmBg.color = Color.white;
            
            Sprite ringSprite = CreateRadialSprite(128, 128);
            
            // Outer golden alchemical bezel (rotates dynamically)
            threatCompassGo = new GameObject("ThreatCompass", typeof(RectTransform), typeof(Image));
            threatCompassGo.transform.SetParent(threatMeterGo.transform, false);
            var tcRect = threatCompassGo.GetComponent<RectTransform>();
            tcRect.anchorMin = Vector2.zero;
            tcRect.anchorMax = Vector2.one;
            tcRect.offsetMin = new Vector2(-10, -10);
            tcRect.offsetMax = new Vector2(10, 10);
            var tcImg = threatCompassGo.GetComponent<Image>();
            tcImg.sprite = CreateAlchemicalCompassBezel(144, 144);
            tcImg.color = new Color(1.0f, 0.85f, 0.1f, 0.95f);
            
            var fillGo = new GameObject("ThreatFill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(threatMeterGo.transform, false);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            
            threatMeterFill = fillGo.GetComponent<Image>();
            threatMeterFill.sprite = ringSprite;
            threatMeterFill.type = Image.Type.Filled;
            threatMeterFill.fillMethod = Image.FillMethod.Radial360;
            threatMeterFill.fillOrigin = (int)Image.Origin360.Top;
            threatMeterFill.fillClockwise = true;
            threatMeterFill.color = new Color(0.9f, 0.1f, 0.1f, 0.95f);
            threatMeterFill.fillAmount = 0f;
            
            // Central warning alchemical eye
            var eyeGo = new GameObject("ThreatEye", typeof(RectTransform), typeof(Image));
            eyeGo.transform.SetParent(threatMeterGo.transform, false);
            var eyeRect = eyeGo.GetComponent<RectTransform>();
            eyeRect.anchorMin = new Vector2(0.22f, 0.22f);
            eyeRect.anchorMax = new Vector2(0.78f, 0.78f);
            eyeRect.offsetMin = eyeRect.offsetMax = Vector2.zero;
            threatEyeGlyphImg = eyeGo.GetComponent<Image>();
            threatEyeGlyphImg.sprite = CreateEyeGlyphSprite(64, 64);
            threatEyeGlyphImg.preserveAspect = true;
            threatEyeGlyphImg.color = new Color(1.0f, 0.2f, 0.05f, 0.85f);
            
            var lblGo = new GameObject("ThreatLabel", typeof(RectTransform));
            lblGo.transform.SetParent(threatMeterGo.transform, false);
            var lblRect = lblGo.GetComponent<RectTransform>();
            lblRect.anchorMin = new Vector2(0.5f, 0f);
            lblRect.anchorMax = new Vector2(0.5f, 0f);
            lblRect.pivot = new Vector2(0.5f, 1f);
            lblRect.anchoredPosition = new Vector2(0, -12f);
            lblRect.sizeDelta = new Vector2(120, 25);
            
            var lblTxt = lblGo.gameObject.AddComponent<TextMeshProUGUI>();
            lblTxt.text = "THREAT LEVEL";
            lblTxt.font = GetTitleFont();
            lblTxt.fontSize = 11;
            lblTxt.fontStyle = FontStyles.Bold;
            lblTxt.alignment = TextAlignmentOptions.Center;
            lblTxt.color = new Color(0.95f, 0.8f, 0.2f, 0.95f);

            // 3. Setup Scroll of Thoth UI
            scrollUiGo = new GameObject("ScrollOfThothUI", typeof(RectTransform), typeof(Image));
            scrollUiGo.transform.SetParent(root, false);
            scrollUiRect = scrollUiGo.GetComponent<RectTransform>();
            scrollUiRect.anchorMin = scrollUiRect.anchorMax = new Vector2(0.5f, 0f);
            scrollUiRect.pivot = new Vector2(0.5f, 0f);
            scrollUiRect.anchoredPosition = new Vector2(0, 100f);
            scrollUiRect.sizeDelta = new Vector2(90, 90);
            
            var scrollBg = scrollUiGo.GetComponent<Image>();
            scrollBg.sprite = goldTrimmedButtonSprite != null ? goldTrimmedButtonSprite : obsidianSprite;
            scrollBg.type = Image.Type.Simple;
            
            var sIconGo = new GameObject("ScrollIcon", typeof(RectTransform), typeof(Image));
            sIconGo.transform.SetParent(scrollUiGo.transform, false);
            var sIconRect = sIconGo.GetComponent<RectTransform>();
            sIconRect.anchorMin = Vector2.zero;
            sIconRect.anchorMax = Vector2.one;
            sIconRect.offsetMin = sIconRect.offsetMax = new Vector2(15, 15);
            
            scrollUiIcon = sIconGo.GetComponent<Image>();
            scrollUiIcon.sprite = CreateProceduralScrollSprite();
            scrollUiIcon.preserveAspect = true;
            scrollUiGo.SetActive(false); // Hidden until collected

            // 4. Setup UI Particle System over Health Bar
            var particlesGo = new GameObject("HealthParticles", typeof(RectTransform));
            particlesGo.transform.SetParent(root, false);
            var partRect = particlesGo.GetComponent<RectTransform>();
            partRect.anchorMin = partRect.anchorMax = new Vector2(0, 1);
            partRect.pivot = new Vector2(0.5f, 0.5f);
            partRect.anchoredPosition = new Vector2(100, -90);
            partRect.sizeDelta = new Vector2(100, 100);
            
            var ps = particlesGo.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 1f;
            main.loop = false;
            main.startLifetime = 0.5f;
            main.startSpeed = 150f;
            main.startSize = 10f;
            main.startColor = new Color(1f, 0.85f, 0.2f, 1f);
            main.gravityModifier = 0.5f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.playOnAwake = false;
            
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 80) });
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 20f;
            
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.85f, 0.2f), 0f), new GradientColorKey(new Color(1f, 0.4f, 0f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);
            
            var psRend = particlesGo.GetComponent<ParticleSystemRenderer>();
            if (psRend != null)
            {
                psRend.material = new Material(Shader.Find("Sprites/Default"));
            }
            
            scrollUiParticles = particlesGo.AddComponent<Coffee.UIExtensions.UIParticle>();
            scrollUiParticles.scale = 1f;

            // 5. Setup Damage Screen Scratch Vignette Overlay
            var damageOverlayGo = new GameObject("DamageOverlay", typeof(RectTransform), typeof(Image));
            damageOverlayGo.transform.SetParent(root, false);
            damageOverlayGo.transform.SetAsFirstSibling(); // Draw in background of layout buttons
            
            var overlayRect = damageOverlayGo.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = overlayRect.offsetMax = Vector2.zero;
            
            damageOverlayImg = damageOverlayGo.GetComponent<Image>();
            damageOverlayImg.sprite = CreateClawVignetteTexture(256, 256);
            damageOverlayImg.color = new Color(1f, 1f, 1f, 0f);
            damageOverlayImg.raycastTarget = false;

            // Start alchemical bubble effects on HUD health and ammo bars
            if (hpBgBarGo != null && healthBarFill != null)
            {
                StartHUDBarBubbles(hpBgBarGo.transform, healthBarFill, new Vector2(208f, 22f), new Color(1.0f, 0.6f, 0.2f, 0.6f));
            }
            var amBgBarGo = GameObject.Find("MobileHUD_Root/AmmoPanel/AmBarBg");
            if (amBgBarGo != null && ammoBarFill != null)
            {
                StartHUDBarBubbles(amBgBarGo.transform, ammoBarFill, new Vector2(208f, 22f), new Color(0.95f, 0.82f, 0.12f, 0.6f));
            }
        }

        public void SplashHealthParticles()
        {
            if (scrollUiParticles != null)
            {
                scrollUiParticles.Play();
            }
        }

        private void UpdateAnimations()
        {
            UpdateThreatMeter();
            UpdateScrollSlide();
            UpdateScrollHeartbeat();
        }

        private void UpdateThreatMeter()
        {
            if (threatMeterFill == null) return;
            
            float threat = 0f;
            var hm = GameObject.FindAnyObjectByType<TheAlchemistsCrypt.AI.HiveMindManager>();
            if (hm != null)
            {
                threat = hm.AggressionScore;
            }
            
            if (threatCompassGo != null)
            {
                float rotSpeed = Mathf.Lerp(10f, 60f, threat);
                threatCompassGo.transform.Rotate(0, 0, rotSpeed * Time.deltaTime);
            }
            
            threatMeterFill.fillAmount = Mathf.Lerp(threatMeterFill.fillAmount, threat, Time.deltaTime * 5f);
            
            Color threatColor = Color.Lerp(new Color(0.95f, 0.6f, 0.1f, 0.95f), new Color(0.95f, 0.05f, 0.05f, 0.95f), threat);
            threatMeterFill.color = threatColor;
            
            if (threatEyeGlyphImg != null)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.Lerp(2f, 8f, threat));
                threatEyeGlyphImg.color = Color.Lerp(new Color(0.95f, 0.6f, 0.1f, 0.6f), new Color(0.95f, 0.05f, 0.05f, 1.0f), threat) * (0.5f + 0.5f * pulse);
            }
            
            if (threat > 0.6f && Time.frameCount % 180 == 0)
            {
                threatMeterGo.transform.DOKill();
                threatMeterGo.transform.localScale = Vector3.one;
                threatMeterGo.transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0f), 0.4f, 4, 0.8f);
            }
        }

        private void UpdateScrollSlide()
        {
            if (TheAlchemistsCrypt.Gameplay.EscapeManager.Instance == null) return;
            if (scrollUiGo == null || scrollUiRect == null) return;

            bool hasScroll = TheAlchemistsCrypt.Gameplay.EscapeManager.Instance.hasKey;
            if (hasScroll != lastHasScroll)
            {
                lastHasScroll = hasScroll;
                if (hasScroll)
                {
                    scrollUiGo.SetActive(true);
                    scrollUiRect.anchoredPosition = new Vector2(scrollUiRect.anchoredPosition.x, -150f);
                    scrollUiRect.localScale = Vector3.zero;
                    
                    scrollUiRect.DOAnchorPosY(100f, 1f).SetEase(Ease.OutBack);
                    scrollUiRect.DOScale(1f, 1f).SetEase(Ease.OutBack).OnComplete(() => {
                        if (scrollUiParticles != null) scrollUiParticles.Play();
                    });
                }
                else
                {
                    scrollUiGo.SetActive(false);
                }
            }
        }

        private void UpdateScrollHeartbeat()
        {
            if (scrollUiGo == null || !scrollUiGo.activeSelf) return;

            float threat = 0f;
            var hm = GameObject.FindAnyObjectByType<TheAlchemistsCrypt.AI.HiveMindManager>();
            if (hm != null)
            {
                threat = hm.AggressionScore;
            }

            float speed = Mathf.Lerp(1.2f, 0.25f, threat);
            if (Mathf.Abs(threat - lastThreatVal) > 0.05f)
            {
                lastThreatVal = threat;
                scrollUiIcon.transform.DOKill();
                scrollUiIcon.transform.localScale = Vector3.one;
                scrollUiIcon.transform.DOScale(1.25f, speed)
                    .SetEase(Ease.InOutQuad)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        }

        private Sprite CreateRadialSprite(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            float centerX = width / 2f;
            float centerY = height / 2f;
            float radius = Mathf.Min(width, height) / 2f;
            float innerRadius = radius * 0.7f;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                    if (dist <= radius && dist >= innerRadius)
                    {
                        tex.SetPixel(x, y, Color.white);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateProceduralScrollSprite()
        {
            int w = 128;
            int h = 128;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            
            Color parchment = new Color(0.85f, 0.75f, 0.55f, 1f);
            Color darkWood = new Color(0.35f, 0.2f, 0.1f, 1f);
            Color goldNode = new Color(0.95f, 0.8f, 0.2f, 1f);
            Color ink = new Color(0.2f, 0.15f, 0.1f, 0.9f);
            Color transparent = new Color(0, 0, 0, 0);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float px = x / (float)w;
                    float py = y / (float)h;

                    if (px < 0.15f || px > 0.85f || py < 0.2f || py > 0.8f)
                    {
                        if ((px >= 0.1f && px < 0.15f) || (px > 0.85f && px <= 0.9f))
                        {
                            if (py >= 0.15f && py <= 0.85f)
                            {
                                tex.SetPixel(x, y, darkWood);
                            }
                            else
                            {
                                tex.SetPixel(x, y, transparent);
                            }
                        }
                        else if (((px >= 0.08f && px < 0.1f) || (px > 0.9f && px <= 0.92f)) && ((py >= 0.1f && py < 0.15f) || (py > 0.85f && py <= 0.9f)))
                        {
                            tex.SetPixel(x, y, goldNode);
                        }
                        else
                        {
                            tex.SetPixel(x, y, transparent);
                        }
                    }
                    else
                    {
                        Color pixelColor = parchment;
                        float grain = Mathf.PerlinNoise(px * 15f, py * 45f) * 0.12f;
                        pixelColor = Color.Lerp(pixelColor, Color.black, grain);

                        if (py > 0.3f && py < 0.7f && px > 0.2f && px < 0.8f)
                        {
                            float linePattern = Mathf.Sin(py * 35f);
                            float dashPattern = Mathf.Sin(px * 40f + py * 10f);
                            if (linePattern > 0.6f && dashPattern > -0.2f)
                            {
                                pixelColor = Color.Lerp(pixelColor, ink, 0.85f);
                            }
                        }
                        tex.SetPixel(x, y, pixelColor);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateClawVignetteTexture(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color transparent = new Color(0, 0, 0, 0);
            Color darkRed = new Color(0.6f, 0.05f, 0.05f, 0.8f);
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = (x - width / 2f) / (width / 2f);
                    float ny = (y - height / 2f) / (height / 2f);
                    float dist = Mathf.Sqrt(nx * nx + ny * ny);
                    
                    float vignette = Mathf.Clamp01((dist - 0.5f) / 0.5f);
                    
                    float angle = Mathf.Atan2(ny, nx);
                    float scratchPattern = Mathf.Sin(angle * 24f) * Mathf.Cos(dist * 12f);
                    float scratchFactor = (scratchPattern > 0.7f) ? 0.3f : 0.0f;
                    
                    float finalAlpha = Mathf.Clamp01(vignette * 0.8f + scratchFactor * vignette);
                    
                    if (finalAlpha > 0.01f)
                    {
                        Color c = Color.Lerp(transparent, darkRed, finalAlpha);
                        tex.SetPixel(x, y, c);
                    }
                    else
                    {
                        tex.SetPixel(x, y, transparent);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateAlchemicalCompassBezel(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            float cx = w / 2f;
            float cy = h / 2f;
            float rOuter = w / 2f - 2f;
            float rInner = rOuter - 6f;
            
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    
                    if ((dist >= rInner && dist <= rOuter) || (dist >= rInner - 12f && dist <= rInner - 10f))
                    {
                        tex.SetPixel(x, y, Color.white);
                    }
                    else if (dist >= rInner - 10f && dist <= rInner)
                    {
                        float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                        if (Mathf.Abs(angle % 30f) < 2f || Mathf.Abs((angle + 15f) % 90f) < 1f)
                        {
                            tex.SetPixel(x, y, Color.white);
                        }
                        else
                        {
                            tex.SetPixel(x, y, Color.clear);
                        }
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateEyeGlyphSprite(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            float cx = w / 2f;
            float cy = h / 2f;
            Color eyeColor = new Color(1.0f, 0.85f, 0.3f, 1f); // Warm gold
            
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - cx) / (w * 0.45f);
                    float dy = (y - cy) / (h * 0.25f);
                    
                    // Outer almond shape: top curve and bottom curve
                    float almondTop = 1f - (dx * dx);
                    float almondBottom = -almondTop;
                    
                    bool insideAlmond = dy < almondTop && dy > almondBottom && Mathf.Abs(dx) < 1f;
                    bool onAlmondBorder = Mathf.Abs(dy - almondTop) < 0.15f || Mathf.Abs(dy - almondBottom) < 0.15f;
                    
                    // Central pupil
                    float pdx = (x - cx) / (w * 0.15f);
                    float pdy = (y - cy) / (h * 0.15f);
                    bool pupil = (pdx * pdx + pdy * pdy) < 1.0f;
                    
                    // Tear duct (left down tail)
                    float tearX = -0.4f;
                    bool tearDuct = false;
                    if (dx > tearX - 0.1f && dx < tearX + 0.1f && dy < 0f && dy > -1.2f)
                    {
                        float distToTearLine = Mathf.Abs(dx - tearX);
                        if (distToTearLine < 0.08f * (1.2f + dy))
                        {
                            tearDuct = true;
                        }
                    }
                    
                    // Spiral tail (right down curl)
                    bool spiralTail = false;
                    if (dx > 0f && dy < 0f)
                    {
                        float targetDy = -0.4f - 0.3f * Mathf.Sin(dx * 5f);
                        if (Mathf.Abs(dy - targetDy) < 0.12f && dx < 0.7f)
                        {
                            spiralTail = true;
                        }
                    }
                    
                    if (pupil || (onAlmondBorder && insideAlmond) || tearDuct || spiralTail)
                    {
                        tex.SetPixel(x, y, eyeColor);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateCircularSandstoneMedallionSprite(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float cx = size / 2f;
            float cy = size / 2f;
            float rOuter = size / 2f - 2f;
            float rInner = rOuter - 6f;
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    
                    if (dist > rOuter)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else if (dist <= rOuter && dist > rInner)
                    {
                        float noise = (float)Mathf.PerlinNoise(x * 0.15f, y * 0.15f) * 0.2f - 0.1f;
                        tex.SetPixel(x, y, new Color(0.95f, 0.8f, 0.2f, 0.95f) * (1.0f + noise));
                    }
                    else if (dist <= rInner)
                    {
                        float noise = (float)Mathf.PerlinNoise(x * 0.25f, y * 0.25f) * 0.12f;
                        tex.SetPixel(x, y, new Color(0.04f, 0.04f, 0.04f, 0.85f) * (1.0f + noise));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
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

        private void StartHUDBarBubbles(Transform parent, Image fillImage, Vector2 barSize, Color bubbleColor)
        {
            StartCoroutine(GenerateHUDBubbles(parent, fillImage, barSize, bubbleColor));
        }

        private IEnumerator GenerateHUDBubbles(Transform parent, Image fillImage, Vector2 barSize, Color bubbleColor)
        {
            var bubbleSprite = CreateCircleSprite(16);
            while (parent != null)
            {
                float fillTarget = (fillImage != null) ? fillImage.rectTransform.anchorMax.x : 0f;
                if (fillTarget > 0.02f)
                {
                    var bubbleGo = new GameObject("Bubble", typeof(RectTransform), typeof(Image));
                    bubbleGo.transform.SetParent(parent, false);
                    var rect = bubbleGo.GetComponent<RectTransform>();
                    
                    float fillWidth = fillTarget * barSize.x;
                    // Position X relative to the filled portion of the bar
                    float randomX = Random.Range(-barSize.x / 2f, -barSize.x / 2f + fillWidth);
                    rect.anchoredPosition = new Vector2(randomX, -barSize.y / 2f);
                    
                    float bubbleSize = Random.Range(3f, 7f);
                    rect.sizeDelta = new Vector2(bubbleSize, bubbleSize);
                    
                    var img = bubbleGo.GetComponent<Image>();
                    img.sprite = bubbleSprite;
                    if (fillImage != null) {
                        img.color = new Color(fillImage.color.r, fillImage.color.g, fillImage.color.b, 0.65f);
                    } else {
                        img.color = bubbleColor;
                    }
                    
                    StartCoroutine(AnimateHUDBubble(rect, img, barSize.y));
                }
                
                yield return new WaitForSeconds(Random.Range(0.12f, 0.28f));
            }
        }

        private IEnumerator AnimateHUDBubble(RectTransform bubbleRect, Image bubbleImg, float barHeight)
        {
            float duration = Random.Range(0.6f, 1.2f);
            float elapsed = 0f;
            Vector2 startPos = bubbleRect.anchoredPosition;
            float endY = barHeight / 2f + 3f;
            float driftWidth = Random.Range(-8f, 8f);
            float driftSpeed = Random.Range(1.5f, 3.5f);
            
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
    }
}
