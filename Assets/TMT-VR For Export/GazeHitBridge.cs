using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Reads hit info from XRGazeInteractor and moves a pointer object to the hit point.
/// This does NOT do selection. It only mirrors the gaze ray hit point.
/// </summary>
public class GazeHitBridge : MonoBehaviour
{
    [Header("Source (XRIT)")]
    [Tooltip("If left empty, this script will auto-find XRGazeInteractor on the same GameObject.")]
    public XRGazeInteractor gazeInteractor;

    [Header("Output (Your pointer)")]
    [Tooltip("The Transform of the pointer visual you want to move (e.g., your aimTarget).")]
    public Transform aimTargetTransform;

    [Header("Fallback (when nothing is hit)")]
    [Tooltip("If the gaze ray hits nothing, place the pointer this far forward from the ray origin.")]
    public float fallbackDistance = 5f;

    [Tooltip("If true, we use the gaze interactor's Ray Origin Transform when available.")]
    public bool preferRayOriginTransform = true;

    private void Awake()
    {
        if (gazeInteractor == null)
            gazeInteractor = GetComponent<XRGazeInteractor>();
    }

    private void LateUpdate()
    {
        if (gazeInteractor == null || aimTargetTransform == null)
            return;

        // TryGetHitInfo returns TRUE when the interactor has a valid 3D hit result.
        if (gazeInteractor.TryGetHitInfo(out var hitPos, out _, out _, out _))
        {
            aimTargetTransform.position = hitPos;
            return;
        }

        // Fallback: place pointer forward in space so it never sits at (0,0,0) by accident.
        Transform originT = gazeInteractor.transform;

        if (preferRayOriginTransform && gazeInteractor.rayOriginTransform != null)
            originT = gazeInteractor.rayOriginTransform;

        aimTargetTransform.position = originT.position + originT.forward * fallbackDistance;
    }
}