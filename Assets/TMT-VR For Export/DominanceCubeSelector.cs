using UnityEngine;

public class DominanceCubeSelector : MonoBehaviour
{
    [Header("Assign your pointer here")]
    public OpenXRAimPointer aimPointer;

    [Header("Lock after first valid touch")]
    public bool lockAfterSelect = true;

    public bool Selected { get; private set; }
    private GameObject probeTip;
    private void OnTriggerEnter(Collider other)
    {
        if (Selected) return;

        // Accept probes from children too
        TipProbeId probe = other.GetComponentInParent<TipProbeId>();
        if (probe == null) return;
        if (probe.trackedTip == null) return;

        probeTip = other.gameObject;
        // Decide what to assign based on current aim mode
        if (aimPointer.Hand  && probe.kind == TipProbeId.ProbeKind.FinTip)
        {
            aimPointer.fingertip = probe.trackedTip;
            Selected = true;
            Debug.Log($"[Dominance] Hand selected: {probe.side}", this);
        }
        else if (aimPointer.Controller && probe.kind == TipProbeId.ProbeKind.ConTip)
        {
            aimPointer.controllerTip = probe.trackedTip;
            Selected = true;
            Debug.Log($"[Dominance] Controller selected: {probe.side}", this);
        }

        if (Selected && lockAfterSelect)
        {
            // Hide cube / stop further triggers
            gameObject.SetActive(false);
        }
    }
    private void OnDisable()
    {
        if (probeTip != null)
        {
            probeTip.SetActive(false);
        }
    }
}
