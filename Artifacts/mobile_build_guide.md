# Production Mobile Build & USB Debugging Guide

This guide details how to resolve Unity Remote USB debugging connection issues and prepare the **Pharaoh Game** for a production-ready Android APK / iOS build in Unity 6.

---

## 1. Troubleshooting Unity Remote & USB Debugging

If Unity Remote 5 was working earlier but stopped responding via USB, follow this systematic resolution protocol.

### Step 1: Editor Connection Settings
1. In the Unity Editor, navigate to **Edit > Project Settings > Editor**.
2. Under the **Unity Remote** section:
   - **Device**: Change from *None* to **Any Android Device** (or **Any iOS Device** if on iOS).
   - **Compression**: Set to **JPEG** (best compatibility) or **PNG** (highest quality).
   - **Resolution**: Set to **Normal** or **Downsize** (reduces latency over USB).

### Step 2: Android USB Debugging Setup (Most Common Failure Point)
1. Ensure the mobile device has **Developer Options** enabled (Tap *Build Number* 7 times in Android Settings).
2. Enable **USB Debugging** and **Install via USB**.
3. **CRITICAL**: Change the USB Connection Mode on your phone notification panel from *Charging Only* to **MIDI** or **PTP (Camera)** / **File Transfer (MTP)**. Unity Remote often fails to detect devices in "Charging" mode.
4. If on macOS, open your terminal and verify the device is recognized by the Android Debug Bridge (ADB):
   ```bash
   ~/Library/Android/sdk/platform-tools/adb devices
   ```
   *If your device list is empty, restart the adb server:*
   ```bash
   ~/Library/Android/sdk/platform-tools/adb kill-server
   ~/Library/Android/sdk/platform-tools/adb start-server
   ```

### Step 3: Unity Remote Launch Sequence
For Unity to hook the device stream, follow this exact sequence:
1. **Close** the Unity Editor.
2. **Disconnect** the USB cable from your computer.
3. Open the **Unity Remote 5** app on your phone.
4. **Reconnect** the USB cable. (Confirm any "Allow USB Debugging" dialogs on the phone).
5. **Open** the Unity Editor.
6. Press **Play** in Unity. The game should instantly stream to your mobile screen!

---

## 2. Production APK / iOS Build Readiness Checklist

To build a production-ready standalone executable, verify the following configuration panels in **Project Settings**:

### A. Player Settings & Identity
* Go to **Edit > Project Settings > Player**.
* **Company Name**: `The Alchemists Crypt`
* **Product Name**: `Pharaoh's Gold`
* **Version**: `1.0.0`
* **Default Icon**: Ensure a high-resolution icon is assigned under the **Icon** tab.

### B. Resolution and Presentation
* **Orientation**: Set **Default Orientation** to **Landscape Left** or **Landscape Right** (lock portrait mode to prevent UI layout breakage).
* **Render outside safe area**: Checked (allows background stretching while controls remain constrained).

### C. Publishing Settings (Android)
* **Keystore**: In production, create a keystore under **Publishing Settings > Keystore Manager** to sign the APK. Unsigned APKs cannot be uploaded to Google Play or updated in the future.
* **Target API Level**: Set to **API Level 34 (Android 14)** or the latest required by Google Play.

### E. Graphics & Shader Optimization (For 60 FPS Mobile Performance)
* **Scripting Backend**: Open **Player Settings > Other Settings** and change **Scripting Backend** from *Mono* to **IL2CPP**.
  * *Why?* IL2CPP compiles C# code directly into high-performance native C++ binaries, delivering a **1.5x to 3x performance boost** essential for mobile.
* **Target Architectures**: Enable both **ARMv7** and **ARM64** (Google Play requires ARM64 support).
* **Shader Stripping**: In URP Asset Settings, enable *Shader Stripping* to reduce APK size and build times.

---

## 3. Recommended Production Auto-Build Command

To build the project directly from the macOS terminal without opening the Unity Editor GUI:

```bash
/Applications/Unity/Hub/Editor/6000.0.38f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -quit \
  -projectPath "/Users/mac/Documents/Hackathon/Hackathon - Pharoah Game" \
  -buildTarget Android \
  -executeMethod TheAlchemistsCrypt.Editor.BuildPipeline.BuildAndroidAPK
```

This ensures a clean, deterministic build directly from your repository workspace!
