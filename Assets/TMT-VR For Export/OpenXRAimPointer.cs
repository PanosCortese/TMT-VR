using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.OpenXR;

public class OpenXRAimPointer : MonoBehaviour
{
    [Header("Aim Source")]
    public bool Head;
    public bool Eye;
    public bool Hand;
    public bool Controller;

    public string aimSource;

    [Header("Hand Tip (optional)")]
    public Transform fingertip;
    public Vector3 fingertipLocalForwardAxis = Vector3.forward;

    [Header("Controller (optional)")]
    public Transform controllerTip;
    public Vector3 controllerLocalForwardAxis = Vector3.back;

    [Header("XR Camera (HMD)")]
    public Camera xrCamera;

    [Header("Cast Settings")]
    public float maxDistance;
    public float floatingDistance;

    [Header("Trigger handling (important)")]
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.UseGlobal;

    [Header("Target Filtering")]
    public bool requireETTag = true;
    public string etTag = "ET";
    public LayerMask hitMask;

    [Header("Pointer Target")]
    public GameObject aimTarget;

    // ---- Runtime outputs ----
    public bool HasValidHit { get; private set; }
    public Vector3 CurrentHitPoint { get; private set; }
    public Collider CurrentHitCollider { get; private set; }

    private Vector3 _origin;
    private Vector3 _dir;
    private bool _loggedMissingCamera;
    public bool RayCasting = false;


    // ------------------------------------------------------------
    // Single-loop update: compute -> cast -> move pointer
    // This eliminates jitter and the "stale hitpoint" bug.
    // ------------------------------------------------------------
    private void LateUpdate()
    {
        if (!RayCasting)
        {
            HasValidHit = false;
            CurrentHitCollider = null;
            aimTarget.SetActive(false);
            return;
        }
        if (aimTarget == null)
            return;

        if (xrCamera == null)
        {
            if (!_loggedMissingCamera)
            {
                Debug.LogError("[OpenXRAimPointer] xrCamera is not assigned.", this);
                _loggedMissingCamera = true;
            }
            return;
        }

        if (RayCasting)
        {
            ComputeOriginAndDirection();
            PerformCastAndUpdateOutputs();
            // Keep visuals consistent (optional).
            if (!aimTarget.activeSelf)
                aimTarget.SetActive(true);

            aimTarget.transform.position = CurrentHitPoint;
        }
    }

    private void ComputeOriginAndDirection()
    {
        // Safe defaults (head ray).
        _origin = xrCamera.transform.position;
        _dir = xrCamera.transform.forward;
        
        if (Head)
        {
            _origin = xrCamera.transform.position;
            _dir = xrCamera.transform.forward;
        }
        else if (Hand)
        {
            if (fingertip != null)
            {
                _origin = fingertip.position;
                _dir = (fingertip.rotation * fingertipLocalForwardAxis).normalized;
            }
        }
        else if (Controller)
        {
            if (controllerTip != null)
            {
                _origin = controllerTip.position;
                _dir = (controllerTip.rotation * controllerLocalForwardAxis).normalized;
            }
        }
        else if (Eye)
        {
            Debug.Log("Raycasting is Controlled By EyeGazer");
            RayCasting = false;
        }
        else
        {
            Debug.Log("No Interaction Mode was Selected");
            RayCasting = false;
        }
    }

    private void PerformCastAndUpdateOutputs()
    {
        RaycastHit hit;
        bool hitSomething;
        
        hitSomething = Physics.Raycast(_origin, _dir, out hit, maxDistance, hitMask, triggerInteraction);

        bool accepted = hitSomething && (!requireETTag || hit.collider.CompareTag(etTag));

        HasValidHit = accepted;
        CurrentHitCollider = accepted ? hit.collider : null;
        CurrentHitPoint = accepted ? hit.point : (_origin + _dir * floatingDistance);
    }

}
