#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text;

namespace TheAlchemistsCrypt.Editor
{
    [InitializeOnLoad]
    public static class CheckLights
    {
        static CheckLights()
        {
            EditorApplication.delayCall += RunCheck;
        }

        private static void RunCheck()
        {
            var sb = new StringBuilder();
            sb.AppendLine("--- CHECKING ALL LIGHTS ---");
            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
            foreach (var l in lights)
            {
                var path = l.name;
                var t = l.transform.parent;
                while (t != null)
                {
                    path = t.name + "/" + path;
                    t = t.parent;
                }
                sb.AppendLine($"Light Path: {path} | Type: {l.type} | Range: {l.range} | Intensity: {l.intensity} | Enabled: {l.enabled} | Color: {l.color} | Position: {l.transform.position}");
            }

            sb.AppendLine("\n--- CHECKING PLAYER HIERARCHY ---");
            var player = GameObject.Find("Player");
            if (player == null)
            {
                var character = Object.FindAnyObjectByType<InfimaGames.LowPolyShooterPack.Character>(FindObjectsInactive.Include);
                if (character != null) player = character.gameObject;
            }

            if (player != null)
            {
                sb.AppendLine("Player Name: " + player.name);
                DumpTransform(player.transform, "", sb);
            }
            else
            {
                sb.AppendLine("Player not found in scene.");
            }

            File.WriteAllText("Assets/lights_log.txt", sb.ToString());
            Debug.Log("[CheckLights] Log written to Assets/lights_log.txt");
        }

        private static void DumpTransform(Transform t, string indent, StringBuilder sb)
        {
            sb.AppendLine($"{indent}- {t.name} (Position: {t.localPosition}, Active: {t.gameObject.activeSelf})");
            foreach (var comp in t.GetComponents<Component>())
            {
                if (comp != null && comp != t)
                {
                    sb.AppendLine($"{indent}  [Comp] {comp.GetType().Name}");
                }
            }
            for (int i = 0; i < t.childCount; i++)
            {
                DumpTransform(t.GetChild(i), indent + "  ", sb);
            }
        }
    }
}
#endif
