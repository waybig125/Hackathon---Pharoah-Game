using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace TheAlchemistsCrypt.Editor
{
    public class ColliderAudit : EditorWindow
    {
        [MenuItem("Tools/Audit Scene Colliders")]
        public static void ShowWindow()
        {
            GetWindow<ColliderAudit>("Collider Audit");
        }

        private Vector2 scrollPos;
        private List<GameObject> suspiciousObjects = new List<GameObject>();

        private void OnGUI()
        {
            if (GUILayout.Button("Find Invisible Colliders"))
            {
                RunAudit();
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            foreach (var obj in suspiciousObjects)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(obj, typeof(GameObject), true);
                if (GUILayout.Button("Select"))
                {
                    Selection.activeGameObject = obj;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private void RunAudit()
        {
            suspiciousObjects.Clear();
            Collider[] colliders = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);
            foreach (var col in colliders)
            {
                if (!col.isTrigger)
                {
                    var renderer = col.GetComponent<Renderer>();
                    if (renderer == null || !renderer.enabled)
                    {
                        suspiciousObjects.Add(col.gameObject);
                    }
                }
            }
            Debug.Log($"Found {suspiciousObjects.Count} suspicious colliders.");
        }
    }
}
