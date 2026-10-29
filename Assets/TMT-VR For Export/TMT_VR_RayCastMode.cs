using UnityEngine;

/// <summary>
/// Ray/aim mode switcher (single source of truth).
/// - No Physics.Raycast here.
/// - Delegates ALL hit computation + pointer placement to OpenXRAimPointer.
/// - Uses ONE enum (no conflicting booleans).
/// </summary>
public class TMT_VR_RayCastMode : MonoBehaviour
{
    // ------------------------------------------------------------------
    // 1) Mode (single source of truth)
    // ------------------------------------------------------------------
    public enum RayMode
    {
        Head,        // Head-forward ray
        Hand,        // Hand tracking fingertip ray (HandTip in OpenXRAimPointer)
        Controller,  // Controller-forward ray (ControllerForward in OpenXRAimPointer)
        Eye          // OpenXR eye-gaze ray
    }

    [Header("Mode (set by CentralExecutive or Inspector)")]
    public RayMode mode = RayMode.Head;

    // ------------------------------------------------------------------
    // 2) References (assign in Inspector)
    // ------------------------------------------------------------------
    [Header("Linked objects (assign in Inspector)")]
    [SerializeField] private GameObject tmtRoot;             // The task root (active while TMT runs)
    [SerializeField] private ET_Manager_TMTVR etManager;     // Only used for ET visuals policy (ET_Mode)
    [SerializeField] private OpenXRAimPointer aimPointer;    // The single raycasting engine

    [Header("Single pointer target (one object for ALL modes)")]
    [SerializeField] private GameObject pointerObject;       // e.g., ET_Pointer

    // ------------------------------------------------------------------
    // 3) Unity lifecycle
    // ------------------------------------------------------------------
    private void Awake()
    {
        // Fail loudly, early (so mis-wiring is obvious in the Console)
        if (tmtRoot == null) Debug.LogError("[TMT_VR_RayCastMode] tmtRoot is not assigned.", this);
        if (etManager == null) Debug.LogError("[TMT_VR_RayCastMode] etManager is not assigned.", this);
        if (aimPointer == null) Debug.LogError("[TMT_VR_RayCastMode] aimPointer is not assigned.", this);
        if (pointerObject == null) Debug.LogError("[TMT_VR_RayCastMode] pointerObject is not assigned.", this);

        // Safety belt: ensure OpenXRAimPointer always moves the correct object.
        // (Even if you also assign aimTarget in the Inspector, this prevents prefab variants breaking.)
        if (aimPointer != null && pointerObject != null)
            aimPointer.aimTarget = pointerObject;
    }

    private void Update()
    {
        // --------------------------------------------------------------
        // If task not active: hide pointer and disable ET visuals
        // --------------------------------------------------------------
        if (tmtRoot == null || !tmtRoot.activeSelf)
        {
            if (pointerObject != null && pointerObject.activeSelf)
                pointerObject.SetActive(false);

            if (etManager != null)
                etManager.ET_Mode = false;

            return;
        }

        // If key components are missing, do nothing safely.
        if (aimPointer == null || etManager == null)
            return;

        // Task is active: ensure pointer object exists (it will be moved by OpenXRAimPointer).
        if (pointerObject != null && !pointerObject.activeSelf)
            pointerObject.SetActive(true);

        // --------------------------------------------------------------
        // Switch aim source + ET visuals based on a SINGLE mode enum
        // --------------------------------------------------------------
        switch (mode)
        {
            case RayMode.Head:
                // Head-forward ray; typically you don't want ET-specific visuals
                etManager.ET_Mode = false;
                aimPointer.aimSource = "Head";
                break;

            case RayMode.Hand:
                // Hand tracking fingertip ray (OpenXRAimPointer.fingertip)
                etManager.ET_Mode = false;
                aimPointer.aimSource = "Hand";
                break;

            case RayMode.Controller:
                // Controller-forward ray
                etManager.ET_Mode = false;
                aimPointer.aimSource = "Controller";
                break;

            case RayMode.Eye:
                // Eye-gaze ray; ET visuals (follower/reticle) may be desirable
                etManager.ET_Mode = true;
                aimPointer.aimSource = "Eye";
                break;
        }
    }

#if UNITY_EDITOR
    // Handy inspector context menu to re-apply aimTarget wiring immediately
    [ContextMenu("Rebind AimTarget to Pointer Object")]
    private void RebindAimTarget_Editor()
    {
        if (aimPointer != null && pointerObject != null)
            aimPointer.aimTarget = pointerObject;
    }
#endif
}
