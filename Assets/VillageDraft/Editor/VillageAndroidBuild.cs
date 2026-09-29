using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.OpenXR;

namespace VillageDraft.Editor
{
    /// <summary>Repeatable Quest 3S APK build entry point; never builds from the template scene.</summary>
    public static class VillageAndroidBuild
    {
        private const string ScenePath = "Assets/VillageDraft/Scenes/VillageDraft.unity";
        private const string MenuRoot = "Tools/VR Village/Android/";
        private static bool buildInProgress;

        [MenuItem(MenuRoot + "Check Quest 3S Readiness")]
        public static void CheckQuest3SReadiness()
        {
            var issues = Inspect();
            foreach (var warning in issues.warnings)
                Debug.LogWarning("[Village Android] " + warning);
            if (issues.errors.Count == 0)
            {
                Debug.Log("[Village Android] Ready to build a Quest 3S APK from " + ScenePath + ".");
                return;
            }

            foreach (var error in issues.errors)
                Debug.LogError("[Village Android] " + error);
            Debug.LogError($"[Village Android] Readiness failed: {issues.errors.Count} issue(s). " +
                           "Resolve the listed issue(s), then check again.");
        }

        /// <summary>Only applies settings required by the installed OpenXR/Android XR packages.</summary>
        [MenuItem(MenuRoot + "Apply Quest 3S Settings")]
        public static void ApplyQuest3SSettings()
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel26)
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.GameActivity;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            Debug.Log("[Village Android] Set IL2CPP, ARM64, Vulkan, Game Activity, Landscape Left, " +
                      "minimum API 26 or current higher level, and APK output. " +
                      "Bundle ID and signing were left for the project owner.");
        }

        [MenuItem(MenuRoot + "Switch Active Target to Android")]
        public static void SwitchActiveTargetToAndroid()
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                Debug.Log("[Village Android] Android is already the active build target.");
                return;
            }
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new BuildFailedException("Android Build Support is not installed for this Unity Editor.");
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                throw new BuildFailedException("Unity could not switch the active build target to Android.");
            Debug.Log("[Village Android] Switched to Android. Wait for asset import and compilation, " +
                      "then run Check Quest 3S Readiness.");
        }

        [MenuItem(MenuRoot + "Build Quest 3S APK")]
        public static void BuildQuest3SApk()
        {
            if (buildInProgress)
                throw new BuildFailedException("A village Android build is already running.");
            var issues = Inspect();
            foreach (var warning in issues.warnings)
                Debug.LogWarning("[Village Android] " + warning);
            if (issues.errors.Count != 0)
                throw new BuildFailedException("Quest 3S preflight failed:\n - " +
                    string.Join("\n - ", issues.errors));

            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var buildFolder = Path.Combine(projectRoot, "Builds");
            Directory.CreateDirectory(buildFolder);
            var apkPath = Path.Combine(buildFolder,
                "VRVillage_Quest3S_Draft_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".apk");

            var oldBundle = EditorUserBuildSettings.buildAppBundle;
            var oldExport = EditorUserBuildSettings.exportAsGoogleAndroidProject;
            var oldDevelopment = EditorUserBuildSettings.development;
            buildInProgress = true;
            try
            {
                EditorUserBuildSettings.buildAppBundle = false;
                EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
                EditorUserBuildSettings.development = false;
                var options = new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = apkPath,
                    target = BuildTarget.Android,
                    options = BuildOptions.None
                };
                Debug.Log("[Village Android] Building Quest APK from " + ScenePath + " to " + apkPath);
                var report = BuildPipeline.BuildPlayer(options);
                if (report == null || report.summary.result != BuildResult.Succeeded)
                    throw new BuildFailedException(DescribeFailure(report, apkPath));
                if (!File.Exists(apkPath))
                    throw new BuildFailedException("Unity reported success, but the APK is missing: " + apkPath);
                var bytes = new FileInfo(apkPath).Length;
                Debug.Log($"[Village Android] APK ready: {apkPath} ({bytes / 1048576f:F1} MiB). " +
                          "Install and test it on a Quest 3S before submission.");
            }
            finally
            {
                EditorUserBuildSettings.buildAppBundle = oldBundle;
                EditorUserBuildSettings.exportAsGoogleAndroidProject = oldExport;
                EditorUserBuildSettings.development = oldDevelopment;
                buildInProgress = false;
            }
        }

        private static (List<string> errors, List<string> warnings) Inspect()
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                errors.Add("Leave Play Mode before building.");
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                errors.Add("Wait for compilation and asset import to finish.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                errors.Add("Install Android Build Support with SDK, NDK, and OpenJDK for Unity 6000.3.19f1.");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                errors.Add("Active target is " + EditorUserBuildSettings.activeBuildTarget +
                           "; use Tools > VR Village > Android > Switch Active Target to Android first.");

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                errors.Add("Village scene is missing: " + ScenePath + ". Run Build Draft Scene first.");
            if (!EditorBuildSettings.scenes.Any(s => s.enabled && s.path == ScenePath))
                errors.Add("Village scene is not enabled in Build Settings. Run Build Draft Scene first.");
            var active = EditorSceneManager.GetActiveScene();
            if (active.path == ScenePath && active.isDirty)
                errors.Add("The open village scene has unsaved changes. Save the scene before building.");

            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP)
                errors.Add("Android scripting backend must be IL2CPP.");
            if (PlayerSettings.Android.targetArchitectures != AndroidArchitecture.ARM64)
                errors.Add("Android target architecture must be ARM64 only for OpenXR.");
            if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel26)
                errors.Add("Android minimum API level must be at least 26 for the installed Android XR package.");
            if (PlayerSettings.Android.applicationEntry != AndroidApplicationEntry.GameActivity)
                errors.Add("Android application entry point must be Game Activity.");
            if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.LandscapeLeft)
                errors.Add("Default orientation must be Landscape Left for Meta Quest.");

            var graphicsApis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            if (graphicsApis == null || graphicsApis.Length != 1 || graphicsApis[0] != GraphicsDeviceType.Vulkan)
                errors.Add("Android graphics API must be Vulkan only for this Quest build.");

            var xr = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            if (xr?.AssignedSettings == null ||
                !xr.AssignedSettings.activeLoaders.Any(loader => loader is OpenXRLoader))
                errors.Add("Android XR Plug-in Management must have the OpenXR loader enabled.");
            var openXr = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (openXr == null)
            {
                errors.Add("Android OpenXR settings asset is missing.");
            }
            else
            {
                var features = openXr.GetFeatures();
                if (!features.Any(f => f != null && f.enabled && f.GetType().Name == "MetaQuestFeature"))
                    errors.Add("Enable Meta Quest support in Android OpenXR features.");
                if (!features.Any(f => f != null && f.enabled &&
                                       f.GetType().Name == "MetaQuestTouchPlusControllerProfile"))
                    errors.Add("Enable Meta Quest Touch Plus controller profile in Android OpenXR features.");
                if (openXr.renderMode != OpenXRSettings.RenderMode.SinglePassInstanced)
                    errors.Add("Set Android OpenXR render mode to Single Pass Instanced / Multiview.");
            }

            var pipeline = QualitySettings.renderPipeline as UniversalRenderPipelineAsset ??
                           GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline == null)
                warnings.Add("Active render pipeline is not URP; verify the Android quality profile.");
            else if (pipeline.supportsHDR)
                errors.Add("Disable HDR on the Android URP asset for Android XR.");

            var bundleId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            if (string.IsNullOrEmpty(bundleId) || bundleId.Contains("DefaultCompany"))
                warnings.Add("Android bundle identifier still uses the template value (" + bundleId +
                             "). Set a unique ID before a final submission.");
            if (EditorUserBuildSettings.buildAppBundle)
                warnings.Add("Build App Bundle is enabled; this command overrides it temporarily to produce an APK.");
            return (errors, warnings);
        }

        private static string DescribeFailure(BuildReport report, string apkPath)
        {
            if (report == null)
                return "BuildPipeline returned no report for " + apkPath + ". Check the Unity Console and Editor log.";
            var details = report.steps
                .SelectMany(step => step.messages)
                .Where(message => message.type == LogType.Error || message.type == LogType.Exception)
                .Select(message => message.content)
                .Take(8)
                .ToArray();
            var suffix = details.Length == 0 ? "Check the Unity Console and Editor log." :
                string.Join(" | ", details);
            return $"Android build {report.summary.result} with {report.summary.totalErrors} error(s). " +
                   suffix + " Output: " + apkPath;
        }
    }
}
