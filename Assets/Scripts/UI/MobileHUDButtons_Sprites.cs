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
         private Sprite LoadSpriteFromResources(string path)
                {
                    Sprite s = Resources.Load<Sprite>(path);
                    if (s != null) return s;
                    Texture2D tex = Resources.Load<Texture2D>(path);
                    if (tex != null)
                    {
                        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    }
                    return null;
                }



                private Sprite LoadThemedSprite(string spriteName, string fallbackResourcePath)
                {
                    Sprite result = LoadSpriteFromResources("egypt_themed_icons/" + spriteName);
                    if (result == null) result = LoadSpriteFromResources(fallbackResourcePath);
                    return result;
                }



                private Sprite LoadSlicedSpriteFromResources(string path, Vector4 border)
                {
                    Sprite s = Resources.Load<Sprite>(path);
                    if (s != null && s.border != Vector4.zero) return s; 
                    Texture2D tex = Resources.Load<Texture2D>(path);
                    if (tex != null)
                    {
                        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
                    }
                    return null;
                }



                private void LoadSprites()
                {
                    joystickRingSprite = LoadThemedSprite("joystick_outer", "egypt_themed_icons/joystick_outer");
                    joystickKnobSprite = LoadThemedSprite("joystick_knob", "egypt_themed_icons/joystick_knob");
                    
                    fireIcon = LoadSpriteFromResources("egypt_themed_icons_generated/icon_fire");
                    if (fireIcon == null) fireIcon = LoadThemedSprite("fire", "UI/Icons/Inspiration/bullet");
                    
                    reloadIcon = LoadSpriteFromResources("egypt_themed_icons_generated/icon_reload");
                    if (reloadIcon == null) reloadIcon = LoadThemedSprite("reload_ammo", "UI/Icons/Inspiration/reload");
                    
                    swapIcon = LoadSpriteFromResources("egypt_themed_icons_generated/icon_swap");
                    if (swapIcon == null) swapIcon = LoadThemedSprite("swap_weapon", "UI/Icons/icon_swap");
                    
                    sprintIcon = LoadSpriteFromResources("egypt_themed_icons_generated/icon_sprint");
                    if (sprintIcon == null) sprintIcon = LoadThemedSprite("sprint", "UI/Icons/icon_sprint");
                    
                    jumpIcon = LoadSpriteFromResources("egypt_themed_icons_generated/icon_jump");
                    if (jumpIcon == null) jumpIcon = LoadThemedSprite("jump", "UI/Icons/icon_jump");

                    healthIconSprite = LoadSpriteFromResources("egyptian_items/health_icon");
                    if (healthIconSprite == null) healthIconSprite = CreateProceduralHealthIconSprite();

                    sulphurIconSprite = LoadSpriteFromResources("egyptian_items/sulphur");
                    if (sulphurIconSprite == null) sulphurIconSprite = CreateProceduralSulfurSprite();

                    mercuryIconSprite = LoadSpriteFromResources("egyptian_items/mercury");
                    if (mercuryIconSprite == null) mercuryIconSprite = CreateProceduralMercurySprite();

                    saltIconSprite = LoadSpriteFromResources("egyptian_items/salt");
                    if (saltIconSprite == null) saltIconSprite = CreateProceduralSaltSprite();

                    welcomeBgSprite = LoadSpriteFromResources("egyptian_items/GameStartImage");
                    focusIcon = LoadThemedSprite("focus_icon", "UI/Icons/icon_focus");
                    if (focusIcon == null) focusIcon = CreateProceduralFocusIconSprite();
                }



                private void GenerateProceduralSprites()
                {
                    obsidianSprite = LoadSpriteFromResources("egypt_themed_icons_generated/obsidian_texture");
                    if (obsidianSprite == null) obsidianSprite = CreateObsidianSprite();

                    charcoalSprite = CreateCharcoalSprite(260, 180);
                    goldGradientSprite = CreateGoldenGradientSprite();

                    if (joystickRingSprite == null) joystickRingSprite = LoadSpriteFromResources("egypt_themed_icons_generated/joystick_ring");
                    if (joystickRingSprite == null) joystickRingSprite = CreateRingSprite();

                    if (joystickKnobSprite == null) joystickKnobSprite = LoadSpriteFromResources("egypt_themed_icons_generated/joystick_knob");
                    if (joystickKnobSprite == null) joystickKnobSprite = CreateKnobSprite();

                    sandstoneFrameSprite = LoadSlicedSpriteFromResources("egypt_themed_icons_generated/sandstone_frame", new Vector4(40, 40, 40, 40));
                    if (sandstoneFrameSprite == null) sandstoneFrameSprite = CreateSlicedSandstoneFrameSprite();

                    goldTrimmedButtonSprite = LoadSlicedSpriteFromResources("egypt_themed_icons_generated/btn_gold_trim", new Vector4(12, 12, 12, 12));
                    if (goldTrimmedButtonSprite == null) goldTrimmedButtonSprite = CreateSlicedGoldTrimmedButtonSprite();

                    orangeGlowSprite = CreateSlicedEnergyGlowSprite(new Color(1.0f, 0.55f, 0.05f));
                    cyanGlowSprite = CreateSlicedEnergyGlowSprite(new Color(0.0f, 0.9f, 1.0f));

                    sulfurBarSprite = CreateAlchemicalBarSprite(new Color(0.95f, 0.55f, 0.05f), new Color(1f, 0.85f, 0.1f));
                    mercuryBarSprite = CreateAlchemicalBarSprite(new Color(0.1f, 0.5f, 0.8f), new Color(0.4f, 0.9f, 0.95f));
                    saltBarSprite = CreateAlchemicalBarSprite(new Color(0.9f, 0.7f, 0.2f), new Color(1f, 1f, 1f));
                    punchBarSprite = CreateAlchemicalBarSprite(new Color(0.4f, 0.02f, 0.02f), new Color(0.8f, 0.05f, 0.05f));
                }



                private Sprite CreateAlchemicalBarSprite(Color startCol, Color endCol)
                {
                    int w = 420, h = 28;
                    Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                    for (int y = 0; y < h; y++) {
                        for (int x = 0; x < w; x++) {
                            float t = (float)x / w;
                            Color col = Color.Lerp(startCol, endCol, t);
                            tex.SetPixel(x, y, new Color(col.r, col.g, col.b, 0.95f));
                        }
                    }
                    tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
                }



                private Sprite CreateFireSymbolSprite(int size)
                {
                    Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                    float half = size * 0.5f;
                    Color gold = new Color(0.95f, 0.8f, 0.2f, 0.95f);
                    for (int y = 0; y < size; y++) {
                        for (int x = 0; x < size; x++) {
                            float px = (x - half) / half;
                            float py = (y - half) / half;
                            float widthAtY = (1f - py) * 0.5f;
                            if (py >= -0.6f && py <= 0.7f && Mathf.Abs(px) <= widthAtY) {
                                float distToEdge = Mathf.Min(
                                    Mathf.Abs(Mathf.Abs(px) - widthAtY),
                                    Mathf.Abs(py - -0.6f)
                                );
                                if (distToEdge < 0.18f || (py >= 0.1f && py <= 0.2f)) {
                                    tex.SetPixel(x, y, gold);
                                } else {
                                    tex.SetPixel(x, y, Color.clear);
                                }
                            } else {
                                tex.SetPixel(x, y, Color.clear);
                            }
                        }
                    }
                    tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
                }



                private Sprite CreateReloadSymbolSprite(int size)
                {
                    Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                    float half = size * 0.5f;
                    Color gold = new Color(0.95f, 0.8f, 0.2f, 0.95f);
                    for (int y = 0; y < size; y++) {
                        for (int x = 0; x < size; x++) {
                            float dx = (x - half) / half;
                            float dy = (y - half) / half;
                            float dist = Mathf.Sqrt(dx * dx + dy * dy);
                            if (dist >= 0.5f && dist <= 0.75f) {
                                bool gap = (dx > 0.6f && dy > -0.2f && dy < 0.2f) || (dx < -0.6f && dy > -0.2f && dy < 0.2f);
                                if (!gap) tex.SetPixel(x, y, gold);
                                else tex.SetPixel(x, y, Color.clear);
                            }
                            else if ((dx > 0.4f && dx < 0.85f && dy > 0.2f && dy < 0.45f && Mathf.Abs(dx - 0.62f) < (0.45f - dy)) ||
                                     (dx < -0.4f && dx > -0.85f && dy < -0.2f && dy > -0.45f && Mathf.Abs(dx + 0.62f) < (dy + 0.45f))) {
                                tex.SetPixel(x, y, gold);
                            }
                            else {
                                tex.SetPixel(x, y, Color.clear);
                            }
                        }
                    }
                    tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
                }



                private Sprite CreateSwapSymbolSprite(int size)
                {
                    Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                    float half = size * 0.5f;
                    Color gold = new Color(0.95f, 0.8f, 0.2f, 0.95f);
                    for (int y = 0; y < size; y++) {
                        for (int x = 0; x < size; x++) {
                            float px = (x - half) / half;
                            float py = (y - half) / half;
                            bool line1 = Mathf.Abs(px - py) < 0.08f;
                            bool line2 = Mathf.Abs(px + py) < 0.08f;
                            if (line1 || line2) tex.SetPixel(x, y, gold);
                            else tex.SetPixel(x, y, Color.clear);
                        }
                    }
                    tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
                }



                private Sprite CreateSprintSymbolSprite(int size)
                {
                    Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                    float half = size * 0.5f;
                    Color gold = new Color(0.95f, 0.8f, 0.2f, 0.95f);
                    for (int y = 0; y < size; y++) {
                        for (int x = 0; x < size; x++) {
                            float px = (x - half) / half;
                            float py = (y - half) / half;
                            bool chevron1 = Mathf.Abs(py - (px * 0.5f)) < 0.09f && px >= -0.7f && px <= 0.7f;
                            bool chevron2 = Mathf.Abs(py - 0.2f - (px * 0.5f)) < 0.09f && px >= -0.7f && px <= 0.7f;
                            bool chevron3 = Mathf.Abs(py - 0.4f - (px * 0.5f)) < 0.09f && px >= -0.7f && px <= 0.7f;
                            if (chevron1 || chevron2 || chevron3) tex.SetPixel(x, y, gold);
                            else tex.SetPixel(x, y, Color.clear);
                        }
                    }
                    tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
                }



                private Sprite CreateJumpSymbolSprite(int size)
                {
                    Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                    float half = size * 0.5f;
                    Color gold = new Color(0.95f, 0.8f, 0.2f, 0.95f);
                    for (int y = 0; y < size; y++) {
                        for (int x = 0; x < size; x++) {
                            float px = (x - half) / half;
                            float py = (y - half) / half;
                            bool chevron1 = Mathf.Abs(py - (1.1f - Mathf.Abs(px))) < 0.09f && Mathf.Abs(px) <= 0.75f;
                            bool chevron2 = Mathf.Abs(py + 0.3f - (1.1f - Mathf.Abs(px))) < 0.09f && Mathf.Abs(px) <= 0.75f;
                            if (chevron1 || chevron2) tex.SetPixel(x, y, gold);
                            else tex.SetPixel(x, y, Color.clear);
                        }
                    }
                    tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
                }



                private Sprite CreateBorderSprite(int w, int h, int thickness, Color borderCol)
                {
                    Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                    for (int y = 0; y < h; y++) {
                        for (int x = 0; x < w; x++) {
                            if (x < thickness || x >= w - thickness || y < thickness || y >= h - thickness) tex.SetPixel(x, y, borderCol);
                            else tex.SetPixel(x, y, Color.clear);
                        }
                    }
                    tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
                }



                private Sprite CreateSolidCircleSprite(int s, Color col)
                {
                    Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
                    for (int y = 0; y < s; y++) {
                        for (int x = 0; x < s; x++) {
                            float dx = (float)(x - s / 2) / (s / 2); float dy = (float)(y - s / 2) / (s / 2);
                            float dist = Mathf.Sqrt(dx * dx + dy * dy);
                            if (dist <= 1f) {
                                float alpha = Mathf.Clamp01((1f - dist) * 10f);
                                tex.SetPixel(x, y, new Color(col.r, col.g, col.b, col.a * alpha));
                            } else tex.SetPixel(x, y, Color.clear);
                        }
                    }
                    tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
                }



                private Sprite CreateHealthBarFillSprite(int w, int h)
                {
                    Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                    for (int y = 0; y < h; y++) {
                        float vertRatio = (float)y / h;
                        for (int x = 0; x < w; x++) {
                            float t = (float)x / w;
                            Color baseCol = Color.Lerp(new Color(0.5f, 0.02f, 0.02f, 0.95f), new Color(0.95f, 0.15f, 0.15f, 0.95f), t);
                            // Add a horizontal highlight/sheen on the top half
                            if (vertRatio > 0.6f)
                            {
                                baseCol = Color.Lerp(baseCol, Color.white, (vertRatio - 0.6f) * 0.4f);
                            }
                            tex.SetPixel(x, y, baseCol);
                        }
                    }
                    tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
                }



                private Sprite CreateFramedBarSprite(int w, int h, Color borderColor, Color fillColor, int borderWidth)
                {
                    Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            bool isBorder = (x < borderWidth) || (x >= w - borderWidth) || (y < borderWidth) || (y >= h - borderWidth);
                            if (isBorder)
                            {
                                // Sandstone texture with noise
                                float noise = (float)Mathf.PerlinNoise(x * 0.15f, y * 0.15f) * 0.2f - 0.1f;
                                float grain = (float)Mathf.PerlinNoise(x * 0.8f, y * 0.8f) * 0.1f;
                                float factor = 1.0f + noise + grain;
                                
                                // Beautiful warm sandstone mixed with the gold border color
                                Color sandstoneBase = new Color(0.85f, 0.70f, 0.50f);
                                Color borderPixel = Color.Lerp(sandstoneBase, borderColor, 0.5f) * factor;
                                
                                // Every 15 horizontal or 10 vertical pixels, add a tiny darker runic crack/hieroglyphic notch
                                if ((x % 15 == 0 && y > 1 && y < h - 2) || (y % 10 == 0 && x > 1 && x < w - 2))
                                {
                                    borderPixel *= 0.65f; // Deep crack shadow
                                }
                                
                                tex.SetPixel(x, y, borderPixel);
                            }
                            else
                            {
                                tex.SetPixel(x, y, fillColor);
                            }
                        }
                    }
                    tex.Apply();
                    return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
                }



                private Sprite CreateSolidBarSprite(int w, int h, Color c)
                {
                    Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                    for (int y = 0; y < h; y++) {
                        for (int x = 0; x < w; x++) tex.SetPixel(x, y, c);
                    }
                    tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
                }



                private Sprite CreateProceduralFocusIconSprite()
                {
                    int size = 128;
                    var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            float u = (x - size * 0.5f) / (size * 0.5f);
                            float v = (y - size * 0.5f) / (size * 0.5f);
                            float dist = Mathf.Sqrt(u * u + v * v);
                            bool isCircle = Mathf.Abs(dist - 0.7f) < 0.05f || Mathf.Abs(dist - 0.3f) < 0.04f;
                            bool isCrosshair = (Mathf.Abs(u) < 0.05f && Mathf.Abs(v) > 0.15f && Mathf.Abs(v) < 0.8f) ||
                                               (Mathf.Abs(v) < 0.05f && Mathf.Abs(u) > 0.15f && Mathf.Abs(u) < 0.8f);
                            if (isCircle || isCrosshair)
                            {
                                tex.SetPixel(x, y, new Color(0.95f, 0.8f, 0.2f, 0.9f));
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


                 private Sprite CreateProceduralHealthIconSprite()
                {
                    int size = 64;
                    Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                    Color col = new Color(0.85f, 0.15f, 0.15f, 0.95f); // Beautiful Crimson Red
                    Color bg = new Color(0f, 0f, 0f, 0f);
                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            tex.SetPixel(x, y, bg);
                        }
                    }

                    float cx = size * 0.5f;

                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            float dx = x - cx;

                            // Oval loop on top (center: cx, size * 0.68f)
                            float loopY = (y - size * 0.68f) * 0.7f; // scale vertically to make it a bit taller/oval
                            float distToLoopCenter = Mathf.Sqrt(dx * dx + loopY * loopY);
                            if (distToLoopCenter >= 8f && distToLoopCenter <= 11f && y >= size * 0.48f)
                            {
                                tex.SetPixel(x, y, col);
                            }

                            // Vertical line of the Cross (from y = size*0.1 to size*0.5)
                            if (y >= size * 0.1f && y <= size * 0.5f)
                            {
                                if (Mathf.Abs(dx) <= 1.5f)
                                {
                                    tex.SetPixel(x, y, col);
                                }
                            }

                            // Horizontal bar of the Cross (at y = size*0.38)
                            if (y >= size * 0.38f && y <= size * 0.38f + 3f)
                            {
                                if (Mathf.Abs(dx) <= size * 0.22f)
                                {
                                    tex.SetPixel(x, y, col);
                                }
                            }
                        }
                    }
                    tex.Apply();
                    return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
                }




                private Sprite CreateProceduralSulfurSprite()
                {
                    int size = 64;
                    Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                    Color col = new Color(0.95f, 0.55f, 0.05f, 0.95f);
                    Color bg = new Color(0f, 0f, 0f, 0f);
                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            tex.SetPixel(x, y, bg);
                        }
                    }

                    float cx = size * 0.5f;

                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            float dx = x - cx;

                            // Triangle
                            if (y >= size * 0.45f && y <= size * 0.85f)
                            {
                                float triWidth = (size * 0.85f - y) * 0.5f;
                                if (Mathf.Abs(dx) <= triWidth && Mathf.Abs(dx) >= triWidth - 3f)
                                {
                                    tex.SetPixel(x, y, col);
                                }
                                if (y >= size * 0.45f && y <= size * 0.45f + 3f && Mathf.Abs(dx) <= (size * 0.85f - y) * 0.5f)
                                {
                                    tex.SetPixel(x, y, col);
                                }
                            }

                            // Vertical line for Cross
                            if (y >= size * 0.1f && y <= size * 0.45f)
                            {
                                if (Mathf.Abs(dx) <= 1.5f)
                                {
                                    tex.SetPixel(x, y, col);
                                }
                            }

                            // Horizontal bar for Cross
                            if (y >= size * 0.25f && y <= size * 0.25f + 3f)
                            {
                                if (Mathf.Abs(dx) <= size * 0.2f)
                                {
                                    tex.SetPixel(x, y, col);
                                }
                            }
                        }
                    }
                    tex.Apply();
                    return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
                }



                private Sprite CreateProceduralMercurySprite()
                {
                    int size = 64;
                    Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                    Color col = new Color(0.1f, 0.75f, 0.95f, 0.95f);
                    Color bg = new Color(0f, 0f, 0f, 0f);
                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            tex.SetPixel(x, y, bg);
                        }
                    }

                    float cx = size * 0.5f;

                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            float dx = x - cx;

                            // Central Circle (center: cx, size * 0.45f)
                            float circleY = y - size * 0.45f;
                            float distToCircleCenter = Mathf.Sqrt(dx * dx + circleY * circleY);
                            if (distToCircleCenter >= 9f && distToCircleCenter <= 12f)
                            {
                                tex.SetPixel(x, y, col);
                            }

                            // Horns/Crescent on top (center: cx, size * 0.75f, radius 10, only y <= size * 0.75f)
                            float crescentY = y - size * 0.75f;
                            float distToCrescentCenter = Mathf.Sqrt(dx * dx + crescentY * crescentY);
                            if (distToCrescentCenter >= 9f && distToCrescentCenter <= 12f && y <= size * 0.75f && y >= size * 0.58f)
                            {
                                tex.SetPixel(x, y, col);
                            }

                            // Vertical line for Cross below circle (from y = size*0.1 to size*0.3)
                            if (y >= size * 0.1f && y <= size * 0.31f)
                            {
                                if (Mathf.Abs(dx) <= 1.5f)
                                {
                                    tex.SetPixel(x, y, col);
                                }
                            }

                            // Horizontal bar for Cross (at y = size*0.2)
                            if (y >= size * 0.2f && y <= size * 0.2f + 3f)
                            {
                                if (Mathf.Abs(dx) <= size * 0.15f)
                                {
                                    tex.SetPixel(x, y, col);
                                }
                            }
                        }
                    }
                    tex.Apply();
                    return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
                }



                private Sprite CreateProceduralSaltSprite()
                {
                    int size = 64;
                    Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                    Color col = new Color(0.95f, 0.95f, 0.95f, 0.95f);
                    Color bg = new Color(0f, 0f, 0f, 0f);
                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            tex.SetPixel(x, y, bg);
                        }
                    }

                    float cx = size * 0.5f;
                    float cy = size * 0.5f;

                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            float dx = x - cx;
                            float dy = y - cy;

                            // Circle (radius 18 pixels)
                            float distToCircleCenter = Mathf.Sqrt(dx * dx + dy * dy);
                            if (distToCircleCenter >= 15f && distToCircleCenter <= 18f)
                            {
                                tex.SetPixel(x, y, col);
                            }

                            // Horizontal line across the circle (inside the circle)
                            if (y >= size * 0.5f - 1.5f && y <= size * 0.5f + 1.5f)
                            {
                                if (Mathf.Abs(dx) <= 17f)
                                {
                                    tex.SetPixel(x, y, col);
                                }
                            }
                        }
                    }
                    tex.Apply();
                    return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
                }



                private Sprite CreateProceduralGradientSprite(int w, int h, Color innerColor, Color outerColor)
                {
                    Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                    float cx = w * 0.5f;
                    float cy = h * 0.5f;
                    float maxDist = Mathf.Sqrt(cx * cx + cy * cy);
                    for (int y = 0; y < h; y++) {
                        for (int x = 0; x < w; x++) {
                            float dx = x - cx;
                            float dy = y - cy;
                            float dist = Mathf.Sqrt(dx * dx + dy * dy);
                            float t = Mathf.Clamp01(dist / maxDist);
                            tex.SetPixel(x, y, Color.Lerp(innerColor, outerColor, t));
                        }
                    }
                    tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
                }



                private Sprite CreateObsidianSprite()
                {
                    int width = 128, height = 128;
                    Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    for (int y = 0; y < height; y++) {
                        float t = (float)y / height;
                        Color obsColor = Color.Lerp(new Color(0.08f, 0.08f, 0.08f, 0.95f), new Color(0.2f, 0.2f, 0.2f, 0.95f), t);
                        for (int x = 0; x < width; x++) {
                            float dx = (float)(x - width / 2) / (width / 2); float dy = (float)(y - height / 2) / (height / 2);
                            float dist = Mathf.Sqrt(dx * dx + dy * dy);
                            if (dist <= 1f) {
                                float alpha = Mathf.Clamp01((1f - dist) * 10f);
                                tex.SetPixel(x, y, new Color(obsColor.r, obsColor.g, obsColor.b, obsColor.a * alpha));
                            } else tex.SetPixel(x, y, Color.clear);
                        }
                    }
                    tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
                }



                private Sprite CreateGoldenGradientSprite()
                {
                    int width = 128, height = 128;
                    Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    for (int y = 0; y < height; y++) {
                        float t = (float)y / height;
                        Color goldColor = Color.Lerp(new Color(0.85f, 0.6f, 0.1f, 0.95f), new Color(1f, 0.85f, 0.3f, 0.95f), t);
                        for (int x = 0; x < width; x++) {
                            float dx = (float)(x - width / 2) / (width / 2); float dy = (float)(y - height / 2) / (height / 2);
                            float dist = Mathf.Sqrt(dx * dx + dy * dy);
                            if (dist <= 1f) {
                                float alpha = Mathf.Clamp01((1f - dist) * 10f);
                                tex.SetPixel(x, y, new Color(goldColor.r, goldColor.g, goldColor.b, goldColor.a * alpha));
                            } else tex.SetPixel(x, y, Color.clear);
                        }
                    }
                    tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
                }



                private Sprite CreateSlicedSandstoneFrameSprite()
                {
                    int size = 256;
                    Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                    int borderThickness = 40;
                    Color sandColor = new Color(0.78f, 0.62f, 0.40f, 0.90f); // Warm sandstone
                    Color sandShadow = new Color(0.55f, 0.40f, 0.22f, 0.95f); // Shadow color for bevel/cracks
                    Color sandHighlight = new Color(0.92f, 0.82f, 0.65f, 0.95f); // Highlight color
                    
                    for (int y = 0; y < size; y++) {
                        for (int x = 0; x < size; x++) {
                            bool isBorder = (x < borderThickness || x >= size - borderThickness || y < borderThickness || y >= size - borderThickness);
                            if (isBorder) {
                                float grain = UnityEngine.Random.Range(-0.06f, 0.06f);
                                Color pixelCol = sandColor;
                                pixelCol.r += grain; pixelCol.g += grain; pixelCol.b += grain;
                                
                                if (y >= size - 6 || x < 6) {
                                    pixelCol = Color.Lerp(pixelCol, sandHighlight, 0.6f);
                                }
                                else if (y < 6 || x >= size - 6) {
                                    pixelCol = Color.Lerp(pixelCol, sandShadow, 0.6f);
                                }
                                
                                bool crack1 = Mathf.Abs((x - 50) + (y - (size - 50))) < 1.5f && (x < 80 && y > size - 80);
                                bool crack2 = Mathf.Abs((x - (size - 60)) + (y - 60)) < 1.5f && (x > size - 90 && y < 90);
                                
                                if (crack1 || crack2) {
                                    pixelCol = Color.Lerp(pixelCol, sandShadow, 0.8f);
                                }
                                
                                tex.SetPixel(x, y, pixelCol);
                            } else {
                                tex.SetPixel(x, y, new Color(0.08f, 0.05f, 0.05f, 0.45f));
                            }
                        }
                    }
                    tex.Apply();
                    return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(borderThickness, borderThickness, borderThickness, borderThickness));
                }



                private Sprite CreateSlicedGoldTrimmedButtonSprite()
                {
                    int size = 64;
                    Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                    Color sandColor = new Color(0.76f, 0.62f, 0.42f, 0.95f); // Rich warm sandstone
                    Color goldColor = new Color(0.95f, 0.8f, 0.2f, 0.98f); // Golden trim
                    Color shadowColor = new Color(0.48f, 0.35f, 0.2f, 0.95f); // Bevel shadow
                    Color highlightColor = new Color(0.9f, 0.82f, 0.68f, 0.95f); // Bevel highlight
                    
                    for (int y = 0; y < size; y++) {
                        for (int x = 0; x < size; x++) {
                            bool isGoldTrim = (x < 4 || x >= size - 4 || y < 4 || y >= size - 4);
                            bool isBevel = !isGoldTrim && (x < 7 || x >= size - 7 || y < 7 || y >= size - 7);
                            
                            if (isGoldTrim) {
                                tex.SetPixel(x, y, goldColor);
                            } else if (isBevel) {
                                if (y >= size - 7 || x < 7) {
                                    tex.SetPixel(x, y, highlightColor);
                                } else {
                                    tex.SetPixel(x, y, shadowColor);
                                }
                            } else {
                                float grain = UnityEngine.Random.Range(-0.04f, 0.04f);
                                Color pixelCol = sandColor;
                                pixelCol.r += grain; pixelCol.g += grain; pixelCol.b += grain;
                                tex.SetPixel(x, y, pixelCol);
                            }
                        }
                    }
                    tex.Apply();
                    return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(12, 12, 12, 12));
                }



                private Sprite CreateSlicedEnergyGlowSprite(Color glowColor)
                {
                    int size = 64;
                    Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                    for (int y = 0; y < size; y++) {
                        for (int x = 0; x < size; x++) {
                            float distToEdgeX = Mathf.Min(x, size - 1 - x);
                            float distToEdgeY = Mathf.Min(y, size - 1 - y);
                            float minDist = Mathf.Min(distToEdgeX, distToEdgeY);
                            
                            float t = Mathf.Clamp01(minDist / 12f);
                            Color c = glowColor;
                            c.a = (1f - t) * 0.7f;
                            tex.SetPixel(x, y, c);
                        }
                    }
                    tex.Apply();
                    return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(16, 16, 16, 16));
                }



                private Sprite CreateRingSprite()
                {
                    int width = 512, height = 512;
                    Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    for (int y = 0; y < height; y++) {
                        for (int x = 0; x < width; x++) {
                            float dx = (float)(x - width / 2) / (width / 2); 
                            float dy = (float)(y - height / 2) / (height / 2);
                            float dist = Mathf.Sqrt(dx * dx + dy * dy);
                            
                            // Outer glow halo (from dist 1.0 to 1.15)
                            if (dist > 1.0f && dist <= 1.15f) {
                                float glowAlpha = (1.15f - dist) / 0.15f;
                                tex.SetPixel(x, y, new Color(0.0f, 0.85f, 0.95f, glowAlpha * 0.25f));
                            }
                            // Ring structure (from dist 0.78 to 1.0)
                            else if (dist >= 0.78f && dist <= 1.0f) {
                                Color stoneCol = new Color(0.2f, 0.24f, 0.26f, 0.85f);
                                
                                float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                                if (angle < 0) angle += 360f;
                                
                                float sector = angle / 30f;
                                float fraction = sector - Mathf.Floor(sector);
                                
                                bool isGlyph = false;
                                if (dist >= 0.84f && dist <= 0.94f) {
                                    int sectorInt = Mathf.FloorToInt(sector);
                                    if (fraction > 0.15f && fraction < 0.85f) {
                                        if (sectorInt % 3 == 0) {
                                            isGlyph = Mathf.Abs(fraction - 0.5f) < 0.08f || Mathf.Abs(dist - 0.89f) < 0.02f;
                                        } else if (sectorInt % 3 == 1) {
                                            isGlyph = Mathf.Abs(fraction - 0.3f) < 0.08f || Mathf.Abs(fraction - 0.7f) < 0.08f;
                                        } else {
                                            isGlyph = Mathf.Abs(dist - 0.89f) < 0.04f && (fraction < 0.4f || fraction > 0.6f);
                                        }
                                    }
                                }
                                
                                if (isGlyph) {
                                    tex.SetPixel(x, y, new Color(0.1f, 0.95f, 1.0f, 1.0f));
                                } else {
                                    float rim = (dist > 0.96f || dist < 0.82f) ? 0.6f : 1.0f;
                                    tex.SetPixel(x, y, new Color(stoneCol.r * rim, stoneCol.g * rim, stoneCol.b * rim, stoneCol.a));
                                }
                            } else if (dist < 0.78f && dist >= 0.75f) {
                                tex.SetPixel(x, y, new Color(0.1f, 0.12f, 0.13f, 0.9f));
                            } else {
                                tex.SetPixel(x, y, Color.clear);
                            }
                        }
                    }
                    tex.Apply(); 
                    return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
                }



                private Sprite CreateKnobSprite()
                {
                    int width = 256, height = 256;
                    Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    for (int y = 0; y < height; y++) {
                        for (int x = 0; x < width; x++) {
                            float dx = (float)(x - width / 2) / (width / 2); 
                            float dy = (float)(y - height / 2) / (height / 2);
                            float dist = Mathf.Sqrt(dx * dx + dy * dy);
                            
                            if (dist <= 1.0f) {
                                float alpha = Mathf.Clamp01((1.0f - dist) * 12f);
                                
                                if (dist <= 0.25f) {
                                    float highlight = Mathf.Clamp01(1.0f - (Mathf.Sqrt((dx - 0.08f) * (dx - 0.08f) + (dy - 0.08f) * (dy - 0.08f)) / 0.3f));
                                    Color gemCol = Color.Lerp(new Color(0.7f, 0.0f, 0.35f, 1.0f), new Color(1.0f, 0.2f, 0.6f, 1.0f), highlight);
                                    tex.SetPixel(x, y, new Color(gemCol.r, gemCol.g, gemCol.b, gemCol.a * alpha));
                                }
                                else if (dist <= 0.35f) {
                                    Color frameCol = new Color(0.12f, 0.12f, 0.12f, 1.0f);
                                    tex.SetPixel(x, y, new Color(frameCol.r, frameCol.g, frameCol.b, frameCol.a * alpha));
                                }
                                else {
                                    Color stoneCol = new Color(0.35f, 0.38f, 0.4f, 0.95f);
                                    float shade = 1.0f;
                                    if (dist >= 0.78f && dist <= 0.88f) {
                                        shade = 0.75f;
                                    } else if (dist > 0.9f) {
                                        shade = 0.65f;
                                    }
                                    tex.SetPixel(x, y, new Color(stoneCol.r * shade, stoneCol.g * shade, stoneCol.b * shade, stoneCol.a * alpha));
                                }
                            } else {
                                tex.SetPixel(x, y, Color.clear);
                            }
                        }
                    }
                    tex.Apply(); 
                    return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
                }



                private Sprite CreateCharcoalSprite(int w, int h)
                {
                    Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                    for (int y = 0; y < h; y++) {
                        for (int x = 0; x < w; x++) {
                            float grain = UnityEngine.Random.Range(-0.025f, 0.025f);
                            float val = Mathf.Clamp01(0.12f + grain);
                            tex.SetPixel(x, y, new Color(val, val, val, 0.96f));
                        }
                    }
                    tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
                }



                private Sprite CreateSettingsMedallionSprite(int w, int h)
                {
                    Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                    for (int y = 0; y < h; y++) {
                        for (int x = 0; x < w; x++) {
                            float dx = (float)(x - w / 2) / (w / 2); float dy = (float)(y - h / 2) / (h / 2);
                            float dist = Mathf.Sqrt(dx * dx + dy * dy);
                            if (dist <= 1.0f) {
                                float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                                float gearTeeth = Mathf.Sin(angle * 8 * Mathf.Deg2Rad);
                                Color medallionCol = new Color(0.95f, 0.8f, 0.2f, 0.95f);
                                if (dist > 0.85f && gearTeeth > 0.1f) tex.SetPixel(x, y, medallionCol);
                                else if (dist <= 0.85f && dist > 0.75f) tex.SetPixel(x, y, new Color(0.6f, 0.45f, 0.1f, 0.95f));
                                else if (dist <= 0.75f && dist > 0.25f) tex.SetPixel(x, y, new Color(0.08f, 0.08f, 0.08f, 0.9f));
                                else if (dist <= 0.25f) tex.SetPixel(x, y, medallionCol);
                                else tex.SetPixel(x, y, Color.clear);
                            } else tex.SetPixel(x, y, Color.clear);
                        }
                    }
                    tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
                }

    }
}
