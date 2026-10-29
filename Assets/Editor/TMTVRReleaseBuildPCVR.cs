// Builds the specified TMT-VR Windows PCVR release variant, records its identity and checksum, and restores the previous PlayerSettings after the build.
using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class TMTVRReleaseBuildPCVR
{
    private static string ProjectRoot => FullPath(Path.Combine(Application.dataPath, ".."));
    private const string UnityVersion = "6000.2.10f1";
    private const string RecordPackage = "com.DefaultCompany.VRTemplate";

    [Serializable]
    private sealed class Receipt
    {
        public string variant;
        public string buildTarget = "StandaloneWindows64";
        public string unityVersion;
        public string projectPath;
        public string scene;
        public string output;
        public string packageId;
        public string productName;
        public string version;
        public string startedUtc;
        public string finishedUtc;
        public string result = "NotStarted";
        public string failureReason = "";
        public int errors;
        public int warnings;
        public int acceptedCursorErrors;
        public int unapprovedErrors;
        public string[] errorMessages;
        public long artifactBytes;
        public string artifactSha256 = "";
        public double buildSeconds;
        public bool identitiesCaptured;
        public bool identitiesRestored;
    }

    private sealed class GuardFailure : Exception
    {
        public GuardFailure(string reason) : base(reason) { }
    }

    public static void BuildHeadEN()
    {
        Build("HEAD, EN", "Assets/TMTVR_Head_EN.unity",
              "com.DefaultCompany.VRTemplate.head.en", "TMT-VR Head EN", "1.1.0 (HEAD, EN, PCVR)");
    }

    public static void BuildHeadGR()
    {
        Build("HEAD, GR", "Assets/TMTVR_Head_GR.unity",
              "com.DefaultCompany.VRTemplate.head.gr", "TMT-VR Head GR", "1.1.0 (HEAD, GR, PCVR)");
    }

    public static void BuildControllerEN()
    {
        Build("CONTROLLER, EN", "Assets/TMTVR_Controller_EN.unity",
              "com.DefaultCompany.VRTemplate.controller.en", "TMT-VR Controller EN", "1.1.0 (CONTROLLER, EN, PCVR)");
    }

    public static void BuildControllerGR()
    {
        Build("CONTROLLER, GR", "Assets/TMTVR_Controller_GR.unity",
              "com.DefaultCompany.VRTemplate.controller.gr", "TMT-VR Controller GR", "1.1.0 (CONTROLLER, GR, PCVR)");
    }

    private static void Require(bool condition, string reason)
    {
        if (!condition) throw new GuardFailure(reason);
    }

    private static string FullPath(string path)
    {
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static void RejectReparseAncestors(string path)
    {
        string current = Path.GetFullPath(path);
        while (!String.IsNullOrEmpty(current))
        {
            if (File.Exists(current) || Directory.Exists(current))
                Require((File.GetAttributes(current) & FileAttributes.ReparsePoint) == 0,
                        "Output/project path contains a reparse point.");
            string parent = Path.GetDirectoryName(current);
            if (String.Equals(parent, current, StringComparison.OrdinalIgnoreCase)) break;
            current = parent;
        }
    }

    private static string OutputArgument()
    {
        string[] args = Environment.GetCommandLineArgs();
        string output = null;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] != "-tmtvrOutput") continue;
            Require(output == null && i + 1 < args.Length, "Expected exactly one -tmtvrOutput value.");
            output = args[++i];
        }
        Require(!String.IsNullOrWhiteSpace(output) && Path.IsPathRooted(output), "An absolute -tmtvrOutput is required.");
        Require(output.IndexOf('"') < 0 && output.IndexOf('\n') < 0 && output.IndexOf('\r') < 0,
                "Invalid output path characters.");
        string full = FullPath(output);
        Require(full.IndexOf(':', 2) < 0, "Alternate data streams are forbidden.");
        string builds = FullPath(Path.Combine(ProjectRoot, "Builds"));
        Require(full.StartsWith(builds + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase),
                "Output must be beneath this project's Builds directory.");
        Require(String.Equals(Path.GetExtension(full), ".exe", StringComparison.OrdinalIgnoreCase), "Output must be a Windows executable.");
        Require(!File.Exists(full) && !Directory.Exists(full), "Output exists; refusing overwrite.");
        Require(!File.Exists(Path.ChangeExtension(full, ".build.json")), "Build receipt exists; refusing overwrite.");
        RejectReparseAncestors(full);
        Require(Directory.Exists(Path.GetDirectoryName(full)), "Output directory must already exist.");
        return full;
    }

    private static string Sha256(string path)
    {
        using (SHA256 sha = SHA256.Create())
        using (FileStream stream = File.OpenRead(path))
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
    }

    private static void Build(string variant, string scene, string packageId, string product, string version)
    {
        Receipt receipt = new Receipt {
            variant = variant, unityVersion = Application.unityVersion, projectPath = ProjectRoot,
            scene = scene, packageId = packageId, productName = product, version = version,
            startedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
        };
        string priorPackage = null;
        string priorProduct = null;
        string priorVersion = null;
        string receiptPath = null;
        bool succeeded = false;
        try
        {
            Require(Application.isBatchMode, "This entry point is batchmode only.");
            Require(Application.unityVersion == UnityVersion, "Unexpected Unity version.");
            string actualRoot = FullPath(Path.Combine(Application.dataPath, ".."));
            Require(File.Exists(Path.Combine(actualRoot, "ProjectSettings", "ProjectVersion.txt")),
                    "A Unity project root is required.");
            RejectReparseAncestors(actualRoot);
            Require(packageId != RecordPackage && (packageId == "com.DefaultCompany.VRTemplate.head.en"
                    || packageId == "com.DefaultCompany.VRTemplate.head.gr"
                    || packageId == "com.DefaultCompany.VRTemplate.controller.en"
                    || packageId == "com.DefaultCompany.VRTemplate.controller.gr"), "Record/default package is forbidden.");
            Require(EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64, "Windows 64-bit must already be active.");
            Require(PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone) == ScriptingImplementation.IL2CPP,
                    "Existing Windows scripting backend must be IL2CPP.");
            Require(!EditorUserBuildSettings.development, "Development build setting must already be off.");
            string scenePath = Path.Combine(ProjectRoot, scene.Replace('/', Path.DirectorySeparatorChar));
            RejectReparseAncestors(scenePath);
            Require(File.Exists(scenePath), "The designated release scene does not exist.");
            receipt.output = OutputArgument();
            receiptPath = Path.ChangeExtension(receipt.output, ".build.json");

            priorPackage = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Standalone);
            priorProduct = PlayerSettings.productName;
            priorVersion = PlayerSettings.bundleVersion;
            receipt.identitiesCaptured = true;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, packageId);
            PlayerSettings.productName = product;
            PlayerSettings.bundleVersion = version;
            Require(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Standalone) == packageId
                    && PlayerSettings.productName == product && PlayerSettings.bundleVersion == version,
                    "Build identity assignment did not persist in PlayerSettings.");

            BuildPlayerOptions options = new BuildPlayerOptions {
                scenes = new[] { scene }, locationPathName = receipt.output,
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.CompressWithLz4
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            Require(report != null, "BuildPipeline returned no report.");
            BuildSummary summary = report.summary;
            receipt.result = summary.result.ToString();
            receipt.errors = checked((int)summary.totalErrors);
            receipt.warnings = checked((int)summary.totalWarnings);
            receipt.buildSeconds = summary.totalTime.TotalSeconds;
            var reportedErrors = new System.Collections.Generic.List<string>();
            const string acceptedCursorError = "Failed to set the cursor because the specified texture ('GERIATRIC_LOGO_Final') was not CPU accessible.";
            foreach (var step in report.steps)
            foreach (var message in step.messages)
            {
                if (message.type != LogType.Error && message.type != LogType.Assert && message.type != LogType.Exception) continue;
                string content = (message.content ?? "").Trim();
                reportedErrors.Add(content);
                if (String.Equals(content, acceptedCursorError, StringComparison.Ordinal)) receipt.acceptedCursorErrors++;
                else receipt.unapprovedErrors++;
            }
            receipt.errorMessages = reportedErrors.ToArray();
            Require(summary.result == BuildResult.Succeeded && receipt.unapprovedErrors == 0
                    && receipt.errors == receipt.acceptedCursorErrors,
                    "Unity did not report a successful build containing only the approved cursor diagnostic.");
            Require(File.Exists(receipt.output) && new FileInfo(receipt.output).Length > 0,
                    "Successful build has no nonempty Windows executable.");
            receipt.artifactBytes = new FileInfo(receipt.output).Length;
            receipt.artifactSha256 = Sha256(receipt.output);
            succeeded = true;
        }
        catch (Exception error)
        {
            receipt.result = "Failed";
            receipt.failureReason = error is GuardFailure ? error.Message : "Build exception: " + error.GetType().Name;
        }
        finally
        {
            if (receipt.identitiesCaptured)
            {
                try
                {
                    PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, priorPackage);
                    PlayerSettings.productName = priorProduct;
                    PlayerSettings.bundleVersion = priorVersion;
                    receipt.identitiesRestored = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Standalone) == priorPackage
                        && PlayerSettings.productName == priorProduct && PlayerSettings.bundleVersion == priorVersion;
                }
                catch (Exception error)
                {
                    receipt.failureReason = "Identity restoration exception: " + error.GetType().Name;
                }
                if (!receipt.identitiesRestored)
                {
                    succeeded = false;
                    receipt.result = "Failed";
                    if (String.IsNullOrEmpty(receipt.failureReason)) receipt.failureReason = "Original identities were not restored.";
                }
            }
            receipt.finishedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            string json = JsonUtility.ToJson(receipt);
            if (receiptPath != null)
            {
                try
                {
                    using (FileStream stream = new FileStream(receiptPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                    using (StreamWriter writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false)))
                        writer.WriteLine(json);
                }
                catch (Exception error)
                {
                    succeeded = false;
                    receipt.result = "Failed";
                    receipt.failureReason = "Build receipt write exception: " + error.GetType().Name;
                    json = JsonUtility.ToJson(receipt);
                }
            }
            Debug.Log("TMTVR_RELEASE_BUILD " + json);
        }
        if (!succeeded) EditorApplication.Exit(2);
        // Success returns to the command line's normal -quit; never auto-run/install.
    }
}
