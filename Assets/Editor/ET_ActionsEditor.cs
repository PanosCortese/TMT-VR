#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ET_Actions))]
[CanEditMultipleObjects]
public class ET_ActionsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw all the normal fields first (so your Inspector stays familiar)
        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Assigns:\n" +
            "• Parent = transform.parent\n" +
            "• AudioSource = Parent.GetComponent<AudioSource>()\n" +
            "• childObject = sibling named 'Selection' under Parent\n\n" +
            "Works in Edit Mode. Select one or multiple objects and press the button.",
            MessageType.Info
        );

        if (GUILayout.Button("Assign Selection Sibling"))
        {
            // Multi-object friendly: apply to all selected targets
            foreach (var obj in targets)
            {
                var script = (ET_Actions)obj;

                // Register Undo (so Ctrl+Z works)
                Undo.RecordObject(script, "Assign Selection Sibling");

                // Perform assignment with warnings enabled (Editor feedback)
                script.AssignSelectionSiblingAndDependencies(logWarnings: true);

                // Mark dirty so Unity saves changes to scene/prefab
                EditorUtility.SetDirty(script);

                // If the object is part of a prefab instance, record modifications
                PrefabUtility.RecordPrefabInstancePropertyModifications(script);
            }
        }
    }
}
#endif
