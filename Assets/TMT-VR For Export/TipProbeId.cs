using UnityEngine;

public class TipProbeId : MonoBehaviour
{
    public enum ProbeKind { FinTip, ConTip }
    public enum Side { Left, Right }

    [Header("What is this probe?")]
    public ProbeKind kind;
    public Side side;

    [Header("The tracked transform this probe follows (same as FollowTargetPhysics.target)")]
    public Transform trackedTip;  // drag the SAME transform you set in FollowTargetPhysics.target
}
