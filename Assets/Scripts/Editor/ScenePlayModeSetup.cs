using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TheAlchemistsCrypt.Editor
{
    [InitializeOnLoad]
    public static class ScenePlayModeSetup
    {
        static ScenePlayModeSetup()
        {
            string bootScenePath = "Assets/Scenes/BootScene.unity";
            SceneAsset bootScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(bootScenePath);
            
            if (bootScene != null)
            {
                if (EditorSceneManager.playModeStartScene != bootScene)
                {
                    EditorSceneManager.playModeStartScene = bootScene;
                    Debug.Log($"<color=#ffaa00>[The Alchemist Crypt]</color> Play Mode start scene automatically set to: <b>{bootScenePath}</b>");
                }
            }
            else
            {
                Debug.LogWarning("[The Alchemist Crypt] PlayModeStartSceneSetup: Could not find BootScene at path: " + bootScenePath);
            }
        }
    }
}
