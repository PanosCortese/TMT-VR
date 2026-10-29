// Optionally records controller-ray and head poses together with task selection events, buffering observations during a run and writing them at run end without changing interaction, instructions or scoring.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

[DefaultExecutionOrder(200)]
public class TMTVR_MotionLogger : MonoBehaviour
{
    [Header("Switch (off = no effect at all)")]
    public bool motionLogEnabled = false;

    [Header("Scene references (found automatically if empty, inactive objects included)")]
    public TMTVR_TestManager testManager;
    public TMTVR_ScoreManager scoreManager;
    public TMTVR_CentralExecutive centralExecutive;
    public OpenXRAimPointer aimPointer;
    public Camera xrCamera;

    [Header("Records")]
    public bool writePose = true;

    // ---- runtime ----
    private TMTVR_MotionTracker tracker;
    private TMTVRManager runMgr;
    private List<GameObject> runTargets;
    private readonly List<string> runNames = new List<string>();   // target names at run start (the task may clear its list)
    private int runSelected, runNumber, poseRows, runFrames;
    private string runPart = "", runStage = "";
    private double runStartS, dwellS = double.NaN;
    private readonly Dictionary<Collider, int> colliderTarget = new Dictionary<Collider, int>();
    private readonly StringBuilder eventBuf = new StringBuilder(), poseBuf = new StringBuilder();

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static string F(double v, string fmt = "0.###") { return double.IsNaN(v) || double.IsInfinity(v) ? "null" : v.ToString(fmt, Inv); }
    private static string C(double v, string fmt = "0.####") { return double.IsNaN(v) || double.IsInfinity(v) ? "" : v.ToString(fmt, Inv); }
    private static string Q(string s) { return "\"" + (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\""; }
    private static TMTVR_V3 V(Vector3 v) { return new TMTVR_V3(v.x, v.y, v.z); }

    private void Start()
    {
        if (!motionLogEnabled) { enabled = false; return; }            // OFF: nothing below runs
        if (testManager == null) testManager = FindFirstObjectByType<TMTVR_TestManager>(FindObjectsInactive.Include);
        if (scoreManager == null) scoreManager = FindFirstObjectByType<TMTVR_ScoreManager>(FindObjectsInactive.Include);
        if (centralExecutive == null) centralExecutive = FindFirstObjectByType<TMTVR_CentralExecutive>(FindObjectsInactive.Include);
        if (aimPointer == null) aimPointer = FindFirstObjectByType<OpenXRAimPointer>(FindObjectsInactive.Include);
        if (xrCamera == null) xrCamera = aimPointer != null && aimPointer.xrCamera != null ? aimPointer.xrCamera : Camera.main;
    }

    // ------------------------------------------------------------------------------------------- per frame
    private void LateUpdate()
    {
        if (testManager == null) return;
        TMTVRManager active = ActiveRunManager();
        if (runMgr != null && (active != runMgr || runMgr.Targets != runTargets || runMgr.TargetsSelected < runSelected))
        {
            // Read the outgoing run's last selections first (unless its manager was reset, which clears the count).
            if (runMgr.TargetsSelected >= runSelected && runMgr.Targets == runTargets) ReadSelections();
            EndRun();
        }
        if (runMgr == null && active != null) StartRun(active);
        if (runMgr == null) return;

        ReadSelections();
        RayAndHead(out Vector3 origin, out Vector3 dir, out Vector3 headPos, out Quaternion headRot);
        int hit = HitTarget();
        double tMs = Time.time * 1000.0;
        tracker.Frame(tMs, hit, V(dir), V(headRot * Vector3.forward), runMgr.totalMistakes);
        runFrames++;

        if (writePose)
        {
            Vector3 hp = aimPointer != null ? aimPointer.CurrentHitPoint : Vector3.zero;
            bool hv = aimPointer != null && aimPointer.HasValidHit;
            poseBuf.Append(runNumber).Append(',').Append(C(tMs, "0.#")).Append(',').Append(tracker.Current).Append(',').Append(hit).Append(',')
                   .Append(C(headPos.x)).Append(',').Append(C(headPos.y)).Append(',').Append(C(headPos.z)).Append(',')
                   .Append(C(headRot.x, "0.#####")).Append(',').Append(C(headRot.y, "0.#####")).Append(',').Append(C(headRot.z, "0.#####")).Append(',').Append(C(headRot.w, "0.#####")).Append(',')
                   .Append(C(origin.x)).Append(',').Append(C(origin.y)).Append(',').Append(C(origin.z)).Append(',')
                   .Append(C(dir.x, "0.#####")).Append(',').Append(C(dir.y, "0.#####")).Append(',').Append(C(dir.z, "0.#####")).Append(',')
                   .Append(hv ? 1 : 0).Append(',').Append(hv ? C(hp.x) : "").Append(',').Append(hv ? C(hp.y) : "").Append(',').Append(hv ? C(hp.z) : "").Append(',')
                   .Append(runMgr.totalMistakes).Append('\n');
            poseRows++;
        }
    }

    private TMTVRManager ActiveRunManager()
    {
        foreach (var m in new[] { testManager.tutorial_partA_Manager, testManager.partA_Manager, testManager.tutorial_partB_Manager, testManager.partB_Manager })
            if (m != null && m.gameObject.activeInHierarchy && m.Targets != null && m.Targets.Count > 0) return m;
        return null;
    }

    // Correct selections since the last read; each one's time is the task's previousSelectionTime.
    private void ReadSelections()
    {
        double tObs = Time.time * 1000.0;
        while (runMgr.TargetsSelected > runSelected)
        {
            runSelected++;
            tracker.Selection(runSelected, runMgr.previousSelectionTime * 1000.0, tObs, NextGeometry(runSelected));
        }
    }

    // The ray as OpenXRAimPointer computes it (read-only copy of its ComputeOriginAndDirection), and the head pose.
    private void RayAndHead(out Vector3 origin, out Vector3 dir, out Vector3 headPos, out Quaternion headRot)
    {
        headPos = xrCamera != null ? xrCamera.transform.position : Vector3.zero;
        headRot = xrCamera != null ? xrCamera.transform.rotation : Quaternion.identity;
        origin = headPos; dir = headRot * Vector3.forward;
        if (aimPointer == null) return;
        if (aimPointer.Hand && aimPointer.fingertip != null)
        { origin = aimPointer.fingertip.position; dir = (aimPointer.fingertip.rotation * aimPointer.fingertipLocalForwardAxis).normalized; }
        else if (aimPointer.Controller && aimPointer.controllerTip != null)
        { origin = aimPointer.controllerTip.position; dir = (aimPointer.controllerTip.rotation * aimPointer.controllerLocalForwardAxis).normalized; }
    }

    // 1-based number of the run's target whose collider the ray hits (0 = none).
    private int HitTarget()
    {
        if (aimPointer == null || !aimPointer.HasValidHit || aimPointer.CurrentHitCollider == null) return 0;
        var col = aimPointer.CurrentHitCollider;
        if (colliderTarget.TryGetValue(col, out int n)) return n;
        n = 0;
        for (int i = 0; i < runTargets.Count; i++)
            if (runTargets[i] != null && col.transform.IsChildOf(runTargets[i].transform)) { n = i + 1; break; }
        colliderTarget[col] = n;
        return n;
    }

    // Geometry of the link that begins with the selection of target 'selectedNumber' (its target is selectedNumber + 1):
    // the angle at the ray origin between the two targets' centres, the new target's angular radius and distance.
    private TMTVR_LinkGeometry NextGeometry(int selectedNumber)
    {
        var g = new TMTVR_LinkGeometry { StraightDeg = double.NaN, AngularRadiusDeg = double.NaN, DistanceM = double.NaN };
        GameObject a = Target(selectedNumber), b = Target(selectedNumber + 1);
        if (a == null || b == null) return g;
        RayAndHead(out Vector3 origin, out _, out _, out _);
        var ga = Geometry(a); var gb = Geometry(b);
        g.StraightDeg = Vector3.Angle(ga.centre - origin, gb.centre - origin);
        float d = Vector3.Distance(origin, gb.centre);
        g.DistanceM = d;
        g.AngularRadiusDeg = d <= 1e-4f ? 90.0 : Mathf.Atan2(gb.halfSize, d) * Mathf.Rad2Deg;
        return g;
    }

    private GameObject Target(int number) { return runTargets != null && number >= 1 && number <= runTargets.Count ? runTargets[number - 1] : null; }

    // Centre and half of the largest extent of the target's collider (the selectable region), else its renderer.
    private static (Vector3 centre, float halfSize) Geometry(GameObject g)
    {
        Bounds b; var col = g.GetComponentInChildren<Collider>(true); var ren = g.GetComponentInChildren<Renderer>(true);
        if (col != null) b = col.bounds; else if (ren != null) b = ren.bounds; else b = new Bounds(g.transform.position, Vector3.one * 0.2f);
        return (b.center, Mathf.Max(b.extents.x, Mathf.Max(b.extents.y, b.extents.z)));
    }

    // ------------------------------------------------------------------------------------------- runs
    private void StartRun(TMTVRManager m)
    {
        runMgr = m; runTargets = m.Targets; runSelected = 0; runNumber++; poseRows = 0; runFrames = 0; runStartS = Time.time;
        colliderTarget.Clear();
        runNames.Clear(); foreach (var t in runTargets) runNames.Add(t != null ? t.name : "");
        tracker = new TMTVR_MotionTracker();
        tracker.Begin(m.totalMistakes);
        bool partA = m == testManager.partA_Manager || m == testManager.tutorial_partA_Manager;
        runPart = partA ? "A" : "B";
        bool tutorial = m.targetPlacer != null && m.targetPlacer.Tutorial;
        int done = partA ? testManager.RealRunsA_Completed : testManager.RealRunsB_Completed;
        runStage = tutorial ? "tutorial" : (done == 0 ? "practice" : "test");
        var et = runTargets.Count > 0 && runTargets[0] != null ? runTargets[0].GetComponentInChildren<ET_Actions>(true) : null;
        dwellS = et != null ? et.confirmationTime : double.NaN;

        string hand = "?";
        if (aimPointer != null && aimPointer.Controller && aimPointer.controllerTip != null)
        {
            hand = aimPointer.controllerTip.name;
            foreach (var p in FindObjectsByType<TipProbeId>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (p.trackedTip == aimPointer.controllerTip) { hand = p.side.ToString() + " (" + aimPointer.controllerTip.name + ")"; break; }
        }
        Vector3 rig = xrCamera != null && xrCamera.transform.parent != null ? xrCamera.transform.parent.position : Vector3.zero;

        var sb = new StringBuilder("{\"event\":\"run_start\",\"run\":").Append(runNumber).Append(",\"part\":").Append(Q(runPart)).Append(",\"stage\":").Append(Q(runStage))
            .Append(",\"session_id\":").Append(scoreManager != null ? scoreManager.SessionID : -1)
            .Append(",\"mode\":").Append(Q(centralExecutive != null ? centralExecutive.intMode.ToString() : "?"))
            .Append(",\"ray_source\":").Append(Q(aimPointer == null ? "?" : aimPointer.Controller ? "controller" : aimPointer.Hand ? "hand" : aimPointer.Head ? "head" : "other"))
            .Append(",\"controller_hand\":").Append(Q(hand)).Append(",\"dwell_s\":").Append(F(dwellS))
            .Append(",\"app_version\":").Append(Q(Application.version)).Append(",\"rig_offset_m\":[").Append(F(rig.x, "0.####")).Append(',').Append(F(rig.y, "0.####")).Append(',').Append(F(rig.z, "0.####")).Append(']')
            .Append(",\"t_start_s\":").Append(F(runStartS, "0.####")).Append(",\"utc\":").Append(Q(DateTime.UtcNow.ToString("o", Inv)))
            .Append(",\"targets\":").Append(runTargets.Count).Append(",\"layout\":[");
        for (int i = 0; i < runTargets.Count; i++)
        {
            var t = runTargets[i]; if (i > 0) sb.Append(',');
            if (t == null) { sb.Append("null"); continue; }
            var geo = Geometry(t);
            sb.Append("{\"n\":").Append(i + 1).Append(",\"name\":").Append(Q(t.name)).Append(",\"x\":").Append(F(geo.centre.x, "0.####")).Append(",\"y\":").Append(F(geo.centre.y, "0.####"))
              .Append(",\"z\":").Append(F(geo.centre.z, "0.####")).Append(",\"half_size_m\":").Append(F(geo.halfSize, "0.####")).Append('}');
        }
        eventBuf.Append(sb).Append("]}\n");
    }

    private void EndRun()
    {
        double tEnd = Time.time * 1000.0;
        tracker.End(tEnd);
        // Links, in order, with a check against the task's own link times (selectionTimes holds one value per link).
        double maxDiffMs = double.NaN; int compared = 0;
        var taskTimes = runMgr.selectionTimes;
        for (int i = 0; i < tracker.Finished.Count; i++)
        {
            var l = tracker.Finished[i];
            if (taskTimes != null && i < taskTimes.Count)
            {
                double d = Math.Abs(l.LinkMs - taskTimes[i] * 1000.0);
                maxDiffMs = double.IsNaN(maxDiffMs) ? d : Math.Max(maxDiffMs, d); compared++;
            }
            eventBuf.Append("{\"event\":\"link\",\"run\":").Append(runNumber).Append(",\"part\":").Append(Q(runPart)).Append(",\"stage\":").Append(Q(runStage))
                .Append(",\"target\":").Append(l.Target).Append(",\"target_name\":").Append(Q(l.Target >= 1 && l.Target <= runNames.Count ? runNames[l.Target - 1] : ""))
                .Append(",\"prev_selection_s\":").Append(F(l.PrevSelectionMs / 1000, "0.####")).Append(",\"selection_s\":").Append(F(l.SelectionMs / 1000, "0.####"))
                .Append(",\"selection_observed_s\":").Append(F(l.SelectionObservedMs / 1000, "0.####"))
                .Append(",\"link_ms\":").Append(F(l.LinkMs, "0.#")).Append(",\"acquisition_ms\":").Append(F(l.AcquisitionMs, "0.#")).Append(",\"confirmation_ms\":").Append(F(l.ConfirmationMs, "0.#"))
                .Append(",\"first_entry_s\":").Append(F(l.FirstEntryMs / 1000, "0.####")).Append(",\"final_entry_s\":").Append(F(l.FinalEntryMs / 1000, "0.####"))
                .Append(",\"entries\":").Append(l.Entries).Append(",\"interrupted_dwells\":").Append(l.InterruptedDwells).Append(",\"interrupted_ms\":").Append(F(l.InterruptedMs, "0.#"))
                .Append(",\"errors_in_link\":").Append(l.ErrorsInLink)
                .Append(",\"ray_path_deg\":").Append(F(l.RayPathDeg, "0.##")).Append(",\"ray_peak_speed_deg_s\":").Append(F(l.RayPeakSpeedDegS, "0.#")).Append(",\"head_path_deg\":").Append(F(l.HeadPathDeg, "0.##"))
                .Append(",\"straight_deg\":").Append(F(l.StraightDeg, "0.##")).Append(",\"path_ratio\":").Append(F(l.PathRatio, "0.###"))
                .Append(",\"angular_radius_deg\":").Append(F(l.AngularRadiusDeg, "0.###")).Append(",\"distance_m\":").Append(F(l.DistanceM, "0.###"))
                .Append(",\"frames\":").Append(l.Frames).Append(",\"flags\":").Append(Q(string.Join(";", l.Flags))).Append("}\n");
        }
        foreach (var e in tracker.Errors)
            eventBuf.Append("{\"event\":\"error\",\"run\":").Append(runNumber).Append(",\"t_s\":").Append(F(e.tMs / 1000, "0.####")).Append(",\"target\":").Append(e.target)
                    .Append(",\"count\":").Append(e.count).Append("}\n");
        foreach (var e in tracker.Episodes)
            eventBuf.Append("{\"event\":\"episode\",\"run\":").Append(runNumber).Append(",\"target\":").Append(e.Target).Append(",\"role\":").Append(Q(e.Role))
                    .Append(",\"start_s\":").Append(F(e.StartMs / 1000, "0.####")).Append(",\"end_s\":").Append(F(e.EndMs / 1000, "0.####"))
                    .Append(",\"duration_ms\":").Append(F(e.DurationMs, "0.#")).Append(",\"outcome\":").Append(Q(e.Outcome)).Append("}\n");

        double durS = Math.Max(1e-3, Time.time - runStartS);
        int expected = Math.Max(0, runNames.Count - 1);
        eventBuf.Append("{\"event\":\"run_end\",\"run\":").Append(runNumber).Append(",\"part\":").Append(Q(runPart)).Append(",\"stage\":").Append(Q(runStage))
            .Append(",\"selections\":").Append(runSelected).Append(",\"links\":").Append(tracker.Finished.Count).Append(",\"links_expected\":").Append(expected)
            .Append(",\"task_link_times\":").Append(taskTimes != null ? taskTimes.Count : -1).Append(",\"task_links_compared\":").Append(compared)
            .Append(",\"max_link_diff_ms\":").Append(F(maxDiffMs, "0.###")).Append(",\"wrong_target_total\":").Append(runMgr.totalMistakes)
            .Append(",\"episodes\":").Append(tracker.Episodes.Count).Append(",\"frames\":").Append(runFrames).Append(",\"frames_per_s\":").Append(F(runFrames / durS, "0.##"))
            .Append(",\"median_frame_ms\":").Append(F(tracker.MedianFrameMs(), "0.##")).Append(",\"pose_rows\":").Append(poseRows)
            .Append(",\"t_end_s\":").Append(F(tEnd / 1000, "0.####")).Append("}\n");
        Debug.Log("TMTVR_MotionLogger: run " + runNumber + " (" + runPart + " " + runStage + ") selections " + runSelected + ", links " + tracker.Finished.Count + "/" + expected +
                  ", task link times " + (taskTimes != null ? taskTimes.Count : -1) + ", max link diff ms " + F(maxDiffMs) + ", episodes " + tracker.Episodes.Count +
                  ", frames/s " + F(runFrames / durS, "0.##") + ", pose rows " + poseRows);
        Flush();
        runMgr = null; runTargets = null; tracker = null;
    }

    private void OnApplicationPause(bool paused) { if (paused) Flush(); }
    private void OnDisable()
    {
        if (runMgr != null && tracker != null) { if (runMgr.TargetsSelected >= runSelected) ReadSelections(); EndRun(); }
        else Flush();
    }

    // ------------------------------------------------------------------------------------------- records
    private void Flush()
    {
        if (eventBuf.Length == 0 && poseBuf.Length == 0) return;
        try
        {
            string dir = Path.Combine(Application.persistentDataPath, "TMTVR_Session_Logs");
            Directory.CreateDirectory(dir);
            string stem = Path.Combine(dir, "motionID" + (scoreManager != null ? scoreManager.SessionID : -1).ToString(Inv));
            if (eventBuf.Length > 0) { File.AppendAllText(stem + "_events.jsonl", eventBuf.ToString(), new UTF8Encoding(false)); eventBuf.Clear(); }
            if (poseBuf.Length > 0)
            {
                string f = stem + "_pose.csv";
                if (!File.Exists(f)) File.WriteAllText(f, "run,t_ms,current_target,hit_target,head_x,head_y,head_z,head_qx,head_qy,head_qz,head_qw,ray_ox,ray_oy,ray_oz,ray_dx,ray_dy,ray_dz,hit_valid,hit_x,hit_y,hit_z,wrong_target_total\n", new UTF8Encoding(false));
                File.AppendAllText(f, poseBuf.ToString(), new UTF8Encoding(false)); poseBuf.Clear();
            }
        }
        catch (Exception e) { Debug.LogWarning("TMTVR_MotionLogger: records not written: " + e.Message); }
    }
}
