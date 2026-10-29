using UnityEngine;

/// <summary>
/// Physics-friendly follower for a collider/rigidbody "probe" object.
/// Use this for fingertip/controller-tip colliders that must reliably trigger overlaps.
///
/// REQUIREMENTS (important):
/// - This GameObject must have a Rigidbody (isKinematic = true recommended).
/// - Your targets can be Trigger colliders (common for TMT targets).
/// - At least ONE of the two colliders in a trigger interaction must have a Rigidbody.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class FollowTargetPhysics : MonoBehaviour
{
    [Tooltip("Transform to follow (e.g., a proxy transform driven by OpenXR pose actions).")]
    public Transform target;

    [Tooltip("Also match rotation (useful for controller tip alignment).")]
    public bool followRotation = false;

    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        // For trigger-based probes, kinematic is typically what you want:
        // - You control motion explicitly
        // - Physics does not "push back" on your probe
        _rb.isKinematic = true;

        // Helps smoothing when FixedUpdate and render frames differ
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void FixedUpdate()
    {
        if (target == null) return;

        // MovePosition is the correct way to move kinematic rigidbodies for consistent trigger events.
        _rb.MovePosition(target.position);

        if (followRotation)
            _rb.MoveRotation(target.rotation);
    }
}
