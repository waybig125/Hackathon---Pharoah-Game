using UnityEngine;
using UnityEditor;

namespace TheAlchemistsCrypt.Editor
{
    [InitializeOnLoad]
    public static class AutoGenerator
    {
        static AutoGenerator()
        {
            // Run automatically on compile/load to ensure the gorgeous procedural terrain, 
            // dune bumps, and breakable yard crates are instantly populated in the active scene!
            EditorApplication.delayCall += () => {
                if (!EditorApplication.isPlaying)
                {
                    Debug.Log("[AutoGenerator] Automatically regenerating the gorgeous procedural Egyptian City...");
                    StaticEgyptianCityGenerator.QuickRegen();
                }
            };
        }
    }
}
// Trigger compilation: 174
