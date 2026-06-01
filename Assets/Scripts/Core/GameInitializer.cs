using UnityEngine;

namespace TheAlchemistsCrypt.Core
{
    public class GameInitializer : MonoBehaviour
    {
        [Header("Performance Settings")]
        [SerializeField] private int targetFrameRate = 30; // 30 FPS lock for mobile stability

        private void Awake()
        {
            // Set target frame rate for mobile stability
            Application.targetFrameRate = targetFrameRate;
            
            // Disable screen dimming
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // Hide developer console and stats in production/mobile builds
            Debug.developerConsoleVisible = false;

            Debug.Log($"Game Initialized. Target Frame Rate: {Application.targetFrameRate}");
        }
    }
}
