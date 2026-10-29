using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyChildrenWithLineRenderer : MonoBehaviour
{
    private void OnDisable()
    {
        // Get all children with a LineRenderer component
        LineRenderer[] childrenWithLineRenderer = GetComponentsInChildren<LineRenderer>();

        // Loop through each child with a LineRenderer component
        foreach (var child in childrenWithLineRenderer)
        {
            // Check if the GameObject is not null
            if (child != null)
            {
                // Destroy the GameObject associated with the LineRenderer
                Destroy(child.gameObject);
            }
        }
    }
}
