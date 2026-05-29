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
             
             if (healthBarFill) healthBarFill.fillAmount = fillTarget;
             if (healthValueText) healthValueText.text = Mathf.RoundToInt(Mathf.Clamp(h, 0f, 100f)) + "%";
             
             if (healthCatchUpFill != null)
             {
                 if (fillTarget >= healthCatchUpFill.fillAmount)
                 {
                     if (catchUpTween != null) catchUpTween.Kill();
                     healthCatchUpFill.fillAmount = fillTarget;
                 }
                 else
                 {
                     if (catchUpTween != null) catchUpTween.Kill();
                     catchUpTween = healthCatchUpFill.DOFillAmount(fillTarget, 1.5f).SetEase(Ease.OutQuad);
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
                    var focus = GameObject.FindAnyObjectByType<TheAlchemistsCrypt.Weapons.AlchemicalFocus>();
                    if (focus != null)
                    {
                        switch (focus.CurrentMode)
                        {
                            case TheAlchemistsCrypt.Weapons.AlchemicalFocus.FireMode.Sulfur:
                                modeName = "SULPHUR";
                                tickColor = new Color(0.95f, 0.55f, 0.05f, 0.95f);
                                break;
                            case TheAlchemistsCrypt.Weapons.AlchemicalFocus.FireMode.Mercury:
                                modeName = "MERCURY";
                                tickColor = new Color(0.1f, 0.75f, 0.95f, 0.95f);
                                break;
                            case TheAlchemistsCrypt.Weapons.AlchemicalFocus.FireMode.Salt:
                                modeName = "SALT";
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
                                if (wName.Contains("sulfur")) { tickColor = new Color(0.95f, 0.55f, 0.05f, 0.95f); modeName = "SULPHUR"; }
                                else if (wName.Contains("mercury")) { tickColor = new Color(0.1f, 0.75f, 0.95f, 0.95f); modeName = "MERCURY"; }
                                else if (wName.Contains("salt")) { tickColor = new Color(0.95f, 0.95f, 0.95f, 0.95f); modeName = "SALT"; }
                            }
                        }
                    }

                    for (int i = 0; i < 30; i++)
                    {
                        if (i < ammoTicks.Count && ammoTicks[i] != null)
                        {
                            if (i < c)
                            {
                                // Vibrant gold color for active ammo ticks
                                ammoTicks[i].color = new Color(1.0f, 0.82f, 0.12f, 0.95f);
                            }
                            else
                            {
                                // Dark warm empty slot
                                ammoTicks[i].color = new Color(0.15f, 0.1f, 0.05f, 0.5f);
                            }
                        }
                    }

                    if (ammoValueText)
                    {
                        ammoValueText.text = modeName;
                        ammoValueText.color = tickColor;
                    }
                }

    }
}
