using UnityEngine;

/// <summary>
/// Enforces a SINGLE interaction mode for the whole app.
/// This is a "hard reset" controller: it disables auto-switching/assistance,
/// and activates exactly one rig subtree (Controllers / Hands / Gaze).
///
/// Use this when you build separate apps per mode, or when you switch ONCE at startup.
/// </summary>
public class XRHardResetMode: MonoBehaviour
{
    public enum Mode { Controllers, Hands, Gaze, HeadOnly }

    [Header("Select the one mode for this build")]
    public Mode mode = Mode.HeadOnly;

    [Header("Rig roots (make these empty parents and re-parent the rig parts)")]
    public GameObject controllersRoot;
    public GameObject handsRoot;
    public GameObject gazeRoot;

    [Header("Template 'smart' components to disable (drag them here)")]
    [Tooltip("XRInputModalityManager, XR Gaze Assistance, gesture detectors, controller input managers, etc.")]
    public Behaviour[] forceDisable;

    [Header("Optional: Input Action Managers per mode (only if you actually created them)")]
    public Behaviour controllersInputActionManager;
    public Behaviour handsInputActionManager;
    public Behaviour gazeInputActionManager;

    private void Awake()
    {
        Apply();
    }

    public void Apply()
    {
        // 1) Kill anything that auto-switches / auto-assists / gesture-toggles.
        if (forceDisable != null)
        {
            for (int i = 0; i < forceDisable.Length; i++)
                if (forceDisable[i] != null)
                    forceDisable[i].enabled = false;
        }

        // 2) Activate exactly one subtree (hard reset).
        SafeSetActive(controllersRoot, mode == Mode.Controllers);
        SafeSetActive(handsRoot, mode == Mode.Hands);
        SafeSetActive(gazeRoot, mode == Mode.Gaze);

        // HeadOnly means: no controllers/hands/gaze interactor active.
        if (mode == Mode.HeadOnly)
        {
            SafeSetActive(controllersRoot, false);
            SafeSetActive(handsRoot, false);
            SafeSetActive(gazeRoot, false);
        }

        // 3) Optional: gate action assets (only matters if you have multiple managers).
        SafeSetEnabled(controllersInputActionManager, mode == Mode.Controllers);
        SafeSetEnabled(handsInputActionManager, mode == Mode.Hands);
        SafeSetEnabled(gazeInputActionManager, mode == Mode.Gaze);
    }

    private static void SafeSetActive(GameObject go, bool active)
    {
        if (go == null) return;
        if (go.activeSelf == active) return;
        go.SetActive(active);
    }

    private static void SafeSetEnabled(Behaviour b, bool enabled)
    {
        if (b == null) return;
        if (b.enabled == enabled) return;
        b.enabled = enabled;
    }
}
