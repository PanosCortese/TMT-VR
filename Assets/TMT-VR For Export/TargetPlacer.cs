// Places ordered TMT-VR targets using the existing candidate, band and selection rules, with bounded retries for failed sorts and optional seeded layout replay.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;   // unused; kept from the version of record so that the diff stays minimal

[System.Serializable]
public class TargetGroup
{
    public List<GameObject> targets = new List<GameObject>();
}

public class TargetPlacer : MonoBehaviour
{
    // ---------------- serialised fields of the version of record (unchanged, same order) ----------------
    [Header("Target Group Parents")]
    public GameObject group1Parent;
    public GameObject group2Parent;
    public GameObject group3Parent;
    public GameObject group4Parent;
    public GameObject group5Parent;
    public GameObject group6Parent;

    [Header("Target Groups")]
    public TargetGroup group1 = new TargetGroup();
    public TargetGroup group2 = new TargetGroup();
    public TargetGroup group3 = new TargetGroup();
    public TargetGroup group4 = new TargetGroup();
    public TargetGroup group5 = new TargetGroup();
    public TargetGroup group6 = new TargetGroup();

    [Header("Settings")]
    public bool Tutorial = false; // Toggle tutorial mode

    public GameObject firstTarget;
    public List<GameObject> orderedTargets;
    public List<float> averageDistances;
    public bool PlacementDone;
    public float longestDistance = float.MinValue;
    public float shortestDistance = float.MaxValue;

    // ---------------- Seed, replay and retry settings ----------------
    // New serialised fields are appended AFTER the fields of record, so existing prefab data loads
    // unchanged; Unity gives the new fields their default values below.
    [Header("Seed, replay and retry")]
    // When true, the next placement uses replaySeed instead of drawing a fresh seed. The
    // Inspector sets these two fields BEFORE the placer is enabled.
    public bool useReplaySeed = false;
    public int replaySeed = 0;
    // Upper bound on restarts after an inconsistent-comparer exception. The exception occurs in about
    // 2.4 x 10^-4 of placements, so 20 attempts is never reached in practice.
    public int maxPlacementAttempts = 20;

    // ---------------- NEW: read-only record of the last placement (not serialised) ----------------
    // Public fields rather than properties so that loggers and Editor tools can read them directly.
    [System.NonSerialized] public int lastLayoutSeed;            // root seed of the last placement (logged)
    [System.NonSerialized] public int lastAttempts;              // 1 = first attempt succeeded
    [System.NonSerialized] public int lastBadComparerExceptions; // exceptions caught and retried
    [System.NonSerialized] public bool lastPlacementFailed;      // true only if every attempt failed

    // Which scripting backend compiled this code. ENABLE_IL2CPP is defined by Unity for IL2CPP
    // players; the Editor and Mono players leave it undefined. Not read by any script in this release.
#if ENABLE_IL2CPP
    public const string ScriptingBackend = "IL2CPP";
#else
    public const string ScriptingBackend = "Mono";
#endif

    // Raised after every successful placement (no subscriber in this release; adds no behaviour by itself).
    public static System.Action<TargetPlacer> LayoutPlaced;

    // The single source of seeds for every System.Random used during one placement.
    private System.Random seedSource;

    // Thrown only by SortWithRandomComparer() to request a restart of the placement.
    private sealed class BadComparerException : System.Exception
    {
        public BadComparerException(System.Exception inner) : base("Inconsistent random comparer in List.Sort", inner) { }
    }

    private Dictionary<string, (float min, float max)> distanceCategories = new Dictionary<string, (float, float)>
    {
        {"DistanceVeryShort", (1f, 1.99f)},
        {"DistanceShort", (2f, 2.99f)},
        {"DistanceShortMedium", (3f, 3.99f)},
        {"DistanceLongMedium", (4f, 4.99f)},
        {"DistanceLong", (5f, 5.99f)},
        {"DistanceVeryLong", (6f, 6.99f)}
    };

    private void OnEnable()
    {
        // ---- choose and log the root seed of this placement ----
        // Not replaying: draw a fresh seed. `new System.Random().Next()` takes it from the class
        // library's own hidden per-thread generator, i.e. from the same source the version of record
        // used for every generator; the only difference is that we now keep the value.
        lastLayoutSeed = useReplaySeed ? replaySeed : new System.Random().Next();
        seedSource = new System.Random(lastLayoutSeed);
        lastAttempts = 0;
        lastBadComparerExceptions = 0;
        lastPlacementFailed = false;

        // ---- attempt the placement; restart only on the inconsistent-comparer exception ----
        while (true)
        {
            lastAttempts++;
            ResetGroupsAndLists();            // lines 51-69 of the version of record, unchanged
            try
            {
                PlaceTargets();               // the algorithm of record (seeded generators, wrapped sorts)
                break;                        // placed: leave the loop
            }
            catch (BadComparerException e)
            {
                lastBadComparerExceptions++;
                // A warning, not an error: the restart is the designed behaviour. The message carries the
                // seed and attempt so that the event can be traced in the device log.
                Debug.LogWarning($"TargetPlacer: inconsistent comparer caught (seed {lastLayoutSeed}, attempt {lastAttempts}); restarting placement. {e.InnerException?.GetType().Name}");
                if (lastAttempts >= maxPlacementAttempts)
                {
                    // Practically unreachable. Behaves as the version of record did on a failure:
                    // PlacementDone stays false and nothing is activated; the flag lets a manager react.
                    lastPlacementFailed = true;
                    Debug.LogError($"TargetPlacer: placement failed after {lastAttempts} attempts (seed {lastLayoutSeed}).");
                    return;
                }
                // Partial attempts activate nothing (targets are only activated in FinalizePlacement),
                // so there is nothing to undo on screen; ResetGroupsAndLists() restores the candidates.
            }
        }

        // Announce the finished layout to any logger (no subscriber = no effect).
        if (PlacementDone && orderedTargets.Count == (Tutorial ? 7 : 24) + 1)
            LayoutPlaced?.Invoke(this);
    }

    // Exactly the body of OnEnable() lines 51-69 of the version of record, moved into a method so that a
    // restart starts from the same state as a fresh enable. The order of statements is unchanged.
    private void ResetGroupsAndLists()
    {
        group1.targets.Clear();
        group2.targets.Clear();
        group3.targets.Clear();
        group4.targets.Clear();
        group5.targets.Clear();
        group6.targets.Clear();
        PlacementDone = false;
        orderedTargets.Clear();
        averageDistances.Clear();
        PopulateListWithChildren(group1Parent, group1.targets);
        PopulateListWithChildren(group2Parent, group2.targets);
        PopulateListWithChildren(group3Parent, group3.targets);
        PopulateListWithChildren(group4Parent, group4.targets);
        PopulateListWithChildren(group5Parent, group5.targets);
        PopulateListWithChildren(group6Parent, group6.targets);
        orderedTargets = new List<GameObject>();
        averageDistances = new List<float>();
        longestDistance = float.MinValue;
        shortestDistance = float.MaxValue;
    }

    void PopulateListWithChildren(GameObject parent, List<GameObject> list)
    {
        foreach (Transform child in parent.transform)
        {
            list.Add(child.gameObject);
        }
    }

    // The same random-comparer sort as lines 99 and 101 of the version of record:
    // identical List.Sort call, identical lambda, identical generator. Only the exception is caught.
    // ArgumentException is what Unity's class library throws (ThrowOrIgnoreBadComparer); the
    // InvalidOperationException branch covers runtimes that report the fault that way.
    private static void SortWithRandomComparer(List<string> keys, System.Random rand)
    {
        try
        {
            keys.Sort((a, b) => rand.Next(-1, 2));
        }
        catch (System.ArgumentException e) { throw new BadComparerException(e); }
        catch (System.InvalidOperationException e) { throw new BadComparerException(e); }
    }

    void PlaceTargets()
    {
        orderedTargets.Clear();
        averageDistances.Clear();
        // WAS: System.Random rand = new System.Random();
        // NOW: the seed comes from the logged seedSource instead of the hidden per-thread generator.
        System.Random rand = new System.Random(seedSource.Next());
        List<string> distanceKeys = new List<string>(distanceCategories.Keys);
        List<string> groupKeys = GenerateGroupKeys();

        orderedTargets.Add(firstTarget);
        GameObject lastTarget = firstTarget;

        int targetsToPlace = Tutorial ? 7 : 24; // Place only 7 targets in tutorial mode, otherwise 24

        for (int i = 0; i < targetsToPlace; i++)
        {
            bool targetPlaced = false;

            List<string> shuffledGroupKeys = new List<string>(groupKeys);
            SortWithRandomComparer(shuffledGroupKeys, rand);      // WAS: shuffledGroupKeys.Sort((a, b) => rand.Next(-1, 2));
            List<string> shuffledDistanceKeys = new List<string>(distanceKeys);
            SortWithRandomComparer(shuffledDistanceKeys, rand);   // WAS: shuffledDistanceKeys.Sort((a, b) => rand.Next(-1, 2));

            foreach (var groupKey in shuffledGroupKeys)
            {
                int groupIndex = int.Parse(groupKey.Substring(5, 1)) - 1;
                TargetGroup currentGroup = GetGroupByIndex(groupIndex);

                foreach (var distanceKey in shuffledDistanceKeys)
                {
                    (float min, float max) = distanceCategories[distanceKey];
                    GameObject selectedTarget = SelectTargetFromGroup(lastTarget, currentGroup.targets, min, max);
                    if (selectedTarget != null)
                    {
                        orderedTargets.Add(selectedTarget);
                        lastTarget = selectedTarget;
                        targetPlaced = true;
                        break;
                    }
                }
                if (targetPlaced) break;
            }

            if (!targetPlaced)
            {
                Debug.LogError("Failed to place all targets. Adjust criteria or increase pool of potential targets.");
                break;
            }
        }

        PlacementDone = true;
        if (orderedTargets.Count == targetsToPlace + 1) // +1 accounts for the first target
        {
            FinalizePlacement();
        }
        else
        {
            Debug.LogError("Could not place the required number of targets.");
        }

        // After all targets are placed, calculate the longest and shortest distances
        for (int i = 0; i < orderedTargets.Count - 1; i++)
        {
            float distance = Vector3.Distance(orderedTargets[i].transform.position, orderedTargets[i + 1].transform.position);
            // Update longest and shortest distances if necessary
            if (distance > longestDistance)
            {
                longestDistance = distance;
            }
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
            }
        }
        // Calculate and store average distances, activate targets, etc., as required
        PlacementDone = true;
    }

    List<string> GenerateGroupKeys()
    {
        List<string> keys = new List<string>();
        for (int i = 1; i <= 6; i++)
        {
            for (char c = 'a'; c <= 'd'; c++)
            {
                keys.Add($"Group{i}{c}");
            }
        }
        return keys;
    }

    TargetGroup GetGroupByIndex(int index)
    {
        switch (index)
        {
            case 0: return group1;
            case 1: return group2;
            case 2: return group3;
            case 3: return group4;
            case 4: return group5;
            case 5: return group6;
            default: return null; // Should not happen
        }
    }

    GameObject SelectTargetFromGroup(GameObject lastTarget, List<GameObject> group, float minDistance, float maxDistance)
    {
        // WAS: System.Random rand = new System.Random();
        // NOW: seeded from the same logged seedSource, in the same call order as the class library's
        // hidden generator would have been consulted, so the pick is identical for the same root seed.
        System.Random rand = new System.Random(seedSource.Next());
        // Filter for possible targets based on the distance criteria
        List<GameObject> possibleTargets = group.FindAll(target =>
            Vector3.Distance(lastTarget.transform.position, target.transform.position) >= minDistance &&
            Vector3.Distance(lastTarget.transform.position, target.transform.position) <= maxDistance &&
            !orderedTargets.Contains(target));

        if (possibleTargets.Count > 0)
        {
            // Select a random target from those that meet the criteria
            GameObject selectedTarget = possibleTargets[rand.Next(possibleTargets.Count)];
            // Remove the selected target from the original group to prevent reselection
            group.Remove(selectedTarget);
            return selectedTarget;
        }
        return null; // Return null if no suitable target is found
    }

    // After placing all targets, we calculate average distances and activate targets
    void FinalizePlacement()
    {
        CalculateAverageDistances();
        ActivateTargets();
    }

    void CalculateAverageDistances()
    {
        averageDistances.Clear(); // Ensure the list is empty before starting

        // Initialize sum and count for each distance category
        Dictionary<string, (float sum, int count)> distanceSumsAndCounts = new Dictionary<string, (float, int)>();
        foreach (var key in distanceCategories.Keys)
        {
            distanceSumsAndCounts[key] = (0f, 0);
        }

        // Calculate sums and counts
        for (int i = 0; i < orderedTargets.Count - 1; i++)
        {
            float distance = Vector3.Distance(orderedTargets[i].transform.position, orderedTargets[i + 1].transform.position);
            string category = GetDistanceCategory(distance);
            if (category != null)
            {
                distanceSumsAndCounts[category] = (distanceSumsAndCounts[category].sum + distance, distanceSumsAndCounts[category].count + 1);
            }
        }

        // Calculate and store averages
        foreach (var key in distanceCategories.Keys)
        {
            var (sum, count) = distanceSumsAndCounts[key];
            float average = count > 0 ? sum / count : 0;
            averageDistances.Add(average);
        }
    }

    void ActivateTargets()
    {
        foreach (GameObject target in orderedTargets)
        {
            target.SetActive(true);
        }
    }

    string GetDistanceCategory(float distance)
    {
        foreach (var category in distanceCategories)
        {
            if (distance >= category.Value.min && distance <= category.Value.max)
            {
                return category.Key;
            }
        }
        return null;
    }

    public void ResetPlacement()
    {
        // Loop through each GameObject in the orderedTargets list and deactivate them
        foreach (GameObject target in orderedTargets)
        {
            if (target != null) // Check if the GameObject reference is not null to avoid errors
            {
                target.SetActive(false);
            }
        }
    }
}
