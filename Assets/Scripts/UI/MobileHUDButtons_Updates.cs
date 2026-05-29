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
         public void UpdateHealth(float h)
         {
             if (healthText) healthText.text = "";
             float fillTarget = Mathf.Clamp01(h / 100f);
             
             if (healthBarFill) healthBarFill.rectTransform.anchorMax = new Vector2(fillTarget, 1f);
             if (healthValueText) healthValueText.text = Mathf.RoundToInt(Mathf.Clamp(h, 0f, 100f)) + "%";
             
             if (healthCatchUpFill != null)
             {
                 if (fillTarget >= healthCatchUpFill.rectTransform.anchorMax.x)
                 {
                     if (catchUpTween != null) catchUpTween.Kill();
                     healthCatchUpFill.rectTransform.anchorMax = new Vector2(fillTarget, 1f);
                 }
                 else
                 {
                     if (catchUpTween != null) catchUpTween.Kill();
                     catchUpTween = healthCatchUpFill.rectTransform.DOAnchorMax(new Vector2(fillTarget, 1f), 1.5f).SetEase(Ease.OutQuad);
                 }
             }

             if (gameplayBloodVignette != null)
             {
                 if (fillTarget < 0.35f && fillTarget > 0f)
                 {
                     // Opacity pulse: pulse rate and max opacity increases as health decreases
                     float dangerFactor = 1f - (fillTarget / 0.35f); // 0 at 35% health, 1 at 0% health
                     float pulseSpeed = 3f + dangerFactor * 7f; // faster pulse at lower health
                     float maxAlpha = 0.15f + dangerFactor * 0.65f; // deeper red at lower health
                     float sine = Mathf.Sin(Time.time * pulseSpeed);
                     float normalizedSine = (sine + 1f) * 0.5f; // 0 to 1
                     float targetAlpha = maxAlpha * (0.3f + normalizedSine * 0.7f);
                     
                     var c = gameplayBloodVignette.color;
                     gameplayBloodVignette.color = new Color(c.r, c.g, c.b, targetAlpha);
                 }
                 else
                 {
                     var c = gameplayBloodVignette.color;
                     gameplayBloodVignette.color = new Color(c.r, c.g, c.b, 0f);
                 }
             }
         }



         public void UpdateAmmo(int c, int t)
         {
             if (ammoText) ammoText.text = "";
             
             if (c < lastAmmoCount)
             {
                 if (ammoValueText != null)
                 {
                     ammoValueText.transform.DOKill();
                     ammoValueText.transform.localScale = Vector3.one;
                     ammoValueText.transform.DOPunchScale(new Vector3(0.3f, 0.3f, 0f), 0.15f, 10, 1f);
                 }
             }
             else if (c > lastAmmoCount && lastAmmoCount != -1)
             {
                 if (ammoValueText != null)
                 {
                     ammoValueText.transform.DOKill();
                     ammoValueText.transform.localScale = Vector3.one;
                     ammoValueText.transform.DOPunchScale(new Vector3(0.4f, 0.4f, 0f), 0.4f, 8, 1f);
                     
                     Color originalColor = ammoValueText.color;
                     ammoValueText.color = Color.white;
                     ammoValueText.DOColor(originalColor, 0.4f);
                 }
             }
             lastAmmoCount = c;
             
             string modeName = "SULPHUR";
             Color tickColor = new Color(0.95f, 0.8f, 0.2f, 0.95f);
             Sprite fillSprite = sulfurBarSprite;

             var focus = GameObject.FindAnyObjectByType<TheAlchemistsCrypt.Weapons.AlchemicalFocus>();
             if (focus != null)
             {
                 switch (focus.CurrentMode)
                 {
                     case TheAlchemistsCrypt.Weapons.AlchemicalFocus.FireMode.Sulfur:
                         modeName = "SULPHUR";
                         tickColor = new Color(0.95f, 0.55f, 0.05f, 0.95f);
                         fillSprite = sulfurBarSprite;
                         break;
                     case TheAlchemistsCrypt.Weapons.AlchemicalFocus.FireMode.Mercury:
                         modeName = "MERCURY";
                         tickColor = new Color(0.1f, 0.75f, 0.95f, 0.95f);
                         fillSprite = mercuryBarSprite;
                         break;
                     case TheAlchemistsCrypt.Weapons.AlchemicalFocus.FireMode.Salt:
                         modeName = "SALT";
                         tickColor = new Color(0.95f, 0.95f, 0.95f, 0.95f);
                         fillSprite = saltBarSprite;
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
                         if (wName.Contains("sulfur")) { tickColor = new Color(0.95f, 0.55f, 0.05f, 0.95f); modeName = "SULPHUR"; fillSprite = sulfurBarSprite; }
                         else if (wName.Contains("mercury")) { tickColor = new Color(0.1f, 0.75f, 0.95f, 0.95f); modeName = "MERCURY"; fillSprite = mercuryBarSprite; }
                         else if (wName.Contains("salt")) { tickColor = new Color(0.95f, 0.95f, 0.95f, 0.95f); modeName = "SALT"; fillSprite = saltBarSprite; }
                     }
                 }
             }

             if (ammoBarFill != null)
             {
                 ammoBarFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01((float)c / 30f), 1f);
                 ammoBarFill.color = tickColor;
             }

             if (ammoValueText != null)
             {
                 ammoValueText.text = $"{c}/30";
                 ammoValueText.color = tickColor;
             }
         }

    }
}
