// Maintains passive motion-log bookkeeping for target selections, ray episodes and per-link timing intervals; it uses System types and does not control the task or calculate adjusted scores.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public struct TMTVR_V3
{
    public double X, Y, Z;
    public TMTVR_V3(double x, double y, double z) { X = x; Y = y; Z = z; }
    public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);
    public static TMTVR_V3 operator -(TMTVR_V3 a, TMTVR_V3 b) => new TMTVR_V3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    // Angle between two directions in degrees (0 when either is zero-length).
    public static double AngleDeg(TMTVR_V3 a, TMTVR_V3 b)
    {
        double la = a.Length, lb = b.Length;
        if (la < 1e-12 || lb < 1e-12) return 0;
        double c = (a.X * b.X + a.Y * b.Y + a.Z * b.Z) / (la * lb);
        return Math.Acos(Math.Max(-1.0, Math.Min(1.0, c))) * 180.0 / Math.PI;
    }
}

public sealed class TMTVR_RayEpisode
{
    public int Target;            // 1-based target number in the run's order
    public string Role = "";      // current / selected / future (when the episode began)
    public double StartMs, EndMs = double.NaN;
    public string Outcome = "PASS";
    public double DurationMs => double.IsNaN(EndMs) ? double.NaN : EndMs - StartMs;
}

public sealed class TMTVR_MotionLink
{
    public int Target;
    public double PrevSelectionMs, SelectionMs, SelectionObservedMs = double.NaN;
    public double FinalEntryMs = double.NaN, FirstEntryMs = double.NaN;
    public int Entries, InterruptedDwells, ErrorsInLink;
    public double InterruptedMs;
    public double RayPathDeg, RayPeakSpeedDegS, HeadPathDeg;
    public double StraightDeg = double.NaN, AngularRadiusDeg = double.NaN, DistanceM = double.NaN;
    public int Frames;
    public List<string> Flags = new List<string>();
    public double LinkMs => SelectionMs - PrevSelectionMs;
    public double AcquisitionMs => double.IsNaN(FinalEntryMs) ? double.NaN : FinalEntryMs - PrevSelectionMs;
    public double ConfirmationMs => double.IsNaN(FinalEntryMs) ? double.NaN : SelectionMs - FinalEntryMs;
    public double PathRatio => double.IsNaN(StraightDeg) || StraightDeg < 1e-6 ? double.NaN : RayPathDeg / StraightDeg;
}

// Geometry of a link's target, supplied by the logger when the link begins (all optional).
public struct TMTVR_LinkGeometry
{
    public double StraightDeg, AngularRadiusDeg, DistanceM;
}

public sealed class TMTVR_MotionTracker
{
    // Tolerance for "the episode was open at the selection": the selection is timed on the physics grid and the
    // episode on frames, so an episode that ended up to this long before the selection still counts as the final one.
    public double SelectionToleranceMs = 40.0;

    public readonly List<TMTVR_RayEpisode> Episodes = new List<TMTVR_RayEpisode>();
    public readonly List<TMTVR_MotionLink> Finished = new List<TMTVR_MotionLink>();
    // Each rise in the task's wrong-target count: (frame time, target the ray hit then or last hit, rise).
    public readonly List<(double tMs, int target, int count)> Errors = new List<(double, int, int)>();

    private int current = 1;                     // target due now (1-based)
    private int selected;                        // number of targets selected so far
    private TMTVR_RayEpisode open;               // episode in progress (null when the ray hits no target)
    private TMTVR_RayEpisode lastClosed;         // the most recently closed episode
    private TMTVR_MotionLink link;               // link in progress (target = current), null before the first selection
    private int mistakes;                        // the task's wrong-target count at the last frame
    private readonly List<TMTVR_RayEpisode> linkEpisodes = new List<TMTVR_RayEpisode>();   // episodes on the current target in this link
    private bool haveDir; private TMTVR_V3 lastRay, lastHead; private double lastT;
    private readonly double[] tHist = new double[3]; private readonly TMTVR_V3[] dHist = new TMTVR_V3[3]; private int hist;
    private readonly List<double> frameDt = new List<double>();

    public int Selected => selected;
    public int Current => current;

    // Call once at run start with the task's wrong-target count (it is not reset between runs in every version).
    public void Begin(int mistakesNow) { mistakes = mistakesNow; current = 1; selected = 0; }

    // One rendered frame. hitTarget = 1-based target the ray hits (0 = none); rayDir/headFwd are world directions.
    public void Frame(double tMs, int hitTarget, TMTVR_V3 rayDir, TMTVR_V3 headFwd, int mistakesNow)
    {
        // 1. Kinematics, accumulated into the link in progress.
        if (haveDir)
        {
            double dt = tMs - lastT;
            if (dt > 0) frameDt.Add(dt);
            if (link != null)
            {
                link.RayPathDeg += TMTVR_V3.AngleDeg(lastRay, rayDir);
                link.HeadPathDeg += TMTVR_V3.AngleDeg(lastHead, headFwd);
                link.Frames++;
            }
        }
        tHist[hist % 3] = tMs; dHist[hist % 3] = rayDir; hist++;
        if (hist >= 3 && link != null)
        {
            int a = hist % 3;                                      // oldest of the last three samples
            double span = tMs - tHist[a];
            if (span > 1e-6)
            {
                double v = TMTVR_V3.AngleDeg(dHist[a], rayDir) / span * 1000.0;
                if (v > link.RayPeakSpeedDegS) link.RayPeakSpeedDegS = v;
            }
        }
        lastRay = rayDir; lastHead = headFwd; lastT = tMs; haveDir = true;

        // 2. A rise in the wrong-target count: the task completed a dwell on a wrong target this frame (or since the last).
        if (mistakesNow > mistakes)
        {
            int d = mistakesNow - mistakes;
            if (link != null) link.ErrorsInLink += d;
            // The task's dwell runs on physics steps, so the ray may already have left the wrong target on this frame:
            // attribute the error to the open episode, else to the episode that closed most recently.
            var e = open ?? lastClosed;
            if (e != null && e.Target != current && e.Outcome != "SELECTED") e.Outcome = "ERROR";
            Errors.Add((tMs, e != null ? e.Target : 0, d));
        }
        mistakes = mistakesNow;

        // 3. Ray episodes.
        if (open != null && open.Target != hitTarget) Close(tMs);
        if (open == null && hitTarget > 0)
        {
            open = new TMTVR_RayEpisode { Target = hitTarget, StartMs = tMs, Role = hitTarget == current ? "current" : (hitTarget < current ? "selected" : "future") };
            Episodes.Add(open);
            if (hitTarget == current && link != null)
            {
                link.Entries++;
                if (double.IsNaN(link.FirstEntryMs)) link.FirstEntryMs = tMs;
                linkEpisodes.Add(open);
            }
        }
    }

    // The task's correct selection of target 'number' at tSelMs (its own clock), seen by the logger at tObsMs.
    // geoNext describes the next link's target (number + 1), measured by the logger now.
    public void Selection(int number, double tSelMs, double tObsMs, TMTVR_LinkGeometry geoNext)
    {
        // Close the link in progress (target 'number').
        if (link != null && link.Target == number)
        {
            link.SelectionMs = tSelMs; link.SelectionObservedMs = tObsMs;
            TMTVR_RayEpisode final = null;
            // The final entry: the episode on this target that was open at the selection, or ended within the tolerance.
            for (int i = linkEpisodes.Count - 1; i >= 0; i--)
            {
                var e = linkEpisodes[i];
                double end = double.IsNaN(e.EndMs) ? double.PositiveInfinity : e.EndMs;
                if (e.StartMs <= tObsMs && end >= tSelMs - SelectionToleranceMs) { final = e; break; }
            }
            if (final != null)
            {
                final.Outcome = "SELECTED";
                link.FinalEntryMs = final.StartMs;
                if (linkEpisodes.Count > 0 && ReferenceEquals(linkEpisodes[0], final)) link.Flags.Add("FIRST_ENTRY_SELECTED");
            }
            else link.Flags.Add("NO_FINAL_ENTRY");
            foreach (var e in linkEpisodes)
            {
                if (ReferenceEquals(e, final) || double.IsNaN(e.EndMs)) continue;
                e.Outcome = "INTERRUPTED"; link.InterruptedDwells++; link.InterruptedMs += e.DurationMs;
            }
            Finished.Add(link);
        }
        else if (open != null && open.Target == number) open.Outcome = "SELECTED";   // the first target (no link)

        selected = number; current = number + 1;
        linkEpisodes.Clear();
        // The open episode on the selected target stays open until the ray leaves; its role does not change.
        link = new TMTVR_MotionLink { Target = current, PrevSelectionMs = tSelMs, StraightDeg = geoNext.StraightDeg, AngularRadiusDeg = geoNext.AngularRadiusDeg, DistanceM = geoNext.DistanceM };
        // If the ray is already on the new current target (it can happen with adjacent targets), count that entry.
        if (open != null && open.Target == current) { link.Entries++; link.FirstEntryMs = open.StartMs; linkEpisodes.Add(open); open.Role = "current"; }
    }

    // End of run: close the open episode; the link in progress (target not selected) is dropped, as the task drops it.
    public void End(double tMs)
    {
        if (open != null) Close(tMs);
        link = null; linkEpisodes.Clear();
    }

    // Median frame interval of the run so far (ms), for the frame-rate flag and the run summary.
    public double MedianFrameMs() { return Median(frameDt); }

    public static double Median(List<double> v)
    {
        if (v == null || v.Count == 0) return double.NaN;
        var s = new List<double>(v); s.Sort();
        int n = s.Count; return n % 2 == 1 ? s[n / 2] : 0.5 * (s[n / 2 - 1] + s[n / 2]);
    }

    private void Close(double tMs)
    {
        open.EndMs = tMs;
        lastClosed = open;
        open = null;
    }
}
