# Building TMT-VR

## 1. Install

- Unity Hub and **Unity 6000.2.10f1** with **Android Build Support** (OpenJDK, Android SDK and NDK) for the APKs.
- For the PC VR builds: **Windows Build Support (IL2CPP)** for Unity 6000.2.10f1, and Visual Studio 2022 with the C++ build tools and a Windows SDK.
- Keep the package versions in `Packages/packages-lock.json`. Upgrading packages changes the apps.

## 2. Open the project and restore the package samples

Some package sample content is not redistributed in this repository (see `NOTICE.md`). Restore it before opening any scene, otherwise those scenes show missing references.

1. Open the project in Unity 6000.2.10f1. Unity resolves the locked packages.
2. In **Window > Package Manager**, import these samples into their default locations:

   | Package | Version | Sample | Location after import |
   |---|---|---|---|
   | OpenXR Plugin (`com.unity.xr.openxr`) | 1.15.1 | Controller | `Assets/Samples/OpenXR Plugin/1.15.1/Controller` |
   | OpenXR Plugin | 1.15.1 | Controller XRInput | `Assets/Samples/OpenXR Plugin/1.15.1/Controller XRInput` |
   | XR Hands | 1.7.0 | HandVisualizer | `Assets/Samples/XR Hands/1.7.0/HandVisualizer` |

3. Close Unity. Copy `Assets/Samples/XR Hands/1.7.0/HandVisualizer/Materials/Blue.mat` to `Assets/TMTVR_Head_Geriatric/Samples_XR Hands_1.7.0_HandVisualizer_Materials_Blue.mat`. Beside the copy, create the file `Samples_XR Hands_1.7.0_HandVisualizer_Materials_Blue.mat.meta` with exactly this content, so that the copy keeps the GUID that `Joint.prefab` refers to:

   ```
   fileFormatVersion: 2
   guid: 344c028d9f8712b4ab1309e7190f9fdf
   NativeFormatImporter:
     externalObjects: {}
     mainObjectFileID: 2100000
     userData: 
     assetBundleName: 
     assetBundleVariant: 
   ```

   Then reopen the project. The material is used only by the hand-joint debug view, which is off in all four scenes. The released builds used a copy converted to the Universal Render Pipeline; this does not change what the apps show.

## 3. Build the four APKs

Switch the platform to **Android** (File > Build Profiles). The editor script `Assets/Editor/TMTVRReleaseBuild.cs` sets the scene, application ID, product name and version label for each app, builds, and then restores the previous settings. It has four entry points:

| Method | Scene | Application ID | Version label |
|---|---|---|---|
| `TMTVRReleaseBuild.BuildHeadEN` | `Assets/TMTVR_Head_EN.unity` | `com.DefaultCompany.VRTemplate.head.en` | `1.1.0 (HEAD, EN)` |
| `TMTVRReleaseBuild.BuildHeadGR` | `Assets/TMTVR_Head_GR.unity` | `com.DefaultCompany.VRTemplate.head.gr` | `1.1.0 (HEAD, GR)` |
| `TMTVRReleaseBuild.BuildControllerEN` | `Assets/TMTVR_Controller_EN.unity` | `com.DefaultCompany.VRTemplate.controller.en` | `1.1.0 (CONTROLLER, EN)` |
| `TMTVRReleaseBuild.BuildControllerGR` | `Assets/TMTVR_Controller_GR.unity` | `com.DefaultCompany.VRTemplate.controller.gr` | `1.1.0 (CONTROLLER, GR)` |

From the command line (one app per run; create the output folder first):

```
Unity -batchmode -quit -projectPath <path-to-this-repository> -buildTarget Android -executeMethod TMTVRReleaseBuild.BuildHeadEN -tmtvrOutput <absolute-path>/Builds/TMTVR_Head_EN.apk
```

The output must be a new `.apk` under the project's `Builds/` folder. The script also writes a `.build.json` receipt with the build result and the APK's SHA-256.

Unity reports one error during each build: "Failed to set the cursor because the specified texture ('GERIATRIC_LOGO_Final') was not CPU accessible." The build still completes. The script accepts this exact message and fails the build on any other error. The four released APKs were built with this message present.

Your APKs will not be byte-identical to the released ones, because builds embed timestamps and signing differs.

## 4. Install on the headset

Enable developer mode on the VIVE Focus Vision, connect it by USB and run:

```
adb install -r Builds/TMTVR_Head_EN.apk
```

Each app has its own application ID, so the four apps can be installed side by side. Uninstalling an app deletes its data folder on the headset; copy any result files off first (see `docs/DATA_OUTPUTS.md`).

## 5. Build the four PC VR builds

Switch the platform to **Windows**, architecture x64 (File > Build Profiles). The editor script `Assets/Editor/TMTVRReleaseBuildPCVR.cs` works like the Android script, with the same method names, scenes, application IDs and product names. Its version labels add the platform:

| Method | Scene | Version label |
|---|---|---|
| `TMTVRReleaseBuildPCVR.BuildHeadEN` | `Assets/TMTVR_Head_EN.unity` | `1.1.0 (HEAD, EN, PCVR)` |
| `TMTVRReleaseBuildPCVR.BuildHeadGR` | `Assets/TMTVR_Head_GR.unity` | `1.1.0 (HEAD, GR, PCVR)` |
| `TMTVRReleaseBuildPCVR.BuildControllerEN` | `Assets/TMTVR_Controller_EN.unity` | `1.1.0 (CONTROLLER, EN, PCVR)` |
| `TMTVRReleaseBuildPCVR.BuildControllerGR` | `Assets/TMTVR_Controller_GR.unity` | `1.1.0 (CONTROLLER, GR, PCVR)` |

From the command line (one build per run; create the output folder first):

```
Unity -batchmode -quit -projectPath <path-to-this-repository> -buildTarget Win64 -executeMethod TMTVRReleaseBuildPCVR.BuildHeadEN -tmtvrOutput <absolute-path>/Builds/TMTVR_Head_EN_PCVR/TMTVR_Head_EN_PCVR.exe
```

The output must be a new `.exe` under the project's `Builds/` folder. Unity writes the player's data folder and runtime files beside the `.exe`. The script writes its `.build.json` receipt into the same folder. The receipt holds your project path, so remove it before sharing the folder. Folders whose names end in `_BackUpThisFolder_ButDontShipItWithYourGame` or `_DoNotShip` are build by-products and are not part of the player. If Unity reports the cursor message described in section 3, the script accepts it in the same way.

## 6. Run a PC VR build

1. Connect the VIVE Focus Vision to the PC for PC VR through an OpenXR runtime that supports it, and make that runtime the active OpenXR runtime on the PC.
2. Keep all files of the build folder together and run the `.exe`.

Controller input reaches the app through one of the OpenXR controller profiles enabled for Windows in this project, among them HTC Vive Controller, Valve Index, Oculus Touch, Meta Quest Touch Plus and Khronos Simple Controller. The runtime must offer one of them for the headset's controllers.

Two PC VR builds (TMT-VR Head EN and TMT-VR Controller EN) were checked visually on the headset with SteamVR 2.14.5.

Result files are written on the PC, not on the headset (see `docs/DATA_OUTPUTS.md`).
