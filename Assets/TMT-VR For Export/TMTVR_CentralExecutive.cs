using System.Collections;
using UnityEngine;

/// <summary>
/// Central executive / experiment orchestrator for TMT-VR.
/// 
/// Responsibilities (only the essentials):
/// 1) Wait for ScoreManager to finish CSV initialization.
/// 2) Enable ONLY the required tracking/probe objects for the selected interaction mode.
/// 3) Configure the OpenXRAimPointer to use the desired aim source (Eye/Hand/Head/Controller).
/// 4) Apply the "single mode" scene lockdown (disable unwanted interactors / modality managers).
/// 5) (Hand/Controller only) Run DominanceCheck: user touches a cube with the dominant hand/controller probe.
/// 6) Run familiarization delay, then Part A, Part B, then save score and end.
/// 
/// IMPORTANT DESIGN CHOICE:
/// - We assume all controller/hand "actions" are disabled. We only rely on tracking (poses/transforms).
/// - Dominance is determined by a simple TOUCH of a cube using a probe object (kinematic Rigidbody).
/// </summary>
public class TMTVR_CentralExecutive : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Interaction Modes
    // -------------------------------------------------------------------------
    public enum InteractionMode
    {
        eye,        // Eye tracking gaze ray (OpenXR eye gaze)
        hand,       // Hand index fingertip ray (tracked pose / transform)
        head,       // Head-forward ray (camera forward)
        controller  // Controller-forward ray (tracked controller tip transform)
    }

    [Header("Interaction Mode")]
    [Tooltip("Select which interaction mode will be used for this session.\n\n" +
             "eye: OpenXR eye gaze (XR_EXT_eye_gaze_interaction)\n" +
             "hand: index fingertip (pose-driven transform)\n" +
             "head: ray from XR camera forward\n" +
             "controller: ray from controller tip transform")]
    public InteractionMode intMode;

    // -------------------------------------------------------------------------
    // Timing / Flow
    // -------------------------------------------------------------------------
    [Header("Timing")]
    [Tooltip("Familiarization duration (seconds).\nThis is the duration AFTER dominance selection (hand/controller).")]
    public float familDuration;

    [Tooltip("Optional delay (seconds) to ensure assets & XR subsystems are fully ready before starting.\n" +
             "Useful on Android/Standalone where first frames can be 'cold'.")]
    public float delayForLoading;

    // -------------------------------------------------------------------------
    // Core objects
    // -------------------------------------------------------------------------
    [Header("Task Objects")]
    [Tooltip("The GameObject that contains the TMT-VR task manager (TMTVR_TestManager).")]
    public GameObject TMT;

    [Tooltip("Shown at the end of the experiment.")]
    public GameObject EndofExperiment;

    [Tooltip("Optional: platform/environment root (if you need to enable/disable it). Not used here.")]
    public GameObject Platform;

    [Tooltip("Score manager responsible for CSV initialization and saving scores.")]
    public TMTVR_ScoreManager ScoreManager;

    // -------------------------------------------------------------------------
    // Dominance selection cube
    // -------------------------------------------------------------------------
    [Header("Dominance Selection")]
    [Tooltip("A cube (or any collider) used during setup.\n" +
             "User will TOUCH this with the dominant hand/controller probe.\n" +
             "The DominanceCubeSelector on this object must set Selected=true when a valid probe touches it.")]
    public GameObject dominanceCube;

    // -------------------------------------------------------------------------
    // Pointer
    // -------------------------------------------------------------------------
    [Header("Pointer & Mode Lock")]
    [Tooltip("Your raycasting pointer script (OpenXRAimPointer). This will be configured once at start.")]
    public OpenXRAimPointer aimPointer;

    // -------------------------------------------------------------------------
    // Scene objects for Hands / Controllers
    // (probe objects with kinematic rigidbodies and triggers)
    // -------------------------------------------------------------------------
    [Header("Eye-Tracking")]
    [Tooltip("Eye Gaze Root")]
    public GameObject EyeGazeRoot;

    [Header("Controller Tracking + Probes")]
    [Tooltip("Controllers Root")]
    public GameObject controllersRoot;

    [Tooltip("Right controller-tip probe GameObject (kinematic Rigidbody + collider).\nEnable only in Controller mode.")]
    public GameObject contipR;

    [Tooltip("Left controller-tip probe GameObject (kinematic Rigidbody + collider).\nEnable only in Controller mode.")]
    public GameObject contipL;

    [Header("Hand Tracking + Probes")]
    [Tooltip("Hands Root")]
    public GameObject handsRoot;

    [Tooltip("Right fingertip probe GameObject (kinematic Rigidbody + collider).\nEnable only in Hand mode.")]
    public GameObject fintipR;

    [Tooltip("Left fingertip probe GameObject (kinematic Rigidbody + collider).\nEnable only in Hand mode.")]
    public GameObject fintipL;

    // Cached component to avoid repeated GetComponent calls
    private DominanceCubeSelector _dominanceSelector;


    public string Eye_Interaction;
    public string Hand_Interaction;
    public string Controller_Interaction;
    public string Head_Interaction;

    private void Awake()
    {
        // Cache DominanceCubeSelector once (faster + safer).
        if (dominanceCube != null)
            _dominanceSelector = dominanceCube.GetComponent<DominanceCubeSelector>();
    }

    private void Start()
    {
        StartCoroutine(ExecuteTasks());
    }

    /// <summary>
    /// Main experiment flow coroutine.
    /// </summary>
    private IEnumerator ExecuteTasks()
    {
        // 1) Ensure ScoreManager has initialized its CSV schema/header.
        yield return new WaitUntil(() => ScoreManager != null && ScoreManager.CSVInitialization);

        // 2) Give XR subsystems and scene a moment to fully come alive (optional).
        if (delayForLoading > 0f)
            yield return new WaitForSeconds(delayForLoading);
        
        if (aimPointer == null)
        {
            Debug.LogError("[TMTVR_CentralExecutive] aimPointer not assigned.", this);
            yield break;
        }

        // 4) Enable only what's needed for this mode.
        switch (intMode)
        {
            case InteractionMode.hand:
                aimPointer.Hand = true; 
                aimPointer.aimSource = Hand_Interaction;
                // Hand mode requires hand tracking + fingertip probes to be active.
                SetHandsActive(true);
                SetControllersActive(false);
                EyeGazeRoot.SetActive(false);
                break;

            case InteractionMode.controller:
                aimPointer.Controller = true; 
                aimPointer.aimSource = Controller_Interaction;
                // Controller mode requires controller tracking + controller tip probes to be active.
                SetControllersActive(true);
                SetHandsActive(false);
                EyeGazeRoot.SetActive(false);
                break;

            // eye/head: no hands/controllers needed for the ray origin
            case InteractionMode.eye:
                aimPointer.Eye = true; 
                aimPointer.aimSource = Eye_Interaction;
                EyeGazeRoot.SetActive(true);
                SetHandsActive(false);
                SetControllersActive(false);
                break;
            case InteractionMode.head:
                aimPointer.Head = true; 
                aimPointer.aimSource = Head_Interaction;
                SetHandsActive(false);
                SetControllersActive(false);
                EyeGazeRoot.SetActive(false);
                break;
        }
    
        // Only needed for Hand/Controller modes, because user must choose Left vs Right.
        if (intMode == InteractionMode.hand || intMode == InteractionMode.controller)
            yield return StartCoroutine(DominanceCheck());
        
        DisableTips();

        aimPointer.RayCasting = true;

        // 8) Familiarization:
        // At this point dominance should already be locked (pointer uses correct side).
        if (familDuration > 0f)
            yield return new WaitForSeconds(familDuration);

        // 9) Run tasks.
        yield return StartCoroutine(TMTVR_PartA());
        yield return StartCoroutine(TMTVR_PartB());

        // 10) Save score then end.
        yield return StartCoroutine(ScoreManager.SaveScore());
        ExperimentEnd();
    }

    private void DisableTips()
    {
        if (fintipL != null)
        {
            fintipL.SetActive(false);
        }
        if (fintipR != null)
        {
            fintipR.SetActive(false);
        }
        if (contipL != null)
        {
            contipL.SetActive(false);
        }
        if (contipR != null)
        {
            contipR.SetActive(false);
        }
    }
    /// <summary>
    /// Shows the dominance cube and waits until its selector marks Selected=true.
    /// The selector is responsible for assigning the correct tip transform to OpenXRAimPointer.
    /// </summary>
    private IEnumerator DominanceCheck()
    {
        if (dominanceCube == null)
        {
            Debug.LogError("[TMTVR_CentralExecutive] dominanceCube is not assigned.", this);
            yield break;
        }

        if (_dominanceSelector == null)
        {
            Debug.LogError("[TMTVR_CentralExecutive] DominanceCubeSelector missing on dominanceCube.", this);
            yield break;
        }

        // Assumes a fresh session, so DominanceCubeSelector.Selected starts false.

        dominanceCube.SetActive(true);

        // Wait until the cube selector reports selection complete.
        yield return new WaitUntil(() => _dominanceSelector.Selected);

        dominanceCube.SetActive(false);
    }

    /// <summary>
    /// Enable/disable all hand objects + fingertip probes.
    /// </summary>
    private void SetHandsActive(bool on)
    {
        if (handsRoot != null) handsRoot.SetActive(on);
        if (fintipL != null) fintipL.SetActive(on);
        if (fintipR != null) fintipR.SetActive(on);
    }

    /// <summary>
    /// Enable/disable all controller objects + controller tip probes.
    /// </summary>
    private void SetControllersActive(bool on)
    {
        if (controllersRoot != null) controllersRoot.SetActive(on);
        if (contipL != null) contipL.SetActive(on);
        if (contipR != null) contipR.SetActive(on);
    }

    /// <summary>
    /// Called at end of the experiment.
    /// </summary>
    public void ExperimentEnd()
    {
        if (EndofExperiment != null)
            EndofExperiment.SetActive(true);
    }

    /// <summary>
    /// Runs TMT Part A.
    /// </summary>
    private IEnumerator TMTVR_PartA()
    {
        if (TMT == null)
        {
            Debug.LogError("[TMTVR_CentralExecutive] TMT object not assigned.", this);
            yield break;
        }

        TMT.SetActive(true);

        var testMgr = TMT.GetComponent<TMTVR_TestManager>();
        if (testMgr == null)
        {
            Debug.LogError("[TMTVR_CentralExecutive] TMTVR_TestManager missing on TMT object.", this);
            yield break;
        }

        testMgr.TMTA();
        yield return new WaitUntil(() => testMgr.TMT_PartAcompleted);

        TMT.SetActive(false);
    }

    /// <summary>
    /// Runs TMT Part B.
    /// </summary>
    private IEnumerator TMTVR_PartB()
    {
        if (TMT == null)
        {
            Debug.LogError("[TMTVR_CentralExecutive] TMT object not assigned.", this);
            yield break;
        }

        TMT.SetActive(true);

        var testMgr = TMT.GetComponent<TMTVR_TestManager>();
        if (testMgr == null)
        {
            Debug.LogError("[TMTVR_CentralExecutive] TMTVR_TestManager missing on TMT object.", this);
            yield break;
        }

        testMgr.TMTB();
        yield return new WaitUntil(() => testMgr.TMT_PartBcompleted);

        TMT.SetActive(false);
    }
}
