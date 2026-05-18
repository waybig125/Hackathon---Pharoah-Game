using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace TheAlchemistsCrypt.UI
{
    public class MobileHUDButtons : MonoBehaviour
    {
        public static MobileHUDButtons Instance { get; private set; }

        private Sprite reloadIcon;
        private Sprite fireIcon;
        private Sprite swapIcon;
        private Sprite sprintIcon;
        private Sprite jumpIcon;

        private Text healthText;
        private Text ammoText;
        private Text weaponText;

        private Image healthBarFill;
        private Image ammoBarFill;
        private System.Collections.Generic.List<Image> ammoTicks = new System.Collections.Generic.List<Image>();
        private Sprite sulfurBarSprite;
        private Sprite mercuryBarSprite;
        private Sprite saltBarSprite;
        private Sprite punchBarSprite;

        private GameObject settingsModalInstance = null;
        private GameObject hudRootGo;
        private GameObject deathPanelInstance = null;

        private bool sprintToggleState = false;
        private Image sprintIconImage;
        private Image sprintShadowImage;

        private Sprite obsidianSprite;
        private Sprite charcoalSprite;
        private Sprite goldGradientSprite;
        private Sprite joystickRingSprite;
        private Sprite joystickKnobSprite;
        
        private Sprite healthIconSprite;
        private Sprite sulphurIconSprite;
        private Sprite mercuryIconSprite;
        private Sprite saltIconSprite;
        private Sprite welcomeBgSprite;

        private Image ammoIconImage;
        private Text sprintButtonText;

        private void Awake()
        {
            Instance = this;
            LoadSprites();
            GenerateProceduralSprites();
            SetupCanvas();
            BuildHUD();
        }

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
            Sprite result = Resources.Load<Sprite>("egypt_themed_icons/" + spriteName);
            if (result == null) result = Resources.Load<Sprite>(fallbackResourcePath);
            return result;
        }

        private void LoadSprites()
        {
            joystickRingSprite = LoadThemedSprite("joystick_outer", "UI/Icons/joystick_ring_fallback");
            joystickKnobSprite = LoadThemedSprite("joystick_knob", "UI/Icons/joystick_knob_fallback");
            
            fireIcon = LoadThemedSprite("fire", "UI/Icons/Inspiration/bullet");
            reloadIcon = LoadThemedSprite("reload_ammo", "UI/Icons/Inspiration/reload");
            swapIcon = LoadThemedSprite("swap_weapon", "UI/Icons/icon_swap");
            sprintIcon = LoadThemedSprite("sprint", "UI/Icons/icon_sprint");
            jumpIcon = LoadThemedSprite("jump", "UI/Icons/icon_jump");

            healthIconSprite = LoadSpriteFromResources("egyptian_items/health_icon");
            if (healthIconSprite == null) healthIconSprite = CreateProceduralHealthIconSprite();

            sulphurIconSprite = LoadSpriteFromResources("egyptian_items/sulphur");
            if (sulphurIconSprite == null) sulphurIconSprite = CreateProceduralSulfurSprite();

            mercuryIconSprite = LoadSpriteFromResources("egyptian_items/mercury");
            if (mercuryIconSprite == null) mercuryIconSprite = CreateProceduralMercurySprite();

            saltIconSprite = LoadSpriteFromResources("egyptian_items/salt");
            if (saltIconSprite == null) saltIconSprite = CreateProceduralSaltSprite();

            welcomeBgSprite = LoadSpriteFromResources("egyptian_items/GameStartImage");
        }

        private void GenerateProceduralSprites()
        {
            obsidianSprite = CreateObsidianSprite();
            charcoalSprite = CreateCharcoalSprite(260, 180);
            goldGradientSprite = CreateGoldenGradientSprite();
            if (joystickRingSprite == null) joystickRingSprite = CreateRingSprite();
            if (joystickKnobSprite == null) joystickKnobSprite = CreateKnobSprite();

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
                for (int x = 0; x < w; x++) {
                    float t = (float)x / w;
                    Color col = Color.Lerp(new Color(0.7f, 0.05f, 0.12f, 0.95f), new Color(0.95f, 0.15f, 0.2f, 0.95f), t);
                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateSolidBarSprite(int w, int h, Color c)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++) {
                for (int x = 0; x < w; x++) tex.SetPixel(x, y, c);
            }
            tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
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

        private Sprite CreateRingSprite()
        {
            int width = 512, height = 512;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    float dx = (float)(x - width / 2) / (width / 2); float dy = (float)(y - height / 2) / (height / 2);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist >= 0.85f && dist <= 1f) {
                        float alpha = (dist > 0.95f) ? (1f - dist) / 0.05f : ((dist < 0.9f) ? (dist - 0.85f) / 0.05f : 1f);
                        tex.SetPixel(x, y, new Color(0.95f, 0.8f, 0.2f, alpha * 0.45f));
                    } else if (dist < 0.85f) tex.SetPixel(x, y, new Color(0.05f, 0.05f, 0.05f, 0.15f));
                    else tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateKnobSprite()
        {
            int width = 256, height = 256;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++) {
                float t = (float)y / height;
                Color goldColor = Color.Lerp(new Color(0.95f, 0.8f, 0.2f, 0.95f), new Color(1f, 0.95f, 0.6f, 0.95f), t);
                for (int x = 0; x < width; x++) {
                    float dx = (float)(x - width / 2) / (width / 2); float dy = (float)(y - height / 2) / (height / 2);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist <= 1f) {
                        float alpha = Mathf.Clamp01((1f - dist) * 10f);
                        if (dist >= 0.4f && dist <= 0.5f) tex.SetPixel(x, y, new Color(0.6f, 0.45f, 0.1f, alpha * 0.95f));
                        else tex.SetPixel(x, y, new Color(goldColor.r, goldColor.g, goldColor.b, goldColor.a * alpha));
                    } else tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
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

            var visualImage = knobVisual.GetComponent<Image>();
            visualImage.color = Color.white; visualImage.raycastTarget = false;
            if (joystickKnobSprite != null) visualImage.sprite = joystickKnobSprite;

            var onScreenStick = joystickHandle.gameObject.AddComponent<UnityEngine.InputSystem.OnScreen.OnScreenStick>();
            onScreenStick.movementRange = 180f; 
            onScreenStick.controlPath = "<Gamepad>/leftStick"; 

            // --- ACTION BUTTONS (Circular translucent gold themed, identical to fd582c0) ---
            var btnContainer = new GameObject("ButtonContainer", typeof(RectTransform)).GetComponent<RectTransform>();
            btnContainer.SetParent(root, false);
            btnContainer.anchorMin = btnContainer.anchorMax = new Vector2(1, 0);
            btnContainer.anchoredPosition = new Vector2(-50, 50); // Safe bezel padding

            CreateButton(btnContainer, "FIRE", new Vector2(-280, 280), 380, fireIcon, () => SetFire(true), () => SetFire(false));
            CreateButton(btnContainer, "RELOAD", new Vector2(-620, 150), 200, reloadIcon, () => Reload());
            CreateButton(btnContainer, "SWAP", new Vector2(-480, 520), 200, swapIcon, () => Swap());
            CreateSprintButton(btnContainer, new Vector2(-680, 350), 200);
            CreateButton(btnContainer, "JUMP", new Vector2(-150, 580), 220, jumpIcon, () => SetJump(true), () => SetJump(false));

            HideDebugLabels();

            // --- REFINED HEALTH PANEL ---
            var healthPanel = new GameObject("CustomHealthPanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            healthPanel.SetParent(root, false);
            healthPanel.anchorMin = healthPanel.anchorMax = new Vector2(0, 1);
            healthPanel.pivot = new Vector2(0f, 1f);
            healthPanel.anchoredPosition = new Vector2(120, -120);
            healthPanel.sizeDelta = new Vector2(400, 85);
            healthPanel.GetComponent<Image>().sprite = charcoalSprite;
            
            var hpIconGo = new GameObject("HealthIcon", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            hpIconGo.SetParent(healthPanel, false);
            hpIconGo.anchorMin = hpIconGo.anchorMax = new Vector2(0f, 0.5f);
            hpIconGo.pivot = new Vector2(0f, 0.5f);
            hpIconGo.anchoredPosition = new Vector2(15, 0);
            hpIconGo.sizeDelta = new Vector2(55, 55);
            var hpIconImg = hpIconGo.GetComponent<Image>();
            hpIconImg.sprite = healthIconSprite;
            hpIconImg.preserveAspect = true;

            var healthTxtGo = new GameObject("HealthText", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            healthTxtGo.SetParent(healthPanel, false);
            healthTxtGo.sizeDelta = Vector2.zero;
            healthText = healthTxtGo.GetComponent<Text>();
            healthText.text = "";

            var hpBgBar = new GameObject("HpBarBg", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            hpBgBar.SetParent(healthPanel, false);
            hpBgBar.anchorMin = hpBgBar.anchorMax = new Vector2(0f, 0.5f);
            hpBgBar.pivot = new Vector2(0f, 0.5f);
            hpBgBar.anchoredPosition = new Vector2(85, 0);
            hpBgBar.sizeDelta = new Vector2(295, 30);
            hpBgBar.GetComponent<Image>().sprite = CreateSolidBarSprite(295, 30, new Color(0.04f, 0.04f, 0.04f, 0.8f));

            var hpFillGo = new GameObject("HpFill", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            hpFillGo.SetParent(hpBgBar, false);
            hpFillGo.anchorMin = Vector2.zero; hpFillGo.anchorMax = Vector2.one;
            hpFillGo.offsetMin = hpFillGo.offsetMax = Vector2.zero;
            healthBarFill = hpFillGo.GetComponent<Image>();
            healthBarFill.sprite = CreateHealthBarFillSprite(295, 30);
            healthBarFill.type = Image.Type.Filled;
            healthBarFill.fillMethod = Image.FillMethod.Horizontal;
            healthBarFill.fillAmount = 1.0f;

            // --- REFINED AMMO PANEL ---
            var ammoPanel = new GameObject("CustomAmmoPanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            ammoPanel.SetParent(root, false);
            ammoPanel.anchorMin = ammoPanel.anchorMax = new Vector2(0, 1);
            ammoPanel.pivot = new Vector2(0f, 1f);
            ammoPanel.anchoredPosition = new Vector2(120, -225);
            ammoPanel.sizeDelta = new Vector2(400, 85);
            ammoPanel.GetComponent<Image>().sprite = charcoalSprite;
            
            var amIconGo = new GameObject("AmmoIcon", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            amIconGo.SetParent(ammoPanel, false);
            amIconGo.anchorMin = amIconGo.anchorMax = new Vector2(0f, 0.5f);
            amIconGo.pivot = new Vector2(0f, 0.5f);
            amIconGo.anchoredPosition = new Vector2(15, 0);
            amIconGo.sizeDelta = new Vector2(55, 55);
            ammoIconImage = amIconGo.GetComponent<Image>();
            ammoIconImage.sprite = sulphurIconSprite;
            ammoIconImage.preserveAspect = true;

            var ammoTxtGo = new GameObject("AmmoText", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            ammoTxtGo.SetParent(ammoPanel, false);
            ammoTxtGo.sizeDelta = Vector2.zero;
            ammoText = ammoTxtGo.GetComponent<Text>();
            ammoText.text = "";

            var ammoGridGo = new GameObject("AmmoGrid", typeof(RectTransform)).GetComponent<RectTransform>();
            ammoGridGo.SetParent(ammoPanel, false);
            ammoGridGo.anchorMin = ammoGridGo.anchorMax = new Vector2(0f, 0.5f);
            ammoGridGo.pivot = new Vector2(0f, 0.5f);
            ammoGridGo.anchoredPosition = new Vector2(85, 0);
            ammoGridGo.sizeDelta = new Vector2(295, 30);

            ammoTicks.Clear();
            float tickWidth = 6f;
            float tickHeight = 30f;
            float spacing = 4f;
            for (int i = 0; i < 30; i++)
            {
                var tickGo = new GameObject("Tick_" + i, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                tickGo.SetParent(ammoGridGo, false);
                tickGo.anchorMin = tickGo.anchorMax = new Vector2(0f, 0.5f);
                tickGo.pivot = new Vector2(0f, 0.5f);
                tickGo.anchoredPosition = new Vector2(i * (tickWidth + spacing) + tickWidth * 0.5f, 0);
                tickGo.sizeDelta = new Vector2(tickWidth, tickHeight);

                var img = tickGo.GetComponent<Image>();
                img.sprite = CreateSolidBarSprite((int)tickWidth, (int)tickHeight, Color.white);
                ammoTicks.Add(img);
            }

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
            sHelper.onUp = () => OpenSettingsModal(root);

            // --- TARGETING RETICLE ---
            var reticleGo = new GameObject("TargetingReticle", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            reticleGo.SetParent(root, false);
            reticleGo.anchorMin = reticleGo.anchorMax = new Vector2(0.5f, 0.5f);
            reticleGo.anchoredPosition = Vector2.zero;
            reticleGo.sizeDelta = new Vector2(80, 80);
            var reticleImg = reticleGo.GetComponent<Image>();
            reticleImg.sprite = CreateTargetingReticleSprite(128);
            reticleImg.raycastTarget = false;

            new GameObject("MinimapCanvasContainer", typeof(RectTransform), typeof(MinimapUI)).transform.SetParent(root, false);
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

            var txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            txtGo.SetParent(go, false);

            var txt = txtGo.GetComponent<Text>();
            txt.font = GetRobustFont();
            txt.fontStyle = FontStyle.Bold;
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
                txt.alignment = TextAnchor.MiddleCenter;
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
                txt.alignment = TextAnchor.MiddleLeft;
                txt.fontSize = 20;
            }

            var helper = go.gameObject.AddComponent<ButtonInputHelper>();
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

            var txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            txtGo.SetParent(go, false);
            txtGo.anchorMin = Vector2.zero;
            txtGo.anchorMax = Vector2.one;
            txtGo.offsetMin = new Vector2(70, 0);
            txtGo.offsetMax = Vector2.zero;

            sprintButtonText = txtGo.GetComponent<Text>();
            sprintButtonText.font = GetRobustFont();
            sprintButtonText.fontSize = 20;
            sprintButtonText.fontStyle = FontStyle.Bold;
            sprintButtonText.alignment = TextAnchor.MiddleLeft;
            sprintButtonText.color = new Color(0.95f, 0.8f, 0.2f, 0.95f);
            sprintButtonText.text = "SPRINT: OFF";

            var helper = go.gameObject.AddComponent<ButtonInputHelper>();
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

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            iconGo.SetParent(go, false); iconGo.anchorMin = Vector2.zero; iconGo.anchorMax = Vector2.one; iconGo.offsetMin = iconGo.offsetMax = Vector2.zero;
            var iImg = iconGo.GetComponent<Image>(); iImg.sprite = iconSprite; iImg.color = Color.white; iImg.raycastTarget = false;
            iImg.preserveAspect = true; 

            var helper = go.gameObject.AddComponent<ButtonInputHelper>();
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
            sprintShadowImage = shadowGo.GetComponent<Image>(); sprintShadowImage.sprite = obsidianSprite; sprintShadowImage.raycastTarget = false;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            iconGo.SetParent(go, false); iconGo.anchorMin = Vector2.zero; iconGo.anchorMax = Vector2.one; iconGo.offsetMin = iconGo.offsetMax = Vector2.zero;
            sprintIconImage = iconGo.GetComponent<Image>(); sprintIconImage.sprite = sprintIcon; sprintIconImage.raycastTarget = false;
            sprintIconImage.preserveAspect = true;

            var helper = go.gameObject.AddComponent<ButtonInputHelper>();
            helper.onDown = () => { sprintToggleState = !sprintToggleState; UpdateSprintVisuals(); SetSprint(sprintToggleState); };
        }

        private void UpdateSprintVisuals() {
            if (sprintShadowImage && sprintIconImage) {
                sprintShadowImage.sprite = sprintToggleState ? goldGradientSprite : obsidianSprite;
                sprintIconImage.color = sprintToggleState ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1f);
            }
        }

        private class ButtonInputHelper : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {
            public System.Action onDown; public System.Action onUp;
            public void OnPointerDown(PointerEventData data) => onDown?.Invoke();
            public void OnPointerUp(PointerEventData data) => onUp?.Invoke();
        }

        private class SliderDragHelper : MonoBehaviour, IDragHandler {
            public System.Action<Vector2> onDrag;
            public void OnDrag(PointerEventData data) => onDrag?.Invoke(data.position);
        }

        private void HideDebugLabels() {
            string[] names = { "Text Timescale", "Text Cursor Lock", "Text Tutorial", "Text Tutorial Text", "Text Tutorial Prompt", "Version Text", "Mouse Lock" };
            foreach (var n in names) { var l = GameObject.Find(n); if (l != null) l.SetActive(false); }
        }

        private void Update()
        {
            if (settingsModalInstance != null) {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                if (TheAlchemistsCrypt.Input.MobileInputManager.Instance) TheAlchemistsCrypt.Input.MobileInputManager.Instance.enabled = false;
            }

            // ON desktop, escape should trigger settings toggling using modern Input System API.
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame) {
                if (settingsModalInstance != null) {
                    var bg = settingsModalInstance;
                    Destroy(bg);
                    settingsModalInstance = null;
                    Time.timeScale = 1f; // RESUME THE GAME!
                    if (TheAlchemistsCrypt.Input.MobileInputManager.Instance) TheAlchemistsCrypt.Input.MobileInputManager.Instance.enabled = true;
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                } else {
                    var canvas = GetComponent<Canvas>();
                    if (canvas != null) {
                        OpenSettingsModal(canvas.GetComponent<RectTransform>());
                    }
                }
            }

            // Aggressively disable competing canvases, including clones and weapon UI
            var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            foreach (var c in canvases) {
                if (c.gameObject.name == "MobileHUD_Root" || 
                    c.gameObject.name == "StartScreenOverlay" || 
                    c.gameObject.name == "DeathCanvas" || 
                    (c.gameObject.name == "Canvas" && c.gameObject.GetComponent<MobileHUDButtons>() != null)) continue;
                string nameLower = c.gameObject.name.ToLower();
                if (nameLower.Contains("lpsp") || nameLower.Contains("weaponui") || nameLower.Contains("hud") || nameLower.Contains("canvas") || nameLower.Contains("joystick")) {
                    if (c.gameObject != gameObject && c.gameObject.name != "MobileHUD_Root" && c.gameObject.name != "StartScreenOverlay" && c.gameObject.name != "DeathCanvas") {
                        c.gameObject.SetActive(false);
                    }
                }
            }

            // Update alchemical mode icon in Ammo panel
            Sprite activeElementIcon = sulphurIconSprite;
            var focus = GameObject.FindAnyObjectByType<TheAlchemistsCrypt.Weapons.AlchemicalFocus>(FindObjectsInactive.Include);
            if (focus != null)
            {
                switch (focus.CurrentMode)
                {
                    case TheAlchemistsCrypt.Weapons.AlchemicalFocus.FireMode.Sulfur:
                        activeElementIcon = sulphurIconSprite;
                        break;
                    case TheAlchemistsCrypt.Weapons.AlchemicalFocus.FireMode.Mercury:
                        activeElementIcon = mercuryIconSprite;
                        break;
                    case TheAlchemistsCrypt.Weapons.AlchemicalFocus.FireMode.Salt:
                        activeElementIcon = saltIconSprite;
                        break;
                }
            }
            else
            {
                var character = GameObject.FindAnyObjectByType<InfimaGames.LowPolyShooterPack.Character>();
                if (character != null)
                {
                    var weapon = character.GetEquippedWeapon();
                    if (weapon != null)
                    {
                        string wName = weapon.name.ToLower();
                        if (wName.Contains("sulfur")) activeElementIcon = sulphurIconSprite;
                        else if (wName.Contains("mercury")) activeElementIcon = mercuryIconSprite;
                        else if (wName.Contains("salt")) activeElementIcon = saltIconSprite;
                    }
                }
            }
            if (ammoIconImage != null && activeElementIcon != null)
            {
                ammoIconImage.sprite = activeElementIcon;
            }

            // Dynamically tint alchemical weapons
            TryTintWeapons();

            int current = 30;
            int total = 30;
            if (focus != null)
            {
                current = focus.CurrentAmmo;
                total = focus.MaxAmmo;
            }
            else
            {
                var character = GameObject.FindAnyObjectByType<InfimaGames.LowPolyShooterPack.Character>();
                if (character != null) {
                    var weapon = character.GetEquippedWeapon();
                    if (weapon != null) {
                        current = weapon.GetAmmunitionCurrent();
                        total = weapon.GetAmmunitionTotal();
                    }
                }
            }
            UpdateAmmo(current, total);
            var health = GameObject.FindAnyObjectByType<TheAlchemistsCrypt.Player.PlayerHealth>();
            if (health != null) UpdateHealth(health.currentHealth);
        }

        private void SetFire(bool s) => TheAlchemistsCrypt.Input.MobileInputManager.Instance?.SetFiring(s);
        private void SetSprint(bool s) => TheAlchemistsCrypt.Input.MobileInputManager.Instance?.SetSprinting(s);
        private void SetJump(bool s) => TheAlchemistsCrypt.Input.MobileInputManager.Instance?.SetJumping(s);
        private void Reload() { if (TheAlchemistsCrypt.Input.MobileInputManager.Instance != null) TheAlchemistsCrypt.Input.MobileInputManager.Instance.IsReloading = true; }
        private void Swap() { if (TheAlchemistsCrypt.Input.MobileInputManager.Instance != null) TheAlchemistsCrypt.Input.MobileInputManager.Instance.IsSwappingWeapon = true; }

        private void OpenSettingsModal(RectTransform parentCanvas)
        {
            if (settingsModalInstance != null) return;
            Time.timeScale = 0f; // PAUSE THE GAME!
            if (TheAlchemistsCrypt.Input.MobileInputManager.Instance) TheAlchemistsCrypt.Input.MobileInputManager.Instance.enabled = false;
            
            // Background blur overlay
            var modalBg = new GameObject("SettingsModal", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            modalBg.SetParent(parentCanvas, false); modalBg.anchorMin = Vector2.zero; modalBg.anchorMax = Vector2.one; modalBg.offsetMin = modalBg.offsetMax = Vector2.zero;
            modalBg.GetComponent<Image>().color = new Color(0, 0, 0, 0.85f); settingsModalInstance = modalBg.gameObject;
            
            // Dialog box
            var dialog = new GameObject("Dialog", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            dialog.SetParent(modalBg, false); dialog.anchorMin = dialog.anchorMax = new Vector2(0.5f, 0.5f); dialog.sizeDelta = new Vector2(800, 600);
            dialog.GetComponent<Image>().sprite = charcoalSprite;
            
            // Add a beautiful gold border around the dialog!
            var border = new GameObject("Border", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            border.SetParent(dialog, false); border.anchorMin = Vector2.zero; border.anchorMax = Vector2.one;
            border.offsetMin = new Vector2(4, 4); border.offsetMax = new Vector2(-4, -4);
            var borderImg = border.GetComponent<Image>();
            borderImg.color = new Color(0.95f, 0.8f, 0.2f, 0.2f); // Golden glow border
            borderImg.sprite = charcoalSprite; // Use charcoal base or transparent fill
            
            // Title Text
            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            titleGo.SetParent(dialog, false); titleGo.anchoredPosition = new Vector2(0, 240); titleGo.sizeDelta = new Vector2(700, 60);
            var titleTxt = titleGo.GetComponent<Text>();
            titleTxt.font = GetRobustFont(); titleTxt.fontSize = 28; titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter; titleTxt.color = new Color(0.95f, 0.8f, 0.2f, 0.95f);
            titleTxt.text = "THE PHARAOH'S VAULT - SETTINGS";

            // Row 1: Swipe Sensitivity (SLIDER)
            float currentSens = PlayerPrefs.GetFloat("MobileSensitivity", 0.08f);
            var sensRow = CreateSettingsSliderRow(dialog, "TOUCH SENSITIVITY", new Vector2(0, 130), 0.02f, 0.30f, currentSens,
                (val) => {
                    PlayerPrefs.SetFloat("MobileSensitivity", val); PlayerPrefs.Save();
                    var sz = GameObject.FindAnyObjectByType<LookSwipeZone>();
                    if (sz != null) sz.sensitivity = val;
                },
                (val) => val.ToString("F2")
            );

            // Row 2: Master Volume (SLIDER)
            float currentVol = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
            AudioListener.volume = currentVol;
            var volRow = CreateSettingsSliderRow(dialog, "MASTER VOLUME", new Vector2(0, 45), 0f, 1f, currentVol,
                (val) => {
                    PlayerPrefs.SetFloat("MasterVolume", val); PlayerPrefs.Save();
                    AudioListener.volume = val;
                },
                (val) => Mathf.RoundToInt(val * 100f) + "%"
            );

            // Row 3: Hive Narration Toggle (CHECKBOX TOGGLE)
            int showNar = PlayerPrefs.GetInt("ShowNarration", 1);
            var narrationRow = CreateSettingsToggleRow(dialog, "HIVE NARRATION", new Vector2(0, -40), showNar == 1,
                (val) => {
                    int nextVal = val ? 1 : 0;
                    PlayerPrefs.SetInt("ShowNarration", nextVal); PlayerPrefs.Save();
                    if (nextVal == 0 && narrationPanel != null) narrationPanel.SetActive(false);
                }
            );

            // Row 4: Visual Fidelity (SELECTOR)
            int currentQuality = QualitySettings.GetQualityLevel();
            string[] qualityNames = { "LOW", "MEDIUM", "ULTRA" };
            string initialQualityName = currentQuality < qualityNames.Length ? qualityNames[currentQuality] : "ULTRA";
            var qualRow = CreateSettingsRow(dialog, "VISUAL QUALITY", new Vector2(0, -125), initialQualityName,
                () => {
                    int q = QualitySettings.GetQualityLevel();
                    q = Mathf.Clamp(q - 1, 0, 2);
                    QualitySettings.SetQualityLevel(q, true);
                    return qualityNames[q];
                },
                () => {
                    int q = QualitySettings.GetQualityLevel();
                    q = Mathf.Clamp(q + 1, 0, 2);
                    QualitySettings.SetQualityLevel(q, true);
                    return qualityNames[q];
                }
            );

            // Close Button
            var closeGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            closeGo.SetParent(dialog, false); closeGo.anchoredPosition = new Vector2(0, -230); closeGo.sizeDelta = new Vector2(250, 60);
            closeGo.GetComponent<Image>().sprite = charcoalSprite;
            
            // Add gold highlights to the close button
            var closeHighlight = new GameObject("Highlight", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            closeHighlight.SetParent(closeGo, false); closeHighlight.anchorMin = Vector2.zero; closeHighlight.anchorMax = Vector2.one;
            closeHighlight.offsetMin = closeHighlight.offsetMax = Vector2.zero;
            closeHighlight.GetComponent<Image>().color = new Color(0.95f, 0.8f, 0.2f, 0.15f);
            
            var closeTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            closeTxtGo.SetParent(closeGo, false);
            closeTxtGo.anchorMin = Vector2.zero; closeTxtGo.anchorMax = Vector2.one;
            closeTxtGo.offsetMin = closeTxtGo.offsetMax = Vector2.zero;
            var closeTxt = closeTxtGo.GetComponent<Text>();
            closeTxt.font = GetRobustFont(); closeTxt.fontSize = 22; closeTxt.fontStyle = FontStyle.Bold;
            closeTxt.alignment = TextAnchor.MiddleCenter; closeTxt.color = new Color(0.95f, 0.8f, 0.2f, 0.95f);
            closeTxt.text = "RETURN TO GAME";

            closeGo.gameObject.AddComponent<ButtonInputHelper>().onUp = () => {
                Destroy(modalBg.gameObject);
                settingsModalInstance = null;
                Time.timeScale = 1f; // RESUME THE GAME!
                if (TheAlchemistsCrypt.Input.MobileInputManager.Instance) TheAlchemistsCrypt.Input.MobileInputManager.Instance.enabled = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            };
        }

        private GameObject CreateSettingsSliderRow(RectTransform parent, string labelText, Vector2 pos, float minVal, float maxVal, float initialVal, System.Action<float> onValueChange, System.Func<float, string> formatFunc)
        {
            var row = new GameObject("Row_" + labelText.Replace(" ", ""), typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(parent, false); row.anchoredPosition = pos; row.sizeDelta = new Vector2(700, 70);

            // Label
            var lblGo = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            lblGo.SetParent(row, false); lblGo.anchorMin = new Vector2(0, 0.5f); lblGo.anchorMax = new Vector2(0.4f, 0.5f);
            lblGo.pivot = new Vector2(0, 0.5f); lblGo.anchoredPosition = new Vector2(20, 0); lblGo.sizeDelta = new Vector2(250, 50);
            var lblTxt = lblGo.GetComponent<Text>();
            lblTxt.font = GetRobustFont(); lblTxt.fontSize = 20; lblTxt.fontStyle = FontStyle.Bold;
            lblTxt.alignment = TextAnchor.MiddleLeft; lblTxt.color = new Color(0.95f, 0.85f, 0.6f, 0.95f);
            lblTxt.text = labelText;

            // Slider Background track
            var sliderBgGo = new GameObject("SliderBg", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            sliderBgGo.SetParent(row, false); sliderBgGo.anchoredPosition = new Vector2(210, 0); sliderBgGo.sizeDelta = new Vector2(270, 14);
            sliderBgGo.GetComponent<Image>().sprite = charcoalSprite;
            sliderBgGo.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            // Slider Fill track (golden/crimson)
            var sliderFillGo = new GameObject("SliderFill", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            sliderFillGo.SetParent(sliderBgGo, false);
            sliderFillGo.anchorMin = new Vector2(0, 0);
            sliderFillGo.anchorMax = new Vector2((initialVal - minVal) / (maxVal - minVal), 1);
            sliderFillGo.offsetMin = sliderFillGo.offsetMax = Vector2.zero;
            sliderFillGo.GetComponent<Image>().sprite = charcoalSprite;
            sliderFillGo.GetComponent<Image>().color = new Color(0.95f, 0.8f, 0.2f, 0.9f);

            // Handle knob
            var knobGo = new GameObject("SliderHandle", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            knobGo.SetParent(sliderBgGo, false);
            knobGo.anchorMin = knobGo.anchorMax = new Vector2((initialVal - minVal) / (maxVal - minVal), 0.5f);
            knobGo.anchoredPosition = Vector2.zero;
            knobGo.sizeDelta = new Vector2(26, 26);
            knobGo.GetComponent<Image>().sprite = CreateSettingsMedallionSprite(32, 32);

            // Value text label at the right
            var valGo = new GameObject("SliderValueText", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            valGo.SetParent(row, false); valGo.anchoredPosition = new Vector2(400, 0); valGo.sizeDelta = new Vector2(100, 50);
            var valTxt = valGo.GetComponent<Text>();
            valTxt.font = GetRobustFont(); valTxt.fontSize = 20; valTxt.fontStyle = FontStyle.Bold;
            valTxt.alignment = TextAnchor.MiddleLeft; valTxt.color = new Color(1f, 0.95f, 0.8f, 0.95f);
            valTxt.text = formatFunc != null ? formatFunc(initialVal) : initialVal.ToString("F2");

            // Direct interactive drag listener!
            var sliderHelper = sliderBgGo.gameObject.AddComponent<ButtonInputHelper>();
            System.Action<Vector2> updateSliderVal = (screenPos) => {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(sliderBgGo, screenPos, null, out Vector2 localPoint);
                float width = sliderBgGo.rect.width;
                float pct = Mathf.Clamp01((localPoint.x + width * 0.5f) / width);
                float val = Mathf.Lerp(minVal, maxVal, pct);
                sliderFillGo.anchorMax = new Vector2(pct, 1);
                knobGo.anchorMin = knobGo.anchorMax = new Vector2(pct, 0.5f);
                valTxt.text = formatFunc != null ? formatFunc(val) : val.ToString("F2");
                onValueChange?.Invoke(val);
            };

            sliderHelper.onDown = () => {
                Vector2 mousePos = UnityEngine.InputSystem.Pointer.current != null ? UnityEngine.InputSystem.Pointer.current.position.ReadValue() : 
                                   (UnityEngine.InputSystem.Mouse.current != null ? UnityEngine.InputSystem.Mouse.current.position.ReadValue() : Vector2.zero);
                updateSliderVal(mousePos);
            };

            var dragHelper = sliderBgGo.gameObject.AddComponent<SliderDragHelper>();
            dragHelper.onDrag = (screenPos) => {
                updateSliderVal(screenPos);
            };

            return row.gameObject;
        }

        private GameObject CreateSettingsRow(RectTransform parent, string labelText, Vector2 pos, string initialVal, System.Func<string> onDec, System.Func<string> onInc)
        {
            var row = new GameObject("Row_" + labelText.Replace(" ", ""), typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(parent, false); row.anchoredPosition = pos; row.sizeDelta = new Vector2(700, 70);

            // Label
            var lblGo = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            lblGo.SetParent(row, false); lblGo.anchorMin = new Vector2(0, 0.5f); lblGo.anchorMax = new Vector2(0.4f, 0.5f);
            lblGo.pivot = new Vector2(0, 0.5f); lblGo.anchoredPosition = new Vector2(20, 0); lblGo.sizeDelta = new Vector2(250, 50);
            var lblTxt = lblGo.GetComponent<Text>();
            lblTxt.font = GetRobustFont(); lblTxt.fontSize = 20; lblTxt.fontStyle = FontStyle.Bold;
            lblTxt.alignment = TextAnchor.MiddleLeft; lblTxt.color = new Color(0.95f, 0.85f, 0.6f, 0.95f);
            lblTxt.text = labelText;

            // Dec Button [-]
            var decGo = new GameObject("DecBtn", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            decGo.SetParent(row, false); decGo.anchoredPosition = new Vector2(100, 0); decGo.sizeDelta = new Vector2(50, 50);
            decGo.GetComponent<Image>().sprite = charcoalSprite;
            var decHighlight = new GameObject("Highlight", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            decHighlight.SetParent(decGo, false); decHighlight.anchorMin = Vector2.zero; decHighlight.anchorMax = Vector2.one; decHighlight.offsetMin = decHighlight.offsetMax = Vector2.zero;
            decHighlight.GetComponent<Image>().color = new Color(0.95f, 0.8f, 0.2f, 0.15f);
            var decTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            decTxtGo.SetParent(decGo, false); decTxtGo.anchorMin = Vector2.zero; decTxtGo.anchorMax = Vector2.one; decTxtGo.offsetMin = decTxtGo.offsetMax = Vector2.zero;
            var decTxt = decTxtGo.GetComponent<Text>();
            decTxt.font = GetRobustFont(); decTxt.fontSize = 24; decTxt.fontStyle = FontStyle.Bold;
            decTxt.alignment = TextAnchor.MiddleCenter; decTxt.color = new Color(0.95f, 0.8f, 0.2f, 0.95f); decTxt.text = "-";

            // Value text box
            var valGo = new GameObject("ValBtn", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            valGo.SetParent(row, false); valGo.anchoredPosition = new Vector2(210, 0); valGo.sizeDelta = new Vector2(150, 50);
            valGo.GetComponent<Image>().sprite = charcoalSprite;
            var valTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            valTxtGo.SetParent(valGo, false); valTxtGo.anchorMin = Vector2.zero; valTxtGo.anchorMax = Vector2.one; valTxtGo.offsetMin = valTxtGo.offsetMax = Vector2.zero;
            var valTxt = valTxtGo.GetComponent<Text>();
            valTxt.font = GetRobustFont(); valTxt.fontSize = 20; valTxt.fontStyle = FontStyle.Bold;
            valTxt.alignment = TextAnchor.MiddleCenter; valTxt.color = new Color(1f, 0.95f, 0.8f, 0.95f); valTxt.text = initialVal;

            // Inc Button [+]
            var incGo = new GameObject("IncBtn", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            incGo.SetParent(row, false); incGo.anchoredPosition = new Vector2(320, 0); incGo.sizeDelta = new Vector2(50, 50);
            incGo.GetComponent<Image>().sprite = charcoalSprite;
            var incHighlight = new GameObject("Highlight", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            incHighlight.SetParent(incGo, false); incHighlight.anchorMin = Vector2.zero; incHighlight.anchorMax = Vector2.one; incHighlight.offsetMin = incHighlight.offsetMax = Vector2.zero;
            incHighlight.GetComponent<Image>().color = new Color(0.95f, 0.8f, 0.2f, 0.15f);
            var incTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            incTxtGo.SetParent(incGo, false); incTxtGo.anchorMin = Vector2.zero; incTxtGo.anchorMax = Vector2.one; incTxtGo.offsetMin = incTxtGo.offsetMax = Vector2.zero;
            var incTxt = incTxtGo.GetComponent<Text>();
            incTxt.font = GetRobustFont(); incTxt.fontSize = 24; incTxt.fontStyle = FontStyle.Bold;
            incTxt.alignment = TextAnchor.MiddleCenter; incTxt.color = new Color(0.95f, 0.8f, 0.2f, 0.95f); incTxt.text = "+";

            // Position controls to the right
            decGo.anchorMin = decGo.anchorMax = new Vector2(1, 0.5f); decGo.anchoredPosition = new Vector2(-270, 0);
            valGo.anchorMin = valGo.anchorMax = new Vector2(1, 0.5f); valGo.anchoredPosition = new Vector2(-160, 0);
            incGo.anchorMin = incGo.anchorMax = new Vector2(1, 0.5f); incGo.anchoredPosition = new Vector2(-50, 0);

            decGo.gameObject.AddComponent<ButtonInputHelper>().onUp = () => { valTxt.text = onDec(); };
            incGo.gameObject.AddComponent<ButtonInputHelper>().onUp = () => { valTxt.text = onInc(); };

            return row.gameObject;
        }

        private GameObject narrationPanel = null;
        private Text narrationText = null;
        private Coroutine narrationFadeRoutine = null;
        private GameObject orbTooltipPanel = null;
        private Text orbTooltipText = null;
        private Coroutine orbTooltipFadeRoutine = null;

        public void ShowNarration(string message)
        {
            if (PlayerPrefs.GetInt("ShowNarration", 1) == 0)
            {
                if (narrationPanel != null) narrationPanel.SetActive(false);
                return;
            }

            if (narrationPanel == null)
            {
                var canvas = GetComponent<Canvas>();
                if (canvas == null) return;
                var root = canvas.GetComponent<RectTransform>();

                // Sleek, borderless translucent black pill panel for narration
                var panelGo = new GameObject("NarrationPanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                panelGo.SetParent(root, false);
                panelGo.anchorMin = panelGo.anchorMax = new Vector2(0.5f, 0f);
                panelGo.pivot = new Vector2(0.5f, 0f);
                panelGo.anchoredPosition = new Vector2(0, 220); // Placed beautifully above dual joysticks
                panelGo.sizeDelta = new Vector2(900, 75);
                panelGo.GetComponent<Image>().sprite = charcoalSprite;
                panelGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f); // Sleek transparent backing
                narrationPanel = panelGo.gameObject;

                // Text (No golden border, clean modern look)
                var txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
                txtGo.SetParent(panelGo, false);
                txtGo.anchorMin = Vector2.zero; txtGo.anchorMax = Vector2.one;
                txtGo.offsetMin = new Vector2(30, 5); txtGo.offsetMax = new Vector2(-30, -5);
                narrationText = txtGo.GetComponent<Text>();
                narrationText.font = GetRobustFont();
                narrationText.fontSize = 19;
                narrationText.fontStyle = FontStyle.Italic;
                narrationText.alignment = TextAnchor.MiddleCenter;
                narrationText.color = new Color(0.95f, 0.85f, 0.6f, 0.95f); // Rich warm cream/gold
                narrationText.horizontalOverflow = HorizontalWrapMode.Wrap;
                narrationText.verticalOverflow = VerticalWrapMode.Truncate;
            }

            narrationPanel.SetActive(true);
            narrationText.text = message;

            if (narrationFadeRoutine != null) StopCoroutine(narrationFadeRoutine);
            narrationFadeRoutine = StartCoroutine(NarrationFadeOutSequence());
        }

        private IEnumerator NarrationFadeOutSequence()
        {
            float duration = 6f;
            float elapsed = 0f;
            var cg = narrationPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = narrationPanel.AddComponent<CanvasGroup>();
            cg.alpha = 1f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            elapsed = 0f;
            float fadeTime = 1.5f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                yield return null;
            }

            narrationPanel.SetActive(false);
        }

        public void HideOrbTooltip()
        {
            if (orbTooltipPanel != null) orbTooltipPanel.SetActive(false);
            if (orbTooltipFadeRoutine != null) StopCoroutine(orbTooltipFadeRoutine);
        }

        public void ShowOrbTooltip(string message)
        {
            if (orbTooltipPanel == null)
            {
                var canvas = GetComponent<Canvas>();
                if (canvas == null) return;
                var root = canvas.GetComponent<RectTransform>();

                // Golden/crimson themed container
                var panelGo = new GameObject("OrbTooltipPanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                panelGo.SetParent(root, false);
                panelGo.anchorMin = panelGo.anchorMax = new Vector2(0.5f, 0f);
                panelGo.pivot = new Vector2(0.5f, 0f);
                panelGo.anchoredPosition = new Vector2(0, 150);
                panelGo.sizeDelta = new Vector2(600, 60);
                panelGo.GetComponent<Image>().sprite = charcoalSprite;
                panelGo.GetComponent<Image>().color = new Color(0.12f, 0.02f, 0.04f, 0.9f); // Crimson tint charcoal!
                orbTooltipPanel = panelGo.gameObject;

                // Gold border highlight
                var hlGo = new GameObject("Highlight", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                hlGo.SetParent(panelGo, false);
                hlGo.anchorMin = Vector2.zero; hlGo.anchorMax = Vector2.one;
                hlGo.offsetMin = new Vector2(2, 2); hlGo.offsetMax = new Vector2(-2, -2);
                var hlImg = hlGo.GetComponent<Image>();
                hlImg.sprite = charcoalSprite;
                hlImg.color = new Color(0.95f, 0.8f, 0.2f, 0.3f); // Solid gold border

                // Text
                var txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
                txtGo.SetParent(panelGo, false);
                txtGo.anchorMin = Vector2.zero; txtGo.anchorMax = Vector2.one;
                txtGo.offsetMin = new Vector2(15, 5); txtGo.offsetMax = new Vector2(-15, -5);
                orbTooltipText = txtGo.GetComponent<Text>();
                orbTooltipText.font = GetRobustFont();
                orbTooltipText.fontSize = 16;
                orbTooltipText.fontStyle = FontStyle.Bold;
                orbTooltipText.alignment = TextAnchor.MiddleCenter;
                orbTooltipText.color = new Color(0.95f, 0.85f, 0.6f, 0.95f); // Gold tint text
                orbTooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;
                orbTooltipText.verticalOverflow = VerticalWrapMode.Truncate;
            }

            orbTooltipPanel.SetActive(true);
            orbTooltipText.text = message;

            if (orbTooltipFadeRoutine != null) StopCoroutine(orbTooltipFadeRoutine);
            orbTooltipFadeRoutine = StartCoroutine(OrbTooltipFadeOutSequence());
        }

        private IEnumerator OrbTooltipFadeOutSequence()
        {
            float duration = 3.5f;
            float elapsed = 0f;
            var cg = orbTooltipPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = orbTooltipPanel.AddComponent<CanvasGroup>();
            cg.alpha = 1f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            elapsed = 0f;
            float fadeTime = 0.8f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                yield return null;
            }

            orbTooltipPanel.SetActive(false);
        }

        public void UpdateHealth(float h)
        {
            if (healthText) healthText.text = "";
            if (healthBarFill) healthBarFill.fillAmount = Mathf.Clamp01(h / 100f);
        }

        public void UpdateAmmo(int c, int t)
        {
            if (ammoText) ammoText.text = "";
            
            Color tickColor = new Color(0.95f, 0.8f, 0.2f, 0.95f);
            var focus = GameObject.FindAnyObjectByType<TheAlchemistsCrypt.Weapons.AlchemicalFocus>();
            if (focus != null)
            {
                switch (focus.CurrentMode)
                {
                    case TheAlchemistsCrypt.Weapons.AlchemicalFocus.FireMode.Sulfur:
                        tickColor = new Color(0.95f, 0.55f, 0.05f, 0.95f);
                        break;
                    case TheAlchemistsCrypt.Weapons.AlchemicalFocus.FireMode.Mercury:
                        tickColor = new Color(0.1f, 0.75f, 0.95f, 0.95f);
                        break;
                    case TheAlchemistsCrypt.Weapons.AlchemicalFocus.FireMode.Salt:
                        tickColor = new Color(0.95f, 0.95f, 0.95f, 0.95f);
                        break;
                }
            }
            else
            {
                var character = GameObject.FindAnyObjectByType<InfimaGames.LowPolyShooterPack.Character>();
                if (character != null)
                {
                    var weapon = character.GetEquippedWeapon();
                    if (weapon != null)
                    {
                        string wName = weapon.name.ToLower();
                        if (wName.Contains("sulfur")) tickColor = new Color(0.95f, 0.55f, 0.05f, 0.95f);
                        else if (wName.Contains("mercury")) tickColor = new Color(0.1f, 0.75f, 0.95f, 0.95f);
                        else if (wName.Contains("salt")) tickColor = new Color(0.95f, 0.95f, 0.95f, 0.95f);
                    }
                }
            }

            for (int i = 0; i < 30; i++)
            {
                if (i < ammoTicks.Count && ammoTicks[i] != null)
                {
                    if (i < c)
                    {
                        ammoTicks[i].color = tickColor;
                    }
                    else
                    {
                        ammoTicks[i].color = new Color(0.04f, 0.04f, 0.04f, 0.8f);
                    }
                }
            }
        }

        public static bool HasStartedGame = false;

        private void Start()
        {
            if (!HasStartedGame)
            {
                CreateStartScreen();
            }
        }

        private void CreateStartScreen()
        {
            // Set scale to 0 to pause game
            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // Disable mobile input manager temporarily
            if (TheAlchemistsCrypt.Input.MobileInputManager.Instance)
            {
                TheAlchemistsCrypt.Input.MobileInputManager.Instance.enabled = false;
            }

            // Create Canvas
            var startCanvasGo = new GameObject("StartScreenOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = startCanvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var scaler = startCanvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1.0f;

            // Background
            var bgGo = new GameObject("StartBackground", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            bgGo.SetParent(startCanvasGo.transform, false);
            bgGo.anchorMin = Vector2.zero; bgGo.anchorMax = Vector2.one;
            bgGo.offsetMin = bgGo.offsetMax = Vector2.zero;
            var bgImg = bgGo.GetComponent<Image>();
            var bgSprite = Resources.Load<Sprite>("egyptian_items/GameStartImage");
            if (bgSprite != null)
            {
                bgImg.sprite = bgSprite;
            }
            else
            {
                bgImg.sprite = CreateProceduralGradientSprite(1920, 1080, new Color(0.08f, 0.04f, 0f, 1f), new Color(0.02f, 0.01f, 0f, 1f));
            }
            bgImg.color = Color.white;
            bgImg.preserveAspect = false;

            // Elegant Left Obsidian Menu Panel (Transparent & Left-aligned)
            var menuPanelGo = new GameObject("MenuPanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            menuPanelGo.SetParent(startCanvasGo.transform, false);
            menuPanelGo.anchorMin = menuPanelGo.anchorMax = new Vector2(0f, 0.5f);
            menuPanelGo.anchoredPosition = new Vector2(320, 0); // Beautifully centered on left half of the screen
            menuPanelGo.sizeDelta = new Vector2(500, 700);

            var menuImg = menuPanelGo.GetComponent<Image>();
            menuImg.sprite = null; // Perfect borderless rectangle
            menuImg.color = new Color(0f, 0f, 0f, 0f); // Completely transparent to remove black background

            // Title Text
            var titleGo = new GameObject("TitleText", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            titleGo.SetParent(menuPanelGo, false);
            titleGo.anchorMin = titleGo.anchorMax = new Vector2(0.5f, 1f);
            titleGo.anchoredPosition = new Vector2(0, -100);
            titleGo.sizeDelta = new Vector2(440, 150);
            var titleTxt = titleGo.GetComponent<Text>();
            titleTxt.font = GetTitleFont(); // Premium dynamic MedievalSharp font
            titleTxt.fontSize = 44;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = new Color(1.0f, 0.84f, 0.0f, 0.98f); // Premium bright glowing gold
            titleTxt.text = "THE ALCHEMIST'S\nCRYPT";

            // START Button
            var startBtnGo = new GameObject("StartButton", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            startBtnGo.SetParent(menuPanelGo, false);
            startBtnGo.anchorMin = startBtnGo.anchorMax = new Vector2(0.5f, 0.5f);
            startBtnGo.anchoredPosition = new Vector2(0, -60);
            startBtnGo.sizeDelta = new Vector2(360, 80);
            var startBtnImg = startBtnGo.GetComponent<Image>();
            startBtnImg.sprite = null; // Modern flat rectangle
            startBtnImg.color = new Color(1.0f, 0.78f, 0.0f, 0.95f); // Vibrant bold gold

            var startBtnTextGo = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            startBtnTextGo.SetParent(startBtnGo, false);
            startBtnTextGo.anchorMin = Vector2.zero; startBtnTextGo.anchorMax = Vector2.one;
            startBtnTextGo.offsetMin = startBtnTextGo.offsetMax = Vector2.zero;
            var startBtnTxt = startBtnTextGo.GetComponent<Text>();
            startBtnTxt.font = GetRobustFont();
            startBtnTxt.fontSize = 28;
            startBtnTxt.fontStyle = FontStyle.Bold;
            startBtnTxt.alignment = TextAnchor.MiddleCenter;
            startBtnTxt.color = new Color(0.08f, 0.08f, 0.08f, 1f); // Rich high-contrast dark charcoal text
            startBtnTxt.text = "START VOYAGE";

            var startHelper = startBtnGo.gameObject.AddComponent<ButtonInputHelper>();
            startHelper.onDown = () =>
            {
                startBtnGo.localScale = new Vector3(0.95f, 0.95f, 1f);
            };
            startHelper.onUp = () =>
            {
                startBtnGo.localScale = new Vector3(1f, 1f, 1f);
                HasStartedGame = true;
                Time.timeScale = 1f;
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                if (TheAlchemistsCrypt.Input.MobileInputManager.Instance)
                {
                    TheAlchemistsCrypt.Input.MobileInputManager.Instance.enabled = true;
                }
                Destroy(startCanvasGo);
            };

            // QUIT Button
            var quitBtnGo = new GameObject("QuitButton", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            quitBtnGo.SetParent(menuPanelGo, false);
            quitBtnGo.anchorMin = quitBtnGo.anchorMax = new Vector2(0.5f, 0.5f);
            quitBtnGo.anchoredPosition = new Vector2(0, -170);
            quitBtnGo.sizeDelta = new Vector2(360, 80);
            var quitBtnImg = quitBtnGo.GetComponent<Image>();
            quitBtnImg.sprite = null; // Modern flat rectangle
            quitBtnImg.color = new Color(0.12f, 0.12f, 0.14f, 0.95f); // Sleek modern dark charcoal

            var quitBtnTextGo = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            quitBtnTextGo.SetParent(quitBtnGo, false);
            quitBtnTextGo.anchorMin = Vector2.zero; quitBtnTextGo.anchorMax = Vector2.one;
            quitBtnTextGo.offsetMin = quitBtnTextGo.offsetMax = Vector2.zero;
            var quitBtnTxt = quitBtnTextGo.GetComponent<Text>();
            quitBtnTxt.font = GetRobustFont();
            quitBtnTxt.fontSize = 28;
            quitBtnTxt.fontStyle = FontStyle.Bold;
            quitBtnTxt.alignment = TextAnchor.MiddleCenter;
            quitBtnTxt.color = new Color(0.95f, 0.8f, 0.2f, 0.95f);
            quitBtnTxt.text = "QUIT GAME";

            var quitHelper = quitBtnGo.gameObject.AddComponent<ButtonInputHelper>();
            quitHelper.onDown = () =>
            {
                quitBtnGo.localScale = new Vector3(0.95f, 0.95f, 1f);
            };
            quitHelper.onUp = () =>
            {
                quitBtnGo.localScale = new Vector3(1f, 1f, 1f);
                Application.Quit();
            };

            // Set UI layer
            SetLayerRecursively(startCanvasGo, 5);
        }

        private void SetLayerRecursively(GameObject go, int layer)
        {
            if (go == null) return;
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private Font GetTitleFont()
        {
            Font f = Resources.Load<Font>("Fonts/MedievalSharp");
            if (f == null) f = GetRobustFont();
            return f;
        }

        private GameObject CreateSettingsToggleRow(RectTransform parent, string labelText, Vector2 pos, bool initialVal, System.Action<bool> onToggle)
        {
            var row = new GameObject("Row_" + labelText.Replace(" ", ""), typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(parent, false); row.anchoredPosition = pos; row.sizeDelta = new Vector2(700, 70);

            // Label
            var lblGo = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            lblGo.SetParent(row, false); lblGo.anchorMin = new Vector2(0, 0.5f); lblGo.anchorMax = new Vector2(0.4f, 0.5f);
            lblGo.pivot = new Vector2(0, 0.5f); lblGo.anchoredPosition = new Vector2(20, 0); lblGo.sizeDelta = new Vector2(250, 50);
            var lblTxt = lblGo.GetComponent<Text>();
            lblTxt.font = GetRobustFont(); lblTxt.fontSize = 20; lblTxt.fontStyle = FontStyle.Bold;
            lblTxt.alignment = TextAnchor.MiddleLeft; lblTxt.color = new Color(0.95f, 0.85f, 0.6f, 0.95f);
            lblTxt.text = labelText;

            // Checkbox Outline (Outer Gold Frame)
            var outlineGo = new GameObject("Outline", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            outlineGo.SetParent(row, false);
            outlineGo.anchorMin = outlineGo.anchorMax = new Vector2(1f, 0.5f);
            outlineGo.anchoredPosition = new Vector2(-160, 0);
            outlineGo.sizeDelta = new Vector2(54, 54);
            var outImg = outlineGo.GetComponent<Image>();
            outImg.sprite = null;
            outImg.color = new Color(0.95f, 0.8f, 0.2f, 0.95f);

            // Checkbox Backing (Inner Card)
            var boxGo = new GameObject("Checkbox", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            boxGo.SetParent(outlineGo, false);
            boxGo.anchorMin = Vector2.zero; boxGo.anchorMax = Vector2.one;
            boxGo.offsetMin = new Vector2(2, 2); boxGo.offsetMax = new Vector2(-2, -2);
            
            var boxImg = boxGo.GetComponent<Image>();
            boxImg.sprite = charcoalSprite;
            boxImg.color = Color.white;

            // Checkmark
            var markGo = new GameObject("Checkmark", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            markGo.SetParent(boxGo, false);
            markGo.anchorMin = markGo.anchorMax = new Vector2(0.5f, 0.5f);
            markGo.sizeDelta = new Vector2(30, 30);
            var markImg = markGo.GetComponent<Image>();
            markImg.sprite = null;
            markImg.color = new Color(1.0f, 0.78f, 0.0f, 0.95f);

            bool currentVal = initialVal;
            markGo.gameObject.SetActive(currentVal);

            var helper = boxGo.gameObject.AddComponent<ButtonInputHelper>();
            helper.onUp = () =>
            {
                currentVal = !currentVal;
                markGo.gameObject.SetActive(currentVal);
                onToggle(currentVal);
            };

            return row.gameObject;
        }

        private Font GetRobustFont()
        {
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (f == null)
            {
                Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
                if (fonts != null && fonts.Length > 0) f = fonts[0];
            }
            return f;
        }

        private Sprite CreateTargetingReticleSprite(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            Color gold = new Color(0.95f, 0.8f, 0.2f, 0.9f);
            Color ruby = new Color(0.85f, 0.1f, 0.1f, 0.95f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // Concentric outer ring
                    if (dist >= 0.78f && dist <= 0.82f)
                    {
                        tex.SetPixel(x, y, gold);
                    }
                    // Inner ring
                    else if (dist >= 0.38f && dist <= 0.42f)
                    {
                        tex.SetPixel(x, y, gold);
                    }
                    // Concentric tick marks
                    else if (dist >= 0.45f && dist <= 0.75f && (Mathf.Abs(dx) < 0.03f || Mathf.Abs(dy) < 0.03f))
                    {
                        tex.SetPixel(x, y, gold);
                    }
                    // Glowing Ruby Center Point
                    else if (dist <= 0.08f)
                    {
                        float alpha = Mathf.Clamp01((1f - dist / 0.08f) * 2f);
                        tex.SetPixel(x, y, new Color(ruby.r, ruby.g, ruby.b, ruby.a * alpha));
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

        public void ShowDeathScreen()
        {
            if (deathPanelInstance != null) return;
            if (hudRootGo != null) hudRootGo.SetActive(false);

            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (TheAlchemistsCrypt.Input.MobileInputManager.Instance)
                TheAlchemistsCrypt.Input.MobileInputManager.Instance.enabled = false;

            // Dedicated Canvas for Death Screen to ensure visibility and interaction above all else
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

            var panelImg = deathPanelGo.GetComponent<Image>();
            
            // Spooky vignette procedural texture
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

            // Spooky red/gold title
            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            titleGo.SetParent(deathPanelGo, false);
            titleGo.anchorMin = titleGo.anchorMax = new Vector2(0.5f, 0.5f);
            titleGo.anchoredPosition = new Vector2(0, 130);
            titleGo.sizeDelta = new Vector2(800, 120);

            var titleText = titleGo.GetComponent<Text>();
            titleText.font = GetRobustFont();
            titleText.fontSize = 80;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(0.95f, 0.8f, 0.2f, 1f); // Spooky gold
            titleText.text = "YOU DIED";

            // Subtitle
            var subGo = new GameObject("Subtitle", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            subGo.SetParent(deathPanelGo, false);
            subGo.anchorMin = subGo.anchorMax = new Vector2(0.5f, 0.5f);
            subGo.anchoredPosition = new Vector2(0, 50);
            subGo.sizeDelta = new Vector2(800, 50);

            var subText = subGo.GetComponent<Text>();
            subText.font = GetRobustFont();
            subText.fontSize = 24;
            subText.alignment = TextAnchor.MiddleCenter;
            subText.color = new Color(0.85f, 0.2f, 0.2f, 0.85f); // Crimson
            subText.text = "The Alchemical Crypt claims your essence...";

            // Restart Button (Clean, block-styled, borderless charcoal slab)
            var btnGo = new GameObject("RestartButton", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            btnGo.SetParent(deathPanelGo, false);
            btnGo.anchorMin = btnGo.anchorMax = new Vector2(0.5f, 0.5f);
            btnGo.anchoredPosition = new Vector2(0, -90);
            btnGo.sizeDelta = new Vector2(340, 80);

            var btnImg = btnGo.GetComponent<Image>();
            btnImg.sprite = charcoalSprite;

            var btnTextGo = new GameObject("ButtonText", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            btnTextGo.SetParent(btnGo, false);
            btnTextGo.anchorMin = Vector2.zero; btnTextGo.anchorMax = Vector2.one;
            btnTextGo.offsetMin = btnTextGo.offsetMax = Vector2.zero;
            var btnText = btnTextGo.GetComponent<Text>();
            btnText.font = GetRobustFont();
            btnText.fontSize = 24;
            btnText.fontStyle = FontStyle.Bold;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = new Color(0.95f, 0.8f, 0.2f, 0.95f); // Spooky gold
            btnText.text = "RESTART VOYAGE";

            var helper = btnGo.gameObject.AddComponent<ButtonInputHelper>();
            helper.onDown = () => {
                btnGo.localScale = new Vector3(0.95f, 0.95f, 1f);
                btnText.color = new Color(0.8f, 0.65f, 0.1f, 0.95f);
            };
            helper.onUp = () => {
                btnGo.localScale = new Vector3(1f, 1f, 1f);
                btnText.color = new Color(0.95f, 0.8f, 0.2f, 0.95f);
                Time.timeScale = 1f;
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            };

            SetLayerRecursively(deathCanvasGo, 5);
        }

        private bool hasTintedWeapons = false;
        private void TryTintWeapons()
        {
            if (hasTintedWeapons) return;
            
            var sulfurGo = GameObject.Find("WEP_Sulfur");
            var mercuryGo = GameObject.Find("WEP_Mercury");
            var saltGo = GameObject.Find("WEP_Salt");

            if (sulfurGo == null && mercuryGo == null && saltGo == null) return;

            hasTintedWeapons = true;
            
            // Sulfur: Fiery Orange
            if (sulfurGo != null) TintWeaponMaterials(sulfurGo, new Color(1.0f, 0.35f, 0.05f));
            
            // Mercury: Cool Cyan
            if (mercuryGo != null) TintWeaponMaterials(mercuryGo, new Color(0.0f, 0.85f, 1.0f));
            
            // Salt: Bright White Crystalline
            if (saltGo != null) TintWeaponMaterials(saltGo, new Color(0.85f, 0.85f, 1.0f));
        }

        private void TintWeaponMaterials(GameObject go, Color col)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                if (r == null) continue;
                foreach (Material m in r.materials)
                {
                    if (m == null) continue;
                    if (m.HasProperty("_Color")) m.SetColor("_Color", col);
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
                    if (m.HasProperty("_EmissionColor"))
                    {
                        m.SetColor("_EmissionColor", col * 1.5f);
                        m.EnableKeyword("_EMISSION");
                    }
                }
            }
        }
    }

    public class LookSwipeZone : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
    {
        public float sensitivity = 0.08f; 
        private int trackedPointerId = -1;
        private void Start() => sensitivity = PlayerPrefs.GetFloat("MobileSensitivity", 0.08f);
        public void OnPointerDown(PointerEventData data) { if (trackedPointerId == -1) trackedPointerId = data.pointerId; }
        public void OnDrag(PointerEventData data) {
            if (data.pointerId != trackedPointerId) return;
            float deviceDpi = Screen.dpi > 0 ? Screen.dpi : 160f;
            Vector2 delta = data.delta * sensitivity * (160f / deviceDpi);
            if (delta.sqrMagnitude > 0.0001f) TheAlchemistsCrypt.Input.MobileInputManager.Instance?.SetLook(delta);
        }
        public void OnPointerUp(PointerEventData data) { if (data.pointerId == trackedPointerId) { trackedPointerId = -1; TheAlchemistsCrypt.Input.MobileInputManager.Instance?.ConsumeLook(); } }
    }

    public class JoystickDragHandler : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        public RectTransform backgroundRing;
        public RectTransform knobVisual;
        public float movementRange = 180f;

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(backgroundRing, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                float dist = localPoint.magnitude;
                if (dist > movementRange)
                {
                    localPoint = localPoint.normalized * movementRange;
                }
                knobVisual.anchoredPosition = localPoint;

                if (TheAlchemistsCrypt.Input.MobileInputManager.Instance != null)
                {
                    TheAlchemistsCrypt.Input.MobileInputManager.Instance.VirtualJoystickInput = localPoint / movementRange;
                }
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            knobVisual.anchoredPosition = Vector2.zero;
            if (TheAlchemistsCrypt.Input.MobileInputManager.Instance != null)
            {
                TheAlchemistsCrypt.Input.MobileInputManager.Instance.VirtualJoystickInput = Vector2.zero;
            }
        }
    }
}
