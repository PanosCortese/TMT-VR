using UnityEngine;

public class ET_Manager_TMTVR : MonoBehaviour
{
    [Header("Task Root (active while TMT runs)")]
    [SerializeField] private GameObject tmtVrTask;

    [Header("Raycast Engine")]
    [SerializeField] private OpenXRAimPointer gazePointer; // single raycast engine

    [Header("Single Pointer Object (used in ALL modes)")]
    [SerializeField] private GameObject gazeTarget;         // e.g., ET_Pointer (do NOT SetActive here)
    [SerializeField] private GameObject gazeTargetFollower; // optional highlight/follower

    [Header("Visual policy")]
    [SerializeField] private bool showPointerRendererOnlyInETMode = false;
    [SerializeField] private bool showFollowerOnlyInETMode = true;

    // Set by TMT_VR_RayCastMode (mode switcher)
    [Header("Runtime (set by Mode Switcher)")]
    public bool ET_Mode;

    private MeshRenderer _gazeTargetRenderer;
    private bool _lastTaskActive;

    private void Awake()
    {
        if (gazeTarget != null)
            gazeTarget.TryGetComponent(out _gazeTargetRenderer);

        // Safety belt: raycaster moves THIS pointer
        if (gazePointer != null && gazeTarget != null)
            gazePointer.aimTarget = gazeTarget;
    }

    private void Update()
    {
        if (tmtVrTask == null) return;

        bool taskActive = tmtVrTask.activeSelf;

        // Transition handling
        if (taskActive != _lastTaskActive)
        {
            ApplyTaskActiveState(taskActive);
            _lastTaskActive = taskActive;
        }

        if (!taskActive) return;

        // ---- During task: ONLY manage visuals (not activation) ----
        if (_gazeTargetRenderer != null)
            _gazeTargetRenderer.enabled = showPointerRendererOnlyInETMode ? ET_Mode : true;

        if (gazeTargetFollower != null)
            gazeTargetFollower.SetActive(showFollowerOnlyInETMode ? ET_Mode : true);
    }

    private void ApplyTaskActiveState(bool taskActive)
    {
        // Enable/disable the raycast engine with the task
        if (gazePointer != null)
            gazePointer.enabled = taskActive;

        // When task ends, hide follower/renderer (but do not deactivate the pointer object)
        if (!taskActive)
        {
            if (gazeTargetFollower != null) gazeTargetFollower.SetActive(false);
            if (_gazeTargetRenderer != null) _gazeTargetRenderer.enabled = false;
        }
    }
}
