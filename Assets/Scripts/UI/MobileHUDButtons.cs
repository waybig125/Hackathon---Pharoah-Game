using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TheAlchemistsCrypt.UI
{
    public class MobileHUDButtons : MonoBehaviour
    {
        private GameObject root;
        private Text healthText;
        private Text ammoText;
        private Image damageOverlay;

        private void Start()
        {
            BuildHUD();
        }

        private void BuildHUD()
        {
            root = new GameObject("MobileHUD_Root");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            root.AddComponent<GraphicRaycaster>();

            // Center Crosshair
            var crosshair = new GameObject("Crosshair");
            crosshair.transform.SetParent(root.transform, false);
            var crossRect = crosshair.AddComponent<RectTransform>();
            crossRect.sizeDelta = new Vector2(10, 10);
            var crossImg = crosshair.AddComponent<Image>();
            crossImg.color = new Color(1f, 1f, 1f, 0.7f);

            // Font loading
            Font mainFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (mainFont == null) mainFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Stats Panel (Top Left)
            var stats = new GameObject("StatsPanel");
            stats.transform.SetParent(root.transform, false);
            var statsRect = stats.AddComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0, 1); statsRect.anchorMax = new Vector2(0, 1);
            statsRect.pivot = new Vector2(0, 1); statsRect.anchoredPosition = new Vector2(50, -50);
            statsRect.sizeDelta = new Vector2(600, 200);

            healthText = CreateText(stats.transform, "HealthText", "HEALTH: 100", mainFont, new Color(1f, 0.4f, 0.4f), 50, new Vector2(0, 0));
            ammoText = CreateText(stats.transform, "AmmoText", "AMMO: --", mainFont, new Color(1f, 0.85f, 0.2f), 50, new Vector2(0, -60));

            // Controls
            CreateJoystick(root.transform);
            CreateActionButton(root.transform, "FireButton", new Vector2(-150, 150), Color.red, () => {
                var input = TheAlchemistsCrypt.Input.MobileInputManager.Instance;
                if (input != null) input.SetFiring(true);
            });
            
            CreateActionButton(root.transform, "SwapButton", new Vector2(-400, 300), Color.yellow, () => {
                var charSvc = InfimaGames.LowPolyShooterPack.ServiceLocator.Current.Get<InfimaGames.LowPolyShooterPack.IGameModeService>();
                charSvc?.GetPlayerCharacter()?.GetComponent<InfimaGames.LowPolyShooterPack.Inventory>()?.GetEquipped()?.Reload();
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
            var shadow = obj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.8f); shadow.effectDistance = new Vector2(2, -2);
            return t;
        }

        private void CreateJoystick(Transform parent)
        {
            var joyRoot = new GameObject("Joystick");
            joyRoot.transform.SetParent(parent, false);
            var rect = joyRoot.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0); rect.anchorMax = new Vector2(0, 0);
            rect.pivot = new Vector2(0, 0); rect.anchoredPosition = new Vector2(200, 200);
            rect.sizeDelta = new Vector2(300, 300);

            var bg = new GameObject("BG");
            bg.transform.SetParent(joyRoot.transform, false);
            var bgRect = bg.AddComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(300, 300);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(1, 1, 1, 0.2f);

            var handle = new GameObject("Handle");
            handle.transform.SetParent(bg.transform, false);
            var hRect = handle.AddComponent<RectTransform>();
            hRect.sizeDelta = new Vector2(120, 120);
            var hImg = handle.AddComponent<Image>();
            hImg.color = new Color(1, 1, 1, 0.5f);

            var trigger = bg.AddComponent<EventTrigger>();
            AddTrigger(trigger, EventTriggerType.Drag, (e) => OnDrag((PointerEventData)e, bgRect, hRect));
            AddTrigger(trigger, EventTriggerType.PointerUp, (e) => {
                hRect.anchoredPosition = Vector2.zero;
                TheAlchemistsCrypt.Input.MobileInputManager.Instance?.SetMovement(Vector2.zero);
            });
        }

        private void OnDrag(PointerEventData eventData, RectTransform bg, RectTransform handle)
        {
            Vector2 pos = Vector2.zero;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(bg, eventData.position, eventData.pressEventCamera, out pos)) {
                float radius = bg.sizeDelta.x * 0.5f;
                pos = Vector2.ClampMagnitude(pos, radius);
                handle.anchoredPosition = pos;
                TheAlchemistsCrypt.Input.MobileInputManager.Instance?.SetMovement(pos / radius);
            }
        }

        private void CreateActionButton(Transform parent, string name, Vector2 pos, Color color, System.Action onClick)
        {
            var btn = new GameObject(name);
            btn.transform.SetParent(parent, false);
            var rect = btn.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0); rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(1, 0); rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(200, 200);
            var img = btn.AddComponent<Image>();
            img.color = new Color(color.r, color.g, color.b, 0.4f);
            var trigger = btn.AddComponent<EventTrigger>();
            AddTrigger(trigger, EventTriggerType.PointerDown, (e) => onClick());
            AddTrigger(trigger, EventTriggerType.PointerUp, (e) => {
                if (name == "FireButton") TheAlchemistsCrypt.Input.MobileInputManager.Instance?.SetFiring(false);
            });
        }

        private void AddTrigger(EventTrigger trigger, EventTriggerType type, System.Action<BaseEventData> callback)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener((e) => callback(e));
            trigger.triggers.Add(entry);
        }

        private void Update()
        {
            if (root == null) return;
            var charSvc = InfimaGames.LowPolyShooterPack.ServiceLocator.Current.Get<InfimaGames.LowPolyShooterPack.IGameModeService>();
            var character = charSvc?.GetPlayerCharacter();
            
            if (character != null) {
                var healthComp = character.GetComponent<TheAlchemistsCrypt.Player.PlayerHealth>();
                if (healthComp != null) healthText.text = $"HEALTH: {Mathf.CeilToInt(healthComp.currentHealth)}";
                
                var weapon = character.GetComponent<InfimaGames.LowPolyShooterPack.Inventory>()?.GetEquipped();
                if (weapon != null) ammoText.text = $"AMMO: {weapon.GetAmmunitionCurrent()} / {weapon.GetAmmunitionTotal()}";
            }
        }
    }
}
