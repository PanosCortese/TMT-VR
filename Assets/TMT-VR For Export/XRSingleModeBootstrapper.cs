using UnityEngine;

public class XRSingleModeBootstrapper : MonoBehaviour
{
    [Header("Your pointer")]
    public OpenXRAimPointer aimPointer;

    [Header("Tracking roots (keep ON only if needed for this mode)")]
    public GameObject controllersTrackingRoot; // must be ON for Controller mode
    public GameObject handsTrackingRoot;       // must be ON for Hand mode

    [Header("XRI Gaze root")]
    public GameObject xriGazeRoot;

    public TMTVR_CentralExecutive ceo; // optional, just to call Apply() from there if you want

    public void Apply()
    {
        if (ceo != null)
        {
            if (aimPointer != null)
            {
                if (aimPointer.Eye)
                {
                    SafeSetActive(xriGazeRoot, true);
                    SafeSetActive(controllersTrackingRoot, false);
                    SafeSetActive(handsTrackingRoot, false);
                }
                else if (aimPointer.Hand)
                {
                    SafeSetActive(xriGazeRoot, false);
                    SafeSetActive(controllersTrackingRoot, false);
                    SafeSetActive(handsTrackingRoot, true);
                }
                else if (aimPointer.Controller)
                {
                    SafeSetActive(xriGazeRoot, false);
                    SafeSetActive(controllersTrackingRoot, true);
                    SafeSetActive(handsTrackingRoot, false);
                }
                else if (aimPointer.Head)
                {
                    SafeSetActive(xriGazeRoot, false);
                    SafeSetActive(controllersTrackingRoot, false);
                    SafeSetActive(handsTrackingRoot, false);
                }
                else 
                {
                    Debug.Log("No Interaction Mode Detected");
                }
            }
        }
    }

    private static void SafeSetActive(GameObject go, bool active)
    {
        if (go == null) return;
        if (go.activeSelf == active) return;
        go.SetActive(active);
    }
}
