# TMT-VR build variants

The project has four scenes, one per app. Each scene is built twice: as an Android APK for the standalone headset and as a Windows 64-bit build for PC VR. The four scenes contain the same objects and scripts and differ only in the serialized settings listed here, including the placement and font of the English end-screen panel. The two builds of one scene differ only in target platform and version label (section 4). "The four builds" below means the four apps on either platform.

> **English scenes.** The English scenes were derived from the Greek scenes by the serialized changes in section 3. Before release, each English APK was checked briefly on the headset: start-up, the first spoken instruction and the first instruction overlay, all in English. The end screen was not reached during this check. No validation study of the English versions is reported.

Related documents: [ARCHITECTURE.md](ARCHITECTURE.md), [SCRIPTS.md](SCRIPTS.md), [DATA_OUTPUTS.md](DATA_OUTPUTS.md).

## 1. The four builds

| Build | Scene | Interaction mode (`intMode`) | Language | Motion logging (`motionLogEnabled`) |
|---|---|---|---|---|
| Head, English | `Assets/TMTVR_Head_EN.unity` | 2 (`head`) | English | off |
| Head, Greek | `Assets/TMTVR_Head_GR.unity` | 2 (`head`) | Greek | off |
| Controller, English | `Assets/TMTVR_Controller_EN.unity` | 3 (`controller`) | English | on |
| Controller, Greek | `Assets/TMTVR_Controller_GR.unity` | 3 (`controller`) | Greek | on |

- **Head builds.** The ray follows the head (Main Camera forward direction). There is no dominance step. The motion logger is present but switches itself off at start, so the score file is the only data output.
- **Controller builds.** After start-up the participant touches a cube with one controller. That controller's tip becomes the ray origin. The passive motion logger is on and writes an event file and a pose file per session.

Eye-tracking and hand-tracking modes exist in code but are not used by any of the four builds. They have not been validated.

## 2. Head versus Controller

The Head and Controller scenes of one language differ in two serialized values only.

| Object | Component | Field | Head | Controller |
|---|---|---|---|---|
| `Managers` | `TMTVR_CentralExecutive` | `intMode` | 2 (`head`) | 3 (`controller`) |
| `Managers` | `TMTVR_MotionLogger` | `motionLogEnabled` | 0 (off) | 1 (on) |

Everything else that differs at runtime follows from `intMode`:
- the `OpenXRAimPointer` flag set at start-up (`Head` or `Controller`);
- whether the dominance cube (`ProbeDomiCube`) and its spoken prompt appear (Controller only);
- which tutorial instruction clips play: `Tutorial_A_Head` and `Tutorial_B_Head` in the Head builds, `Tutorial_A_Pointer` and `Tutorial_B_Pointer` in the Controller builds;
- whether `ControllersInteractionRoot` is active. Its XR Interaction Toolkit tooltips ("UI Press", "Grab", "Turn", "Move", "Blink") are English text in both languages and can appear only in the Controller builds.

## 3. English versus Greek

The English and Greek scenes of one mode differ only in the settings below. All English and Greek audio clips and text objects are already present and assigned in every scene, so no audio clip, text content or script differs. The English scenes also centre the `NotesR` panel and point its font asset at `LiberationSans SDF` (row 12). The shared prefab `TMT-VR_Greek.prefab` is not edited. All settings are made in the scene: rows 1–5 as overrides on the prefab instance, rows 6–12 on scene objects.

| # | Object | Component and field | Greek scenes | English scenes |
|---|---|---|---|---|
| 1 | `TMT-VR` (prefab instance root) | `TMTVR_TestManager.isEnglish` | not serialized; the C# default `false` applies | `true`, as a scene-level prefab override (prefab target fileID `5374707306608010895`) |
| 2 | `TMT-VR/TMT-VR_Part_A` | `TargetTextAssigner.isEnglish` / `isGreek` | `false` / `true` (prefab values) | `true` / `false` (override on target `7440203550295234658`) |
| 3 | `TMT-VR/TMT-VR_Part_A_Tutorial` | `TargetTextAssigner.isEnglish` / `isGreek` | `false` / `true` | `true` / `false` (target `5374707306954138934`) |
| 4 | `TMT-VR/TMT-VR_Part_B` | `TargetTextAssigner.isEnglish` / `isGreek` | `false` / `true` | `true` / `false` (target `383912637465432505`) |
| 5 | `TMT-VR/TMT-VR_Part_B_Tutorial` | `TargetTextAssigner.isEnglish` / `isGreek` | `false` / `true` | `true` / `false` (target `4931307184793502720`) |
| 6 | `ProbeDomiCube` | `OnEnablePlayAudio.language` | 1 (`Greek`) | 0 (`English`) |
| 7 | `InstructGR` (under `TMT-VR_Part_A_Tutorial`) | GameObject active | on | off |
| 8 | `InstructENG` (under `TMT-VR_Part_A_Tutorial`) | GameObject active | off | on |
| 9 | `InstructGR (1)` (under `TMT-VR_Part_B_Tutorial`) | GameObject active | on | off |
| 10 | `InstructENG (1)` (under `TMT-VR_Part_B_Tutorial`) | GameObject active | off | on |
| 11 | `EndOfExperiment_Greek/NotesR_Greek` | GameObject active | on | off |
| 12 | `EndOfExperiment_Greek/NotesR` | GameObject active; Transform; TextMeshPro font | off | on; centred like `NotesR_Greek`; font `LiberationSans SDF` (see 3.2) |

`NotesL` and `NotesL_Greek` stay off in all four scenes. The end-screen root keeps its historical name `EndOfExperiment_Greek` in all scenes; the name is never shown.

### 3.1 What each setting does

- **`TMTVR_TestManager.isEnglish`** selects the English clips (`General_Instructions`, `Tutorial_A/B_Head`, `Tutorial_A/B_Pointer`, `Practice_A/B`, `Actual_A/B_Start`, and the legacy `Tutorial_A/B_EyeTracking` clips, which these builds never play) or the Greek `_lang` clips, and the English practice-run panels (`InstructENG_A`, `InstructENG _B`) or the Greek ones (`InstructGR _A`, `InstructGR _B`). The English clips are the files `Assets/TMT-VR For Export/EnglishInstruction/00_EN_*.wav` to `10_EN_*.wav`; the Greek clips are `Assets/TMT-VR For Export/GreekInstructions/00_GR_*.wav` to `10_GR_*.wav`. The confirmation sound is the same in both languages.
- **`TargetTextAssigner`** writes the target labels. Exactly one of `isEnglish` and `isGreek` must be true; if they are equal, no labels are written. Only the Part B boards show a visible difference: letters A–D (tutorial) and A–L (scored board) in English, Α–Δ and Α–Μ in Greek. The Part A boards show numbers only, but their flags must still be set consistently because of the equal-flags check.
- **`OnEnablePlayAudio.language`** chooses `DomHand_ENG.mp3` or `DomHand_GR.mp3`. The prompt is heard only in the Controller builds; the value is set to match the scene language in the Head builds as well.
- **Tutorial overlays** (`InstructENG`, `InstructGR` and their `(1)` variants) have no script. Only their active state selects the language.
- **End-screen panels** (`NotesR`, `NotesR_Greek`) have no script. Only their active state selects the language.

### 3.2 English end-screen panel

`NotesR` is a separate panel from `NotesR_Greek`, with its own Transform and its own text component. The English scenes change two things on it; its text is unchanged.

1. **Placement.** `NotesR_Greek` is centred under `EndOfExperiment_Greek`: local position (0, 0, 0), no rotation. In the Greek scenes `NotesR` is offset to the side (local position x = 1.5, z = -0.45) and turned 50° about the vertical axis. In the English scenes `NotesR` is centred like `NotesR_Greek`, at local position (0, 0, 0) with no rotation, so that the English panel appears where the Greek one does. Both panels have the same local scale.
2. **Font.** In the Greek scenes the `NotesR` text refers to a TextMeshPro font asset that is not in the project. In the English scenes its font asset and material point at `LiberationSans SDF`, the font of the Greek panel.

### 3.3 What does not change with language

- The interaction mode, the motion-log switch, the target layout logic, the dwell time, the scoring and the data files.
- The non-verbal sounds: the confirmation and error sounds, and the end-screen audio.
- The institutional logos and banner textures, which contain English text and are the same in both languages.
- The XR Interaction Toolkit controller tooltips, which are English in both languages (Controller builds only).
- The legacy instruction-video objects (`PlayVideoEngGR`, `TMTVR_InstructionsManager` on `Instructions TMT-A/B`). They are inactive in all four scenes and are left as they are.

The instruction audio, the overlay texts and the end-screen texts are content of record in both languages.

## 4. Build identity

`Assets/Editor/TMTVRReleaseBuild.cs` builds one APK per call, and `Assets/Editor/TMTVRReleaseBuildPCVR.cs` one Windows 64-bit PC VR build per call, in Unity batch mode. Each entry point names one scene and sets three values for that variant only:

- the **application ID** (`PlayerSettings` application identifier);
- the **product name** (`PlayerSettings.productName`);
- the **version label** (`PlayerSettings.bundleVersion`), which the app reports as `Application.version` and writes to the `AppVersion` column of the score file and the `app_version` field of the motion log.

Each script restores the previous PlayerSettings after each build. Neither sets or checks the interaction mode, the language or the motion-log switch: those come from the scene.

**APKs** (`TMTVRReleaseBuild`):

| Variant | Entry point | Application ID | Product name | Version label |
|---|---|---|---|---|
| Head, English | `BuildHeadEN()` | `com.DefaultCompany.VRTemplate.head.en` | `TMT-VR Head EN` | `1.1.0 (HEAD, EN)` |
| Head, Greek | `BuildHeadGR()` | `com.DefaultCompany.VRTemplate.head.gr` | `TMT-VR Head GR` | `1.1.0 (HEAD, GR)` |
| Controller, English | `BuildControllerEN()` | `com.DefaultCompany.VRTemplate.controller.en` | `TMT-VR Controller EN` | `1.1.0 (CONTROLLER, EN)` |
| Controller, Greek | `BuildControllerGR()` | `com.DefaultCompany.VRTemplate.controller.gr` | `TMT-VR Controller GR` | `1.1.0 (CONTROLLER, GR)` |

**PC VR builds** (`TMTVRReleaseBuildPCVR`): the same entry-point names, application IDs and product names, with the platform added to the version label:

| Variant | Entry point | Version label |
|---|---|---|
| Head, English | `BuildHeadEN()` | `1.1.0 (HEAD, EN, PCVR)` |
| Head, Greek | `BuildHeadGR()` | `1.1.0 (HEAD, GR, PCVR)` |
| Controller, English | `BuildControllerEN()` | `1.1.0 (CONTROLLER, EN, PCVR)` |
| Controller, Greek | `BuildControllerGR()` | `1.1.0 (CONTROLLER, GR, PCVR)` |

Each variant has its own application ID. Each script's package guard accepts only these four IDs and rejects the template ID `com.DefaultCompany.VRTemplate`; a new variant needs its own entry point and its ID added to both guards. If two APKs shared an application ID, they would replace each other on the headset and share one data folder and one session counter *(inferred)*. On a PC, the data folder and session counter follow the product name instead (see [DATA_OUTPUTS.md](DATA_OUTPUTS.md)).

The version label names the mode, the language and, for PC VR, the platform, because the score file records none of them (section 5).

The scene is passed to the build explicitly, so the Editor's Build Settings scene list is not used by the scripted builds.

## 5. Telling the variants apart in the data

- The score file records neither the interaction mode, the language nor the platform. The `AppVersion` column carries the version label set by the build script, which names the mode, the language and, for the PC VR builds, the platform. Record the build used with every session.
- Session IDs are counted per app on each device *(inferred)*, so the same session number can occur in more than one build.
- In the Controller builds the motion log records the mode (`mode`, `ray_source`) and the chosen controller (`controller_hand`) in each `run_start` record.

See [DATA_OUTPUTS.md](DATA_OUTPUTS.md).
