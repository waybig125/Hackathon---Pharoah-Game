using UnityEngine;

namespace TheAlchemistsCrypt.Core
{
    public class GameInitializer : MonoBehaviour
    {
        [Header("Performance Settings")]
        [SerializeField] private int targetFrameRate = 30;

        private void Awake()
        {
            // Set target frame rate for mobile stability
            Application.targetFrameRate = targetFrameRate;
            
            // Disable screen dimming
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            Debug.Log($"Game Initialized. Target Frame Rate: {Application.targetFrameRate}");
        }
    }
}
