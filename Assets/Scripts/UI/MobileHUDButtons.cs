using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace TheAlchemistsCrypt.UI
{
    public class MobileHUDButtons : MonoBehaviour
    {
        public static MobileHUDButtons Instance { get; private set; }

        private Text healthText;
        private Text ammoText;
        private Image damageOverlay;
        
        private Color goldColor = new Color(0.9f, 0.75f, 0.2f);
        private Color darkColor = new Color(0.1f, 0.08f, 0.05f, 0.8f);

        private void Awake()
        {
            Instance = this;
            BuildHUD();
        }

        private void BuildHUD()
        {
            var root = new GameObject("HUD_Root");
            root.transform.SetParent(transform, false);
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero; rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero; rootRect.offsetMax = Vector2.zero;

            // Center Crosshair
            var crosshair = new GameObject("Crosshair");
            crosshair.transform.SetParent(root.transform, false);
            var crossRect = crosshair.AddComponent<RectTransform>();
            crossRect.sizeDelta = new Vector2(12, 12);
            var crossImg = crosshair.AddComponent<Image>();
            crossImg.color = new Color(1f, 0.9f, 0.5f, 0.8f);

            // Font loading - FIXED for Unity 6
            Font mainFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Stats Panel (Top Left) - Premium Egyptian Gold
            var stats = new GameObject("StatsPanel");
            stats.transform.SetParent(root.transform, false);
            var statsRect = stats.AddComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0, 1); statsRect.anchorMax = new Vector2(0, 1);
            statsRect.pivot = new Vector2(0, 1); statsRect.anchoredPosition = new Vector2(60, -60);
            statsRect.sizeDelta = new Vector2(600, 200);

            healthText = CreateText(stats.transform, "HealthText", "HEALTH: 100", mainFont, new Color(1f, 0.3f, 0.3f), 55, new Vector2(0, 0));
            ammoText = CreateText(stats.transform, "AmmoText", "AMMO: --", mainFont, goldColor, 55, new Vector2(0, -70));

            // Controls
            CreateJoystick(root.transform);
            
            // Primary Buttons
            CreateActionButton(root.transform, "Fire", new Vector2(-180, 180), new Color(0.8f, 0.2f, 0.1f), () => {
                var input = TheAlchemistsCrypt.Input.MobileInputManager.Instance;
                if (input != null) input.SetFiring(true);
            }, true);

            CreateActionButton(root.transform, "Reload", new Vector2(-180, 420), goldColor, () => {
                var charSvc = InfimaGames.LowPolyShooterPack.ServiceLocator.Current.Get<InfimaGames.LowPolyShooterPack.IGameModeService>();
                charSvc?.GetPlayerCharacter()?.GetComponent<InfimaGames.LowPolyShooterPack.Inventory>()?.GetEquipped()?.Reload();
            });

            // Switch button with Elemental Theme (Mercury Aqua, Sulfur Red/Orange, Salt White)
            CreateActionButton(root.transform, "Switch", new Vector2(-420, 180), new Color(0.2f, 0.8f, 0.9f), () => {
                var inventory = InfimaGames.LowPolyShooterPack.ServiceLocator.Current.Get<InfimaGames.LowPolyShooterPack.IGameModeService>()?.GetPlayerCharacter()?.GetComponent<InfimaGames.LowPolyShooterPack.Inventory>();
                if (inventory != null) inventory.Equip(inventory.GetNextIndex());
            });

            // Damage Overlay
            var overlayObj = new GameObject("DamageOverlay");
            overlayObj.transform.SetParent(root.transform, false);
            overlayObj.transform.SetAsFirstSibling();
            var ovRect = overlayObj.AddComponent<RectTransform>();
            ovRect.anchorMin = Vector2.zero; ovRect.anchorMax = Vector2.one;
            ovRect.offsetMin = Vector2.zero; ovRect.offsetMax = Vector2.zero;
            damageOverlay = overlayObj.AddComponent<Image>();
            damageOverlay.color = new Color(1f, 0f, 0f, 0f);
            damageOverlay.raycastTarget = false;
        }

        private Text CreateText(Transform parent, string name, string val, Font font, Color color, int size, Vector2 pos)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1); rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(500, 80);
            var t = obj.AddComponent<Text>();
            t.font = font; t.fontSize = size; t.color = color; t.alignment = TextAnchor.MiddleLeft;
            t.fontStyle = FontStyle.Bold;
            var shadow = obj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.9f); shadow.effectDistance = new Vector2(3, -3);
            return t;
        }

        private void CreateJoystick(Transform parent)
        {
            var joyRoot = new GameObject("Joystick_Anchor");
            joyRoot.transform.SetParent(parent, false);
            var rect = joyRoot.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero; rect.anchoredPosition = new Vector2(150, 150);
            rect.sizeDelta = new Vector2(350, 350);
            
            // Visual circle
            var bg = new GameObject("BG");
            bg.transform.SetParent(joyRoot.transform, false);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = darkColor;
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(350, 350);
            
            // Handle
            var handle = new GameObject("Handle");
            handle.transform.SetParent(bg.transform, false);
            var hImg = handle.AddComponent<Image>();
            hImg.color = goldColor;
            var hRect = handle.GetComponent<RectTransform>();
            hRect.sizeDelta = new Vector2(120, 120);
        }

        private void CreateActionButton(Transform parent, string label, Vector2 pos, Color color, System.Action onClick, bool isBig = false)
        {
            var btnObj = new GameObject(label);
            btnObj.transform.SetParent(parent, false);
            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.one; rect.anchorMax = Vector2.one;
            rect.anchoredPosition = pos;
            float size = isBig ? 220f : 160f;
            rect.sizeDelta = new Vector2(size, size);

            var img = btnObj.AddComponent<Image>();
            img.color = darkColor;
            
            // Border
            var border = new GameObject("Border");
            border.transform.SetParent(btnObj.transform, false);
            var bImg = border.AddComponent<Image>();
            bImg.color = color;
            var bRect = border.GetComponent<RectTransform>();
            bRect.anchorMin = Vector2.zero; bRect.anchorMax = Vector2.one;
            bRect.offsetMin = new Vector2(-4, -4); bRect.offsetMax = new Vector2(4, 4);
            border.transform.SetAsFirstSibling();

            var btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            var lblObj = new GameObject("Label");
            lblObj.transform.SetParent(btnObj.transform, false);
            var lblT = lblObj.AddComponent<Text>();
            lblT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lblT.text = label.ToUpper();
            lblT.color = goldColor;
            lblT.fontSize = isBig ? 40 : 30;
            lblT.alignment = TextAnchor.MiddleCenter;
            lblObj.GetComponent<RectTransform>().sizeDelta = rect.sizeDelta;
        }

        public void UpdateHealth(float health)
        {
            if (healthText != null) healthText.text = $"HEALTH: {Mathf.CeilToInt(health)}";
            if (health < 30) StartCoroutine(FlashHealth());
        }

        public void UpdateAmmo(int current, int total)
        {
            if (ammoText != null) ammoText.text = $"AMMO: {current} / {total}";
        }

        public void TriggerDamage()
        {
            if (damageOverlay != null) StartCoroutine(DamageFlashRoutine());
        }

        private IEnumerator DamageFlashRoutine()
        {
            damageOverlay.color = new Color(1f, 0f, 0f, 0.4f);
            float t = 0;
            while (t < 1f) {
                t += Time.deltaTime * 2f;
                damageOverlay.color = new Color(1f, 0f, 0f, Mathf.Lerp(0.4f, 0f, t));
                yield return null;
            }
        }

        private IEnumerator FlashHealth()
        {
            healthText.color = Color.white;
            yield return new WaitForSeconds(0.2f);
            healthText.color = new Color(1f, 0.3f, 0.3f);
        }
    }
}
