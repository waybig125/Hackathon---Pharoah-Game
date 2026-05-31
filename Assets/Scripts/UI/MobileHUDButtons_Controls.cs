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


                private void UpdateSprintVisuals() {
                    if (sprintShadowImage && sprintButtonIconImg) {
                        sprintShadowImage.gameObject.SetActive(sprintToggleState);
                        // Full opacity when active (1.0), 80% opacity when idle (0.8)
                        sprintButtonIconImg.color = sprintToggleState ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 1f, 1f, 0.8f);
                    }
                }

         private void HideDebugLabels() {
                    string[] names = { "Text Timescale", "Text Cursor Lock", "Text Tutorial", "Text Tutorial Text", "Text Tutorial Prompt", "Version Text", "Mouse Lock" };
                    foreach (var n in names) { var l = GameObject.Find(n); if (l != null) l.SetActive(false); }
                }

         private void TryInitializeCache()
                {
                    if (cachedCharacter == null) cachedCharacter = GameObject.FindAnyObjectByType<InfimaGames.LowPolyShooterPack.Character>(FindObjectsInactive.Include);
                    if (cachedFocus == null) cachedFocus = GameObject.FindAnyObjectByType<TheAlchemistsCrypt.Weapons.AlchemicalFocus>(FindObjectsInactive.Include);
                    if (cachedHealth == null) cachedHealth = GameObject.FindAnyObjectByType<TheAlchemistsCrypt.Player.PlayerHealth>();
                    isCacheInitialized = (cachedCharacter != null || cachedHealth != null);
                }



                private void DisableCompetingCanvases()
                {
                    var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
                    foreach (var c in canvases) {
                        if (c.gameObject.name == "MobileHUD_Root" || 
                            c.gameObject.name == "StartScreenOverlay" || 
                            c.gameObject.name == "DeathCanvas" || 
                            c.gameObject.name == "VictoryCanvas" || 
                            (c.gameObject.name == "Canvas" && c.gameObject.GetComponent<MobileHUDButtons>() != null)) continue;
                        string nameLower = c.gameObject.name.ToLower();
                        if (nameLower.Contains("lpsp") || nameLower.Contains("weaponui") || nameLower.Contains("hud") || nameLower.Contains("canvas") || nameLower.Contains("joystick")) {
                            if (c.gameObject != gameObject && c.gameObject.name != "MobileHUD_Root" && c.gameObject.name != "StartScreenOverlay" && c.gameObject.name != "DeathCanvas" && c.gameObject.name != "VictoryCanvas") {
                                c.gameObject.SetActive(false);
                            }
                        }
                    }
                }

    }
}
