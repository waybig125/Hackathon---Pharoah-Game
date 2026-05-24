using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TheAlchemistsCrypt.Editor
{
    public class AndroidBuildSettingsConfigurator
    {
        [MenuItem("Egyptian/Optimize Android Build Settings", false, 10)]
        public static void OptimizeAndroidSettings()
        {
            // 1. Switch Active Build Target to Android if not already there
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.Log("[AndroidOptimizer] Switching Active Build Target to Android...");
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }

            // 2. Set Scripting Backend to IL2CPP (much faster execution)
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            Debug.Log("[AndroidOptimizer] Scripting backend set to IL2CPP.");

            // 3. Target ARM64 and ARMv7 architectures
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
            Debug.Log("[AndroidOptimizer] Target architectures configured for ARMv7 and ARM64.");

            // 4. Configure Graphics APIs to prefer Vulkan, then OpenGLES3
            GraphicsDeviceType[] apis = new GraphicsDeviceType[] {
                GraphicsDeviceType.Vulkan,
                GraphicsDeviceType.OpenGLES3
            };
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, apis);
            PlayerSettings.colorSpace = ColorSpace.Linear; // Linear lighting is standard and high quality
            Debug.Log("[AndroidOptimizer] Graphics APIs configured to prefer Vulkan, then OpenGL ES 3.");

            // 5. Enable Multithreaded Rendering
            PlayerSettings.MTRendering = true;
            PlayerSettings.mobileMipsSplit = true;
            Debug.Log("[AndroidOptimizer] Multithreaded rendering enabled.");

            // 6. Enable GPU Skinning to offload animation skinning from CPU to GPU
            PlayerSettings.gpuSkinning = true;
            Debug.Log("[AndroidOptimizer] GPU skinning enabled.");

            // 7. Configure Default Texture Compression to ASTC
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
            Debug.Log("[AndroidOptimizer] Android texture compression subtarget set to ASTC.");

            // 8. Optimize Android specific compiler flags
            PlayerSettings.Android.minifyDebug = true;
            PlayerSettings.Android.minifyRelease = true;
            PlayerSettings.Android.minifyWithR8 = true;
            
            // 9. Low memory / high performance settings
            PlayerSettings.Android.forceSDCardPermission = false;
            PlayerSettings.Android.targetSandbox = true;

            // Save settings
            AssetDatabase.SaveAssets();
            Debug.Log("[AndroidOptimizer] Android Build Settings successfully optimized for maximum performance!");
        }
    }
}
