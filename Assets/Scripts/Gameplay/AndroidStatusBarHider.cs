using UnityEngine;

namespace TheAlchemistsCrypt.Gameplay
{
    public class AndroidStatusBarHider : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            var go = new GameObject("AndroidStatusBarHider");
            go.AddComponent<AndroidStatusBarHider>();
            DontDestroyOnLoad(go);
            Debug.Log("[AndroidStatusBarHider] Spawned persistent hider GameObject.");
            #endif
        }

        private void Start()
        {
            ApplyImmersiveMode();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                ApplyImmersiveMode();
            }
        }

        private void ApplyImmersiveMode()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    if (activity == null)
                    {
                        Debug.LogError("[AndroidStatusBarHider] activity is null on Unity main thread.");
                        return;
                    }

                    activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                    {
                        try
                        {
                            using (var unityPlayerInner = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                            using (var activityInner = unityPlayerInner.GetStatic<AndroidJavaObject>("currentActivity"))
                            {
                                if (activityInner == null)
                                {
                                    Debug.LogError("[AndroidStatusBarHider] activityInner is null on UI thread.");
                                    return;
                                }
                                using (var window = activityInner.Call<AndroidJavaObject>("getWindow"))
                                {
                                    if (window == null)
                                    {
                                        Debug.LogError("[AndroidStatusBarHider] window is null on UI thread.");
                                        return;
                                    }
                                    using (var decorView = window.Call<AndroidJavaObject>("getDecorView"))
                                    {
                                        if (decorView == null)
                                        {
                                            Debug.LogError("[AndroidStatusBarHider] decorView is null on UI thread.");
                                            return;
                                        }
                                        // Flags for sticky immersive fullscreen
                                        int uiOptions = 0x00000002 | // SYSTEM_UI_FLAG_HIDE_NAVIGATION
                                                        0x00000004 | // SYSTEM_UI_FLAG_FULLSCREEN
                                                        0x00000100 | // SYSTEM_UI_FLAG_LAYOUT_STABLE
                                                        0x00000200 | // SYSTEM_UI_FLAG_LAYOUT_HIDE_NAVIGATION
                                                        0x00000400 | // SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN
                                                        0x00001000;  // SYSTEM_UI_FLAG_IMMERSIVE_STICKY

                                        decorView.Call("setSystemUiVisibility", uiOptions);
                                        Debug.Log("[AndroidStatusBarHider] Applied sticky immersive UI visibility flags.");
                                    }
                                }
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError("[AndroidStatusBarHider] Failed to set window UI visibility flags: " + e.Message);
                        }
                    }));
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[AndroidStatusBarHider] Failed to get Android activity: " + e.Message);
            }
            #endif
        }
    }
}
