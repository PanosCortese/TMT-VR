using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class TMTVRManager : MonoBehaviour
{
    public TargetPlacer targetPlacer;
    public List<GameObject> Targets = new List<GameObject>(); // The 25 targets to be selected
    public GameObject CurrentTarget; // The target that needs to be currently selected
    public GameObject PreviousTarget; // The last target that was selected
    public GameObject NextTarget; // The next target that needs to be selected
    public int CurrentTargetIndex = 0; // The index of the current target in the list of targets
    public int TargetsSelected = 0; // The number of targets that have been selected
    // Line rendering between previous and current target
    public Color lineColor = Color.yellow; // Public feature to adjust the color in the Unity Editor
    public float lineWidth = 0.1f; // Public feature to adjust the thickness in the Unity Editor
    private LineRenderer lineRenderer;

    // Timing and accuracy data
    public float previousSelectionTime = 0f; // Time that the previous target was selected
    public List<float> selectionTimes = new List<float>(); // Time required for each target to be selected
    public float selectionTime; // Time required for the current target to be selected
    public float AverageSelectionTime; // Average time required for selecting all targets
    public float totalSelectionTime = 0f; // Total time required for selecting all targets
    public List<float> accuracies = new List<float>(); // Accuracy of the selection of each target
    public float accuracy; // Accuracy of the current target selection
    public float AverageAccuracy; // Average accuracy of all selections
    public int totalMistakes = 0; // Total number of mistakes made
    public bool Completed = false;
    private bool listPopulated = false;
    private bool calculated = false;
    public float startingTime;
    private void OnEnable()
    {
        PreviousTarget = null;
        calculated = false;
        listPopulated = false;
        TargetsSelected = 0;
        totalMistakes = 0;
        selectionTimes.Clear();
        accuracies.Clear();
        totalSelectionTime = 0f;
        CurrentTargetIndex = 0;
        Targets.Clear();
        Completed = false;
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.positionCount = 2; // Since we're only drawing a line between two points
        }

        StartCoroutine(WaitForPlacementAndPopulateList());
    }
    IEnumerator WaitForPlacementAndPopulateList()
    {
        // Wait until placement is done and Targets list is empty and not yet populated
        yield return new WaitUntil(() => targetPlacer.PlacementDone && Targets.Count == 0 && !listPopulated);

        // Once the conditions are met, proceed with the logic
        Targets = targetPlacer.orderedTargets;
        CurrentTarget = Targets[CurrentTargetIndex];
        NextTarget = Targets[CurrentTargetIndex + 1];
        listPopulated = true;

        // Additional logic to handle targets can be placed here
    }

    // Call this method when a target is selected to update timing and accuracy data
    public void OnTargetSelected()
    {
        float now = Time.time;

        // --- Timing: store 24 link-times (not 25 target-times) ---
        if (TargetsSelected == 0)
        {
            // First selection: arm the timer, do NOT record a delta
            startingTime = now;
            previousSelectionTime = now;
        }
        else
        {
            // Subsequent selections: record delta since previous correct selection
            selectionTime = now - previousSelectionTime;
            previousSelectionTime = now;
            // Store the selection time for this link (24 values is fine)
            selectionTimes.Add(selectionTime);
            // Accuracy is also recorded here (24 values is fine)
            accuracies.Add(accuracy);
        }

        TargetsSelected += 1;


        // Example of how to draw a line between the PreviousTarget and CurrentTarget
        // This assumes that the PreviousTarget and CurrentTarget are set elsewhere in the code when a target is selected
        if (PreviousTarget != null && CurrentTarget != null)
        {
            // Create a new GameObject to hold the LineRenderer component for this line
            GameObject lineObject = new GameObject("Line" + TargetsSelected);
            lineObject.transform.SetParent(this.transform); // Optional: Set as a child of the manager for organization

            // Add a LineRenderer to the new GameObject and configure it
            LineRenderer newLineRenderer = lineObject.AddComponent<LineRenderer>();
            newLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            newLineRenderer.startColor = lineColor;
            newLineRenderer.endColor = lineColor;
            newLineRenderer.startWidth = lineWidth;
            newLineRenderer.endWidth = lineWidth;
            newLineRenderer.positionCount = 2;

            // Set the positions to draw the line between the PreviousTarget and CurrentTarget
            newLineRenderer.SetPosition(0, PreviousTarget.transform.position);
            newLineRenderer.SetPosition(1, CurrentTarget.transform.position);
        }
        //accuracies.Add(accuracy);
        //selectionTimes.Add(selectionTime);
        //totalSelectionTime += selectionTime;
        PreviousTarget = CurrentTarget;
        CurrentTarget = NextTarget;
        CurrentTargetIndex += 1;
        if (CurrentTargetIndex < Targets.Count - 1)
            NextTarget = Targets[CurrentTargetIndex + 1];
        if (targetPlacer.Tutorial)
        {
            if (TargetsSelected == 8)
            {
                Destroy(lineRenderer);
                targetPlacer.ResetPlacement();
                Completed = true;
            }
        }
        else if (TargetsSelected == 25 && !calculated)
        {
            totalSelectionTime = Time.time - startingTime; // New Code
            CalculateAverageSelectionTime();
            CalculateAverageAccuracy();
            calculated = true;
            Destroy(lineRenderer);
            targetPlacer.ResetPlacement();
            Completed = true;
        }
    }

    // Call these methods to calculate and get the average selection time and accuracy
    public void CalculateAverageSelectionTime()
    {
        // Use the count of selectionTimes for the average, which should be 24 for 25 targets
        // First selection does not have a selection time value, so it is not included in the average
        AverageSelectionTime = (selectionTimes.Count > 0) ? (totalSelectionTime / selectionTimes.Count) : 0f;

    }

    public void CalculateAverageAccuracy()
    {
        // Use the count of accuracies for the average, which should be 24 for 25 targets;
        // First selection does not have an accuracy value, so it is not included in the average
        AverageAccuracy = (accuracies.Count > 0) ? accuracies.Average() : 0f;
    }
}
