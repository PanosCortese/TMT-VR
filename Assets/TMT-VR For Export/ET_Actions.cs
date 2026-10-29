using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ET_Actions : MonoBehaviour
{
    public TMTVRManager tmtVR;

    // Parent object (the one that contains BOTH this object and the sibling "Selection")
    public GameObject Parent;

    public string targetTag = "YourTargetTag";

    // The sibling GameObject named "Selection" under Parent
    public GameObject childObject;

    public AudioClip soundEnter;
    public AudioClip soundConfirm;
    public AudioClip soundFalse;
    public AudioSource audioSource;
    public float confirmationTime = 0f;

    private List<float> accurs = new List<float>();
    private float accur;
    private float timeInTrigger = 0f;
    private bool isObjectInside = false;
    private Coroutine flickerCoroutine;
    private bool selected = false;
    private Color originalColor;
    private bool timeSet = false;
    private float timeStart = 0f;
    private bool tmtVRSet = false;

    // ------------------------------------------------------------
    // Config: sibling name to find under the same Parent
    // ------------------------------------------------------------
    private const string SelectionSiblingName = "Selection";

    // ------------------------------------------------------------
    // IMPORTANT: Keep runtime safe even if someone forgets to press the button.
    // This will only assign if fields are null (won’t override manual assignments).
    // ------------------------------------------------------------
    private void Awake()
    {
        EnsureWiringIfMissing();
    }

    private void Start()
    {
        EnsureWiringIfMissing();

        // Cache original color safely
        if (TryGetComponent<MeshRenderer>(out var mr))
            originalColor = mr.material.color;
    }

    private void OnEnable()
    {
        EnsureWiringIfMissing();

        accur = 0f;
        accurs.Clear();
        selected = false;

        // Prevent NullReference if childObject wasn't assigned for any reason
        if (childObject != null)
            childObject.SetActive(false);

        // Refresh parent + audio safely
        if (TryGetComponent<MeshRenderer>(out var mr))
            originalColor = mr.material.color;
    }

    // ------------------------------------------------------------
    // This is what the Editor button will call.
    // Safe to call anytime, in edit mode or play mode.
    // ------------------------------------------------------------
    public void AssignSelectionSiblingAndDependencies(bool logWarnings = true)
    {
        // 1) Parent = transform.parent
        if (transform.parent == null)
        {
            if (logWarnings)
                Debug.LogWarning($"[ET_Actions] '{name}' has no parent Transform. Cannot locate sibling '{SelectionSiblingName}'.", this);
            return;
        }

        Parent = transform.parent.gameObject;

        // 2) AudioSource on Parent (if present)
        audioSource = Parent.GetComponent<AudioSource>();
        if (audioSource == null && logWarnings)
            Debug.LogWarning($"[ET_Actions] No AudioSource found on Parent '{Parent.name}'. (This may be OK if handled elsewhere.)", this);

        // 3) Find sibling named "Selection" under Parent (direct child search)
        Transform selectionT = Parent.transform.Find(SelectionSiblingName);
        if (selectionT == null)
        {
            if (logWarnings)
                Debug.LogWarning($"[ET_Actions] Could not find '{SelectionSiblingName}' under Parent '{Parent.name}'. Check the hierarchy/name.", this);
            childObject = null;
            return;
        }

        childObject = selectionT.gameObject;
    }

    // ------------------------------------------------------------
    // Runtime fallback: assign ONLY when missing (won’t override).
    // This avoids accidental prefab churn but prevents runtime null refs.
    // ------------------------------------------------------------
    private void EnsureWiringIfMissing()
    {
        bool needsParent = (Parent == null);
        bool needsAudio = (audioSource == null);
        bool needsChild = (childObject == null);

        if (!needsParent && !needsAudio && !needsChild)
            return;

        // Call the explicit assign method but without noisy warnings in play mode.
        AssignSelectionSiblingAndDependencies(logWarnings: false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag) && !selected)
        {
            Debug.Log($"[ET_Actions] '{name}' detected entry of target '{other.name}'. Starting confirmation timer.", this);
            isObjectInside = true;
            accurs.Clear();
            timeSet = false;
            tmtVRSet = false;
            flickerCoroutine = StartCoroutine(FlickerChild());
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(targetTag) && !selected)
        {
            Debug.Log($"[ET_Actions] '{name}' is still detecting target '{other.name}'. Time in trigger: {timeInTrigger:F2}s", this);
            if (!timeSet)
            {
                // audioSource?.PlayOneShot(soundEnter); // optional
                timeStart = Time.time;
                timeSet = true;
            }

            accur = Vector3.Distance(other.transform.position, transform.position);
            accurs.Add(accur);

            timeInTrigger = Time.time;

            if (timeInTrigger >= timeStart + confirmationTime && !tmtVRSet)
            {
                if (Parent == tmtVR.CurrentTarget)
                {
                    if (audioSource != null && soundConfirm != null)
                        audioSource.PlayOneShot(soundConfirm);

                    if (childObject != null)
                        childObject.SetActive(true);

                    selected = true;
                    if (tmtVR.TargetsSelected > 0)
                    {
                        tmtVR.accuracy = accurs.Average();
                    }
                    else                     {
                        tmtVR.accuracy = 0f; // First target, no accuracy to record
                    }
                    tmtVR.OnTargetSelected();
                }
                else
                {
                    if (TryGetComponent<MeshRenderer>(out var mr))
                        mr.material.color = Color.red;

                    if (audioSource != null && soundFalse != null)
                        audioSource.PlayOneShot(soundFalse);

                    tmtVR.totalMistakes += 1;
                }

                tmtVRSet = true;

                if (flickerCoroutine != null)
                {
                    StopCoroutine(flickerCoroutine);
                    flickerCoroutine = null;
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            Debug.Log($"[ET_Actions] '{name}' detected exit of target '{other.name}'. Resetting state.", this);
            timeSet = false;
            tmtVRSet = false;
            isObjectInside = false;

            if (flickerCoroutine != null)
            {
                StopCoroutine(flickerCoroutine);
                flickerCoroutine = null;
            }

            if (!selected && childObject != null)
                childObject.SetActive(false);

            timeInTrigger = 0f;

            if (TryGetComponent<MeshRenderer>(out var mr))
                mr.material.color = originalColor;
        }
    }

    private IEnumerator FlickerChild()
    {
        // If not wired, do nothing (avoids spammy exceptions)
        if (childObject == null)
            yield break;

        while (isObjectInside)
        {
            childObject.SetActive(!childObject.activeSelf);
            yield return new WaitForSeconds(0.1f);
        }
    }
}
