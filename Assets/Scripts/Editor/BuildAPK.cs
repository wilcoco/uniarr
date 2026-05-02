using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;

/// <summary>
/// Guardian AR → Build APK
/// </summary>
public class BuildAPK : Editor
{
    const string PackageName    = "com.guardianar.game";
    const string AppVersion     = "0.1.0";
    const int    BundleVersion  = 1;
    const string ScenePath      = "Assets/Scenes/GuardianAR.unity";
    const string OutputDir      = "Builds/Android";
    const string OutputFileName = "GuardianAR.apk";

    [MenuItem("Guardian AR/Build APK")]
    public static void Build()
    {
        // ── 사전 확인 ──────────────────────────────────────────────
        if (!File.Exists(ScenePath))
        {
            EditorUtility.DisplayDialog("Error",
                $"Scene not found:\n{ScenePath}\n\nRun 'Guardian AR → Setup Scene' first,\nthen save as GuardianAR.unity",
                "OK");
            return;
        }

        if (!File.Exists("Assets/google-services.json"))
        {
            bool cont = EditorUtility.DisplayDialog("Warning",
                "google-services.json not found in Assets/.\nFirebase push notifications will not work.\n\nContinue anyway?",
                "Continue", "Cancel");
            if (!cont) return;
        }

        // ── Player Settings ────────────────────────────────────────
        PlayerSettings.applicationIdentifier       = PackageName;
        PlayerSettings.productName                 = "Guardian AR";
        PlayerSettings.bundleVersion               = AppVersion;
        PlayerSettings.Android.bundleVersionCode   = BundleVersion;
        PlayerSettings.Android.minSdkVersion       = AndroidSdkVersions.AndroidApiLevel24; // Android 7.0
        PlayerSettings.Android.targetSdkVersion    = AndroidSdkVersions.AndroidApiLevel34;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

        // Internet + 위치 권한
        PlayerSettings.Android.forceInternetPermission = true;

        // ── 그래픽 설정 (URP) ──────────────────────────────────────
        PlayerSettings.colorSpace = ColorSpace.Linear;

        // ── 출력 폴더 ──────────────────────────────────────────────
        Directory.CreateDirectory(OutputDir);
        string outputPath = Path.Combine(OutputDir, OutputFileName);

        // ── 빌드 옵션 ──────────────────────────────────────────────
        var options = new BuildPlayerOptions
        {
            scenes        = new[] { ScenePath },
            locationPathName = outputPath,
            target        = BuildTarget.Android,
            options       = BuildOptions.None,
        };

        // ── 빌드 실행 ──────────────────────────────────────────────
        Debug.Log("[Build] Starting APK build...");
        var report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            long sizeMB = (long)summary.totalSize / (1024 * 1024);
            EditorUtility.DisplayDialog("Build Succeeded",
                $"APK built successfully!\n\nOutput: {outputPath}\nSize: {sizeMB} MB\nTime: {summary.totalTime.TotalSeconds:F1}s",
                "OK");

            // 출력 폴더 열기
            EditorUtility.RevealInFinder(outputPath);
        }
        else
        {
            EditorUtility.DisplayDialog("Build Failed",
                $"Build failed with {summary.totalErrors} error(s).\nCheck the Console for details.",
                "OK");
        }
    }

    [MenuItem("Guardian AR/Build Settings (Check)")]
    public static void CheckSettings()
    {
        var issues = new System.Text.StringBuilder();

        if (!File.Exists(ScenePath))
            issues.AppendLine("✗ GuardianAR.unity scene missing");
        else
            issues.AppendLine("✓ Scene found");

        if (!File.Exists("Assets/google-services.json"))
            issues.AppendLine("✗ google-services.json missing");
        else
            issues.AppendLine("✓ google-services.json found");

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            issues.AppendLine("✗ Build target is not Android\n  → File > Build Settings > Android > Switch Platform");
        else
            issues.AppendLine("✓ Android build target active");

        bool xrOk = IsARCoreEnabled();
        issues.AppendLine(xrOk
            ? "✓ ARCore enabled"
            : "✗ ARCore not enabled\n  → Project Settings > XR Plugin Management > Android > ARCore");

        EditorUtility.DisplayDialog("Build Settings Check", issues.ToString(), "OK");
    }

    static bool IsARCoreEnabled()
    {
        try
        {
            var xrSettings = UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget
                .XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            if (xrSettings?.Manager?.activeLoaders == null) return false;
            foreach (var loader in xrSettings.Manager.activeLoaders)
                if (loader.GetType().Name.Contains("ARCore")) return true;
            return false;
        }
        catch { return false; }
    }
}
