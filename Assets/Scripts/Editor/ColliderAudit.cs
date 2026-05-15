using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

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
        private List<MeshCollider> meshColliders = new List<MeshCollider>();

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("This tool lists all MeshColliders in the scene. Use it to find and remove invisible walls or problematic collision meshes manually.", MessageType.Info);

            if (GUILayout.Button("Find All Mesh Colliders", GUILayout.Height(30)))
            {
                RunAudit();
            }

            if (meshColliders.Count > 0)
            {
                EditorGUILayout.LabelField($"Found {meshColliders.Count} MeshColliders", EditorStyles.boldLabel);
                
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
                
                for (int i = meshColliders.Count - 1; i >= 0; i--)
                {
                    var col = meshColliders[i];
                    if (col == null) 
                    {
                        meshColliders.RemoveAt(i);
                        continue;
                    }

                    EditorGUILayout.BeginHorizontal("box");
                    
                    EditorGUILayout.ObjectField(col.gameObject, typeof(GameObject), true, GUILayout.Width(200));
                    
                    if (GUILayout.Button("Select", GUILayout.Width(80)))
                    {
                        Selection.activeGameObject = col.gameObject;
                        EditorGUIUtility.PingObject(col.gameObject);
                    }
                    
                    GUI.backgroundColor = Color.red;
                    if (GUILayout.Button("Delete", GUILayout.Width(80)))
                    {
                        Undo.DestroyObjectImmediate(col);
                        meshColliders.RemoveAt(i);
                    }
                    GUI.backgroundColor = Color.white;
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();
            }
        }

        private void RunAudit()
        {
            meshColliders.Clear();
            meshColliders = Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None).ToList();
            Debug.Log($"Found {meshColliders.Count} MeshColliders in the scene.");
        }
    }
}
