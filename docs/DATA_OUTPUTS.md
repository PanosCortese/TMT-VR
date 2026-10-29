# TMT-VR data outputs

This document describes the files that the TMT-VR apps write on the headset (APKs) or on the PC (PC VR builds): what they contain, where they are stored and when they are written. All formats are described from the code that writes them: `TMTVR_ScoreManager.cs` for the score file and `MotionLog/TMTVR_MotionLogger.cs` with `MotionLog/TMTVR_MotionLogCore.cs` for the motion logs. Column names are given exactly as the code writes them.

Related documents: [ARCHITECTURE.md](ARCHITECTURE.md), [SCRIPTS.md](SCRIPTS.md), [VARIANTS.md](VARIANTS.md). Terms such as run, scored board, link, dwell and episode are defined in the [glossary](ARCHITECTURE.md#glossary).

Statements marked *(inferred)* come from reading the code. They were not observed at runtime.

## Privacy

The repository contains no participant data. It holds no score files, motion logs or other session records. The apps write data only on the device where they run: the headset for the APKs, the PC for the PC VR builds.

The apps collect no name or other direct identifier. The score file has a `ParticipantID` column, but the app fills it with the fixed text `PLACE HERE PARTICIPANT'S ID`. Sessions are linked to participants outside the app, through `SessionID` and the build used. The files do include device information (`SystemInfo.deviceModel`, `SystemInfo.deviceType`) and UTC timestamps. Handle the files under the data-protection terms of your study.

## Summary

| Output | Written by | Builds | Location | When |
|---|---|---|---|---|
| Score file `scoresID<SessionID>.csv` | `TMTVR_ScoreManager` | All four | `Application.persistentDataPath/TMTVR_Participants_Data/` | Header at start-up; one row at the end of the session |
| Session counter, PlayerPrefs key `uniqueID` | `TMTVR_ScoreManager` | All four | PlayerPrefs | At start-up |
| `motionID<SessionID>_events.jsonl` | `TMTVR_MotionLogger` | Controller builds only | `Application.persistentDataPath/TMTVR_Session_Logs/` | At the end of each run, on disable and on application pause |
| `motionID<SessionID>_pose.csv` | `TMTVR_MotionLogger` | Controller builds only | `Application.persistentDataPath/TMTVR_Session_Logs/` | As for the event file |

On Android, `Application.persistentDataPath` is the app's own files folder, usually `/storage/emulated/0/Android/data/<application ID>/files` *(inferred from Unity's platform behaviour)*. Each APK has its own application ID, so each APK has its own data folder and its own session counter.

On Windows (PC VR builds), `Application.persistentDataPath` is `%USERPROFILE%\AppData\LocalLow\<company name>\<product name>`, and PlayerPrefs, including the session counter, are stored in the registry under `HKEY_CURRENT_USER\Software\<company name>\<product name>` *(both inferred from Unity's platform behaviour)*. The company name is the one in Player Settings (`Panagiotis Kourtesis `, stored with a trailing space); the product name is that of the build, for example `TMT-VR Head EN`. Each PC VR build therefore has its own data folder and session counter on each PC, separate from those of the APKs.

The apps also write diagnostic messages (`Debug.Log`): the APKs to the Android device log, the PC VR builds to Unity's player log on the PC. These are not data files.

---

## 1. Session ID

`TMTVR_ScoreManager.Start` assigns the session ID once per app launch:

1. It reads the PlayerPrefs integer `uniqueID`, adds 1 (or uses 1 if the key is missing), saves it and uses it as `SessionID`.
2. While a score file named `scoresID<SessionID>.csv` already exists, it adds 1 again and saves the new value.

Each launch therefore gets a new score file. The motion logger reuses the same `SessionID` for its file names. It does not check its own files for name clashes; if an ID were ever reused, new records would be appended to the existing motion files. If no score manager is found, the motion files use `-1`.

Session IDs are counted per app on each device *(inferred)*. The same number can therefore occur in more than one build.

---

## 2. Score file

- **Path:** `Application.persistentDataPath/TMTVR_Participants_Data/scoresID<SessionID>.csv`. The folder is created if missing.
- **Header:** written at start-up, before the task begins, with `File.WriteAllText(..., Encoding.UTF8)`. The session does not start until this succeeds. .NET's `Encoding.UTF8` writes a byte-order mark, so the first header name, `TimestampUTC`, is preceded by one *(inferred)*. CSV readers should allow for it.
- **Data row:** one row, appended after Part B with `File.AppendAllText(..., Encoding.UTF8)`.
- **Format:** comma-separated. Values that contain a comma, quote or line break are quoted, with quotes doubled. Numbers use the invariant culture (decimal point). Not-a-number and infinite values are written as `NA`. Floating-point values are written with the default general format of `System.Single` under the invariant culture (`f.ToString(CultureInfo.InvariantCulture)`), with no explicit precision. Under Unity's .NET class library this gives up to about seven significant digits, not a round-trip representation *(inferred; not checked on the device)*.
- **Column order:** the order of the `variables` list on `TMTVR_ScoreManager` in the scene. A key that is in the schema but not set is written as an empty field.

### 2.1 What the row measures

The row is built from the **second run** on each scored board (`TMT-VR_Part_A` and `TMT-VR_Part_B`). The tutorial runs and the first run on each scored board are not written. Each scored run has 25 targets and therefore 24 links.

Three values are read from each part's `TMTVRManager`:

| Source value | Meaning |
|---|---|
| `totalSelectionTime` | Seconds from the first correct selection (target 1) to the 25th. The time taken to find target 1 is not included. Unity game time (`Time.time`). |
| `totalMistakes` | Number of errors. One error is counted each time the dwell completes on a target that is not the current target. Each entry into a wrong target counts at most once. Dwelling on an already selected target is not an error. |
| `AverageAccuracy` | Spatial value in Unity world units (metres). For each of the 24 links, the mean distance between the pointer centre and the target centre during the confirming dwell; then the mean over the 24 links. Lower means closer. A raw descriptive value, not a proportion. |

### 2.2 Columns

The scene schema has 32 columns, in this order. In the formulas, *links* is 24 for one part and 48 for the total.

| # | Column | Definition in code |
|---|---|---|
| 1 | `TimestampUTC` | UTC time of the save, ISO 8601 round-trip format |
| 2 | `DeviceModel` | `SystemInfo.deviceModel` |
| 3 | `DeviceType` | `SystemInfo.deviceType` |
| 4 | `AppVersion` | `Application.version`, the version label set at build time |
| 5 | `SessionID` | Session ID (section 1) |
| 6 | `ParticipantID` | The fixed text `PLACE HERE PARTICIPANT'S ID` |
| 7 | `Task A - Task Time` | Part A `totalSelectionTime` (s) |
| 8 | `Task A - Errors` | Part A `totalMistakes` |
| 9 | `Task A - Error Rate` | errors / 24 |
| 10 | `Task A - Spatial Accuracy` | Part A `AverageAccuracy` (m) |
| 11 | `Task A - Accuracy` | 24 / (24 + errors); 1 when there are no errors |
| 12 | `Task A - Adjusted Time` | Task Time / Accuracy |
| 13–18 | `Task B - Task Time`, `Task B - Errors`, `Task B - Error Rate`, `Task B - Spatial Accuracy`, `Task B - Accuracy`, `Task B - Adjusted Time` | The same six values for Part B |
| 19 | `Total Time` | A time + B time |
| 20 | `Total Errors` | A errors + B errors |
| 21 | `Total Error Rate` | total errors / 48 |
| 22 | `Total Spatial Accuracy` | (A spatial accuracy + B spatial accuracy) / 2 |
| 23 | `Total Accuracy` | 48 / (48 + total errors) |
| 24 | `Total Adjusted Time` | Total Time / Total Accuracy |
| 25 | `Difference Time` | B time − A time |
| 26 | `Difference Errors` | B errors − A errors |
| 27 | `Difference Error Rate` | B error rate − A error rate |
| 28 | `Difference Spatial Accuracy` | B spatial accuracy − A spatial accuracy |
| 29 | `Difference Accuracy` | B accuracy − A accuracy |
| 30 | `Difference Adjusted Time` | B adjusted time − A adjusted time |
| 31 | `Ratio Time` | B time / A time; `NA` if A time is 0 |
| 32 | `Norm. Difference Time` | (B time − A time) / A time; `NA` if A time is 0 |

The `Adjusted Time` columns are an error-adjusted time in the inverse-efficiency style: time divided by the attempt accuracy defined in column 11. They are computed only from the task's time and error count. They are not a motor correction and use no motion data.

The derived columns are arithmetic definitions in code. This document makes no claim about their validity.

The score file has no column for the interaction mode, the language or the platform; only the `AppVersion` label names them. Record the build used with every session (see [VARIANTS.md](VARIANTS.md)).

### 2.3 Faults

- If any required reference is missing at save time, no row is written and an error is logged.
- If fewer than two runs were completed on a scored board, an error is logged **but the row is still written**, from whatever values the managers hold.
- If the session stops before the task (for example, a missing pointer reference), the file keeps only its header *(inferred)*.

---

## 3. Motion logs (Controller builds only)

`TMTVR_MotionLogger` is a passive recorder. It is switched on in the Controller builds (`motionLogEnabled` on) and off in the Head builds, where it writes nothing. It changes no task logic, timing, scoring, instruction, audio or display.

> **Analysis rules.**
> - The motion log records events and poses only. It computes no adjusted or motor-corrected score, and no validated motor measure may be claimed from it.
> - **Runs without `run_end` must be excluded from analysis.**
> - A `run_end` record does not by itself show that a run was complete. Check also that `selections` equals the run's `targets` and that `links` equals `links_expected`.

### 3.1 Files and timing

- **Folder:** `Application.persistentDataPath/TMTVR_Session_Logs/`, created if missing.
- **Event file:** `motionID<SessionID>_events.jsonl`. JSON Lines: one JSON object per line, UTF-8 without a byte-order mark, appended.
- **Pose file:** `motionID<SessionID>_pose.csv`. UTF-8 without a byte-order mark. The header row is written when the file is created; rows are appended.
- **When written.** Records are kept in memory during a run and written (1) when the run ends, (2) when the logger is disabled, and (3) when the application is paused. A pause can therefore write part of a run. If a write fails, a warning is logged; the unwritten records stay in memory and are retried at the next write. They are lost only if no later write succeeds before the app closes. The event and pose files are written separately, so one can succeed while the other is kept for a retry.
- **Runs.** A run is one board activation. A complete session has six runs, numbered in order *(inferred from the task order)*: Part A tutorial, Part A first run, Part A second run, Part B tutorial, Part B first run, Part B second run.
- **Clock.** All times come from Unity game time (`Time.time`), which starts at app launch. Selection times come from the task's physics-step timing; episode and pose times come from rendered frames.
- **Missing values.** In the event file, a value that cannot be computed is written as `null`. In the pose file, the hit-point columns are empty when there is no valid hit.

### 3.2 Event records

Each line has an `event` field. The record types are written in this order for each run: `run_start`, then the `link`, `error` and `episode` records, then `run_end`. All records for a run are written together when the run ends.

**`run_start`** (one per run)

| Field | Meaning |
|---|---|
| `run` | Run number within the session, from 1 |
| `part` | `A` or `B` |
| `stage` | `tutorial` (tutorial board), `practice` (first run on the scored board) or `test` (second run, the one in the score file) |
| `session_id` | `SessionID` from the score manager, or -1 |
| `mode` | Interaction mode from `TMTVR_CentralExecutive.intMode` (`controller` in these builds) |
| `ray_source` | `controller`, `hand`, `head` or `other`, from the pointer flags |
| `controller_hand` | Side and tracked point of the controller chosen at the dominance check, for example `Left (TriggerLeft)`; `?` if unknown |
| `dwell_s` | Configured dwell, `ET_Actions.confirmationTime` of the first target (s). A setting, not a measurement. |
| `app_version` | `Application.version` |
| `rig_offset_m` | Position of the camera's parent (the rig offset), `[x, y, z]` in metres |
| `t_start_s` | Run start time (s) |
| `utc` | UTC time at run start, ISO 8601 |
| `targets` | Number of targets on the board |
| `layout` | One entry per target, in selection order: `n` (1-based number), `name`, `x`, `y`, `z` (centre, m) and `half_size_m` (half of the largest extent of the target's collider, or of its renderer if it has no collider) |

**`link`** (one per correct selection from the second target on; a link runs from the selection of target k−1 to the selection of target k)

| Field | Meaning |
|---|---|
| `run`, `part`, `stage` | As in `run_start` |
| `target`, `target_name` | Number and name of target k |
| `prev_selection_s` | Selection time of target k−1 (s) |
| `selection_s` | Selection time of target k, from the task (s) |
| `selection_observed_s` | Frame time at which the logger saw the selection (s) |
| `link_ms` | `selection_s` − `prev_selection_s` (ms) |
| `acquisition_ms` | Final entry of the ray on target k − selection of target k−1 (ms) |
| `confirmation_ms` | Selection of target k − final entry (ms) |
| `first_entry_s`, `final_entry_s` | Times of the first and the final ray entry on target k within the link (s) |
| `entries` | Number of ray entries on target k within the link |
| `interrupted_dwells`, `interrupted_ms` | Number and total duration of ray stays on target k that ended without a selection |
| `errors_in_link` | Rise in the task's wrong-target count during the link |
| `ray_path_deg` | Summed frame-to-frame rotation of the ray direction (degrees) |
| `ray_peak_speed_deg_s` | Peak ray rotation speed over a three-sample window (degrees per second) |
| `head_path_deg` | Summed frame-to-frame rotation of the head's forward direction (degrees) |
| `straight_deg` | Angle at the ray origin between the centres of targets k−1 and k, at the start of the link (degrees) |
| `path_ratio` | `ray_path_deg` / `straight_deg` |
| `angular_radius_deg` | Angular radius of target k seen from the ray origin (degrees) |
| `distance_m` | Distance from the ray origin to the centre of target k (m) |
| `frames` | Rendered frames during the link |
| `flags` | `;`-separated flags: `FIRST_ENTRY_SELECTED` (the final entry was the first entry) or `NO_FINAL_ENTRY` (no ray entry matched the selection) |

The final entry is the latest ray stay on target k that began at or before the observed selection and ended no earlier than 40 ms before the task's selection time.

**`error`** (one per rise in the task's wrong-target count)

| Field | Meaning |
|---|---|
| `run` | Run number |
| `t_s` | Frame time of the rise (s) |
| `target` | Target of the open ray episode, or else of the most recently closed one; 0 if none. This attributes the error to a ray stay; the task itself does not report which target caused it. |
| `count` | Size of the rise |

**`episode`** (one per continuous stretch of frames in which the ray hits one target)

| Field | Meaning |
|---|---|
| `run` | Run number |
| `target` | Target number |
| `role` | Role of the target when the episode began: `current`, `selected` (already selected) or `future`. One exception: if the ray is already on the next target when the current one is selected, that open episode is relabelled `current`. |
| `start_s`, `end_s`, `duration_ms` | Start, end and duration |
| `outcome` | `SELECTED` (the stay that produced the selection), `INTERRUPTED` (a stay on the target that ended without selection), `ERROR` (a stay on a target other than the current one to which an error was attributed) or `PASS` (any other stay) |

**`run_end`** (one per run that the logger closes)

A run is closed, and `run_end` written, when the board is switched off after completion (in the same frame on a tutorial board; on a scored board about 1 s plus the completion sound after the last selection, so `t_end_s`, `frames`, `frames_per_s` and `pose_rows` include that interval), when the tracked board is reset or replaced, or when the logger is disabled, for example when the app quits. A run cut off part-way therefore also gets a `run_end`, with `selections` below `targets` and `links` below `links_expected`. Treat a run as complete only if `run_end` is present, `selections` equals `targets` and `links` equals `links_expected`.

| Field | Meaning |
|---|---|
| `run`, `part`, `stage` | As in `run_start` |
| `selections` | Correct selections read in the run |
| `links`, `links_expected` | Links logged, and targets − 1 |
| `task_link_times` | Number of link times held by the task |
| `task_links_compared` | Number of links compared with the task's own times |
| `max_link_diff_ms` | Largest difference between `link_ms` and the task's own link time. A consistency check between the log and the task, not an outcome measure. |
| `wrong_target_total` | The task's wrong-target count at run end |
| `episodes`, `frames`, `frames_per_s`, `median_frame_ms` | Episode count, rendered frames, mean frame rate and median frame interval |
| `pose_rows` | Pose rows written for the run |
| `t_end_s` | Run end time (s) |

### 3.3 Pose file columns

One row per rendered frame while a run is open. Coordinates are Unity world space: metres, left-handed axes, y up.

| Column | Meaning |
|---|---|
| `run` | Run number (matches the event file) |
| `t_ms` | Frame time (ms) |
| `current_target` | Number of the target to be selected next |
| `hit_target` | Number of the target the ray hits; 0 if none |
| `head_x`, `head_y`, `head_z` | Head (camera) position |
| `head_qx`, `head_qy`, `head_qz`, `head_qw` | Head rotation as a quaternion |
| `ray_ox`, `ray_oy`, `ray_oz` | Ray origin |
| `ray_dx`, `ray_dy`, `ray_dz` | Ray direction (unit vector) |
| `hit_valid` | 1 if the pointer has a valid hit on a tagged target collider, else 0 |
| `hit_x`, `hit_y`, `hit_z` | Hit point; empty when `hit_valid` is 0 |
| `wrong_target_total` | The task's wrong-target count at this frame |

The ray origin and direction are recomputed by the logger in the same way as `OpenXRAimPointer`. The logger does not change the pointer.

### 3.4 Status of the motion measures

The link intervals and kinematic sums are operational definitions of recorded times and movements. None of them isolates a single cognitive or motor process, and none has been validated as a motor measure. Frame rate with the log on, file sizes and timing against an external clock have not been verified on the VIVE Focus Vision.

---

## 4. Build receipts (Editor only)

When an APK is built with `Assets/Editor/TMTVRReleaseBuild.cs`, the script writes a JSON receipt beside the APK (`<name>.build.json` for `<name>.apk`). It records the variant, Unity version, project path (the absolute path of the project folder on the build machine), scene, output path, package ID, product name, version label, start and end times, result, error and warning counts with the error messages, APK size and APK SHA-256. It also prints one console line beginning `TMTVR_RELEASE_BUILD`. This is a build record. It contains no participant data, but it does hold the build machine's project path and output path, so check it before sharing.

`Assets/Editor/TMTVRReleaseBuildPCVR.cs` writes the same receipt for a PC VR build, with an extra `buildTarget` field, next to the `.exe` inside the build folder (`<name>.build.json` for `<name>.exe`). Its SHA-256 covers the `.exe` only. Remove the receipt before sharing the build folder.
