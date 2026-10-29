using System.Collections;
using System.Collections.Generic;
using System.Globalization;   // ✅ Needed for InvariantCulture formatting
using System.IO;
using System.Text;
using UnityEngine;
using System;


public class TMTVR_ScoreManager : MonoBehaviour
{
    [SerializeField] private bool autoAddMetadataColumns = true;

    private void EnsureColumn(string name, VariableEntry.VariableType type)
    {
        if (variables == null) variables = new List<VariableEntry>();
        if (variables.Exists(v => v.variableName == name)) return;

        variables.Add(new VariableEntry
        {
            variableName = name,
            variableType = type
        });
    }

    [SerializeField] private bool warnOnUnknownColumns = true;
    private readonly HashSet<string> _warnedMissingKeys = new HashSet<string>();
    private void WarnIfKeyNotInSchema(string key)
    {
        if (!warnOnUnknownColumns || variables == null) return;

        for (int i = 0; i < variables.Count; i++)
            if (variables[i].variableName == key) return;

        // warn only once per key
        if (_warnedMissingKeys.Add(key))
            Debug.LogWarning($"[TMTVR_ScoreManager] Key '{key}' has no matching column in variables list. It will NOT be written to CSV.");
    }

    [System.Serializable]
    public class VariableEntry
    {
        public string variableName;
        public enum VariableType { Int, Float, String }
        public VariableType variableType;
    }

    public List<VariableEntry> variables = new List<VariableEntry>();

    private Dictionary<string, string> values = new Dictionary<string, string>();
    private List<Dictionary<string, string>> rows = new List<Dictionary<string, string>>();
    /// <summary>
    /// 
    /// </summary>
    public TMTVRManager tmtvrManagerTMTA;
    public TMTVRManager tmtvrManagerTMTB;
    public TMTVR_TestManager testManager;
    /// <summary>
    /// 
    /// </summary>
    // Path for the CSV file
    public string csvFilePath;
    public string fileName;
    public int SessionID;
    public int TrialNumber = 0; // May be deleted
    public bool CSVInitialization = false;
    private void Start()
    {
        TrialNumber = 0;
        // Key for storing the ID
        string idKey = "uniqueID";

        // Check if the ID already exists
        if (PlayerPrefs.HasKey(idKey))
        {
            // ID exists, retrieve it, increment, and save
            int currentID = PlayerPrefs.GetInt(idKey);
            currentID++; // Increment the ID
            PlayerPrefs.SetInt(idKey, currentID); // Save the new ID
        }
        else
        {
            // ID does not exist, this is the first run, set ID to 1
            PlayerPrefs.SetInt(idKey, 1); // Save the initial ID
        }

        // Always call PlayerPrefs.Save() after setting some PlayerPrefs to ensure data is written to disk
        PlayerPrefs.Save();

        // Optional: To verify the operation, log the current ID to the console
        Debug.Log("Current Session ID: " + PlayerPrefs.GetInt(idKey));

        SessionID = PlayerPrefs.GetInt(idKey);


        /* OLD CODE  WHO WORKS ON PC BUT NOT ON ANDROID, MAY BE DELETED
         * // Determine the base path depending on whether it's a development build
        string basePath = Application.isEditor ? Application.dataPath : Path.GetDirectoryName(Application.dataPath);

        // Set the path for the CSV file
        csvFilePath = Path.Combine(basePath, "scoresID" + PlayerPrefs.GetInt(idKey) + ".csv");

        // Initialize CSV file with column headers
        InitializeCSV();*/

        // Always writable on PC + Android
        string basePath = Path.Combine(Application.persistentDataPath, "TMTVR_Participants_Data");
        Directory.CreateDirectory(basePath);

        csvFilePath = Path.Combine(basePath, $"scoresID{SessionID}.csv");

        while (File.Exists(csvFilePath))
        {
            SessionID++;
            PlayerPrefs.SetInt(idKey, SessionID);
            PlayerPrefs.Save();
            csvFilePath = Path.Combine(basePath, $"scoresID{SessionID}.csv");
        }
        Debug.Log($"[TMTVR_ScoreManager] Final SessionID={SessionID} | CSV={csvFilePath}");

        if (autoAddMetadataColumns)
        {
            EnsureColumn("SessionID", VariableEntry.VariableType.Int);
            EnsureColumn("TimestampUTC", VariableEntry.VariableType.String);
            EnsureColumn("DeviceModel", VariableEntry.VariableType.String);
            EnsureColumn("DeviceType", VariableEntry.VariableType.String);
            EnsureColumn("AppVersion", VariableEntry.VariableType.String);
        }
        InitializeCSV();
        Debug.Log("CSV file path: " + csvFilePath);
    }

    // ---------------------------------------------------------------------
    // TMT scoring helpers (classic 25 circles => 24 links / selections)
    // ---------------------------------------------------------------------
    private const float LINKS_PER_PART = 24f;  // A: 24, B: 24
    private const float LINKS_TOTAL = 48f;     // A+B

    /// <summary>
    /// Error rate per part. Uses float division (no integer truncation).
    /// </summary>
    private static float ErrorRate(int errors, float links) => errors / links;

    /// <summary>
    /// "Attempt accuracy": proportion of correct attempts if each mistake reflects an additional wrong attempt.
    /// Range: (0, 1]. If errors==0 => 1.
    /// </summary>
    private static float AttemptAccuracy(int errors, float links) => links / (links + errors);

    /// <summary>
    /// Efficiency-adjusted time (inverse-efficiency style): time / accuracy.
    /// If errors==0 => returns time. Higher = worse.
    /// </summary>
    private static float AdjustedTime(float timeSec, int errors, float links)
    {
        float acc = AttemptAccuracy(errors, links); // safe for errors==0
        return timeSec / acc;
    }


    // =====================================================================
    // CSV helpers (robust across PC/Android, and safe if strings contain commas)
    // =====================================================================

    /// <summary>
    /// Escapes a value for CSV (RFC4180-ish):
    /// - Wrap in quotes if it contains comma, quote, or newline
    /// - Double-up quotes inside quoted fields
    /// </summary>
    private static string CsvEscape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";

        bool mustQuote = s.Contains(",") || s.Contains("\"") || s.Contains("\n") || s.Contains("\r");
        if (s.Contains("\""))
            s = s.Replace("\"", "\"\"");

        return mustQuote ? $"\"{s}\"" : s;
    }

    /// <summary>
    /// Writes text to file safely using UTF8. Uses try/catch so you don't lose the session silently.
    /// </summary>
    private void AppendAllTextUtf8Safe(string path, string content)
    {
        try
        {
            File.AppendAllText(path, content, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TMTVR_ScoreManager] Failed to append to CSV at: {path}\n{ex}");
        }
    }



    /// <summary>
    /// Safe float division: returns NaN if denominator is 0 (or ~0).
    /// We will later serialize NaN to "NA" in UpdateVariable.
    /// </summary>
    private static float SafeDiv(float num, float den)
    {
        return Mathf.Abs(den) < 1e-6f ? float.NaN : (num / den);
    }

    public IEnumerator SaveScore()
    {

        // ------------------------------------------------------------
        // Defensive checks: avoid writing empty rows if refs are missing
        // ------------------------------------------------------------
        if (!CSVInitialization)
        {
            Debug.LogError("[TMTVR_ScoreManager] SaveScore called before CSVInitialization.");
            yield break;
        }

        if (tmtvrManagerTMTA == null || tmtvrManagerTMTB == null)
        {
            Debug.LogError("[TMTVR_ScoreManager] Missing TMTA/TMTB manager references. Score not saved.");
            yield break;
        }

        // ================================================================
        // SAFETY GUARD: Only save after the official (2nd) real run
        // ================================================================
        if (testManager == null)
        {
            Debug.LogError("[TMTVR_ScoreManager] testManager reference missing. Cannot verify official runs.");
            yield break;
        }

        if (testManager.RealRunsA_Completed < 2 || testManager.RealRunsB_Completed < 2)
        {
            Debug.LogError($"[TMTVR_ScoreManager] runs not completed: " +
                           $"RealRunsA={testManager.RealRunsA_Completed}, RealRunsB={testManager.RealRunsB_Completed}");
        }

        // ------------------------------------------------------------
        // metadata
        // ------------------------------------------------------------
        UpdateVariable("TimestampUTC", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
        UpdateVariable("DeviceModel", SystemInfo.deviceModel);
        UpdateVariable("DeviceType", SystemInfo.deviceType.ToString());
        UpdateVariable("AppVersion", Application.version);


        // ================================================================
        // 1) RAW INPUTS (from managers)
        // ================================================================
        // A_time and B_time: total run time of Parts A and B (totalSelectionTime).
        float A_time = tmtvrManagerTMTA.totalSelectionTime;
        int A_err = tmtvrManagerTMTA.totalMistakes;
        float A_spat = tmtvrManagerTMTA.AverageAccuracy;   // VR-specific metric

        float B_time = tmtvrManagerTMTB.totalSelectionTime;
        int B_err = tmtvrManagerTMTB.totalMistakes;
        float B_spat = tmtvrManagerTMTB.AverageAccuracy;   // VR-specific metric


        // ================================================================
        // 2) PART-LEVEL DERIVED METRICS (stable + no integer division)
        // ================================================================
        // NOTE: In classic TMT there are 25 circles => 24 links (transitions).
        // We use float constants so division stays float (no truncation).
        float A_errRate = ErrorRate(A_err, LINKS_PER_PART);   // ErrA / 24
        float B_errRate = ErrorRate(B_err, LINKS_PER_PART);   // ErrB / 24

        // Attempt-based accuracy:
        // Accuracy = links / (links + errors)
        // - If errors = 0 => accuracy = 1.0
        // - If errors > 0 => accuracy < 1.0
        float A_acc = AttemptAccuracy(A_err, LINKS_PER_PART); // 24 / (24 + ErrA)
        float B_acc = AttemptAccuracy(B_err, LINKS_PER_PART); // 24 / (24 + ErrB)

        // "Adjusted time" / inverse-efficiency style:
        // AdjustedTime = time / accuracy
        // - If accuracy=1 (no errors) => AdjustedTime == raw time
        // - If accuracy < 1 => AdjustedTime > raw time
        float A_adj = AdjustedTime(A_time, A_err, LINKS_PER_PART);
        float B_adj = AdjustedTime(B_time, B_err, LINKS_PER_PART);


        // ================================================================
        // 3) TOTALS (A + B)
        // ================================================================
        float totalTime = A_time + B_time;  // overall time
        int totalErr = A_err + B_err;    // overall mistakes (count)

        // Total error rate across A+B:
        // total links = 24 + 24 = 48
        float totalErrRate = ErrorRate(totalErr, LINKS_TOTAL); // totalErr / 48

        // Total attempt-accuracy across A+B:
        // 48 / (48 + totalErr)
        float totalAcc = AttemptAccuracy(totalErr, LINKS_TOTAL);

        // Total adjusted time:
        float totalAdj = AdjustedTime(totalTime, totalErr, LINKS_TOTAL);


        // ================================================================
        // 4) DIFFERENCES (B minus A) - common in TMT reporting
        // ================================================================
        float diffTime = B_time - A_time;        // B − A
        int diffErr = B_err - A_err;         // ErrB − ErrA
        float diffErrRate = B_errRate - A_errRate;  // (ErrB/24) − (ErrA/24)

        // VR-specific spatial accuracy difference:
        float diffSpat = B_spat - A_spat;

        // Accuracy difference (attempt-based):
        float diffAcc = B_acc - A_acc;

        // Adjusted time difference:
        float diffAdj = B_adj - A_adj;


        // ================================================================
        // 5) RATIOS / NORMALIZED DIFFERENCE
        // ================================================================
        // SafeDiv returns float.NaN when denominator is 0.
        // UpdateVariable() will write "NA" instead of "NaN" for CSV.
        float ratioTime = SafeDiv(B_time, A_time);   // B / A
        float normDiffTime = SafeDiv(diffTime, A_time); // (B−A)/A  == (B/A) − 1


        // ================================================================
        // 6) WRITE EVERYTHING TO CSV (via UpdateVariable)
        // ================================================================
        UpdateVariable("SessionID", SessionID);
        UpdateVariable("ParticipantID", "PLACE HERE PARTICIPANT'S ID"); // 

        // ---------- Task A ----------
        UpdateVariable("Task A - Task Time", A_time);
        UpdateVariable("Task A - Errors", A_err);
        UpdateVariable("Task A - Error Rate", A_errRate);
        UpdateVariable("Task A - Spatial Accuracy", A_spat);
        UpdateVariable("Task A - Accuracy", A_acc);
        UpdateVariable("Task A - Adjusted Time", A_adj);

        // ---------- Task B ----------
        UpdateVariable("Task B - Task Time", B_time);
        UpdateVariable("Task B - Errors", B_err);
        UpdateVariable("Task B - Error Rate", B_errRate);
        UpdateVariable("Task B - Spatial Accuracy", B_spat);
        UpdateVariable("Task B - Accuracy", B_acc);
        UpdateVariable("Task B - Adjusted Time", B_adj);

        // ---------- Totals ----------
        UpdateVariable("Total Time", totalTime);
        UpdateVariable("Total Errors", totalErr);
        UpdateVariable("Total Error Rate", totalErrRate);
        UpdateVariable("Total Spatial Accuracy", (A_spat + B_spat) / 2f);
        UpdateVariable("Total Accuracy", totalAcc);
        UpdateVariable("Total Adjusted Time", totalAdj);

        // ---------- Differences ----------
        UpdateVariable("Difference Time", diffTime);
        UpdateVariable("Difference Errors", diffErr);
        UpdateVariable("Difference Error Rate", diffErrRate);
        UpdateVariable("Difference Spatial Accuracy", diffSpat);
        UpdateVariable("Difference Accuracy", diffAcc);
        UpdateVariable("Difference Adjusted Time", diffAdj);

        // ---------- Ratio / Normalized difference ----------
        // Ratio and normalised difference; both are written.
        UpdateVariable("Ratio Time", ratioTime);
        UpdateVariable("Norm. Difference Time", normDiffTime);

        // ================================================================
        // 7) FINALIZE CSV ROW
        // ================================================================
        FinalizeAndAddCurrentRow();

        yield return new WaitForEndOfFrame();
        Debug.Log("CSV file path: " + csvFilePath);
    }


    private void InitializeCSV()
    {
        // If file already exists, do NOT overwrite.
        if (File.Exists(csvFilePath))
        {
            Debug.Log("CSV file already exists. Will append rows: " + csvFilePath);
            CSVInitialization = true;
            return;
        }

        if (variables == null || variables.Count == 0)
        {
            // Prevent "csvContent.Length--" crash and avoid writing a headerless file.
            Debug.LogError("[TMTVR_ScoreManager] Cannot initialize CSV: variables list is empty.");
            CSVInitialization = false;
            return;
        }

        try
        {
            StringBuilder csvContent = new StringBuilder();
            const string delimiter = ",";

            foreach (var variable in variables)
                csvContent.Append(CsvEscape(variable.variableName)).Append(delimiter);

            csvContent.Length--;     // remove last comma
            csvContent.AppendLine(); // header row

            File.WriteAllText(csvFilePath, csvContent.ToString(), Encoding.UTF8);
            CSVInitialization = true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TMTVR_ScoreManager] Failed to initialize CSV at: {csvFilePath}\n{ex}");
            CSVInitialization = false;
        }
    }


    // Function to add a new row of data
    public void AddNewRow(Dictionary<string, string> newRow)
    {
        if (newRow == null)
        {
            Debug.LogError("[TMTVR_ScoreManager] AddNewRow called with null row.");
            return;
        }

        if (variables == null || variables.Count == 0)
        {
            Debug.LogError("[TMTVR_ScoreManager] No variables defined. CSV row not written.");
            return;
        }

        StringBuilder newRowContent = new StringBuilder();
        const string delimiter = ",";

        // We write columns strictly in the order of `variables` (Inspector-defined schema).
        foreach (var variable in variables)
        {
            if (newRow.TryGetValue(variable.variableName, out string value))
            {
                newRowContent.Append(CsvEscape(value)).Append(delimiter);
            }
            else
            {
                // Empty value if not provided
                newRowContent.Append(delimiter);
            }
        }

        // Remove the last delimiter if we appended anything
        if (newRowContent.Length > 0)
            newRowContent.Length--;

        newRowContent.AppendLine();

        // Use UTF8 append explicitly (important for non-ASCII, and consistent across platforms)
        AppendAllTextUtf8Safe(csvFilePath, newRowContent.ToString());
    }


    // Example function to update variables' values
    public void UpdateVariable(string name, object value)
    {
        WarnIfKeyNotInSchema(name);
        if (value == null)
        {
            values[name] = "";
            return;
        }

        // ✅ Handle NaN / Infinity explicitly (common for ratio metrics when denominator is 0)
        if (value is float f)
        {
            values[name] = (float.IsNaN(f) || float.IsInfinity(f))
                ? "NA"
                : f.ToString(CultureInfo.InvariantCulture);
            return;
        }

        if (value is double d)
        {
            values[name] = (double.IsNaN(d) || double.IsInfinity(d))
                ? "NA"
                : d.ToString(CultureInfo.InvariantCulture);
            return;
        }

        // ✅ Any other numeric type (decimal, int, etc.) still gets invariant formatting
        if (value is IFormattable formattable)
            values[name] = formattable.ToString(null, CultureInfo.InvariantCulture);
        else
            values[name] = value.ToString();
    }



    // Call this to finalize and add the current row of data to the CSV
    public void FinalizeAndAddCurrentRow()
    {
        AddNewRow(values);
        values.Clear(); // Clear current values for the next row
    }

    /* OLD CODE 
     * public IEnumerator SaveScore()
    {
        UpdateVariable("SessionID", SessionID);
        UpdateVariable("Task A - TaskTime", tmtvrManagerTMTA.totalSelectionTime); // Keep
        UpdateVariable("Task A - Errors", tmtvrManagerTMTA.totalMistakes); // Keep
        UpdateVariable("Task A - Error Rate", tmtvrManagerTMTA.totalMistakes/24); // Keep
        UpdateVariable("Task A - Spatial Accuracy", tmtvrManagerTMTA.AverageAccuracy); // Keep
        UpdateVariable("Task A - Accuracy", 24/(24/tmtvrManagerTMTA.totalMistakes)); 
        UpdateVariable("Task A - Adjusted Efficiency", tmtvrManagerTMTA.totalSelectionTime / (24 / (24 / tmtvrManagerTMTA.totalMistakes)));

        UpdateVariable("Task B - TaskTime", tmtvrManagerTMTB.totalSelectionTime);
        UpdateVariable("Task B - Errors", tmtvrManagerTMTB.totalMistakes);
        UpdateVariable("Task B - Error Rate", tmtvrManagerTMTB.totalMistakes / 24);
        UpdateVariable("Task B - Spatial Accuracy", tmtvrManagerTMTB.AverageAccuracy);
        UpdateVariable("Task B - Accuracy", 24 / (24 / tmtvrManagerTMTB.totalMistakes));
        UpdateVariable("Task B - Adjusted Efficiency", tmtvrManagerTMTB.totalSelectionTime / (24 / (24 / tmtvrManagerTMTB.totalMistakes)));

        UpdateVariable("Total Time", tmtvrManagerTMTB.totalSelectionTime + tmtvrManagerTMTA.totalSelectionTime); 
        UpdateVariable("Total Errors", tmtvrManagerTMTB.totalMistakes + tmtvrManagerTMTA.totalMistakes);
        UpdateVariable("Total Error Rate", (tmtvrManagerTMTB.totalMistakes + tmtvrManagerTMTA.totalMistakes) / 48);
        UpdateVariable("Total Spatial Accuracy", (tmtvrManagerTMTB.AverageAccuracy + tmtvrManagerTMTA.AverageAccuracy) / 2);
        UpdateVariable("Total Accuracy", 48 / (48 / (tmtvrManagerTMTB.totalMistakes + tmtvrManagerTMTA.totalMistakes)));  
        UpdateVariable("Total Adjusted Efficiency", (tmtvrManagerTMTB.totalSelectionTime + tmtvrManagerTMTA.totalSelectionTime) / (48 
            / (48 / (tmtvrManagerTMTB.totalMistakes + tmtvrManagerTMTA.totalMistakes))));

        UpdateVariable("Difference Time", tmtvrManagerTMTB.totalSelectionTime - tmtvrManagerTMTA.totalSelectionTime); 
        UpdateVariable("Difference Errors", tmtvrManagerTMTB.totalMistakes - tmtvrManagerTMTA.totalMistakes); 
        UpdateVariable("Difference Error Rate", (tmtvrManagerTMTB.totalMistakes/24) - (tmtvrManagerTMTA.totalMistakes/24));
        UpdateVariable("Difference Spatial Accuracy", tmtvrManagerTMTB.AverageAccuracy - tmtvrManagerTMTA.AverageAccuracy);
        UpdateVariable("Difference Accuracy", (24 / (24 / tmtvrManagerTMTB.totalMistakes)) - (24 / (24 / tmtvrManagerTMTA.totalMistakes)));
        UpdateVariable("Difference Adjusted Efficiency", (tmtvrManagerTMTB.totalSelectionTime / (24 / (24 / tmtvrManagerTMTB.totalMistakes))) 
            - (tmtvrManagerTMTA.totalSelectionTime / (24 / (24 / tmtvrManagerTMTA.totalMistakes))));

        UpdateVariable("Ratio Time", tmtvrManagerTMTB.totalSelectionTime/tmtvrManagerTMTA.totalSelectionTime); 
        UpdateVariable("Ratio Errors", tmtvrManagerTMTB.totalMistakes/tmtvrManagerTMTA.totalMistakes); 
        
        UpdateVariable("Norm. Difference Time", (tmtvrManagerTMTB.totalSelectionTime - tmtvrManagerTMTA.totalSelectionTime)/tmtvrManagerTMTA.totalSelectionTime); 
        UpdateVariable("Norm. Difference Errors", (tmtvrManagerTMTB.totalMistakes - tmtvrManagerTMTA.totalMistakes)/tmtvrManagerTMTA.totalMistakes);
        UpdateVariable("Norm. Difference Error Rate", ((tmtvrManagerTMTB.totalMistakes / 24) - (tmtvrManagerTMTA.totalMistakes / 24)) / (tmtvrManagerTMTA.totalMistakes / 24));
        UpdateVariable("Norm. Difference Accuracy", ((24 / (24 / tmtvrManagerTMTB.totalMistakes)) - (24 / (24 / tmtvrManagerTMTA.totalMistakes))) / (24 / (24 / tmtvrManagerTMTA.totalMistakes)));
        UpdateVariable("Norm. Difference Adjusted Efficiency", ((tmtvrManagerTMTB.totalSelectionTime / (24 / (24 / tmtvrManagerTMTB.totalMistakes))) 
            - (tmtvrManagerTMTA.totalSelectionTime / (24 / (24 / tmtvrManagerTMTA.totalMistakes)))) / (tmtvrManagerTMTA.totalSelectionTime / (24 / (24 / tmtvrManagerTMTA.totalMistakes))));

        // Add the current row of data to the CSV
        FinalizeAndAddCurrentRow();

        // Wait for the end of the frame to ensure all data is written to the CSV
        yield return new WaitForEndOfFrame();

        // Optional: Log the path to the CSV file
        Debug.Log("CSV file path: " + csvFilePath);
        yield return null;
    }*/

}
