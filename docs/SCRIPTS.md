# Script reference

This document describes every custom C# script in the TMT-VR Unity project. Read [ARCHITECTURE.md](ARCHITECTURE.md) first for the session flow and how the scripts work together. [VARIANTS.md](VARIANTS.md) lists the settings that differ between the four builds, and [DATA_OUTPUTS.md](DATA_OUTPUTS.md) describes the files the apps write. Terms such as part, board, run, link, dwell, probe and episode are defined in the [glossary](ARCHITECTURE.md#glossary).

## Scope and conventions

- **Custom code** sits in `Assets/TMT-VR For Export/` (with the motion logger in its `MotionLog/` subfolder) and in `Assets/Editor/`.
- **Not covered:** the Unity VR template scripts in `Assets/VRTemplateAssets/Scripts/`, the package samples in `Assets/Samples/`, and Unity packages. These come from Unity and are not specific to TMT-VR.
- **Four scenes, one per app.** Each app is built both as an Android APK and as a Windows PC VR build; the runtime scripts are the same on both platforms.

| Scene | Interaction | `intMode` | `motionLogEnabled` | Language |
|---|---|---|---|---|
| `Assets/TMTVR_Head_EN.unity` | Head | 2 (`head`) | off | English |
| `Assets/TMTVR_Head_GR.unity` | Head | 2 (`head`) | off | Greek |
| `Assets/TMTVR_Controller_EN.unity` | Controller | 3 (`controller`) | on | English |
| `Assets/TMTVR_Controller_GR.unity` | Controller | 3 (`controller`) | on | Greek |

- "Head scenes" means both Head scenes, and "Controller scenes" means both Controller scenes. The Head and Controller scenes differ only in `TMTVR_CentralExecutive.intMode` and `TMTVR_MotionLogger.motionLogEnabled`. The English and Greek scenes of one mode differ only in the language settings, and the English end-panel placement and font, listed in [VARIANTS.md](VARIANTS.md).
- **Present but inactive.** Some code paths exist but are not used by any of the four builds: eye tracking, hand tracking, the instruction-video path (`PlayVideoEngGR`, `TMTVR_InstructionsManager`) and several unplaced helper scripts. They are documented so that readers can recognise them. They have not been validated.
- **Inferred statements.** Statements marked *(inferred)* come from reading the code and the serialized scene data. They were not observed at runtime.
- **Content of record.** The instruction audio, the overlay texts and the instruction script are content of record. This document describes them and does not change them.

Each script section has the same subsections: purpose, where it is used, lifecycle, Inspector fields that matter, public members used by other scripts, outgoing and incoming links, files and persistent data, mode- and language-dependent behaviour, and caveats.

## Index

Columns HE, HG, CE and CG are the four builds: Head English, Head Greek, Controller English and Controller Greek. "yes" means the script runs in that build. "no" means it is present but does not run. "Editor" means it runs only inside the Unity Editor.

| Script | Purpose | Host object | HE | HG | CE | CG |
|---|---|---|---|---|---|---|
| [TMTVR_CentralExecutive](#tmtvr_centralexecutive) | Orchestrates the session from start-up to end screen | `Managers` (scene) | yes | yes | yes | yes |
| [TMTVR_TestManager](#tmtvr_testmanager) | Runs the audio-guided stages of Part A and Part B | `TMT-VR` (root of the task prefab instance) | yes | yes | yes | yes |
| [TMTVRManager](#tmtvrmanager) | Holds the state of one run on one board | The four board objects in `TMT-VR` | yes | yes | yes | yes |
| [TMTVR_ScoreManager](#tmtvr_scoremanager) | Assigns the session ID and writes the score file | `Managers` | yes | yes | yes | yes |
| [TMTVR_InstructionsManager](#tmtvr_instructionsmanager) | Legacy wait for an instruction video | `Instructions TMT-A/B` (prefab, inactive) | no | no | no | no |
| [TargetPlacer](#targetplacer) | Builds the target layout of one board | The four board objects | yes | yes | yes | yes |
| [TargetTextAssigner](#targettextassigner) | Writes the number and letter labels on the targets | The four board objects | yes | yes | yes | yes |
| [DominanceCubeSelector](#dominancecubeselector) | Chooses the pointing controller by a touch on a cube | `ProbeDomiCube` (scene) | no | no | yes | yes |
| [DestroyChildrenWithLineRenderer](#destroychildrenwithlinerenderer) | Removes the drawn trail lines when a board is switched off | The four board objects | yes | yes | yes | yes |
| [FollowTargetPhysics](#followtargetphysics) | Moves a probe collider to follow a tracked point | `ControlllerTip_L/R`, `FingerTip_L/R` | no | no | yes (controller tips) | yes (controller tips) |
| [TipProbeId](#tipprobeid) | Tags a probe with its kind, side and tracked point | `ControlllerTip_L/R`, `FingerTip_L/R` | no | no | yes (controller tips) | yes (controller tips) |
| [OnEnableHeadHeight](#onenableheadheight) | Places an object at head height when it appears | `EndOfExperiment_Greek`, `ProbeDomiCube`, `Instructions TMT-A/B` | yes (end screen) | yes (end screen) | yes (end screen, cube) | yes (end screen, cube) |
| [OpenXRAimPointer](#openxraimpointer) | Casts the pointing ray and moves the pointer | `Managers` | yes | yes | yes | yes |
| [ET_Actions](#et_actions) | Confirms a selection after a dwell and counts errors | 672 `Target_Collider` objects | yes | yes | yes | yes |
| [GazeHitBridge](#gazehitbridge) | Legacy eye-gaze pointer bridge (inert as configured) | `Gaze Interactor` (scene) | no | no | no | no |
| [TMT_VR_RayCastMode](#tmt_vr_raycastmode) | Legacy mode switcher | Not placed | no | no | no | no |
| [ET_Manager_TMTVR](#et_manager_tmtvr) | Legacy pointer visibility manager | Not placed | no | no | no | no |
| [XRSingleModeBootstrapper](#xrsinglemodebootstrapper) | Legacy rig-root switcher | Not placed | no | no | no | no |
| [XRHardResetMode](#xrhardresetmode) | Generic single-mode helper | Not placed | no | no | no | no |
| [ET_ActionsEditor](#et_actionseditor) | Custom Inspector for `ET_Actions` | None | Editor | Editor | Editor | Editor |
| [OnEnablePlayAudio](#onenableplayaudio) | Plays the dominant-hand prompt | `ProbeDomiCube` | no | no | yes | yes |
| [PlayVideoEngGR](#playvideoenggr) | Legacy instruction-video language switch | `Instructions TMT-A/B` (prefab, inactive) | no | no | no | no |
| [TMTVR_MotionLogCore](#tmtvr_motionlogcore) | Plain C# bookkeeping for the motion log | None (created by the logger) | no | no | yes | yes |
| [TMTVR_MotionLogger](#tmtvr_motionlogger) | Passive event and pose logger | `Managers` | no (switched off) | no (switched off) | yes | yes |
| [TMTVRReleaseBuild](#tmtvrreleasebuild) | Batch-mode APK build with guard checks and a receipt | None | Editor | Editor | Editor | Editor |
| [TMTVRReleaseBuildPCVR](#tmtvrreleasebuildpcvr) | Batch-mode Windows PC VR build with guard checks and a receipt | None | Editor | Editor | Editor | Editor |

The motion logger records events and poses only. It computes no adjusted or motor-corrected score, and no validated motor measure may be claimed from its output. Runs without a `run_end` record must be excluded from analysis.

---

## 1. Core flow

These scripts run a session from start to end: the orchestrator, the part runner, the per-run state, the score file, and one legacy video helper.

---

### TMTVR_CentralExecutive

File: `Assets/TMT-VR For Export/TMTVR_CentralExecutive.cs`

#### Purpose
This script is the single entry point for a session. It does the following, in order:
1. waits for the score file;
2. sets the interaction mode on the pointer;
3. runs the dominant-side check in controller mode;
4. runs TMT Part A and Part B;
5. asks the score manager to save;
6. shows the end screen.

It does not present targets or score anything itself.

#### Where it is used
- **Host:** GameObject `Managers` in all four scenes.
- `Managers` also hosts `OpenXRAimPointer`, `TMTVR_ScoreManager` and `TMTVR_MotionLogger`.
- No prefab contains this component.

#### Lifecycle
- **`Awake`:** if `dominanceCube` is set, caches its `DominanceCubeSelector` with `GetComponent`.
- **`Start`:** starts the `ExecuteTasks()` coroutine.

`ExecuteTasks()` runs these steps:
1. **Wait for the score file.** Waits until `ScoreManager` is set and `ScoreManager.CSVInitialization` is true.
2. **Start-up delay.** Waits `delayForLoading` seconds, if that value is above 0.
3. **Pointer check.** If `aimPointer` is null, it logs an error and stops. Nothing after this step runs.
4. **Mode set-up.** Uses `intMode` to:
   - set one flag on `OpenXRAimPointer` (`Hand`, `Controller`, `Eye` or `Head`);
   - copy the matching string field to `aimPointer.aimSource`;
   - switch on only the matching rig objects.

   | Mode | Pointer flag | Rig objects switched on | Rig objects switched off |
   |---|---|---|---|
   | head | `Head` | none | hands root and fingertip probes; controllers root and controller-tip probes; `EyeGazeRoot` |
   | controller | `Controller` | controllers root and controller-tip probes | hands root and fingertip probes; `EyeGazeRoot` |
5. **Dominant-side check (hand and controller modes only).** `DominanceCheck()` does the following:
   - switches `dominanceCube` (`ProbeDomiCube`) on;
   - waits until `DominanceCubeSelector.Selected` is true;
   - switches the cube off.

   The selector assigns the tracked tip of the touching side to the pointer. The Head scenes skip this step.
6. **Probes off.** `DisableTips()` switches off all four tip probes (two fingertip, two controller-tip). The probes are used only for the cube touch. The pointer follows the tracked tip transform that the selector assigned, not the probe object.
7. **Ray on.** Sets `aimPointer.RayCasting = true`.
8. **Familiarisation.** Waits `familDuration` seconds, if that value is above 0.
9. **Task.** Runs `TMTVR_PartA()` and then `TMTVR_PartB()`. Each part:
   - switches the `TMT` GameObject on;
   - gets `TMTVR_TestManager` with `GetComponent`;
   - calls `TMTA()` or `TMTB()`;
   - waits for `TMT_PartAcompleted` or `TMT_PartBcompleted`;
   - switches `TMT` off.
10. **Save.** Yields on `ScoreManager.SaveScore()`.
11. **End screen.** Calls `ExperimentEnd()`, which switches `EndofExperiment` on.

#### Inspector fields that matter

| Field | Role | Value in the scenes |
|---|---|---|
| `intMode` | Interaction mode. Enum: `eye` = 0, `hand` = 1, `head` = 2, `controller` = 3. | 2 in the Head scenes, 3 in the Controller scenes |
| `familDuration` | Familiarisation wait after set-up, in seconds | 3 |
| `delayForLoading` | Start-up wait, in seconds | 1 |
| `TMT` | Root that carries `TMTVR_TestManager` | `TMT-VR`, the instance of `TMT-VR_Greek.prefab`, inactive at load |
| `EndofExperiment` | Panel shown at the end | `EndOfExperiment_Greek`, inactive at load |
| `Platform` | Not used by the code (its tooltip says so) | Instance of the platform prefab |
| `ScoreManager` | `TMTVR_ScoreManager` | `Managers` |
| `dominanceCube` | Cube touched to choose the dominant side | `ProbeDomiCube`, inactive at load |
| `aimPointer` | `OpenXRAimPointer` | `Managers` |
| `EyeGazeRoot` | Eye-gaze rig root | `GazeInteractionRoot` |
| `controllersRoot`, `contipR`, `contipL` | Controller rig root and the two controller-tip probes | `ControllersInteractionRoot`, `ControlllerTip_R`, `ControlllerTip_L` |
| `handsRoot`, `fintipR`, `fintipL` | Hand rig root and the two fingertip probes | `HandsInteractionRoot`, `FingerTip_R`, `FingerTip_L` |
| `Eye_Interaction`, `Hand_Interaction`, `Controller_Interaction`, `Head_Interaction` | Strings copied to `aimPointer.aimSource` | "Eye", "Hand", "Controller", "Head" |

All values except `intMode` are the same in all four scenes.

#### Public members used by other scripts
- **`InteractionMode` enum and `intMode`.** `TMTVR_MotionLogger` reads `intMode` and writes it as the `mode` field of its `run_start` events. This happens only in the Controller scenes.
- **`ExperimentEnd()`.** Public, but called only from `ExecuteTasks()`. No UnityEvent in any scene calls it.

#### Talks to (outgoing)

| Target | Mechanism | What |
|---|---|---|
| `TMTVR_ScoreManager` | Serialized reference; `WaitUntil` flag poll; nested coroutine | Waits on `CSVInitialization`; yields on `SaveScore()` |
| `OpenXRAimPointer` | Serialized reference; field writes | Sets `Hand`, `Controller`, `Eye` or `Head`, `aimSource` and `RayCasting` |
| `DominanceCubeSelector` | `GetComponent` on `dominanceCube`; `WaitUntil` flag poll | Waits on `Selected` (controller and hand modes only) |
| `TMTVR_TestManager` | `GetComponent` on `TMT`; method call; `WaitUntil` flag poll | Calls `TMTA()` and `TMTB()`; waits on `TMT_PartAcompleted` and `TMT_PartBcompleted` |
| Scene GameObjects | `SetActive` | `TMT`, `ProbeDomiCube`, the rig roots, the four tip probes and `EndOfExperiment_Greek` |

Switching `ProbeDomiCube` on starts its `OnEnablePlayAudio`, which plays the dominant-hand prompt. Switching `TMT` on and off runs `OnEnable` and `OnDisable` on `TMTVR_TestManager`.

#### Used by (incoming)
- **`TMTVR_MotionLogger`** finds this component with `FindFirstObjectByType` (its own serialized reference is empty), then reads `intMode`. This happens only when `motionLogEnabled` is true, which is the Controller scenes only. In the Head scenes the logger switches itself off in `Start`, before any lookup runs.
- **`XRSingleModeBootstrapper`** declares a field of this type, which it only null-checks. That component is not attached in any scene or in `TMT-VR_Greek.prefab`, so it never runs.

#### Files and persistent data
None. It does no file I/O and does not use PlayerPrefs.

#### Mode- and language-dependent behaviour
- **Mode.** `intMode` is the only serialized value of this component that differs between scenes:
  - in the Head scenes, the dominance check is skipped, and the controllers and hands rigs are switched off;
  - in the Controller scenes, the dominance check runs, and the session waits until a controller tip touches `ProbeDomiCube`.
- **Language.** This script has no language setting. `EndofExperiment` points to the same object, `EndOfExperiment_Greek`, in all four scenes. Despite its name, this object holds both the English and the Greek end panels. The language shown depends only on which child panel is active:
  - Greek scenes: `NotesR_Greek` on, `NotesR` off;
  - English scenes: `NotesR` on, `NotesR_Greek` off.

  `NotesL` and `NotesL_Greek` stay off in all scenes. The object name is never shown to participants.
- **English end panel.** In the English scenes `NotesR` is centred like `NotesR_Greek`, and its text uses `LiberationSans SDF`, the font of the Greek panel. See [VARIANTS.md](VARIANTS.md), section 3.2.

#### Caveats and legacy notes
- **No timeout on the dominance wait.** In the Controller scenes the wait has no timeout or fallback. `OpenXRAimPointer.controllerTip` is assigned only by the cube touch.
- **No null checks on `EyeGazeRoot`.** It is switched with `SetActive` in every mode branch. It is assigned in all four scenes.
- **Failure modes:**
  - If `aimPointer` is null, `ExecuteTasks()` stops before the task. `SaveScore()` never runs, and the score file keeps only its header *(inferred)*.
  - If the score file cannot be initialised, `CSVInitialization` stays false and the session waits for ever at the first step.
  - If `TMT` or its `TMTVR_TestManager` is missing, the part coroutine logs an error and returns. The flow then continues to `SaveScore()`, which still writes a row (see `TMTVR_ScoreManager`).
- **`Selected` is not reset.** `DominanceCheck()` does not reset `DominanceCubeSelector.Selected`. The code comment assumes one session per scene load.
- **Unimplemented "lockdown".** The class summary mentions a "single mode" lockdown. In code this is only the `SetActive` calls on the rig roots and tip probes.
- **Legacy modes.** The `eye` and `hand` modes, and their branches in this script, exist in code but are not used by the four release builds. They are present-but-inactive legacy paths. Nothing in this release validates them.

---

### TMTVR_TestManager

File: `Assets/TMT-VR For Export/TMTVR_TestManager.cs`

#### Purpose
This script runs the audio-guided stages of each TMT part:
1. general instructions (Part A only);
2. the instruction clip for the current mode;
3. a tutorial run;
4. a run announced as practice;
5. a second run on the same board.

Each run is delegated to a `TMTVRManager`. The script chooses English or Greek clips and panels from `isEnglish`. It sets the completion flags that `TMTVR_CentralExecutive` waits on.

#### Where it is used
- **Prefab:** root GameObject of `Assets/TMT-VR For Export/TMT-VR_Greek.prefab`. The root also holds the `AudioSource` this script plays through.
- **Scenes:** each of the four scenes contains one instance of that prefab. The instance root is renamed `TMT-VR` and is inactive at load. `TMTVR_CentralExecutive.TMT` points to it.
- **Prefab name.** The word "Greek" in the prefab name is historical. The same prefab is used in the English scenes.
- **Values stored as overrides.** Many current values are stored as scene-level prefab overrides, not in the prefab. These include all instruction clips, the four instruction panels and `aimPointer`.
- **Stale prefab data.** The prefab's own record for this component still holds old fields that the class no longer declares, such as `TMTRayMode`, `instructionsA_Manager`, `isfirstTMT_PartA` and `TMTA_audio`. Unity ignores them.
- **Opening the prefab on its own.** The four run managers, `audioSource` and `TMT_Completed_audio` are set. The language clips, the instruction panels and `aimPointer` are not set. `isEnglish` is not serialized in the prefab at all.

#### Lifecycle
- **`OnEnable` and `OnDisable`:** reset `TMT_PartAcompleted` and `TMT_PartBcompleted` to false. `TMTVR_CentralExecutive` switches `TMT-VR` on before each part and off after it, so the flags start false for each part.
- There is no `Start` or `Update`. Work starts only when `TMTA()` or `TMTB()` is called. Each one starts a coroutine.
- Unity stops a component's coroutines when its GameObject is switched off. The central executive switches `TMT-VR` off only after the completion flag is set, so no stage is cut short.

`TMT_PartA()` runs these stages:

| Stage | What happens | Board switched on |
|---|---|---|
| 1. General instructions | Waits 1 s, plays `General_Instructions` (English) or `General_Instructions_lang` (Greek), and waits for the clip to end. | none |
| 2. Mode instructions | Waits 1 s, plays the clip from `GetPartATutorialClip()` (see the mode section), and waits for it to end. | none |
| 3. Tutorial | Switches `tutorial_partA_Manager` on, waits for its `Completed`, then switches it off. | `TMT-VR_Part_A_Tutorial` |
| 4. First run | Plays `Practice_A` or `Practice_A_lang`. Switches `partA_Manager` on and shows `InstructionsA` or `InstructionsA_lang`. Waits for `Completed`, then increments `RealRunsA_Completed` and hides both panels. Plays `TMT_Completed_audio`, then switches the board off. | `TMT-VR_Part_A` |
| 5. Second run | Plays `Actual_A_Start` or `Actual_A_Start_lang`. Switches `partA_Manager` on again, waits for `Completed`, then increments `RealRunsA_Completed`. Plays `TMT_Completed_audio`, then switches the board off. | `TMT-VR_Part_A` |
| 6. Done | Sets `TMT_PartAcompleted = true`. | |

`TMT_PartB()` runs the same stages with the B fields and boards. It has no general-instructions stage.

**Which run reaches the score file.** The code comments call both runs on the scored board "Real Run #1" and "Real Run #2", and both increment the run counter. The first is introduced by the practice clip and is the only run that shows the instruction panel. Both runs use the same `TMTVRManager`. Its `OnEnable` resets the run state, so `TMTVR_ScoreManager` reads the values of the second run only.

#### Inspector fields that matter

| Field | Role | Value in the scenes |
|---|---|---|
| `isEnglish` | Language switch for clips and panels | Greek scenes: not serialized, so the default `false` applies. English scenes: set to `true` as a scene-level override. |
| `aimPointer` | Read to choose the mode instruction clip | `OpenXRAimPointer` on `Managers` (scene override) |
| `partA_Manager`, `partB_Manager` | Scored boards | `TMT-VR_Part_A`, `TMT-VR_Part_B` |
| `tutorial_partA_Manager`, `tutorial_partB_Manager` | Tutorial boards | `TMT-VR_Part_A_Tutorial`, `TMT-VR_Part_B_Tutorial` |
| `InstructionsA`, `InstructionsB` | English panels shown during the first scored run | `InstructENG_A`, `InstructENG _B` (inactive at load) |
| `InstructionsA_lang`, `InstructionsB_lang` | Greek panels shown during the first scored run | `InstructGR _A`, `InstructGR _B` (inactive at load) |
| `audioSource` | Plays every clip | `AudioSource` on the prefab root |
| `TMT_Completed_audio` | Played after each scored run in both languages | `Confirm.wav` |
| English clips: `General_Instructions`, `Tutorial_A/B_EyeTracking`, `Tutorial_A/B_Head`, `Tutorial_A/B_Pointer`, `Practice_A/B`, `Actual_A/B_Start` | English audio | The 11 files `EnglishInstruction/00_EN_*.wav` to `10_EN_*.wav` (scene overrides) |
| `*_lang` clips (same names with `_lang`) | Greek audio | The 11 files `GreekInstructions/00_GR_*.wav` to `10_GR_*.wav` (scene overrides) |
| `RealRunsA_Completed`, `RealRunsB_Completed` | Count runs on the scored boards | 0 at load |
| `TMT_PartAcompleted`, `TMT_PartBcompleted` | Completion flags | Set at runtime |

All four scenes assign all clips in both languages. The instruction audio and panel texts are content of record.

#### Public members used by other scripts
- `TMTA()` and `TMTB()`, called by `TMTVR_CentralExecutive`.
- `TMT_PartAcompleted` and `TMT_PartBcompleted`, polled by `TMTVR_CentralExecutive`.
- `RealRunsA_Completed` and `RealRunsB_Completed`, read by `TMTVR_ScoreManager` and `TMTVR_MotionLogger`.
- The four manager references, read by `TMTVR_MotionLogger`.
- `TMT_PartA()` and `TMT_PartB()` are also public coroutines, but the release code starts them only through `TMTA()` and `TMTB()`.

#### Talks to (outgoing)

| Target | Mechanism | What |
|---|---|---|
| `TMTVRManager` (four boards) | Serialized reference; `SetActive`; `WaitUntil` flag poll | Switches each board on and off and waits on `Completed` |
| `OpenXRAimPointer` | Serialized reference; field reads | Reads `Eye`, `Head`, `Hand` and `Controller` to pick the instruction clip |
| `AudioSource` on the prefab root | Serialized reference | Sets `clip`, calls `Play()` and waits on `isPlaying` |
| `InstructENG_A`, `InstructENG _B`, `InstructGR _A`, `InstructGR _B` | Serialized reference; `SetActive` | Shows the panel for the current language during the first scored run, then hides both |

#### Used by (incoming)
- **`TMTVR_CentralExecutive`:** `GetComponent` on `TMT`, method calls and flag polls.
- **`TMTVR_ScoreManager`:** a serialized reference to this component. It reads the run counters.
- **`TMTVR_MotionLogger`:** `FindFirstObjectByType` with inactive objects included (its own serialized reference is empty). It reads the four manager references and the run counters to label each run with its part and stage. Controller scenes only.

#### Files and persistent data
None.

#### Mode- and language-dependent behaviour
- **Mode instruction clip.** `GetPartATutorialClip()` and `GetPartBTutorialClip()` check the pointer flags in this order: `Eye`, then `Head`, then `Hand` or `Controller`.
  - Head scenes: the central executive sets `Head`, so the `Tutorial_A/B_Head` clips play.
  - Controller scenes: it sets `Controller`, so the `Tutorial_A/B_Pointer` clips play.
  - If `aimPointer` is null, or no flag is set, the Head clip is the fallback.
  - The `Tutorial_A/B_EyeTracking` clips are assigned but are used only in the legacy eye mode.
- **Language.** `isEnglish` selects either the English clips and the `InstructionsA/B` panels, or the `_lang` clips and the `InstructionsA_lang/B_lang` panels. `TMT_Completed_audio` is the same in both languages.
- **What this script does not control:**
  - the static tutorial overlays `InstructENG`, `InstructGR`, `InstructENG (1)` and `InstructGR (1)` (switched by active state only);
  - the target labels (`TargetTextAssigner`);
  - the dominant-hand prompt (`OnEnablePlayAudio`);
  - the end panels.

  Each of these has its own language setting, and all of them must match `isEnglish` in a given scene.

#### Caveats and legacy notes
- **Legacy video path.** The `TMTVR_InstructionsManager` video path is commented out: its two fields and the code that waited on it. Audio instructions replaced it. The video objects remain in the prefab, inactive. This is a present-but-inactive legacy path.
- **No null checks on panels.** Both the English and the Greek panel of a part are switched off without null checks. All four are assigned in every scene.
- **Counters never reset.** `RealRunsA_Completed` and `RealRunsB_Completed` are never reset within a session. Each session is a fresh scene load.
- **Eye-mode clips.** The eye-tracking instruction clips are assigned but belong to a legacy mode that the four builds do not use.

---

### TMTVRManager

File: `Assets/TMT-VR For Export/TMTVRManager.cs`

#### Purpose
This script holds the state of one run on one task board:
- the ordered target list;
- the current, previous and next targets;
- the number of correct selections.

It records a link time and a spatial-accuracy value for each link and draws a line between consecutive correct targets. It sets `Completed` after 8 correct selections on a tutorial board or 25 on a scored board. Errors are counted on this component, but `ET_Actions` writes them.

#### Where it is used
Four instances in `TMT-VR_Greek.prefab`, all inactive until `TMTVR_TestManager` switches them on:

| Board GameObject | Role | Referenced by |
|---|---|---|
| `TMT-VR_Part_A_Tutorial` | Part A tutorial | `TMTVR_TestManager.tutorial_partA_Manager` |
| `TMT-VR_Part_A` | Part A, both scored-board runs | `TMTVR_TestManager.partA_Manager`, `TMTVR_ScoreManager.tmtvrManagerTMTA` |
| `TMT-VR_Part_B_Tutorial` | Part B tutorial | `TMTVR_TestManager.tutorial_partB_Manager` |
| `TMT-VR_Part_B` | Part B, both scored-board runs | `TMTVR_TestManager.partB_Manager`, `TMTVR_ScoreManager.tmtvrManagerTMTB` |

Each board GameObject carries four scripts:
- `TargetPlacer`, which `targetPlacer` points to;
- `TargetTextAssigner`;
- `TMTVRManager`;
- `DestroyChildrenWithLineRenderer`.

Each board has 168 `Target_Collider` objects with `ET_Actions`, and each one references that board's manager through its `tmtVR` field. All four scenes contain the boards through the `TMT-VR` prefab instance.

#### Lifecycle
- **`OnEnable` resets the run state.** It resets the following:
  - `PreviousTarget`;
  - `TargetsSelected`, `totalMistakes` and `CurrentTargetIndex`;
  - `selectionTimes` and `accuracies`;
  - `totalSelectionTime`;
  - `Targets` (cleared);
  - `Completed`;
  - two private flags.

  It does **not** reset `AverageSelectionTime`, `AverageAccuracy`, `accuracy`, `selectionTime`, `startingTime`, `previousSelectionTime`, `CurrentTarget` or `NextTarget`. Those keep the previous run's values until the new run overwrites them. The averages are recomputed only at the 25th selection.
- **Own `LineRenderer`.** If the cached private `lineRenderer` field is null, `OnEnable` adds a `LineRenderer` to the board (`Sprites/Default` shader, `lineColor`, `lineWidth`). The field is null again after each completed run, because completion destroys that renderer.
- **Starting the run.** `OnEnable` then starts `WaitForPlacementAndPopulateList()`. This waits until all three are true: `targetPlacer.PlacementDone`, `Targets` empty, and the list not yet filled. It then binds `Targets` to `targetPlacer.orderedTargets` and sets `CurrentTarget` and `NextTarget` to the first two entries.

**`OnTargetSelected()`** is called by `ET_Actions` when the dwell on the current target completes:
1. **First correct selection.** Starts the timer (`startingTime`) and records no link.
2. **Each later correct selection.** Appends `selectionTime` (the time since the previous correct selection) to `selectionTimes`, and the current `accuracy` to `accuracies`. A full scored run gives 24 values in each list.
3. **Line.** If there is a previous target, it creates a child GameObject `Line<n>` with a `LineRenderer` from the previous target to the current one.
4. **Advance.** Moves `PreviousTarget`, `CurrentTarget`, `NextTarget` and `CurrentTargetIndex` on by one.
5. **Tutorial board (`targetPlacer.Tutorial`).** At 8 correct selections it destroys its own `LineRenderer`, calls `targetPlacer.ResetPlacement()` and sets `Completed = true`.
6. **Scored board.** At 25 correct selections it does the following, once only:
   - sets `totalSelectionTime = Time.time - startingTime`;
   - computes `AverageSelectionTime` (`totalSelectionTime` divided by the number of links);
   - computes `AverageAccuracy` (the mean of `accuracies`);
   - destroys its own `LineRenderer`;
   - calls `ResetPlacement()`;
   - sets `Completed = true`.

The thresholds match the placer: 7 + 1 = 8 targets on a tutorial board and 24 + 1 = 25 on a scored board.

#### Inspector fields that matter

| Field | Role | Value in the prefab |
|---|---|---|
| `targetPlacer` | The `TargetPlacer` on the same board | Assigned on all four boards |
| `lineColor` | Colour of the connecting lines | Yellow (approximately 1, 0.92, 0.016) |
| `lineWidth` | Width of the connecting lines | 0.01 |

All other public fields are runtime state:
- `Targets`, `CurrentTarget`, `PreviousTarget`, `NextTarget`;
- `CurrentTargetIndex`, `TargetsSelected`;
- `selectionTimes`, `selectionTime`, `previousSelectionTime`, `startingTime`, `totalSelectionTime`, `AverageSelectionTime`;
- `accuracies`, `accuracy`, `AverageAccuracy`;
- `totalMistakes`, `Completed`.

The prefab also holds a stale `testAverageSelectionTime` value that the class no longer declares.

#### Public members used by other scripts

| Member | Used by |
|---|---|
| `OnTargetSelected()` | `ET_Actions` |
| `CurrentTarget`, `TargetsSelected` (read) | `ET_Actions` |
| `accuracy`, `totalMistakes` (written) | `ET_Actions` |
| `Completed` | `TMTVR_TestManager` |
| `totalSelectionTime`, `totalMistakes`, `AverageAccuracy` | `TMTVR_ScoreManager` |
| `TargetsSelected`, `Targets`, `totalMistakes`, `selectionTimes`, `previousSelectionTime`, `targetPlacer.Tutorial` | `TMTVR_MotionLogger` (Controller scenes only) |
| `CalculateAverageSelectionTime()`, `CalculateAverageAccuracy()` | Public, but called only by this class |

#### Talks to (outgoing)

| Target | Mechanism | What |
|---|---|---|
| `TargetPlacer` (same board) | Serialized reference; `WaitUntil` flag poll; method call | Waits on `PlacementDone`; reads `orderedTargets` and `Tutorial`; calls `ResetPlacement()` |
| `LineRenderer` | `AddComponent`, `Destroy` | Adds one to the board and one to each new `Line<n>` child; destroys the board's own renderer at completion |

#### Used by (incoming)
- **`ET_Actions`** (serialized `tmtVR` field, 168 per board). It reads the current target and the selection count. It writes `accuracy` before each correct selection and increments `totalMistakes` on each error, then calls `OnTargetSelected()`.
- **`TMTVR_TestManager`** (serialized references). It switches each board on and off and polls `Completed`.
- **`TMTVR_ScoreManager`** (serialized references to the two scored boards). It reads the three scored values.
- **`TMTVR_MotionLogger`.** It reaches the boards through the test manager's four manager references. It reads the members listed above to time links and label runs. This happens only when `motionLogEnabled` is true, which is the Controller scenes only.

#### Files and persistent data
None.

#### Meaning of the values the score file uses
- **`totalSelectionTime`** is the time in seconds from the first correct selection to the 25th. It does not include the time taken to find target 1.
- **`totalMistakes`** is the number of errors. `ET_Actions` adds one each time the dwell timer (`confirmationTime`, set on each `ET_Actions` in the prefab) completes on a target that is not the current target. Each entry into a target's trigger counts at most once. Targets that are already selected are ignored, so dwelling on them is not an error.
- **`AverageAccuracy`** is a spatial measure in Unity world units, not a proportion. For each correct selection after the first, `ET_Actions` averages the distance between the tagged collider in its trigger and the target collider's position. The average is taken over the physics steps since that collider entered the trigger. `AverageAccuracy` is the mean of those 24 values; lower means closer.

  The tagged collider is the pointer object that `OpenXRAimPointer` moves to the ray hit *(inferred)*.

#### Mode- and language-dependent behaviour
None in this script. The same boards and thresholds are used in all four scenes. The pointer that drives `ET_Actions` differs by mode (head ray or controller ray), but this script does not see the mode. Target labels are set by `TargetTextAssigner`.

#### Caveats and legacy notes
- **`Targets` aliasing.** During a run, `Targets` is the same list object as `targetPlacer.orderedTargets`. On the next enable, `TargetPlacer.OnEnable` clears that old list and creates a new one, and the manager then binds `Targets` to the new list. The aliasing has no lasting effect. `ResetPlacement()` only deactivates the targets and does not clear the list, so `Targets` stays populated after completion until the next enable.
- **Line clean-up.** The `Line<n>` children are removed by `DestroyChildrenWithLineRenderer` on the same board. In `OnDisable`, it destroys every GameObject in the board's hierarchy that carries a `LineRenderer`. Two points are unverified:
  - whether `GetComponentsInChildren` finds the children during `OnDisable`, since it skips inactive objects by default *(inferred)*;
  - what happens on an interrupted run. If a board were switched off before its own `LineRenderer` was destroyed, the same call would also return the board itself and could destroy it *(inferred)*. On a scored board, the board's renderer is destroyed at completion, at least one second before the board is switched off. On a tutorial board the board is switched off straight after `Completed` is seen, so the self-destruction risk described under `DestroyChildrenWithLineRenderer` applies there.
- **Degenerate line.** The board's own `LineRenderer` is never given positions. It is inferred to draw a zero-length segment at the origin, which is not visible.
- **Short lists.** `NextTarget` is read as `Targets[1]`, which assumes at least two targets.
- **Failed placement.** If placement fails, `TargetPlacer` flags this with `lastPlacementFailed` and leaves `PlacementDone` false. This manager does not check that flag, so the run would wait for ever. The placer's own comments describe this as practically unreachable.
- **Tutorial runs.** Errors and times on the tutorial boards are kept on the tutorial managers and are not written to the score file.

---

### TMTVR_ScoreManager

File: `Assets/TMT-VR For Export/TMTVR_ScoreManager.cs`

#### Purpose
This script assigns the session ID and creates the per-session score file. At the end of the session it writes one row of TMT Part A and Part B outcomes and derived values.

#### Where it is used
- **Host:** GameObject `Managers` in all four scenes. Its serialized values are the same in every scene.
- No prefab contains this component.

#### Lifecycle
**`Start` prepares the session:**
1. **Session ID.** Reads the PlayerPrefs key `uniqueID`. It adds 1, or uses 1 on the first run, saves it with `PlayerPrefs.Save()`, and stores it in `SessionID`.
2. **Folder.** Creates `Application.persistentDataPath/TMTVR_Participants_Data/` if it is missing.
3. **File name.** Sets the file path to `scoresID<SessionID>.csv`. While a file with that name exists, it increments `SessionID` and writes the new value back to PlayerPrefs. Each session therefore gets a new file.
4. **Metadata columns.** If `autoAddMetadataColumns` is on, it adds any missing metadata column: `SessionID`, `TimestampUTC`, `DeviceModel`, `DeviceType` or `AppVersion`. All five are already in the scene schema.
5. **Header.** Calls `InitializeCSV()`:
   - if `variables` is empty, it logs an error and leaves `CSVInitialization` false;
   - otherwise it writes the header row in schema order, CSV-escaped, with `File.WriteAllText(..., Encoding.UTF8)`, and sets `CSVInitialization = true`;
   - if writing fails, it logs the exception and leaves the flag false;
   - if the file already exists, it only sets the flag. Step 3 makes this case unreachable in normal use.

`TMTVR_CentralExecutive` does not start the session until `CSVInitialization` is true.

**`SaveScore()` writes the row.** This coroutine is called by `TMTVR_CentralExecutive` after Part B.
1. **Guards.** If any of these holds, it logs an error and returns without writing:
   - the file was not initialised;
   - either scored-board manager is missing;
   - `testManager` is missing.
2. **Run check.** If either run counter is below 2, it logs an error **but still writes the row**. The comment above this check calls it a safety guard, but the code does not stop.
3. **Metadata.** Writes the UTC timestamp (ISO 8601), `SystemInfo.deviceModel`, `SystemInfo.deviceType` and `Application.version`.
4. **Inputs.** Reads `totalSelectionTime`, `totalMistakes` and `AverageAccuracy` from the Part A and Part B scored-board managers.
5. **Derived values.** Computes these with 24 links per part and 48 in total:
   - Error Rate = errors / links;
   - Accuracy = links / (links + errors);
   - Adjusted Time = time / Accuracy;
   - Totals: A + B for time and errors, recomputed rate, accuracy and adjusted time;
   - Total Spatial Accuracy = the mean of the A and B values;
   - Differences: B − A for each measure;
   - Ratio Time = B / A;
   - Norm. Difference Time = (B − A) / A.

   The last two are written as `NA` when the Part A time is 0.
6. **Participant ID.** Writes `ParticipantID` as the fixed text `PLACE HERE PARTICIPANT'S ID`.
7. **Append.** `FinalizeAndAddCurrentRow()` appends the row in schema order and leaves any missing key empty. Numbers use `InvariantCulture`, and NaN or Infinity is written as `NA`. The row is appended with `File.AppendAllText(..., Encoding.UTF8)`.

#### Inspector fields that matter

| Field | Role | Value in the scenes |
|---|---|---|
| `variables` | Column schema and order: name and type | 32 columns (listed below) |
| `autoAddMetadataColumns` | Adds missing metadata columns at start | On |
| `warnOnUnknownColumns` | Warns once for any key written that is not in the schema. Such keys are not written. | On |
| `tmtvrManagerTMTA` | Part A scored board | `TMT-VR_Part_A` |
| `tmtvrManagerTMTB` | Part B scored board | `TMT-VR_Part_B` |
| `testManager` | Source of the run counters | `TMTVR_TestManager` on `TMT-VR` |
| `SessionID`, `csvFilePath`, `CSVInitialization` | Runtime state | Set at start |
| `fileName`, `TrialNumber` | Unused. `TrialNumber` is set to 0, and its comment says it may be deleted. | Empty, 0 |

The schema has **32 columns**, in this order:

| Group | Columns |
|---|---|
| Metadata (6) | `TimestampUTC`, `DeviceModel`, `DeviceType`, `AppVersion`, `SessionID`, `ParticipantID` |
| Task A (6) | `Task A - Task Time`, `Task A - Errors`, `Task A - Error Rate`, `Task A - Spatial Accuracy`, `Task A - Accuracy`, `Task A - Adjusted Time` |
| Task B (6) | The same six measures for Task B |
| Total (6) | `Total Time`, `Total Errors`, `Total Error Rate`, `Total Spatial Accuracy`, `Total Accuracy`, `Total Adjusted Time` |
| Difference (6) | `Difference Time`, `Difference Errors`, `Difference Error Rate`, `Difference Spatial Accuracy`, `Difference Accuracy`, `Difference Adjusted Time` |
| Ratios (2) | `Ratio Time`, `Norm. Difference Time` |

#### Public members used by other scripts
- **`SaveScore()` and `CSVInitialization`** are used by `TMTVR_CentralExecutive`.
- **`SessionID`** is used by `TMTVR_MotionLogger`, in the Controller scenes only.
- **Not called elsewhere in the release code:** `AddNewRow(Dictionary<string,string>)`, `UpdateVariable(string, object)`, `FinalizeAndAddCurrentRow()` and `csvFilePath` are public, but only this class uses them.

#### Talks to (outgoing)

| Target | Mechanism | What |
|---|---|---|
| `TMTVRManager` (Part A and Part B scored boards) | Serialized references; field reads | `totalSelectionTime`, `totalMistakes`, `AverageAccuracy` |
| `TMTVR_TestManager` | Serialized reference; field reads | `RealRunsA_Completed`, `RealRunsB_Completed` |
| PlayerPrefs | `GetInt`, `SetInt`, `Save` | Key `uniqueID` |
| File system | `Directory.CreateDirectory`, `File.WriteAllText`, `File.AppendAllText` | The score file |

#### Used by (incoming)
- **`TMTVR_CentralExecutive`:** a serialized reference. It polls `CSVInitialization` and yields on `SaveScore()`.
- **`TMTVR_MotionLogger`:** `FindFirstObjectByType` (its own serialized reference is empty). It reads `SessionID` to name its files. Controller scenes only. In the Head scenes the logger switches itself off before the lookup.

#### Files and persistent data
- **Score file:** `Application.persistentDataPath/TMTVR_Participants_Data/scoresID<SessionID>.csv`. It holds a header row and one data row per completed session. The file is UTF-8, written through .NET's `Encoding.UTF8`. That encoding is inferred to add a byte-order mark before the first header name, `TimestampUTC`, so CSV readers should allow for it.
- **Session counter:** the PlayerPrefs key `uniqueID`, an integer.
- **Scope of both.** On Android, PlayerPrefs and `persistentDataPath` belong to one application identifier *(inferred)*. If each APK has its own identifier, each APK keeps its own session counter and data folder. On Windows, both follow the company and product names in Player Settings *(inferred)*, so each PC VR build keeps its own counter and folder on each PC. Session IDs can therefore repeat across builds, so the build must be recorded alongside the session ID.
- **Which scenes write what:**
  - Head scenes: this file is the only data output.
  - Controller scenes: `TMTVR_MotionLogger` also writes `motionID<SessionID>_events.jsonl` and `motionID<SessionID>_pose.csv` under `persistentDataPath/TMTVR_Session_Logs/`, named with this script's `SessionID`.

#### Mode- and language-dependent behaviour
- This script does not depend on mode or language.
- **No mode, language or platform column.** The score file has none of them. When an app is built with the project's release build scripts, `AppVersion` carries the version label that script sets, which names the mode, the language and, for the PC VR builds, the platform (see [VARIANTS.md](VARIANTS.md), section 4). Builds made otherwise carry whatever version is in Player Settings, so record the build used with each session.
- **Mode in the motion log.** In the Controller scenes, the motion log also records the mode. In the Head scenes, the mode is recorded nowhere else.

#### Caveats and legacy notes
- **Participant ID.** `ParticipantID` is a fixed placeholder. Participants must be linked to rows through `SessionID` and the build used.
- **Second run only.** The row holds the second run on each scored board only (see `TMTVR_TestManager`).
- **Rows can still be written after a fault.**
  - The run check does not stop the save. A session whose runs did not complete would still write a row from whatever values the managers hold.
  - If `ExecuteTasks()` stopped early, the file holds only its header *(inferred)*.
- **Adjusted Time.** This value is an error-adjusted time in the inverse-efficiency style. It is not a motor correction. The motion logger computes no adjusted or motor-corrected score, and no validated motor measure is claimed for any output.
- **Motion-log runs.** Analysis that uses the motion log must exclude runs that have no `run_end` event.
- **Old code.** A commented-out earlier version of `SaveScore()` is kept at the end of the file. It is not compiled.

---

### TMTVR_InstructionsManager

File: `Assets/TMT-VR For Export/TMTVR_InstructionsManager.cs`

#### Purpose
This is a legacy helper for instruction videos. When enabled, it does the following:
1. fetches the `VideoPlayer` on its own GameObject;
2. waits until the video starts playing;
3. waits until it stops;
4. sets `Completed = true`.

No code path in the four release builds uses it. It is a present-but-inactive legacy path and is not validated.

#### Where it is used
- **Prefab:** two GameObjects in `TMT-VR_Greek.prefab`, `Instructions TMT-A` and `Instructions TMT-B`. Both are inactive in the prefab itself.
- **Other components on the same objects:** `PlayVideoEngGR` (a legacy video-language switch) and `OnEnableHeadHeight`.
- **Scenes:** all four scenes contain both objects through the `TMT-VR` prefab instance. The scenes also set `Instructions TMT-A` inactive through a redundant override. `Instructions TMT-B` has no active-state override.
- No script switches either object on.

#### Lifecycle
- **`OnEnable`:** `GetComponent<VideoPlayer>()`, then starts the wait coroutine described above.
- Because the hosts are never switched on, `OnEnable` does not run in the release builds *(inferred)*.

#### Inspector fields that matter

| Field | Role | Value |
|---|---|---|
| `videoPlayer` | The video to watch. It is replaced in `OnEnable` by the `VideoPlayer` on the same object. | Empty in the prefab |
| `Completed` | Set to true when the video ends | false |

#### Public members used by other scripts
None in the current code. `TMTVR_TestManager` used to read `Completed` through two fields, `instructionsA_Manager` and `instructionsB_Manager`. Those fields, and the code that waited on them, are now commented out. The old references survive only as stale data in the prefab's record for `TMTVR_TestManager`, which Unity ignores.

#### Talks to (outgoing)
- **`VideoPlayer`** on the same GameObject: `GetComponent`, then a `WaitUntil` poll on `isPlaying`.

#### Used by (incoming)
None. No other script references this class.

#### Files and persistent data
None.

#### Mode- and language-dependent behaviour
None in this script.

`PlayVideoEngGR` on the same objects has its own English and Greek flags. Its state is as follows:
- the English video slot is empty;
- the Greek video references do not resolve to any file in the project;
- it is also legacy and inactive.

#### Caveats and legacy notes
- `Completed` is never reset to false.
- If the video never starts, the coroutine waits for ever.
- The code comments in `TMTVR_TestManager` keep this path in case video instructions are restored. If it were re-enabled, the video assets would need to be supplied, and the path would need testing. Nothing in this release validates it.

---

## 2. Task and targets

These scripts build each board, label its targets, clean up between runs, let the participant choose the pointing controller, and place panels at head height. All of them live on the shared task prefab `Assets/TMT-VR For Export/TMT-VR_Greek.prefab` or on scene objects. The prefab name is historical; the same prefab serves the English and the Greek scenes.

---

### TargetPlacer

File: `Assets/TMT-VR For Export/TargetPlacer.cs`

#### Purpose

Builds the board for one TMT run. It starts from a fixed first target. It then picks each next target from six groups of candidate positions, so that the distance from the previous target falls in one of six distance bands. A tutorial board has 8 targets (the first plus 7). A scored board has 25 (the first plus 24). When the board is complete, it activates the chosen targets.

The file also declares the small serialisable class `TargetGroup`, which holds a `List<GameObject> targets`.

#### Where it is used

- Not placed directly in any scene.
- In `TMT-VR_Greek.prefab`, on the four part GameObjects: `TMT-VR_Part_A_Tutorial`, `TMT-VR_Part_B_Tutorial`, `TMT-VR_Part_A` and `TMT-VR_Part_B`. Each of these also carries `TMTVRManager`, `TargetTextAssigner` and `DestroyChildrenWithLineRenderer`.
- All four part GameObjects are inactive in the prefab. The prefab root (`TMT-VR` in the scenes) is also inactive at load.
- Identical in all four scenes.

#### Lifecycle

1. `TMTVR_CentralExecutive` activates the `TMT-VR` root before each part and deactivates it afterwards. It then calls `TMTVR_TestManager.TMTA()` or `TMTB()`.
2. `TMTVR_TestManager` activates each part GameObject in turn (tutorial, then the scored board twice). Each activation runs `TargetPlacer.OnEnable()`, so every run gets a new layout. Part A and Part B each run their scored board twice, so the two runs on a scored board use two different layouts.
3. `OnEnable()` chooses a root seed. This is `replaySeed` if `useReplaySeed` is set, and otherwise a fresh value from `new System.Random().Next()`. It builds a seed source from it.
4. It then loops: `ResetGroupsAndLists()` clears the groups and lists, sets `PlacementDone` to false and refills each group from the direct children of its parent. `PlaceTargets()` then builds the board.
5. `PlaceTargets()` creates 24 group keys (`Group1a` to `Group6d`, so each of the six groups appears four times) and the six band keys. For each step it copies both key lists and sorts them with a random comparer (`rand.Next(-1, 2)`). It walks groups, then bands, and asks `SelectTargetFromGroup()` for an unused target in that group whose distance from the previous target lies in that band. The pick is random among the candidates, and the chosen target is removed from its group.
6. The group index comes from the digit in the key. Each group therefore appears four times in the shuffled list. Only the position of a group's first appearance matters: a group that gave no candidate for any band cannot give one on a later duplicate in the same step (*inferred*).
7. `PlacementDone` is set to true, and only then is `FinalizePlacement()` called. It is called only if the full count was placed. It computes per-band average distances and activates every chosen target with `SetActive(true)`. Longest and shortest link distances are then computed, and `PlacementDone` is set true again.
8. If the random comparer makes `List.Sort` throw `ArgumentException` or `InvalidOperationException`, the exception is wrapped in a private `BadComparerException`. `OnEnable()` catches it, logs a warning with the seed and attempt number, and restarts the placement from step 4 with the same seed source. After `maxPlacementAttempts` it sets `lastPlacementFailed`, logs an error and returns with `PlacementDone` false.
9. If the board is complete, it raises the static event `LayoutPlaced`.

Distance bands (metres): 1–1.99, 2–2.99, 3–3.99, 4–4.99, 5–5.99, 6–6.99. Links shorter than 1 m, longer than 6.99 m, or falling in the small gaps between bands (for example 1.99–2.00 m) are never chosen.

#### Inspector fields that matter

| Field | Meaning | Value in the prefab |
|---|---|---|
| `group1Parent` … `group6Parent` | Parents whose direct children are the candidate targets | Child objects `Line1` … `Line6` of each part |
| `Tutorial` | `true`: 7 targets after the first (8 in all). `false`: 24 after the first (25 in all) | `true` on the two `_Tutorial` parts, `false` on `TMT-VR_Part_A` and `TMT-VR_Part_B` |
| `firstTarget` | Fixed start of the trail | A `TargetLarge` object in each part |
| `useReplaySeed`, `replaySeed` | Reproduce a board from a known seed | Not stored in the prefab, so the code defaults apply: `false`, `0` |
| `maxPlacementAttempts` | Restarts allowed after a comparer exception | Not stored in the prefab; code default `20` |
| `group1` … `group6`, `orderedTargets`, `averageDistances`, `PlacementDone`, `longestDistance`, `shortestDistance` | Working values and outputs, shown for inspection | Rebuilt on every enable |

#### Public members used by other scripts

- `PlacementDone` (bool): polled by `TMTVRManager` and `TargetTextAssigner`.
- `orderedTargets` (`List<GameObject>`): the board in selection order. Read by `TMTVRManager` and `TargetTextAssigner`.
- `Tutorial` (bool): read by `TMTVRManager` (8 or 25 selections to complete) and by `TMTVR_MotionLogger` (run stage label).
- `ResetPlacement()`: deactivates every target on the board. Called by `TMTVRManager` when a run completes.
- Diagnostics, not serialised: `lastLayoutSeed`, `lastAttempts`, `lastBadComparerExceptions`, `lastPlacementFailed`; constant `ScriptingBackend` (`"IL2CPP"` or `"Mono"`); static event `System.Action<TargetPlacer> LayoutPlaced`. No script in the project reads these.

#### Talks to (outgoing)

- Target GameObjects: reads positions, calls `SetActive(true)` on the board and `SetActive(false)` in `ResetPlacement()`.
- `LayoutPlaced` subscribers through a static C# event. There are none in the project.
- Unity log: a warning on each comparer restart; errors when a step finds no target or all attempts fail.

#### Used by (incoming)

- `TMTVR_TestManager`: indirectly, by activating the part GameObject (`SetActive(true)`), which runs `OnEnable()`. It holds serialised references to the `TMTVRManager` on each part.
- `TMTVRManager`: serialised reference `targetPlacer` (same GameObject); polls `PlacementDone`, reads `orderedTargets` and `Tutorial`, calls `ResetPlacement()`.
- `TargetTextAssigner`: serialised reference `targetPlacer` (same GameObject); polls `PlacementDone`, reads `orderedTargets`.
- `TMTVR_MotionLogger` (Controller scenes only): reads `Tutorial` through the `targetPlacer` field of the active `TMTVRManager`.

#### Files and persistent data

None. No files and no `PlayerPrefs`. The seed is kept in memory only. It reaches the device log only when a comparer restart or a failure is logged. A code comment marks `lastLayoutSeed` as "(logged)", but nothing in the project logs or saves it.

#### Mode- and language-dependent behaviour

None. The script behaves the same in all four scenes. Board size depends only on `Tutorial`.

#### Caveats and legacy notes

- **Partial board.** If a step finds no target, `PlaceTargets()` logs an error, stops early and still sets `PlacementDone` to true, but does not activate any target. `TMTVRManager` then reads `Targets[1]`; with only the first target placed this would throw (*inferred*). With a longer partial board the run would start with inactive targets (*inferred*).
- **Failed placement.** After `maxPlacementAttempts` restarts, `PlacementDone` stays false. `TMTVRManager` and `TargetTextAssigner` wait on it with no timeout, so the session would stall (*inferred*).
- **Replay.** `useReplaySeed` and `replaySeed` can reproduce a board only if they are set before the part GameObject is enabled. Nothing in the project sets them.
- `using static UnityEngine.GraphicsBuffer;` is unused.
- `PopulateListWithChildren` does not check for a missing group parent; an unassigned parent would throw in `OnEnable()`.

---

### TargetTextAssigner

File: `Assets/TMT-VR For Export/TargetTextAssigner.cs`

#### Purpose

Writes the visible labels on the targets of a finished board. Part A gets 1, 2, 3 … Part B alternates numbers and letters: 1, A, 2, B … in English, or 1, Α, 2, Β … in Greek.

#### Where it is used

- Not placed directly in any scene.
- In `TMT-VR_Greek.prefab`, on the same four part GameObjects as `TargetPlacer`. Its `targetPlacer` field points to the `TargetPlacer` on the same GameObject.
- Present in all four scenes through the `TMT-VR` prefab instance. The English scenes override its language flags (see below).

#### Lifecycle

1. `OnEnable()`: if `isTMT_VR_PartA` is set, forces `isTMT_VR_PartB` to false. Starts `CheckAndAssignTextCoroutine()`.
2. The coroutine waits with `WaitUntil(() => targetPlacer.PlacementDone)` and then calls `AssignTextToTargets(targetPlacer.orderedTargets)`.
3. `AssignTextToTargets` logs an error and stops if neither part flag is set, or if `isEnglish == isGreek`. Otherwise, for each target in order, it finds every `TextMeshProUGUI` under the target with `GetComponentsInChildren<TextMeshProUGUI>()` and sets its text.
4. Part A: target *i* gets the number *i* + 1. Part B: even positions get the next number and odd positions get the next letter. Letters wrap with a modulo, but a 25-target board never reaches the wrap.

Timing: `TargetPlacer` sets `PlacementDone` before it activates the targets, but both happen synchronously inside `TargetPlacer.OnEnable()`. The `WaitUntil` resumes on a later frame, so the targets are already active when the labels are written (*inferred* from Unity's coroutine timing). The default `GetComponentsInChildren` overload searches only active children.

#### Inspector fields that matter

| Field | Meaning | Prefab value | Scene override |
|---|---|---|---|
| `targetPlacer` | Board to label | `TargetPlacer` on the same GameObject | None |
| `isTMT_VR_PartA`, `isTMT_VR_PartB` | Which part | A on `TMT-VR_Part_A` and `TMT-VR_Part_A_Tutorial`; B on `TMT-VR_Part_B` and `TMT-VR_Part_B_Tutorial` | None |
| `isEnglish`, `isGreek` | Label language. Exactly one must be true | `isEnglish` false, `isGreek` true on all four | Greek scenes: none. English scenes: `isEnglish` true, `isGreek` false on all four instances |
| `textAssigned` | Declared but never read or written | — | — |

The code default for `isEnglish` is `true`, but the prefab stores `false`.

#### Public members used by other scripts

None. Other scripts do not call into this component.

#### Talks to (outgoing)

- `TargetPlacer`: serialised reference; polls `PlacementDone` in a coroutine and reads `orderedTargets`.
- Target labels: `GetComponentsInChildren<TextMeshProUGUI>()`, then sets `.text`. The prefab's label placeholders are overwritten at runtime.

#### Used by (incoming)

- `TMTVR_TestManager`, indirectly: activating the part GameObject runs `OnEnable()`.

#### Files and persistent data

None. Unity log only (configuration errors).

#### Mode- and language-dependent behaviour

- Not mode-dependent.
- Language-dependent. This is the only custom script that writes task text. Labels by board:

| Board | English | Greek |
|---|---|---|
| Part A tutorial (8) | 1–8 | 1–8 |
| Part A test (25) | 1–25 | 1–25 |
| Part B tutorial (8) | 1–4 and A–D | 1–4 and Α–Δ |
| Part B test (25) | 1–13 and A–L | 1–13 and Α–Μ |

- The language flags change visible content only on the two Part B instances. The Part A instances still need consistent flags, because equal flags stop all labelling.
- The alphabets are fixed in the script: 26 Latin capitals and 24 Greek capitals.
- In the English scenes the four instances must be overridden together with the other language switches: `TMTVR_TestManager.isEnglish`, `OnEnablePlayAudio.language` on `ProbeDomiCube`, the static overlays `InstructENG`/`InstructGR` and their `(1)` variants, and the end-screen panels `NotesR`/`NotesR_Greek`. See [VARIANTS.md](VARIANTS.md).

#### Caveats and legacy notes

- If `isEnglish == isGreek`, no labels are written and the targets keep their placeholder text.
- On a partial board (see `TargetPlacer`), `PlacementDone` becomes true without the targets being activated, so labels reach only text components that happen to be active (*inferred*).
- An unassigned `targetPlacer` would make the `WaitUntil` predicate throw every frame (*inferred*).

---

### DominanceCubeSelector

File: `Assets/TMT-VR For Export/DominanceCubeSelector.cs`

#### Purpose

Lets the participant choose the controller they will point with, by touching a cube with it. The first valid probe that enters the cube's trigger sets the ray origin in `OpenXRAimPointer`.

#### Where it is used

- Scene object `ProbeDomiCube`, in all four scenes. Not in any prefab.
- `ProbeDomiCube` is inactive at load. It also carries a trigger `BoxCollider`, `OnEnablePlayAudio` (spoken dominance instruction) and `OnEnableHeadHeight` (places the cube 0.2 m below head height).
- It is shown only in the Controller scenes.

#### Lifecycle

1. In Controller mode, `TMTVR_CentralExecutive.DominanceCheck()` activates `ProbeDomiCube`. This also runs `OnEnablePlayAudio` (plays the dominance instruction) and `OnEnableHeadHeight` (moves the cube).
2. `OnTriggerEnter(Collider other)`: ignored once `Selected` is true. Looks up `TipProbeId` with `other.GetComponentInParent<TipProbeId>()`. Ignores colliders without one, or whose `trackedTip` is unassigned. Stores the touching GameObject as `probeTip`.
3. If `aimPointer.Controller` is true and the probe kind is `ConTip`, it sets `aimPointer.controllerTip = probe.trackedTip`, sets `Selected`, and logs `[Dominance] Controller selected: <side>`. The equivalent `Hand` / `FinTip` branch sets `aimPointer.fingertip`.
4. If `lockAfterSelect` is true, the cube deactivates itself.
5. `OnDisable()`: deactivates the stored `probeTip`.
6. `TMTVR_CentralExecutive` waits on `Selected`, then deactivates the cube (already inactive) and calls `DisableTips()`, which turns off all four probes.

The trigger fires because each probe has its own kinematic `Rigidbody` and a non-trigger `SphereCollider`, while the cube's `BoxCollider` is a trigger. `FollowTargetPhysics` moves the probe's `Rigidbody` to follow the tracked point.

#### Inspector fields that matter

| Field | Meaning | Scene value |
|---|---|---|
| `aimPointer` | `OpenXRAimPointer` whose ray origin is set | The `OpenXRAimPointer` on `Managers` |
| `lockAfterSelect` | Hide the cube after a valid touch | `true` |

#### Public members used by other scripts

- `Selected` (read-only bool property): true after a valid touch. Polled by `TMTVR_CentralExecutive.DominanceCheck()` with `WaitUntil`.

#### Talks to (outgoing)

- `OpenXRAimPointer`: serialised reference. Reads `Hand` and `Controller`; writes `controllerTip` (or `fingertip`).
- `TipProbeId`: `GetComponentInParent` on the touching collider. Reads `kind`, `side` and `trackedTip`.
- The touching probe GameObject: `SetActive(false)` in `OnDisable()`.
- Its own GameObject: `SetActive(false)` after selection.

#### Used by (incoming)

- Unity physics: `OnTriggerEnter` when a probe's collider enters the cube.
- `TMTVR_CentralExecutive`: serialised field `dominanceCube`; caches this component with `GetComponent<DominanceCubeSelector>()`; activates and deactivates the cube; polls `Selected`.

#### Files and persistent data

None. Unity log only.

#### Mode- and language-dependent behaviour

- **Head scenes (`intMode` 2):** the cube is never activated. The script does nothing.
- **Controller scenes (`intMode` 3):** the cube is shown after start-up. `OpenXRAimPointer.controllerTip` is unassigned in the scenes, so the controller ray origin comes only from this touch. The session waits until a controller-tip probe touches the cube. There is no timeout and no fallback side.
- **Language:** the script itself has none. The co-hosted `OnEnablePlayAudio` plays `DomHand_GR.mp3` in the Greek scenes (`language` Greek) and `DomHand_ENG.mp3` in the English scenes (`language` English).
- The `Hand` / `FinTip` branch belongs to the hand-tracking path. It is present but inactive in these four builds and has not been validated.

#### Caveats and legacy notes

- There is no method to reset `Selected`. A second dominance check in the same session would pass at once. `TMTVR_CentralExecutive` assumes a fresh session.
- A probe of the wrong kind is ignored for selection but is still stored as `probeTip`, so it would be deactivated when the cube is disabled. In the Controller scenes the fingertip probes are inactive, so this does not arise.
- Disabling the cube also stops its `AudioSource`, so a very quick touch could cut the dominance instruction short (*inferred*).
- `aimPointer` is used without a null check.

---

### DestroyChildrenWithLineRenderer

File: `Assets/TMT-VR For Export/DestroyChildrenWithLineRenderer.cs`

#### Purpose

Clean-up helper on each part. When the part is disabled, it destroys every GameObject under it that carries a `LineRenderer`. The intent is to remove the lines drawn between selected targets, so that the next run of the same part starts with a clean board.

#### Where it is used

- Not placed directly in any scene.
- In `TMT-VR_Greek.prefab`, on the four part GameObjects. Present in all four scenes through the `TMT-VR` prefab instance.

#### Lifecycle

- `OnDisable()`: collects `GetComponentsInChildren<LineRenderer>()` and calls `Destroy(child.gameObject)` on each result.
- `TMTVRManager` (same GameObject) creates a child named `Line<n>` with its own `LineRenderer` for each selection after the first. It also adds a `LineRenderer` to its own GameObject when enabled and destroys that component when the run completes.
- `TMTVR_TestManager` disables each part GameObject at the end of its run. For the tutorial parts this happens in the same step that sees `Completed`. For the scored boards it happens after a one-second wait and the completion audio.

#### Inspector fields that matter

None.

#### Public members used by other scripts

None.

#### Talks to (outgoing)

- Child GameObjects with a `LineRenderer`: found with `GetComponentsInChildren`, removed with `Destroy`.

#### Used by (incoming)

- Unity, through `OnDisable()` when `TMTVR_TestManager` calls `SetActive(false)` on the part GameObject.

#### Files and persistent data

None.

#### Mode- and language-dependent behaviour

None.

#### Caveats and legacy notes

Neither of the following has been checked at runtime.

- **Possible no-op.** The default `GetComponentsInChildren` overload returns only active components. If the hierarchy already counts as inactive during `OnDisable()`, the script finds nothing. The `Line<n>` children would then stay under the part. The scored boards are re-enabled for their second run, so lines from the first run could reappear (*inferred*).
- **Possible self-destruction.** `GetComponentsInChildren` also returns a `LineRenderer` on the host itself. On the tutorial parts the GameObject is disabled in the same frame as `TMTVRManager` calls `Destroy(lineRenderer)`. Unity defers `Destroy` to the end of the frame, so the host's `LineRenderer` may still exist, and the script would then destroy the part GameObject. The tutorial parts are not used again in a session (*inferred*).

---

### FollowTargetPhysics

File: `Assets/TMT-VR For Export/FollowTargetPhysics.cs`

#### Purpose

Moves a probe (a small collider with a kinematic `Rigidbody`) to follow a tracked point every physics step. Moving the `Rigidbody` with `MovePosition` gives reliable trigger contacts with the dominance cube.

#### Where it is used

All four scenes, on four probe GameObjects. All four are inactive at load.

| Probe GameObject | `target` (tracked point) | Path |
|---|---|---|
| `ControlllerTip_L` | `TriggerLeft` | Left controller visual, under `ControllersInteractionRoot` |
| `ControlllerTip_R` | `TriggerRight` | Right controller visual, under `ControllersInteractionRoot` |
| `FingerTip_L` | `LeftTipProxy` | Under `HandsInteractionRoot` |
| `FingerTip_R` | `RightTipProxy` | Under `HandsInteractionRoot` |

The controller-tip names are spelled `ControlllerTip_*` (three l's) in the scenes. The script also sits on `Assets/TMT-VR For Export/IndexFingerColliderL.prefab` (target unassigned). No scene uses that prefab.

#### Lifecycle

- `Awake()`: gets the required `Rigidbody` (`[RequireComponent(typeof(Rigidbody))]`), forces `isKinematic = true` and sets interpolation to `Interpolate`.
- `FixedUpdate()`: if `target` is set, calls `MovePosition(target.position)`; if `followRotation`, also `MoveRotation(target.rotation)`.
- `TMTVR_CentralExecutive` switches the probes on and off. `SetControllersActive` and `SetHandsActive` follow `intMode`. `DisableTips()` turns all four off straight after the dominance check, or straight away in Head mode. The probes therefore exist only for side selection.

#### Inspector fields that matter

| Field | Meaning | Scene value |
|---|---|---|
| `target` | Transform to follow | See table above |
| `followRotation` | Also match rotation | `false` on all four |

#### Public members used by other scripts

None called. `target` is set in the scenes only.

#### Talks to (outgoing)

- Its own `Rigidbody` (`GetComponent`).
- The tracked transform: serialised reference `target`.

#### Used by (incoming)

- `TMTVR_CentralExecutive`: serialised fields `contipL`, `contipR`, `fintipL`, `fintipR`; switches the GameObjects with `SetActive`.
- `DominanceCubeSelector`: receives the probe's trigger contact and deactivates the touching probe in its `OnDisable()`.

#### Files and persistent data

None.

#### Mode- and language-dependent behaviour

- **Controller scenes:** `ControlllerTip_L` and `ControlllerTip_R` are active from mode set-up until the dominance touch.
- **Head scenes:** no probe is ever active.
- The fingertip probes belong to the hand-tracking path. They are present but inactive in these four builds and have not been validated.
- No language dependence.

#### Caveats and legacy notes

- Each probe has its own serialised `Rigidbody`, already kinematic in the scenes. The script requires it and re-forces kinematic mode; it does not supply it.
- The probe `SphereCollider`s are not triggers. The trigger is the cube's `BoxCollider`.
- Hand path only: `LeftTipProxy` and `RightTipProxy` both sit under the `Right Hand` GameObject. Each is driven by an XRI `XRTransformStabilizer`, so the parenting may not matter at runtime (*inferred*, not verified).

---

### TipProbeId

File: `Assets/TMT-VR For Export/TipProbeId.cs`

#### Purpose

A data-only tag on each probe. It says whether the probe is a fingertip or a controller tip, which side it is on, and which tracked transform it follows. It has no Unity callbacks.

#### Where it is used

All four scenes, on the same four probe GameObjects as `FollowTargetPhysics`. Not in any prefab.

| Probe GameObject | `kind` | `side` | `trackedTip` |
|---|---|---|---|
| `ControlllerTip_L` | `ConTip` | `Left` | `TriggerLeft` |
| `ControlllerTip_R` | `ConTip` | `Right` | `TriggerRight` |
| `FingerTip_L` | `FinTip` | `Left` | `LeftTipProxy` |
| `FingerTip_R` | `FinTip` | `Right` | `RightTipProxy` |

#### Lifecycle

None. The component only holds data.

#### Inspector fields that matter

| Field | Meaning |
|---|---|
| `kind` | `ProbeKind.FinTip` or `ProbeKind.ConTip` |
| `side` | `Side.Left` or `Side.Right` |
| `trackedTip` | Tracked transform. It should be the same transform as `FollowTargetPhysics.target` on the same GameObject; in all four scenes it is |

#### Public members used by other scripts

- Nested enums `TipProbeId.ProbeKind` and `TipProbeId.Side`.
- Fields `kind`, `side`, `trackedTip`.

#### Talks to (outgoing)

Nothing.

#### Used by (incoming)

- `DominanceCubeSelector.OnTriggerEnter`: `GetComponentInParent<TipProbeId>()` on the touching collider. Copies `trackedTip` into `OpenXRAimPointer.controllerTip` (or `fingertip`) according to `kind`, and logs `side`.
- `TMTVR_MotionLogger`: `FindObjectsByType<TipProbeId>(FindObjectsInactive.Include, …)` at the start of each run. It finds the probe whose `trackedTip` equals `aimPointer.controllerTip` and writes its `side` into the `controller_hand` field of the `run_start` record.

#### Files and persistent data

None in this script. It supplies the `controller_hand` value that the motion logger writes.

#### Mode- and language-dependent behaviour

- The motion logger runs only in the Controller scenes (`motionLogEnabled` is true there and false in the Head scenes). It reads `TipProbeId` only when `aimPointer.Controller` is true. Otherwise `controller_hand` is `"?"`.
- The motion logger records events and poses only. It computes no adjusted or motor-corrected score, and no validated motor measure should be derived from it. Runs without a `run_end` record must be excluded from analysis.
- No language dependence.

#### Caveats and legacy notes

- The `FinTip` entries serve the hand-tracking path, which is present but inactive in these builds.

---

### OnEnableHeadHeight

File: `Assets/TMT-VR For Export/OnEnableHeadHeight.cs`

#### Purpose

When its GameObject is enabled, moves it vertically to the participant's current head height, optionally raised or lowered by a fixed offset. The x and z position are kept.

#### Where it is used

| Host | Location | Offset | Active in these builds |
|---|---|---|---|
| `ProbeDomiCube` | Scene object, all four scenes | `low`, `lower` 0.2 m (0.2 m below head height) | Controller scenes only |
| `EndOfExperiment_Greek` | Scene object, all four scenes; end-screen root holding both the English and the Greek panels | None | Yes, at the end of every session |
| `Instructions TMT-A`, `Instructions TMT-B` | Inside `TMT-VR_Greek.prefab`; `camHead` set by scene overrides | None | No (legacy) |

The script is also on `Assets/TMT-VR For Export/GER_EndOfExperiment_Greek_.prefab` with `camHead` unassigned. No scene uses that prefab.

#### Lifecycle

- `OnEnable()`: if `withDelay`, stores `appearTime = Time.time` and starts `UpdatePositionCoroutine()`. Otherwise repositions at once.
- `UpdatePositionCoroutine()`: waits until `Time.time > appearTime + delay`, then repositions.
- Repositioning: the height is `camHead.y + higher` if `high`, else `camHead.y - lower` if `low`, else `camHead.y`. The object's world position becomes `(x, height, z)`.
- Who enables the hosts:
  - `ProbeDomiCube`: `TMTVR_CentralExecutive.DominanceCheck()` (Controller mode only).
  - `EndOfExperiment_Greek`: `TMTVR_CentralExecutive.ExperimentEnd()`, through its serialised field `EndofExperiment`, after the score is saved.
  - `Instructions TMT-A` / `TMT-B`: nothing. These GameObjects host the legacy `TMTVR_InstructionsManager` video players. Both GameObjects are inactive in the prefab, and the code in `TMTVR_TestManager` that enabled them is commented out. The live instruction overlays are separate scene objects (`InstructENG_A`, `InstructGR _A`, `InstructENG _B`, `InstructGR _B`) and do not carry this script.

#### Inspector fields that matter

| Field | Meaning | Value on the active hosts |
|---|---|---|
| `camHead` | Head object | `XR Origin Hands (XR Rig)/Camera Offset/Main Camera` |
| `high`, `higher` | Raise above head height by `higher` | Not set |
| `low`, `lower` | Lower below head height by `lower` | `ProbeDomiCube`: `low` true, `lower` 0.2. End screen: not set |
| `withDelay`, `delay` | Reposition after a delay | Not set on any host |
| `currentPosition`, `height`, `appearTime` | Working values, public for inspection | — |

#### Public members used by other scripts

None.

#### Talks to (outgoing)

- `camHead`: serialised reference; reads its world `y`.
- Its own `Transform`: sets the world position.

#### Used by (incoming)

- Unity, through `OnEnable()` when `TMTVR_CentralExecutive` activates `ProbeDomiCube` or the end screen.

#### Files and persistent data

None.

#### Mode- and language-dependent behaviour

- The cube host is active only in the Controller scenes. The end-screen host is active in all four.
- The script has no language logic. The end-screen root is named `EndOfExperiment_Greek` in the source scenes, but it holds both language panels. Which panel shows is set by the active state of `NotesR` (English) and `NotesR_Greek` (Greek). The script moves the root, so it acts the same for both languages.

#### Caveats and legacy notes

- `camHead` is not null-checked. The prefab defaults are empty and rely on scene overrides.
- `else if (!withDelay)` is equivalent to a plain `else`.
- The height is read once, when the object appears. It does not follow later head movement.

---

## 3. Interaction and XR

These scripts cast the pointing ray, move the pointer and confirm selections. The group also holds XR helper scripts that ship with the project but are not used by the four builds. The eye-tracking path is present in code but is not used by any build, has not been validated and, as configured, has no working pointer.

---

### OpenXRAimPointer

File: `Assets/TMT-VR For Export/OpenXRAimPointer.cs`

#### Purpose

The single raycasting component in the project. Each frame it works out the ray origin and direction (head, controller tip or fingertip), casts one ray with `Physics.Raycast`, decides whether the hit counts, and moves the pointer object to the hit point. It does not select anything. Selection happens in `ET_Actions` when the pointer overlaps a target.

#### Where it is used

- Scenes: one instance on the GameObject `Managers` in all four scenes. `Managers` also holds `TMTVR_CentralExecutive`, `TMTVR_ScoreManager` and `TMTVR_MotionLogger`.
- Prefabs: none.

#### Lifecycle

There is no `Awake`, `Start` or `Update`. All work happens in `LateUpdate()`:

1. If `RayCasting` is false, it clears `HasValidHit` and `CurrentHitCollider`, deactivates `aimTarget` and returns. This branch runs before the null check on `aimTarget`, so a missing `aimTarget` would throw a `NullReferenceException` here.
2. If `aimTarget` is null, it returns.
3. If `xrCamera` is null, it logs one error and returns.
4. Otherwise it calls `ComputeOriginAndDirection()` and `PerformCastAndUpdateOutputs()`, activates `aimTarget` if needed, and moves it to `CurrentHitPoint`.

`ComputeOriginAndDirection()` starts from the head ray (camera position and forward vector). The first true mode flag then wins, in this order:

- `Head`: the head ray.
- `Hand`: `fingertip.position` and `fingertip.rotation * fingertipLocalForwardAxis`, if `fingertip` is assigned.
- `Controller`: `controllerTip.position` and `controllerTip.rotation * controllerLocalForwardAxis`, if `controllerTip` is assigned.
- `Eye`: logs "Raycasting is Controlled By EyeGazer" and sets `RayCasting = false`.
- No flag: logs "No Interaction Mode was Selected" and sets `RayCasting = false`.

If `Hand` or `Controller` is set but the tip transform is missing, the ray silently stays on the head ray. No message is logged.

`PerformCastAndUpdateOutputs()` calls `Physics.Raycast(origin, direction, out hit, maxDistance, hitMask, triggerInteraction)`. This returns only the nearest collider in `hitMask`. The hit is accepted only if that collider has the tag `etTag` (when `requireETTag` is true). The cast does not look past a nearer, untagged collider. If the hit is accepted, `CurrentHitPoint` is the hit point. Otherwise the pointer is placed `floatingDistance` metres along the ray.

The pointer becomes visible only once `TMTVR_CentralExecutive` sets `RayCasting = true`. Until then, every frame takes the first branch and keeps the pointer inactive.

#### Inspector fields that matter

| Field | Value in all four scenes | Notes |
|---|---|---|
| `Head`, `Eye`, `Hand`, `Controller` | all false | One is set at run time by `TMTVR_CentralExecutive`. |
| `aimSource` | empty | Set at run time to "Head" or "Controller". Never read by this script. |
| `fingertip` | None | Set at run time by `DominanceCubeSelector` (hand mode only). |
| `fingertipLocalForwardAxis` | (0, 0, 1) | Local forward axis of the fingertip. |
| `controllerTip` | None | Set at run time by `DominanceCubeSelector` (controller mode). |
| `controllerLocalForwardAxis` | (0, 0, -1) | Local forward axis of the controller tip. |
| `xrCamera` | the XR rig camera | Head-ray origin; also read by `TMTVR_MotionLogger`. |
| `maxDistance` | 20 | Metres. |
| `floatingDistance` | 1 | Metres along the ray when no hit is accepted. |
| `triggerInteraction` | Collide | Trigger colliders count as hits. Needed, because the target colliders are triggers. |
| `requireETTag` | true | Only colliders tagged `etTag` are accepted. |
| `etTag` | "ET" | Tag carried by every `Target_Collider`. |
| `hitMask` | Default, TransparentFX, Water, UI | Excludes layer 2 (Ignore Raycast), where the pointer lives, so the ray never hits the pointer. The target colliders are on the UI layer in the prefab and on the Default layer in the scenes (scene-level prefab overrides); both layers are in the mask. |
| `aimTarget` | the scene object `Pointer` | An instance of `Ray_Pointer.prefab`, saved inactive. |
| `RayCasting` | false | Set to true by `TMTVR_CentralExecutive` after mode set-up. |

The pointer object (`Ray_Pointer.prefab`) is tagged `ET_Pointer` and sits on layer 2 (Ignore Raycast). It has a non-trigger `SphereCollider` (radius 0.5) and a kinematic `Rigidbody`. Its root is scaled 0.1, so the sphere's effective radius is about 0.05 m. Its child, `Pointer_Highlight`, has only a mesh.

#### Public members used by other scripts

- Mode flags `Head`, `Eye`, `Hand`, `Controller`; string `aimSource`; flag `RayCasting`.
- Tip transforms `fingertip`, `controllerTip` and their forward axes `fingertipLocalForwardAxis`, `controllerLocalForwardAxis`.
- `xrCamera` and `aimTarget`.
- Read-only outputs `HasValidHit`, `CurrentHitPoint`, `CurrentHitCollider`.
- The component's `enabled` flag (written only by the unused `ET_Manager_TMTVR`).

#### Talks to (outgoing)

- `Pointer` (`aimTarget`), by serialized reference: activates or deactivates it and sets its position every frame.
- XR camera (`xrCamera`), by serialized reference: reads its transform.
- Target colliders, through `Physics.Raycast`. The hit itself does not select anything. The selection is triggered by the pointer's collider overlapping the target (see `ET_Actions`).

#### Used by (incoming)

- `TMTVR_CentralExecutive`, by serialized reference (`aimPointer`). After the score file is initialised and the loading delay has passed, it sets one mode flag and `aimSource`. In controller mode it then waits for the dominance check. Finally it sets `RayCasting = true`.
- `DominanceCubeSelector`, by serialized reference (`aimPointer`). When a tip probe touches the dominance cube, it assigns `controllerTip` (or `fingertip`) from the probe's `TipProbeId.trackedTip`. The tip is the tracked transform, not the probe object, so deactivating the probes afterwards does not affect the ray.
- `TMTVR_TestManager` (on the `TMT-VR` prefab instance), by a scene override of its `aimPointer` field. It reads the mode flags to choose the tutorial audio. `Eye` is checked first, then `Head`, then `Hand` or `Controller`.
- `TMTVR_MotionLogger`, by `FindFirstObjectByType<OpenXRAimPointer>` (its own field is unassigned in the scenes). It runs only when `motionLogEnabled` is on, and with execution order 200, so after this script's `LateUpdate`. It reads `HasValidHit`, `CurrentHitPoint`, `CurrentHitCollider`, the mode flags, the tip transforms and forward axes, and `xrCamera`. It recomputes the ray origin and direction in the same way, without changing anything here. It records events and poses only. It computes no adjusted or motor-corrected score, and no validated motor measure may be claimed from it. Runs without a `run_end` record must be excluded from analysis.
- Code only, not placed in any scene: `TMT_VR_RayCastMode`, `ET_Manager_TMTVR`, `XRSingleModeBootstrapper` (see their sections).

#### Files and persistent data

None. No PlayerPrefs, no files.

#### Mode- and language-dependent behaviour

- Head scenes (`intMode` 2): `Head` is set. The ray starts at the camera and points along the head's forward direction.
- Controller scenes (`intMode` 3): `Controller` is set. The ray starts at the dominant controller's tip and points along `controllerLocalForwardAxis`. The pointer appears only after the dominance cube has been touched.
- Eye mode (present in code, not used by the four builds): on the first frame the script casts one head ray and shows the pointer, then turns `RayCasting` off. On every later frame it hides the shared `Pointer`. An inactive pointer cannot trigger `ET_Actions`, so as configured eye mode has no working selection path.
- No language-dependent behaviour.

#### Caveats and legacy notes

- `aimSource` is written by `TMTVR_CentralExecutive` (and, in code, by `TMT_VR_RayCastMode`) but never read here. Only the four flags control behaviour.
- A nearer collider in `hitMask` that is not tagged `ET` blocks the ray. The pointer then floats 1 m along the ray and no target is reported.
- The "No Interaction Mode was Selected" and eye-mode messages are logged once, on the frame that turns `RayCasting` off.

---

### ET_Actions

File: `Assets/TMT-VR For Export/ET_Actions.cs`

#### Purpose

The selection logic on each TMT target. When the pointer (tag `ET_Pointer`) stays inside a target's trigger for `confirmationTime` seconds, the selection is confirmed. A correct target reports the selection to its part's `TMTVRManager`. A wrong target adds one error and turns red. The "ET" prefix is historical (eye tracking). The same logic serves every mode, because every mode moves the same pointer object.

#### Where it is used

- Prefab: `Assets/TMT-VR For Export/TMT-VR_Greek.prefab` holds 672 instances, one on each GameObject named `Target_Collider`. There are 168 under each of `TMT-VR_Part_A`, `TMT-VR_Part_B`, `TMT-VR_Part_A_Tutorial` and `TMT-VR_Part_B_Tutorial`.
- Scenes: the prefab is instanced once in each of the four scenes (renamed `TMT-VR` in the scene). The scenes override no `ET_Actions` field. The prefab name is historical; the same prefab serves both languages.

Each `Target_Collider` is a child of a `TargetLarge (N)` object. It carries a RectTransform, a MeshFilter, a MeshRenderer, a `BoxCollider` with Is Trigger on, and `ET_Actions`. It is tagged `ET`, which makes it a valid hit for `OpenXRAimPointer`. It is on the UI layer in the prefab; in the scenes, scene-level prefab overrides put it and its children on the Default layer. Both layers are in `OpenXRAimPointer.hitMask`, so the ray reaches it either way.

#### Lifecycle

- `Awake()`, `Start()` and `OnEnable()` each call `EnsureWiringIfMissing()`. This fills `Parent`, `audioSource` and `childObject` only if they are empty.
- `Start()` and `OnEnable()` cache the MeshRenderer's original colour.
- `OnEnable()` also resets the distance samples, sets `selected = false` and hides `childObject`. Each activation of a run therefore starts clean.
- `OnTriggerEnter(other)`: if `other` has tag `targetTag` and the target is not yet selected, it clears the distance samples, resets the confirmation flags and starts `FlickerChild()`. That coroutine toggles `childObject` every 0.1 s while the pointer is inside.
- `OnTriggerStay(other)` runs once per physics step while the pointer overlaps (the project's fixed timestep is 0.02 s):
  - On the first step it records the start time.
  - On each step it appends the distance between the pointer's centre and the `Target_Collider`'s centre.
  - At the first step at or after start time + `confirmationTime`, it evaluates the selection once:
    - **Correct** (`Parent == tmtVR.CurrentTarget`): plays `soundConfirm`, shows `childObject` (the `Selection` sibling), sets `selected = true`, sets `tmtVR.accuracy` and calls `tmtVR.OnTargetSelected()`. `accuracy` is the mean of the distance samples from this entry only, because samples are cleared on each entry. It is 0 for the first target of a run (`TargetsSelected == 0`).
    - **Wrong**: sets the renderer colour to red, plays `soundFalse` and adds 1 to `tmtVR.totalMistakes`.
    - In both cases it stops the flicker.
- `OnTriggerExit(other)`: resets the timer and flags, stops the flicker, hides `childObject` if the target is not selected, and restores the original colour.

#### Inspector fields that matter

| Field | Value in all 672 instances | Notes |
|---|---|---|
| `tmtVR` | the `TMTVRManager` of its own part | Each part's 168 colliders point to that part's manager. |
| `Parent` | the `TargetLarge (N)` object | Compared with `tmtVR.CurrentTarget`. |
| `targetTag` | "ET_Pointer" | Code default is "YourTargetTag". |
| `childObject` | the `Selection` sibling | Flickers during the dwell; stays on after a correct selection. |
| `audioSource` | the AudioSource on `Parent` | |
| `soundConfirm` | `Confirm.wav` | Correct selection. |
| `soundFalse` | `wrong.wav` | Wrong selection. |
| `soundEnter` | `EnterSound.wav` | Assigned but never played (the call is commented out). |
| `confirmationTime` | 0.5 | Seconds of continuous overlap needed to confirm. Code default is 0. |

#### Public members used by other scripts

- `AssignSelectionSiblingAndDependencies(bool logWarnings = true)`: sets `Parent = transform.parent`, `audioSource` from `Parent`, and `childObject` from the direct child of `Parent` named `Selection`. Called by `ET_ActionsEditor` and, without warnings, by `EnsureWiringIfMissing()`.
- `confirmationTime`: read by `TMTVR_MotionLogger`.

#### Talks to (outgoing)

- `TMTVRManager`, by serialized reference (`tmtVR`): reads `CurrentTarget` and `TargetsSelected`; writes `accuracy` and `totalMistakes`; calls `OnTargetSelected()`.
- `Selection` sibling and the AudioSource on `Parent`, by serialized reference (with a run-time fallback using `transform.parent`, `GetComponent<AudioSource>()` and `Transform.Find("Selection")`).
- Its own `FlickerChild()` coroutine, through `StartCoroutine` / `StopCoroutine`.

#### Used by (incoming)

- The pointer object, through Unity physics trigger callbacks (`OnTriggerEnter`, `OnTriggerStay`, `OnTriggerExit`). `OpenXRAimPointer` places the pointer at the ray hit point on the target, and the pointer's sphere collider and kinematic rigidbody then overlap the target's trigger box.
- `TMTVR_MotionLogger`, through `GetComponentInChildren<ET_Actions>(true)` on the first target of each run. It reads `confirmationTime` and writes it as `dwell_s` in the `run_start` record.
- `ET_ActionsEditor`, as a custom Inspector.

#### Files and persistent data

None. It writes only to `TMTVRManager` fields in memory.

#### Mode- and language-dependent behaviour

- The same logic and the same 0.5 s confirmation time apply in Head and Controller scenes.
- The spoken instructions and on-screen labels tell participants to hold for one second. The configured confirmation time is 0.5 s.
- No language-dependent behaviour. The three sounds are non-verbal.

#### Caveats and legacy notes

- A wrong-target dwell adds one error per entry. Leaving and re-entering the same wrong target adds another. The target stays red until the pointer leaves.
- After a correct selection the target colour is not changed; the `Selection` sibling stays visible.
- The value stored as `accuracy` is a mean distance in scene units between two object centres during the confirming dwell. It is a raw descriptive value, not a validated accuracy measure.
- `OnTriggerEnter`, `OnTriggerStay` and `OnTriggerExit` each write a `Debug.Log` message. The `OnTriggerStay` message runs on every physics step during a dwell, so device logs are verbose.

---

### GazeHitBridge

File: `Assets/TMT-VR For Export/GazeHitBridge.cs`

#### Purpose

Copies the hit point of an XR Interaction Toolkit `XRGazeInteractor` onto a pointer Transform. When there is no hit, it places the pointer a fixed distance along the gaze ray. It does no selection. It belongs to the eye-tracking path, which is present in code but not used by the four builds and has not been validated.

#### Where it is used

- Scenes: one instance on the GameObject `Gaze Interactor`, a child of `GazeInteractionRoot`, in all four scenes. `GazeInteractionRoot` is the `EyeGazeRoot` field of `TMTVR_CentralExecutive`.
- Prefabs: none.

#### Lifecycle

- `Awake()`: if `gazeInteractor` is empty, it takes the `XRGazeInteractor` on the same GameObject.
- `LateUpdate()`: returns if `gazeInteractor` or `aimTargetTransform` is empty. Otherwise, if `gazeInteractor.TryGetHitInfo(...)` succeeds, it moves `aimTargetTransform` to the hit position. If not, it places it `fallbackDistance` metres along the forward vector of the interactor's ray origin transform (when `preferRayOriginTransform` is true and that transform exists) or of the interactor itself.

`GazeInteractionRoot` is saved active. `TMTVR_CentralExecutive` deactivates it in head and controller mode only when its start-up coroutine reaches the mode switch, that is after the score file is initialised and after `delayForLoading` (1 s). Until then `Gaze Interactor`, this component and `Pointer_ET_Only` are active, although this component does nothing because it has no output target.

#### Inspector fields that matter

| Field | Value in all four scenes | Notes |
|---|---|---|
| `gazeInteractor` | the `XRGazeInteractor` on the same GameObject | |
| `aimTargetTransform` | None | With no target, `LateUpdate` returns at once. |
| `fallbackDistance` | 2 | Code default is 5. |
| `preferRayOriginTransform` | true | |

#### Public members used by other scripts

None. No project script references this class.

#### Talks to (outgoing)

- `XRGazeInteractor` (XR Interaction Toolkit), by serialized reference, with a `GetComponent` fallback. Reads `TryGetHitInfo` and `rayOriginTransform`.
- `aimTargetTransform`, by serialized reference (empty in all scenes).

#### Used by (incoming)

- None directly. Its host's parent, `GazeInteractionRoot`, is switched by `TMTVR_CentralExecutive` (`EyeGazeRoot.SetActive`), active only in eye mode once the mode switch has run.

#### Files and persistent data

None.

#### Mode- and language-dependent behaviour

- Head and Controller scenes: inert. It has no output target, and its root is deactivated at the mode switch.
- Eye mode (not used by the four builds): still inert as configured, because `aimTargetTransform` is empty. Combined with `OpenXRAimPointer` hiding the shared `Pointer` in eye mode, eye mode has no working pointer path.
- No language-dependent behaviour.

#### Caveats and legacy notes

- Each scene also contains a second `Ray_Pointer.prefab` instance named `Pointer_ET_Only`, under `GazeInteractionRoot`, saved active, tagged `ET_Pointer`, with a sphere collider and kinematic rigidbody. No script references it. It may have been the intended target of this component (inferred). It is deactivated with its root at the mode switch, before the task starts.

---

### TMT_VR_RayCastMode

File: `Assets/TMT-VR For Export/TMT_VR_RayCastMode.cs`

#### Purpose

A legacy mode switcher. It holds one enum, `RayMode { Head, Hand, Controller, Eye }`, and each frame pushes it into `OpenXRAimPointer.aimSource` (as a string) and into `ET_Manager_TMTVR.ET_Mode`. It also shows or hides a pointer object with the task root. It does no raycasting itself.

#### Where it is used

Not placed in any of the four scenes or in any prefab. It is code only.

#### Lifecycle

- `Awake()`: logs an error for each empty reference (`tmtRoot`, `etManager`, `aimPointer`, `pointerObject`). If `aimPointer` and `pointerObject` are both set, it sets `aimPointer.aimTarget = pointerObject`.
- `Update()`:
  - If `tmtRoot` is empty or inactive, it hides `pointerObject` and, if `etManager` is set, sets `ET_Mode = false`. Then it returns.
  - If `aimPointer` or `etManager` is empty, it returns.
  - Otherwise it activates `pointerObject` and switches on `mode`. Head, Hand and Controller set `ET_Mode = false`; Eye sets it to true. Each case sets `aimSource` to "Head", "Hand", "Controller" or "Eye".
- Editor only: a context-menu item, "Rebind AimTarget to Pointer Object", repeats the `aimTarget` binding.

#### Inspector fields that matter

| Field | Code default | Notes |
|---|---|---|
| `mode` | Head | Public enum. |
| `tmtRoot` | none | Task root; private serialized. |
| `etManager` | none | `ET_Manager_TMTVR`. |
| `aimPointer` | none | `OpenXRAimPointer`. |
| `pointerObject` | none | The pointer to bind and show. |

#### Public members used by other scripts

`mode` only. No script reads or writes it.

#### Talks to (outgoing)

- `OpenXRAimPointer`, by serialized reference: writes `aimTarget` and `aimSource`.
- `ET_Manager_TMTVR`, by serialized reference: writes `ET_Mode`.

#### Used by (incoming)

None. Its header says the mode is "set by CentralExecutive or Inspector", but `TMTVR_CentralExecutive` does not reference this class.

#### Files and persistent data

None.

#### Mode- and language-dependent behaviour

Not active in any build. No language-dependent behaviour.

#### Caveats and legacy notes

- `OpenXRAimPointer` never reads `aimSource`. Even if this script were placed in a scene, its mode writes would not change the ray.
- `TMT-VR_Greek.prefab` and a prefab-instance override in all four scenes still carry a serialized key `TMTRayMode` (empty) on `TMTVR_TestManager`. `TMTVR_TestManager.cs` no longer declares that field. Unity ignores such keys. It is probably left over from an earlier version that referenced this class (inferred).

---

### ET_Manager_TMTVR

File: `Assets/TMT-VR For Export/ET_Manager_TMTVR.cs`

#### Purpose

A legacy visual-policy manager for the pointer. It enables `OpenXRAimPointer` together with the task root. While the task runs, it shows or hides the pointer's renderer and an optional follower object according to `ET_Mode`.

#### Where it is used

Not placed in any of the four scenes or in any prefab. It is code only.

#### Lifecycle

- `Awake()`: caches the MeshRenderer of `gazeTarget`, then sets `gazePointer.aimTarget = gazeTarget` if both are set.
- `Update()`: returns if `tmtVrTask` is empty. When `tmtVrTask.activeSelf` differs from the cached value, it calls `ApplyTaskActiveState`. While the task is active:
  - the renderer is enabled, or, if `showPointerRendererOnlyInETMode` is set, enabled only when `ET_Mode` is true;
  - the follower is active, or, if `showFollowerOnlyInETMode` is set, active only when `ET_Mode` is true.
- `ApplyTaskActiveState(bool)`: sets `gazePointer.enabled` to the task state. When the task ends, it hides the follower and the renderer.

The cached task state starts as false. If the task is inactive at start, `ApplyTaskActiveState(false)` is never called, so `OpenXRAimPointer` stays enabled until the task first becomes active.

#### Inspector fields that matter

| Field | Code default | Notes |
|---|---|---|
| `tmtVrTask` | none | Task root. |
| `gazePointer` | none | `OpenXRAimPointer`. |
| `gazeTarget` | none | Pointer object. |
| `gazeTargetFollower` | none | Optional. |
| `showPointerRendererOnlyInETMode` | false | |
| `showFollowerOnlyInETMode` | true | |
| `ET_Mode` | false | Public run-time flag. |

#### Public members used by other scripts

`ET_Mode`, written by `TMT_VR_RayCastMode`.

#### Talks to (outgoing)

- `OpenXRAimPointer`, by serialized reference: writes `aimTarget` and `enabled`.
- `gazeTarget` renderer and `gazeTargetFollower`, by serialized reference.

#### Used by (incoming)

`TMT_VR_RayCastMode` (code only), by serialized reference. Since neither script is placed, the pair is inactive.

#### Files and persistent data

None.

#### Mode- and language-dependent behaviour

Not active in any build. No language-dependent behaviour.

#### Caveats and legacy notes

Legacy eye-tracking support. Not validated.

---

### XRSingleModeBootstrapper

File: `Assets/TMT-VR For Export/XRSingleModeBootstrapper.cs`

#### Purpose

An optional helper that activates only the tracking root needed by the mode flag set on `OpenXRAimPointer`:

- `Eye`: gaze root on, controllers and hands roots off.
- `Hand`: hands root on, the others off.
- `Controller`: controllers root on, the others off.
- `Head`: all three off.
- No flag: logs "No Interaction Mode Detected".

#### Where it is used

Not placed in any of the four scenes or in any prefab. It is code only.

#### Lifecycle

No Unity lifecycle methods. The public `Apply()` does nothing unless both `ceo` and `aimPointer` are set. No script calls `Apply()`.

#### Inspector fields that matter

| Field | Notes |
|---|---|
| `aimPointer` | `OpenXRAimPointer`; its flags choose the root. |
| `controllersTrackingRoot` | |
| `handsTrackingRoot` | |
| `xriGazeRoot` | |
| `ceo` | `TMTVR_CentralExecutive`; used only as a "must be set" gate. |

#### Public members used by other scripts

`Apply()`. It has no caller.

#### Talks to (outgoing)

- `OpenXRAimPointer`, by serialized reference: reads the four mode flags.
- The three root GameObjects, by serialized reference: `SetActive`.
- `TMTVR_CentralExecutive`, by serialized reference: held but never called.

#### Used by (incoming)

None.

#### Files and persistent data

None.

#### Mode- and language-dependent behaviour

Not active in any build. In the four builds, `TMTVR_CentralExecutive` does the root switching itself. No language-dependent behaviour.

#### Caveats and legacy notes

Legacy helper. Not validated.

---

### XRHardResetMode

File: `Assets/TMT-VR For Export/XRHardResetMode.cs`

#### Purpose

A generic helper that forces one interaction mode for the whole application. It disables listed components (for example input modality managers or gaze assistance) and activates exactly one rig subtree.

#### Where it is used

Not placed in any of the four scenes or in any prefab. It is code only.

#### Lifecycle

`Awake()` calls the public `Apply()`, which:

1. sets `enabled = false` on every entry in `forceDisable`;
2. activates `controllersRoot`, `handsRoot` or `gazeRoot` to match `mode`; in `HeadOnly` mode all three are deactivated;
3. enables only the input action manager behaviour that matches the mode.

The helpers skip empty references and objects already in the requested state.

#### Inspector fields that matter

| Field | Code default | Notes |
|---|---|---|
| `mode` | HeadOnly | `Controllers`, `Hands`, `Gaze` or `HeadOnly`. |
| `controllersRoot`, `handsRoot`, `gazeRoot` | none | Rig roots. |
| `forceDisable` | empty | Behaviours to disable at start. |
| `controllersInputActionManager`, `handsInputActionManager`, `gazeInputActionManager` | none | Optional. |

#### Public members used by other scripts

`Apply()`. No other script calls it.

#### Talks to (outgoing)

The listed GameObjects and Behaviours, by serialized reference (`SetActive`, `enabled`). It references no project script.

#### Used by (incoming)

None.

#### Files and persistent data

None.

#### Mode- and language-dependent behaviour

Not active in any build. In the four builds, `TMTVR_CentralExecutive` does the equivalent root switching. No language-dependent behaviour.

#### Caveats and legacy notes

Legacy helper. Not validated.

---

### ET_ActionsEditor

File: `Assets/Editor/ET_ActionsEditor.cs`

#### Purpose

An editor-only custom Inspector for `ET_Actions`. It adds an "Assign Selection Sibling" button that wires `Parent`, `audioSource` and the `Selection` sibling on one or more selected targets.

#### Where it is used

Not in any scene or prefab. It applies to every `ET_Actions` shown in the Inspector. The file sits in `Assets/Editor` and is wrapped in `#if UNITY_EDITOR`, so it is not compiled into the builds.

#### Lifecycle

`OnInspectorGUI()` draws the default Inspector, then a help box describing what the button assigns. When the button is pressed, for every selected `ET_Actions` it:

1. records an Undo step;
2. calls `AssignSelectionSiblingAndDependencies(logWarnings: true)`;
3. marks the object dirty;
4. records prefab-instance property modifications.

#### Inspector fields that matter

None of its own. It is marked `[CustomEditor(typeof(ET_Actions))]` and `[CanEditMultipleObjects]`.

#### Public members used by other scripts

None.

#### Talks to (outgoing)

`ET_Actions.AssignSelectionSiblingAndDependencies`, by direct call on the inspected objects; Unity `Undo`, `EditorUtility` and `PrefabUtility`.

#### Used by (incoming)

The Unity Editor, through the `CustomEditor` attribute.

#### Files and persistent data

None directly. Pressing the button changes serialized fields of the selected objects, which Unity saves with the scene or prefab.

#### Mode- and language-dependent behaviour

None.

#### Caveats and legacy notes

The bullet characters in the help box are not stored as valid UTF-8, so they may display as replacement characters. Cosmetic only.

---

## 4. Media, motion logging and build

These scripts play the dominant-hand prompt, hold the legacy instruction-video player, implement the passive motion logger, and build the APKs and the PC VR builds from the Unity Editor.

---

### OnEnablePlayAudio

File: `Assets/TMT-VR For Export/OnEnablePlayAudio.cs`

#### Purpose
Plays one spoken clip, English or Greek, a fixed delay after its GameObject becomes active. In all four scenes it voices the dominant-hand prompt on the dominance cube.

#### Where it is used
- **Host:** `ProbeDomiCube`, a root GameObject in every scene. It is saved inactive.
- The same object also carries a `BoxCollider`, a `MeshFilter`, a `MeshRenderer`, an `AudioSource`, `OnEnableHeadHeight` and `DominanceCubeSelector`.
- Not used in any prefab.

#### Lifecycle
- `OnEnable()`: gets the `AudioSource` on the same GameObject with `GetComponent<AudioSource>()` and starts the coroutine `DelayToPlay()`.
- `DelayToPlay()`: waits `delayTime` seconds (`WaitForSeconds`), then calls `AudioSource.PlayOneShot` with `engInstruct` when `language` is `English`, or with `grInstruct` when it is `Greek`.
- The coroutine starts every time the object is enabled, so re-enabling the object replays the clip.

#### Inspector fields that matter
| Field | Type | Saved value | Role |
|---|---|---|---|
| `language` | enum `Language { English, Greek }` | English (0) in the EN scenes; Greek (1) in the GR scenes | Selects which clip plays. |
| `engInstruct` | `AudioClip` | `Assets/TMT-VR For Export/DomHand_ENG.mp3` | English prompt. |
| `grInstruct` | `AudioClip` | `Assets/TMT-VR For Export/DomHand_GR.mp3` | Greek prompt. |
| `delayTime` | `float` | 1 | Seconds between activation and playback. |

The `AudioSource` on `ProbeDomiCube` has *Play On Awake* off and no default clip. Its output goes to an Audio Mixer group.

#### Public members used by other scripts
None. The script has no public methods and raises no events. Its public fields are set in the Inspector only.

#### Talks to (outgoing)
- `AudioSource` on the same GameObject: `GetComponent`, then `PlayOneShot`.
- `DomHand_ENG.mp3` / `DomHand_GR.mp3`: serialized `AudioClip` references.

#### Used by (incoming)
- `TMTVR_CentralExecutive` holds `ProbeDomiCube` in its serialized field `dominanceCube`. Its coroutine `DominanceCheck()` calls `dominanceCube.SetActive(true)`. That activation fires `OnEnable` here. No script calls this component directly.
- `DominanceCubeSelector` on the same object deactivates the cube when a controller tip touches it (`lockAfterSelect` is on in all four scenes). `TMTVR_CentralExecutive` then also calls `dominanceCube.SetActive(false)`.

#### Files and persistent data
None.

#### Mode- and language-dependent behaviour
- **Mode.** `TMTVR_CentralExecutive` runs `DominanceCheck()` only in `hand` and `controller` modes. The prompt therefore plays only in the Controller scenes. In the Head scenes the cube is never activated, so this component never runs *(inferred)*.
- **Language.** `language` is the only switch. The EN scenes set it to English and the GR scenes to Greek. In the Head scenes the setting has no audible effect, but it is set to match the scene language.

#### Caveats and legacy notes
- The script does not declare `[RequireComponent(typeof(AudioSource))]`. Without an `AudioSource` on the object, the call after the delay throws a `NullReferenceException`.
- Unity stops a MonoBehaviour's coroutines when its GameObject is deactivated. If the participant touches the cube within `delayTime` (1 s), the cube is deactivated and the prompt never plays *(inferred)*. A deactivated `AudioSource` also stops sounding, so a prompt that has started is cut off once the cube is selected *(inferred)*.
- `PlayOneShot` does not stop a clip that is already playing. Two activations within the clip's length would overlap.
- The audio clips are content of record.

---

### PlayVideoEngGR

File: `Assets/TMT-VR For Export/PlayVideoEngGR.cs`

**Status: present but inactive legacy path.** No live code path activates this component in any of the four scenes. It is documented so that readers can recognise it. It is not part of the instruction flow and has not been validated.

#### Purpose
After a delay on enable, assigns an English or a Greek instruction `VideoClip` to a referenced `VideoPlayer` and plays it.

#### Where it is used
- Not placed directly in any scene.
- **Prefab:** `Assets/TMT-VR For Export/TMT-VR_Greek.prefab`, on two GameObjects: `Instructions TMT-A` and `Instructions TMT-B`. Each object also carries a `VideoPlayer` and a `TMTVR_InstructionsManager`.
- The prefab is instanced once in every scene, renamed `TMT-VR`. Its root is the `TMT` field of `TMTVR_CentralExecutive`. Both objects are inactive in the prefab; the scenes also set `Instructions TMT-A` inactive through a redundant override. The scenes override no `PlayVideoEngGR` field.

#### Lifecycle
- `OnEnable()`: starts the coroutine `PlayVideoAfterDelay()`.
- `PlayVideoAfterDelay()`: waits `delayInSeconds`. If `isEnglish` is true, it sets `videoPlayer.clip = EnglishInstruction`. Otherwise, if `isGreek` is true, it sets `GreekInstruction`. It then calls `videoPlayer.Play()`.
- If both flags are false, it plays whatever clip the `VideoPlayer` already holds. If both are true, English wins.

#### Inspector fields that matter
| Field | Type | Saved value (both prefab instances) | Role |
|---|---|---|---|
| `videoPlayer` | `VideoPlayer` | The `VideoPlayer` on the same object | Player to drive. Not fetched automatically; a null value throws. |
| `EnglishInstruction` | `VideoClip` | None | English clip. |
| `GreekInstruction` | `VideoClip` | A clip reference that does not resolve in the project (see caveats) | Greek clip. |
| `isEnglish` / `isGreek` | `bool` | false / true | Two independent language flags, not an enum. |
| `delayInSeconds` | `float` | 1.5 | Delay before playback. |

The `VideoPlayer` components have *Play On Awake* off.

#### Public members used by other scripts
None. No public methods and no events.

#### Talks to (outgoing)
- `VideoPlayer`: serialized reference; sets `clip` and calls `Play()`.

#### Used by (incoming)
- None at run time. `TMTVR_TestManager` once activated these objects through `instructionsA_Manager.gameObject.SetActive(true)` and `instructionsB_Manager.gameObject.SetActive(true)`. That code and the two fields are now commented out in `TMTVR_TestManager.cs`; the comment states that the instruction videos were replaced by audio instructions. The prefab still stores the two old field values, but no C# field reads them, so they have no effect.
- Instructions are now delivered as audio by `TMTVR_TestManager` (`General_Instructions` / `General_Instructions_lang` and the tutorial clips, chosen by its `isEnglish` flag), together with the instruction overlays `InstructionsA`, `InstructionsB`, `InstructionsA_lang` and `InstructionsB_lang`. See the `TMTVR_TestManager` reference.

#### Files and persistent data
None.

#### Mode- and language-dependent behaviour
None in the four builds, because the component never runs *(inferred)*. Both serialized instances are Greek-only (`isGreek` true, no English clip). The English scenes do not change this component.

#### Caveats and legacy notes
- The two Greek `VideoClip` references do not resolve to any asset in `Assets/` or `Packages/`. The videos are missing from the project. The `EnglishInstruction/` and `GreekInstructions/` folders contain audio only.
- `TMTVR_InstructionsManager` sits on the same inactive objects. It waits for its `VideoPlayer` to start and stop, then sets `Completed`. It is equally unused.
- If someone re-enabled this path, the missing clips and the absent English clip would need to be supplied first.

---

### TMTVR_MotionLogCore

File: `Assets/TMT-VR For Export/MotionLog/TMTVR_MotionLogCore.cs`

#### Purpose
Plain C# bookkeeping for the passive motion log. It uses `System` only, with no `UnityEngine`, so it can be tested outside Unity. `TMTVR_MotionLogger` feeds it one call per rendered frame and one call per correct selection. It turns these into ray *episodes* and per-*link* intervals and kinematic sums.

It records and summarises events. It computes no adjusted or motor-corrected score, and its outputs are not a validated motor measure.

#### Where it is used
Not in any scene or prefab. The file contains no MonoBehaviour. `TMTVR_MotionLogger` creates one `TMTVR_MotionTracker` per run.

#### Types
- **`TMTVR_V3`** (struct): a double-precision 3-vector with `Length`, subtraction and `AngleDeg(a, b)`. `AngleDeg` returns the angle in degrees, or 0 when either vector has zero length.
- **`TMTVR_RayEpisode`**: one continuous stretch of rendered frames in which the ray hits one target. Fields: `Target` (1-based, in the run's order), `Role` when the episode began (`current`, `selected` or `future`; an episode already open on the next target at the moment of a selection is relabelled `current`), `StartMs`, `EndMs`, `Outcome` (default `PASS`) and the derived `DurationMs`.
- **`TMTVR_MotionLink`**: one link, from the selection of target k-1 to the selection of target k. Times: `PrevSelectionMs`, `SelectionMs`, `SelectionObservedMs`, `FirstEntryMs`, `FinalEntryMs`. Counts: `Entries`, `InterruptedDwells`, `ErrorsInLink`, `Frames`. Sums and peaks: `InterruptedMs`, `RayPathDeg`, `RayPeakSpeedDegS`, `HeadPathDeg`. Geometry: `StraightDeg`, `AngularRadiusDeg`, `DistanceM`. Also `Flags`. Derived values:
  - `LinkMs` = selection(k) minus selection(k-1);
  - `AcquisitionMs` = final entry minus selection(k-1);
  - `ConfirmationMs` = selection(k) minus final entry;
  - `PathRatio` = `RayPathDeg` divided by `StraightDeg`.
- **`TMTVR_LinkGeometry`** (struct): straight angle, angular radius and distance, supplied by the logger when a link begins.
- **`TMTVR_MotionTracker`**: the state machine described below.

#### Lifecycle (`TMTVR_MotionTracker`)
- `Begin(mistakesNow)`: sets the current target to 1 and stores the task's wrong-target count. The count is not reset between runs in every task version, so only rises are used.
- `Frame(tMs, hitTarget, rayDir, headFwd, mistakesNow)`, once per rendered frame:
  1. Stores the frame interval. While a link is open, adds the frame-to-frame rotation of the ray and of the head's forward direction, and counts the frame.
  2. Updates the link's peak ray rotation speed over a three-sample window.
  3. On a rise in the wrong-target count: adds the rise to `ErrorsInLink` whenever a link is open. It marks the open episode, or failing that the most recently closed one, as `ERROR`, but only if that episode's target is not the current target and is not already `SELECTED`. It always appends the rise to `Errors`, with target 0 when there is no open or recently closed episode.
  4. Closes the open episode when the ray leaves its target. Opens a new episode when the ray hits a target. An entry on the current target is counted toward the link.
- `Selection(number, tSelMs, tObsMs, geoNext)`: closes the link for target `number`.
  - The final entry is the latest episode on the link's target that started at or before `tObsMs` and ended no earlier than `tSelMs` minus `SelectionToleranceMs` (40 ms). That episode becomes `SELECTED`.
  - Flags: `FIRST_ENTRY_SELECTED` when the final entry was the first entry; `NO_FINAL_ENTRY` when none qualifies.
  - Every other ended episode on the link's target collected during this link is marked `INTERRUPTED` and counted in `InterruptedDwells` and `InterruptedMs`. Episodes on other targets keep their outcome.
  - The first target has no link; an open episode on it is simply marked `SELECTED`.
  - It then opens the next link with the geometry in `geoNext`. If the ray is already on the new current target, that entry is counted.
- `End(tMs)`: closes the open episode and drops the unfinished link, as the task drops it.

#### Public members used by other scripts
| Member | Use |
|---|---|
| `SelectionToleranceMs` (40.0) | Tolerance for matching the final entry to the selection. |
| `Episodes`, `Finished`, `Errors` | Read by `TMTVR_MotionLogger.EndRun()` to write the event file. |
| `Current`, `Selected` | `Current` is written to each pose row. |
| `Begin`, `Frame`, `Selection`, `End` | Called by `TMTVR_MotionLogger`. |
| `MedianFrameMs()`, `static Median(List<double>)` | Median frame interval for the run summary. |

#### Talks to / Used by
- **Talks to:** nothing. No Unity API, no file I/O, no PlayerPrefs, no events.
- **Used by:** `TMTVR_MotionLogger`, by direct instantiation (`new TMTVR_MotionTracker()`) and direct method calls.

#### Files and persistent data
None. All output is read by `TMTVR_MotionLogger`, which writes the files.

#### Mode- and language-dependent behaviour
None. The tracker receives a ray direction and does not know which device produced it.

#### Caveats and legacy notes
- **Two clocks.** Selection times come from the task's physics-step `Time.time`. Episode times come from rendered frames. `ConfirmationMs` therefore carries up to one physics step plus one frame of quantisation. The task's dwell, which runs on the pointer sphere's trigger overlap, need not begin on the frame the ray first hits the target.
- The intervals are operational definitions of recorded times. None of them isolates a single cognitive or motor process.
- Only the flags `NO_FINAL_ENTRY` and `FIRST_ENTRY_SELECTED` are ever written.

---

### TMTVR_MotionLogger

File: `Assets/TMT-VR For Export/MotionLog/TMTVR_MotionLogger.cs`

#### Purpose
An optional, passive MonoBehaviour. When switched on, it reads the run managers, the aim-pointer ray and the head camera every frame, feeds `TMTVR_MotionTracker`, and writes an event file and a pose file per session.

It shows nothing, selects nothing, and changes no task logic, timing, scoring, instruction, audio or participant view. It does not use the eye tracker. It records events and poses only. It computes no adjusted or motor-corrected score, and no validated motor measure may be claimed from its output.

#### Where it is used
- **Host:** root GameObject `Managers` in all four scenes. `Managers` also carries `TMTVR_CentralExecutive`, `TMTVR_ScoreManager` and `OpenXRAimPointer`.
- Not in any prefab. Not wired to any UnityEvent.
- The class has `[DefaultExecutionOrder(200)]`, so its `LateUpdate` runs after `OpenXRAimPointer.LateUpdate` (default order). The ray hit it reads is from the current frame *(inferred from Unity's execution-order rules)*.

#### Lifecycle
- **`Start()`**: if `motionLogEnabled` is false, it disables itself and nothing else runs. Otherwise it fills any empty reference with `FindFirstObjectByType<T>(FindObjectsInactive.Include)` for `TMTVR_TestManager`, `TMTVR_ScoreManager`, `TMTVR_CentralExecutive` and `OpenXRAimPointer`. `xrCamera` falls back to `OpenXRAimPointer.xrCamera`, then `Camera.main`. Inactive objects are included in the search. This matters because `TMTVR_TestManager` sits on the root of the `TMT-VR` prefab instance, not on `Managers`, and that object is inactive at start.
- **`LateUpdate()`**, every frame:
  1. Finds the active run: the first of `TMTVR_TestManager.tutorial_partA_Manager`, `partA_Manager`, `tutorial_partB_Manager` and `partB_Manager` that is active in the hierarchy and has targets.
  2. If the tracked manager has changed, its `Targets` list has been replaced, or its `TargetsSelected` has fallen, the run is closed. Remaining selections are read first, but only if the manager was not reset (`TargetsSelected` has not fallen and `Targets` is the same list). `EndRun()` is then always called. This catches the last selection of a tutorial whose manager is switched off in the same frame.
  3. Starts a run (`StartRun`) when a manager is active and no run is open.
  4. `ReadSelections()`: for each rise in `TMTVRManager.TargetsSelected`, calls `tracker.Selection(...)` with the task's `previousSelectionTime` as the selection time and the current frame time as the observed time.
  5. `RayAndHead()` recomputes the ray as `OpenXRAimPointer` does: from the controller tip when `Controller` is set, from the fingertip when `Hand` is set, otherwise from the head's forward direction. It reads the head pose from `xrCamera`.
  6. `HitTarget()` maps `OpenXRAimPointer.CurrentHitCollider` to a 1-based target number with `Transform.IsChildOf`, caching the result per collider.
  7. Calls `tracker.Frame(...)` with the run manager's `totalMistakes`. When `writePose` is on, it appends one pose row to memory.
- **`StartRun(manager)`**: creates a tracker and calls `Begin`. Sets the part (A or B) and the stage: `tutorial` when `targetPlacer.Tutorial` is true; otherwise `practice` when `TMTVR_TestManager.RealRunsA_Completed` (or `RealRunsB_Completed`) is 0, else `test`. Reads the configured dwell from the first target's `ET_Actions.confirmationTime`. In controller mode it finds the hand by matching `TipProbeId.trackedTip` to `OpenXRAimPointer.controllerTip`. It buffers a `run_start` event.
- **`EndRun()`**: calls `tracker.End`, buffers `link`, `error` and `episode` events and a `run_end` summary, writes one `Debug.Log` summary line, and calls `Flush()`.
- **`OnApplicationPause(true)`**: calls `Flush()`, which can write a partial run.
- **`OnDisable()`**: closes any open run (reading remaining selections first if the count has not fallen), or else flushes.

#### Inspector fields that matter
| Field | Default | Saved value | Role |
|---|---|---|---|
| `motionLogEnabled` | false | Head scenes: off. Controller scenes: on. | Master switch. Off means the component disables itself in `Start()` and has no effect. |
| `testManager` | none | empty | `TMTVR_TestManager`; found automatically. |
| `scoreManager` | none | empty | `TMTVR_ScoreManager`; source of the session ID. |
| `centralExecutive` | none | empty | `TMTVR_CentralExecutive`; source of the interaction mode. |
| `aimPointer` | none | empty | `OpenXRAimPointer`; source of the ray and hit. |
| `xrCamera` | none | empty | Head camera; falls back as described above. |
| `writePose` | true | on | Writes one pose row per rendered frame. |

#### Public members used by other scripts
None. Other scripts do not call this component.

#### Talks to (outgoing)
All reads; it writes no field of another script.
- `TMTVR_TestManager` (found by `FindFirstObjectByType`): the four run-manager references; `RealRunsA_Completed`, `RealRunsB_Completed`.
- `TMTVRManager` (instance reached through the `TMTVR_TestManager` fields): `Targets`, `TargetsSelected`, `previousSelectionTime`, `selectionTimes`, `totalMistakes`, `targetPlacer`.
- `TargetPlacer` (through `TMTVRManager.targetPlacer`): `Tutorial`.
- `ET_Actions` (`GetComponentInChildren` on the first target): `confirmationTime`.
- `OpenXRAimPointer` (found by `FindFirstObjectByType`): `Controller`, `Hand`, `Head`, `controllerTip`, `fingertip`, `controllerLocalForwardAxis`, `fingertipLocalForwardAxis`, `xrCamera`, `HasValidHit`, `CurrentHitCollider`, `CurrentHitPoint`.
- `TipProbeId` (`FindObjectsByType`, inactive included): `trackedTip`, `side`.
- `TMTVR_ScoreManager` (found by `FindFirstObjectByType`): `SessionID`.
- `TMTVR_CentralExecutive` (found by `FindFirstObjectByType`): `intMode`.
- `Camera` (`xrCamera`): head position and rotation, and the parent's position as the rig offset.
- `TMTVR_MotionTracker`: created with `new` and driven by direct calls.

#### Used by (incoming)
None. The component runs from Unity's lifecycle only.

#### Files and persistent data
Directory: `Application.persistentDataPath/TMTVR_Session_Logs/`, created if needed. File stem: `motionID<SessionID>`. `SessionID` is read from `TMTVR_ScoreManager`, or -1 when no score manager is found.

| File | Written | Content |
|---|---|---|
| `motionID<SessionID>_events.jsonl` | Appended, UTF-8 without BOM, one JSON object per line | Event types `run_start`, `link`, `error`, `episode`, `run_end`. |
| `motionID<SessionID>_pose.csv` | Appended; header row written when the file is created | One row per rendered frame while a run is open. |

**Event lines.**
- `run_start`: run number, part, stage, session ID, interaction mode, ray source, controller hand, configured dwell (seconds), `Application.version`, rig offset, start time, UTC time, and the layout of every target (number, name, centre and half-size from the collider or renderer bounds).
- `link`: one per correct selection from the second target on, with the times and measures listed under `TMTVR_MotionLink`, plus `flags`.
- `error`: frame time, target attributed, and the size of the rise in the wrong-target count.
- `episode`: target, role, start, end, duration and outcome.
- `run_end`: selections, links and links expected, the task's own link-time count, `max_link_diff_ms` (largest difference between the logged `link_ms` and the task's `selectionTimes`), wrong-target total, episode count, frames, frames per second, median frame interval, pose rows, end time. `max_link_diff_ms` is a consistency check between the log and the task, not an outcome measure.

**Pose columns.** `run, t_ms, current_target, hit_target, head_x, head_y, head_z, head_qx, head_qy, head_qz, head_qw, ray_ox, ray_oy, ray_oz, ray_dx, ray_dy, ray_dz, hit_valid, hit_x, hit_y, hit_z, wrong_target_total`. Coordinates are Unity world space: metres, left-handed axes, y up.

**When files are written.** Records are held in memory and written when a run ends, on `OnDisable`, and when the application is paused. I/O errors are caught and reported with `Debug.LogWarning`. `Flush()` clears each buffer only after its write succeeds, so unwritten records stay in memory and are retried at the next flush (next run end, pause or disable). They are lost only if no later flush succeeds before the app closes.

**Analysis rule.** Runs without `run_end` must be excluded from analysis. A pause flush or an interrupted session can leave a `run_start`, pose rows or partial events without `run_end`. A `run_end` line does not by itself show that a run was complete: `OnDisable()` calls `EndRun()` for any open run, so a run cut off part-way (for example when the app quits) also gets one. Check also that `selections` equals the run's `targets` and that `links` equals `links_expected`.

**Session ID.** The logger reads no PlayerPrefs itself. `TMTVR_ScoreManager` reads the PlayerPrefs key `uniqueID`, increments it at each launch, and then raises it until no score file with that ID exists in `TMTVR_Participants_Data`. The motion files reuse that ID. They are not checked for collisions themselves; if an ID were ever reused, new records would be appended to the existing motion files.

#### Mode- and language-dependent behaviour
- **Controller scenes:** logging is on. `mode` is `controller` and `ray_source` is `controller`. The controller hand is named from the `TipProbeId` chosen at the dominance check.
- **Head scenes:** logging is off, so the component has no effect. If it were switched on, `ray_source` would be `head`.
- **Present but inactive paths.** The fingertip (`hand`) ray branch is not used by these four builds. In `eye` mode the logger would fall back to the head-forward direction, not the gaze ray; eye mode is not used by these builds. Neither path has been validated.
- **Language:** none. The logger has no language-dependent code; the same settings apply in the English and Greek scenes of one mode.

#### Caveats and legacy notes
- Frame rate with the log on, file sizes and timing against an external clock have not been verified on the VIVE Focus Vision.
- Observations are buffered during a run and written at run end, but `OnApplicationPause(true)` can still flush mid-run. `EndRun()` writes between runs.
- `dwell_s` is the configured `ET_Actions.confirmationTime` read at run start; it is not a measured value.

---

### TMTVRReleaseBuild

File: `Assets/Editor/TMTVRReleaseBuild.cs`

#### Purpose
An Editor-only, batch-mode build entry point. It builds one Android APK per call, after a set of guard checks. It sets the application ID, product name and version label for that build, writes a JSON build receipt with the APK's SHA-256, and restores the previous PlayerSettings. It does not change the scenes, and it does not install or run the APK.

#### Where it is used
- Not in any scene or prefab. It is a static class compiled into the Editor assembly because it sits under `Assets/Editor/`.
- Invocation, one app per run (see `BUILDING.md`): `Unity -batchmode -quit -projectPath <project> -buildTarget Android -executeMethod TMTVRReleaseBuild.BuildHeadEN -tmtvrOutput <project>/Builds/TMTVR_Head_EN.apk`. No calling script is included in the project.

#### Entry points
| Method | Variant | Scene | Package ID | Product name | Version |
|---|---|---|---|---|---|
| `BuildHeadEN()` | `HEAD, EN` | `Assets/TMTVR_Head_EN.unity` | `com.DefaultCompany.VRTemplate.head.en` | `TMT-VR Head EN` | `1.1.0 (HEAD, EN)` |
| `BuildHeadGR()` | `HEAD, GR` | `Assets/TMTVR_Head_GR.unity` | `com.DefaultCompany.VRTemplate.head.gr` | `TMT-VR Head GR` | `1.1.0 (HEAD, GR)` |
| `BuildControllerEN()` | `CONTROLLER, EN` | `Assets/TMTVR_Controller_EN.unity` | `com.DefaultCompany.VRTemplate.controller.en` | `TMT-VR Controller EN` | `1.1.0 (CONTROLLER, EN)` |
| `BuildControllerGR()` | `CONTROLLER, GR` | `Assets/TMTVR_Controller_GR.unity` | `com.DefaultCompany.VRTemplate.controller.gr` | `TMT-VR Controller GR` | `1.1.0 (CONTROLLER, GR)` |

Each method passes its scene path and identity as string constants to the private `Build` method. Distinct application IDs let the four APKs be installed side by side; APKs sharing an ID would replace each other on the headset *(inferred)*. See [VARIANTS.md](VARIANTS.md).

#### Lifecycle (`Build`)
1. **Guards.** Any failed guard throws `GuardFailure`, and the build stops. The build requires:
   - batch mode, and Unity version exactly `6000.2.10f1`;
   - a project root, taken from the project's own folder (the parent of `Application.dataPath`), that contains `ProjectSettings/ProjectVersion.txt` and has no reparse points (symbolic links or junctions) in its path;
   - a package ID that is one of the four release IDs and is not the template ID `com.DefaultCompany.VRTemplate`;
   - settings already in place: active target Android, IL2CPP, ARM64 only, development build off, Release build type, App Bundle off, Android project export off. The script checks these and never changes them;
   - an existing scene file, with no reparse points in its path;
   - exactly one `-tmtvrOutput` argument: an absolute path with no quotes, line breaks or alternate data streams, under `<project>/Builds/`, ending in `.apk`, in an existing directory, with no reparse points. Neither the APK nor its receipt may already exist; the script never overwrites.
2. **Identity.** Captures the current application identifier, `productName` and `bundleVersion`, sets the variant's values, and checks that they persisted.
3. **Build.** Calls `BuildPipeline.BuildPlayer` with the one scene and `BuildOptions.CompressWithLz4`, then collects every error message in the build report. One message is accepted: "Failed to set the cursor because the specified texture ('GERIATRIC_LOGO_Final') was not CPU accessible." The build passes only if Unity reports `Succeeded`, every reported error is that message, and the APK exists and is not empty. The script then records the APK's size and SHA-256.
4. **Restore** (`finally`). If identities were captured, restores and verifies them. A failed restore marks the build failed.
5. **Report.** Writes the receipt (if its path is known), logs one console line, and exits.

#### Inspector fields that matter
None. Constants in the file: `UnityVersion` (`6000.2.10f1`), `RecordPackage` (`com.DefaultCompany.VRTemplate`, the template ID that the guard rejects) and the accepted cursor message. `ProjectRoot` is a property derived from `Application.dataPath`.

#### Public members used by other scripts
`BuildHeadEN()`, `BuildHeadGR()`, `BuildControllerEN()` and `BuildControllerGR()`, called from the command line through `-executeMethod`. No runtime script calls them.

#### Talks to (outgoing)
- `PlayerSettings` and `EditorUserBuildSettings` (Unity Editor API): reads the build settings; temporarily writes the application identifier, product name and bundle version.
- `BuildPipeline.BuildPlayer`: builds the named scene.
- Command-line arguments (`Environment.GetCommandLineArgs`): `-tmtvrOutput`.
- File system: the APK, the receipt, the scene file check.

#### Used by (incoming)
A Unity batch-mode command line. Nothing in the project calls it.

#### Files and persistent data
- **APK** at the `-tmtvrOutput` path.
- **Receipt** `<apk-stem>.build.json` beside it (`Path.ChangeExtension`, so `X.apk` gives `X.build.json`). It is opened with `FileMode.CreateNew`. It holds the serialized `Receipt`: variant, Unity version, project path, scene, output, package ID, product name, version, start and finish UTC, result, failure reason, error and warning counts, the number of accepted cursor errors and of other errors, the error messages, artifact bytes, artifact SHA-256, build seconds, and whether identities were captured and restored.
- The receipt is written only after the output argument has been validated. A failure before that point (batch mode, Unity version, project root, package, Android settings or scene) leaves only the console line.
- **Console line** `TMTVR_RELEASE_BUILD <json>` in every case.
- **Exit code** 2 on any failure (`EditorApplication.Exit(2)`). On success, control returns to the command line's `-quit`.
- For exceptions other than guard failures, the receipt records only the exception type name as the failure reason.
- PlayerSettings are changed only for the duration of the build and then restored. The script reads no PlayerPrefs.

#### Mode- and language-dependent behaviour
- **Mode and language:** one entry point per app. The identity it sets names the mode and the language, but the interaction mode comes from the scene's serialized `intMode` and the language from the scene's language settings, not from this script.

#### Caveats and legacy notes
- The receipt records the absolute project path of the build machine. Check a receipt before sharing it.
- The package IDs still use the Unity template prefix `com.DefaultCompany.VRTemplate`.
- The script does not set, check or record the language, the interaction mode or the motion-log switch of the scene it builds.

---

### TMTVRReleaseBuildPCVR

File: `Assets/Editor/TMTVRReleaseBuildPCVR.cs`

#### Purpose
The Windows 64-bit counterpart of [TMTVRReleaseBuild](#tmtvrreleasebuild). It builds one PC VR player per call, with the same guards, identity handling, accepted cursor message, receipt and restore logic. This section lists only the differences.

#### Differences from TMTVRReleaseBuild
- **Target.** Requires Windows 64-bit (`StandaloneWindows64`) as the active build target and IL2CPP as the Windows scripting backend, and builds for `BuildTarget.StandaloneWindows64`. The Android-only checks (ARM64, Release build type, App Bundle, Android project export) are not made; the development-build check is.
- **Identity.** Sets the application identifier for the Standalone target. Application IDs and product names are those of the APKs; the version labels add the platform, for example `1.1.0 (HEAD, EN, PCVR)` (see [VARIANTS.md](VARIANTS.md), section 4).
- **Output.** `-tmtvrOutput` must be a new `.exe` path under `<project>/Builds/`, in a directory that already exists. Unity writes the player's data folder and runtime files into the same directory. The SHA-256 in the receipt covers the `.exe` only.
- **Receipt.** `<exe-stem>.build.json`, in the same directory as the `.exe`, with an extra `buildTarget` field (`StandaloneWindows64`). It records the absolute project path, so remove it before sharing the build folder.
- **Invocation:** `Unity -batchmode -quit -projectPath <project> -buildTarget Win64 -executeMethod TMTVRReleaseBuildPCVR.BuildHeadEN -tmtvrOutput <project>/Builds/TMTVR_Head_EN_PCVR/TMTVR_Head_EN_PCVR.exe`, and likewise `BuildHeadGR`, `BuildControllerEN` and `BuildControllerGR`.

#### Public members used by other scripts
`BuildHeadEN()`, `BuildHeadGR()`, `BuildControllerEN()` and `BuildControllerGR()`, called from the command line through `-executeMethod`. No runtime script calls them.

#### Caveats and legacy notes
- As for `TMTVRReleaseBuild`. The script does not set, check or record the language, the interaction mode or the motion-log switch of the scene it builds.
