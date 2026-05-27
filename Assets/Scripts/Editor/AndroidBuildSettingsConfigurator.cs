using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Android;
using UnityEngine;
using UnityEngine.Rendering;
using System.IO;
using System.Xml;

namespace TheAlchemistsCrypt.Editor
{
    public class AndroidBuildSettingsConfigurator
    {
        [MenuItem("Egyptian/Optimize Android Build Settings", false, 10)]
        public static void OptimizeAndroidSettings()
        {
            // 1. Set Product and Company Name
            PlayerSettings.productName = "Alchemist Crypt";
            PlayerSettings.companyName = "OffByAnA";
            Debug.Log("[AndroidOptimizer] Game name set to: Alchemist Crypt");

            // 2. Configure Logo as App Icon and Splash Screen
            ConfigureLogoAndSplash();

            // 3. Switch Active Build Target to Android if not already there
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.Log("[AndroidOptimizer] Switching Active Build Target to Android...");
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }

            // 4. Set Scripting Backend to IL2CPP (much faster execution)
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            Debug.Log("[AndroidOptimizer] Scripting backend set to IL2CPP.");

            // 5. Set IL2CPP compilation optimization to Release (maximizes optimization while allowing profiling)
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Android, Il2CppCompilerConfiguration.Release);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Low);
            Debug.Log("[AndroidOptimizer] IL2CPP compiler configured to Release with Low Managed Stripping.");

            // 6. Target ARM64 and ARMv7 architectures
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
            Debug.Log("[AndroidOptimizer] Target architectures configured for ARMv7 and ARM64.");

            // 7. Configure Graphics APIs to prefer Vulkan, then OpenGLES3
            GraphicsDeviceType[] apis = new GraphicsDeviceType[] {
                GraphicsDeviceType.Vulkan,
                GraphicsDeviceType.OpenGLES3
            };
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, apis);
            PlayerSettings.colorSpace = ColorSpace.Linear; // Linear lighting is standard and high quality
            Debug.Log("[AndroidOptimizer] Graphics APIs configured to prefer Vulkan, then OpenGL ES 3.");

            // 8. Enable Multithreaded Rendering
            PlayerSettings.MTRendering = true;
            Debug.Log("[AndroidOptimizer] Multithreaded rendering enabled.");

            // 9. Enable GPU Skinning to offload animation skinning from CPU to GPU
            PlayerSettings.gpuSkinning = true;
            Debug.Log("[AndroidOptimizer] GPU skinning enabled.");

            // 10. Configure Default Texture Compression to ASTC
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
            Debug.Log("[AndroidOptimizer] Android texture compression subtarget set to ASTC.");

            // 11. Optimize Android specific compiler flags
            PlayerSettings.Android.minifyDebug = true;
            PlayerSettings.Android.minifyRelease = true;
            
            // 12. Low memory / high performance settings
            PlayerSettings.Android.forceSDCardPermission = false;

            // 13. Enable Development Build by default to allow fast Application Patching and debugging
            EditorUserBuildSettings.development = true;
            EditorUserBuildSettings.connectProfiler = true;
            EditorUserBuildSettings.allowDebugging = true;
            Debug.Log("[AndroidOptimizer] Development Build options (debugging, profiling) enabled.");

            // Save settings
            AssetDatabase.SaveAssets();
            Debug.Log("[AndroidOptimizer] Android Build Settings successfully optimized for maximum performance!");
        }

        private static void ConfigureLogoAndSplash()
        {
            string logoPath = "Assets/Resources/LOGO_ALCHEMIST_CRYPT.png";
            
            // Set Icons using the logo texture (if available)
            Texture2D logoTex = AssetDatabase.LoadAssetAtPath<Texture2D>(logoPath);
            if (logoTex != null)
            {
                var androidSizes = PlayerSettings.GetIconSizes(NamedBuildTarget.Android, IconKind.Application);
                Texture2D[] androidIcons = new Texture2D[androidSizes.Length];
                for (int i = 0; i < androidIcons.Length; i++)
                {
                    androidIcons[i] = logoTex;
                }
                PlayerSettings.SetIcons(NamedBuildTarget.Android, androidIcons, IconKind.Application);

                var unknownSizes = PlayerSettings.GetIconSizes(NamedBuildTarget.Unknown, IconKind.Application);
                Texture2D[] unknownIcons = new Texture2D[unknownSizes.Length];
                for (int i = 0; i < unknownIcons.Length; i++)
                {
                    unknownIcons[i] = logoTex;
                }
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, unknownIcons, IconKind.Application);
                Debug.Log("[AndroidOptimizer] Logo assigned as application icon.");
            }
            else
            {
                Debug.LogWarning("[AndroidOptimizer] Could not find logo texture for icons at: " + logoPath);
            }

            // Configure Splash Screen: black background with Unity logo
            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.showUnityLogo = true;
            PlayerSettings.SplashScreen.backgroundColor = Color.black;
            PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.LightOnDark;
            PlayerSettings.SplashScreen.background = null;
            PlayerSettings.SplashScreen.backgroundPortrait = null;
            PlayerSettings.SplashScreen.logos = new PlayerSettings.SplashScreenLogo[0]; // Empty = only show Unity logo
            Debug.Log("[AndroidOptimizer] Configured black splash screen with Unity logo.");
        }
    }

    /// <summary>
    /// Post-build processor to inject game category into AndroidManifest.xml
    /// </summary>
    public class AndroidManifestModifier : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 100;

        public void OnPostGenerateGradleAndroidProject(string basePath)
        {
            Debug.Log("[AndroidManifestModifier] Beginning Android manifest modification...");
            
            // Locating the AndroidManifest.xml
            string manifestPath = Path.Combine(basePath, "src/main/AndroidManifest.xml");
            if (!File.Exists(manifestPath))
            {
                manifestPath = Path.Combine(basePath, "unityLibrary/src/main/AndroidManifest.xml");
            }

            if (!File.Exists(manifestPath))
            {
                Debug.LogError("[AndroidManifestModifier] AndroidManifest.xml was not found at: " + manifestPath);
                return;
            }

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(manifestPath);

                XmlNamespaceManager nsManager = new XmlNamespaceManager(doc.NameTable);
                nsManager.AddNamespace("android", "http://schemas.android.com/apk/res/android");

                XmlNode applicationNode = doc.SelectSingleNode("/manifest/application", nsManager);
                if (applicationNode != null)
                {
                    XmlElement applicationElement = (XmlElement)applicationNode;
                    string androidNs = "http://schemas.android.com/apk/res/android";
                    
                    bool changed = false;
                    
                    // Inject android:appCategory="game"
                    if (!applicationElement.HasAttribute("appCategory", androidNs))
                    {
                        applicationElement.SetAttribute("appCategory", androidNs, "game");
                        changed = true;
                        Debug.Log("[AndroidManifestModifier] Injected android:appCategory=\"game\" into manifest.");
                    }
                    else if (applicationElement.GetAttribute("appCategory", androidNs) != "game")
                    {
                        applicationElement.SetAttribute("appCategory", androidNs, "game");
                        changed = true;
                        Debug.Log("[AndroidManifestModifier] Updated android:appCategory to \"game\" in manifest.");
                    }

                    // Inject android:isGame="true" for backward compatibility with older devices
                    if (!applicationElement.HasAttribute("isGame", androidNs))
                    {
                        applicationElement.SetAttribute("isGame", androidNs, "true");
                        changed = true;
                        Debug.Log("[AndroidManifestModifier] Injected android:isGame=\"true\" into manifest.");
                    }
                    else if (applicationElement.GetAttribute("isGame", androidNs) != "true")
                    {
                        applicationElement.SetAttribute("isGame", androidNs, "true");
                        changed = true;
                        Debug.Log("[AndroidManifestModifier] Updated android:isGame to \"true\" in manifest.");
                    }

                    if (changed)
                    {
                        doc.Save(manifestPath);
                        Debug.Log("[AndroidManifestModifier] AndroidManifest.xml successfully saved with game category.");
                    }
                    else
                    {
                        Debug.Log("[AndroidManifestModifier] AndroidManifest.xml already has game category configured.");
                    }
                }
                else
                {
                    Debug.LogError("[AndroidManifestModifier] Could not locate <application> node in AndroidManifest.xml.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[AndroidManifestModifier] Exception during manifest modification: " + ex.Message);
            }
        }
    }
}
