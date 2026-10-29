using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // Ensure you have the TextMesh Pro namespace

public class TargetTextAssigner : MonoBehaviour
{
    public TargetPlacer targetPlacer;
    public bool isTMT_VR_PartA = false;
    public bool isTMT_VR_PartB = false;
    public bool isEnglish = true; // default to English
    public bool isGreek = false;
    public bool textAssigned;

    private string[] englishAlphabet = new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
    private string[] greekAlphabet = new string[] { "Α", "Β", "Γ", "Δ", "Ε", "Ζ", "Η", "Θ", "Ι", "Κ", "Λ", "Μ", "Ν", "Ξ", "Ο", "Π", "Ρ", "Σ", "Τ", "Υ", "Φ", "Χ", "Ψ", "Ω" };

    private void OnEnable()
    {
        if (isTMT_VR_PartA)
        {
            isTMT_VR_PartB = false;
        }
        StartCoroutine(CheckAndAssignTextCoroutine());
    }
    IEnumerator CheckAndAssignTextCoroutine()
    {
        // Wait until the placement is done
        yield return new WaitUntil(() => targetPlacer.PlacementDone);

        List<GameObject> orderedTargets = targetPlacer.orderedTargets;
        AssignTextToTargets(orderedTargets);

    }

    private void AssignTextToTargets(List<GameObject> targets)
    {
        if (!isTMT_VR_PartA && !isTMT_VR_PartB)
        {
            Debug.LogError("Please select either TMT_VR_PartA or TMT_VR_PartB.");
            return;
        }

        if (isEnglish == isGreek)
        {
            Debug.LogError("Please select only one language: English or Greek.");
            return;
        }

        string[] selectedAlphabet = isEnglish ? englishAlphabet : greekAlphabet;

        // Adjusted logic for alternation
        int number = 1; // Start with 1 for numbers
        int letterIndex = 0; // Start with the first letter of the alphabet

        for (int i = 0; i < targets.Count; i++)
        {
            GameObject target = targets[i];
            TextMeshProUGUI[] textMeshes = target.GetComponentsInChildren<TextMeshProUGUI>();


            if (isTMT_VR_PartA)
            {
                string text;
                text = number.ToString();
                number++; // Prepare the next number for use
                          // Assign text to all TextMeshProUGUI components in the current target
                foreach (var textMesh in textMeshes)
                {
                    textMesh.text = text;
                }
            }
            else if (isTMT_VR_PartB)
            {
                string text;
                // Determine whether to use a number or letter based on the iteration
                if (i % 2 == 0) // Even indices get numbers
                {
                    text = number.ToString();
                    number++; // Prepare the next number for use
                }
                else // Odd indices get letters
                {
                    text = selectedAlphabet[letterIndex];
                    letterIndex = (letterIndex + 1) % selectedAlphabet.Length; // Cycle through the alphabet
                }

                // Assign text to all TextMeshProUGUI components in the current target
                foreach (var textMesh in textMeshes)
                {
                    textMesh.text = text;
                }
            }

        }
    }
}
