#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace TheAlchemistsCrypt.Editor
{
    public static class DiagHelper
    {
        [MenuItem("Egyptian/Run Diagnostics", false, 10)]
        public static void RunDiagnostics()
        {
            Debug.Log("--- RUNNING ALCHEMIST DIAGNOSTICS ---");
            
            // 1. Find all AlchemicalFocus components
            var focuses = Object.FindObjectsByType<TheAlchemistsCrypt.Weapons.AlchemicalFocus>(FindObjectsInactive.Include);
            Debug.Log($"Found {focuses.Length} AlchemicalFocus components in the scene.");
            foreach (var f in focuses)
            {
                Debug.Log($"AlchemicalFocus found on GameObject: {f.gameObject.name}, ActiveInHierarchy: {f.gameObject.activeInHierarchy}, Enabled: {f.enabled}, CurrentMode: {f.CurrentMode}");
            }

            // 2. Check for missing scripts on any GameObjects
            var allGo = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            int missingCount = 0;
            foreach (var go in allGo)
            {
                var components = go.GetComponents<Component>();
                foreach (var c in components)
                {
                    if (c == null)
                    {
                        Debug.LogWarning($"GameObject '{go.name}' has a Missing Script component!", go);
                        missingCount++;
                    }
                }
            }
            Debug.Log($"Found {missingCount} missing components total.");
            
            // 3. Print active weapon info
            var character = Object.FindAnyObjectByType<InfimaGames.LowPolyShooterPack.Character>(FindObjectsInactive.Include);
            if (character != null)
            {
                Debug.Log($"Found Character: {character.gameObject.name}");
                var weapon = character.GetEquippedWeapon();
                if (weapon != null)
                {
                    Debug.Log($"Equipped Weapon: {weapon.name}");
                }
                else
                {
                    Debug.Log("No weapon equipped.");
                }
            }
            else
            {
                Debug.Log("No Infima Character found in the scene.");
            }
            
            Debug.Log("--- DIAGNOSTICS COMPLETE ---");
        }
    }
}
#endif
