// You can call the builder function using commands like:
//
// /Path/To/Unity -quit -batchmode -executeMethod Builder.Build Android
// /Path/To/Unity -quit -batchmode -executeMethod Builder.Build AndroidTest
//
// See https://support.unity.com/hc/en-us/articles/115000368846-How-do-I-support-different-configurations-to-build-specific-players-by-command-line-or-auto-build-system

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public class Builder
{
    public const string BUILD_PATH = "Builds/";

    public static void SetBaseSettings()
    {
        // Set core settings for all builds
        PlayerSettings.actionOnDotNetUnhandledException = ActionOnDotNetUnhandledException.Crash;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowFullscreenSwitch = true;
        PlayerSettings.captureSingleScreen = false;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.enableCrashReportAPI = true;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.macRetinaSupport = true;
        // PlayerSettings.resetResolutionOnWindowResize = true;
        // PlayerSettings.runInBackground = true;
        PlayerSettings.statusBarHidden = true;
        PlayerSettings.SetStackTraceLogType(LogType.Error, StackTraceLogType.Full);
        PlayerSettings.SetStackTraceLogType(LogType.Exception, StackTraceLogType.Full);
        PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, "");
        PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Android, "");
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, "");
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, "");

        // Android settings
        PlayerSettings.Android.androidIsGame = true;
        PlayerSettings.Android.androidTargetDevices = AndroidTargetDevices.AllDevices;
        PlayerSettings.Android.androidTVCompatibility = false;
        PlayerSettings.Android.chromeosInputEmulation = false;
        PlayerSettings.Android.defaultWindowWidth = 1920;
        PlayerSettings.Android.defaultWindowHeight = 1080;
        PlayerSettings.Android.forceInternetPermission = true;
        // PlayerSettings.Android.fullscreenMode = FullScreenMode.FullScreenWindow;
        // https://docs.unity3d.com/ScriptReference/AndroidArchitecture.html
        PlayerSettings.Android.targetArchitectures =
            AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
        PlayerSettings.Android.buildApkPerCpuArchitecture = false;
        // https://docs.unity3d.com/ScriptReference/AndroidSdkVersions.html
        /* PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24; */
        /* PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel28; */

        var version = Environment.GetEnvironmentVariable("OKEY_BUILD_VERSION");
        if (!string.IsNullOrEmpty(version))
        {
            PlayerSettings.bundleVersion = version;
        }
        else
        {
            PlayerSettings.bundleVersion = "0.0.0";
        }
    }

    public static void SetTestSettings()
    {
        // Set extra settings for a test build
        PlayerSettings.enableFrameTimingStats = true;
        PlayerSettings.enableInternalProfiler = true;
        PlayerSettings.forceSingleInstance = false;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.usePlayerLog = true;
    }

    public static void SetReleaseSettings()
    {
        // Set extra settings for a test build
        PlayerSettings.enableFrameTimingStats = false;
        PlayerSettings.enableInternalProfiler = false;
        PlayerSettings.forceSingleInstance = true;
        PlayerSettings.resizableWindow = false;
        PlayerSettings.usePlayerLog = false;
    }

    public static List<string> GetEditorScenes()
    {
        // Get all the scenes in the build settings of the editor
        var EditorScenes = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                EditorScenes.Add(scene.path);
            }
        }
        return EditorScenes;
    }

    [MenuItem("MyTools/Test Build/Android Test Build")]
    public static void AndroidTestBuild()
    {
        SetBaseSettings();
        SetTestSettings();

        // Save path for the build relative to the Unity project root
        var RelativeSaveLocation = "Builds/Android/OkeyTest";

        // Build the player
        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = GetEditorScenes().ToArray(),
            locationPathName = RelativeSaveLocation,
            target = BuildTarget.Android,
            options =
                BuildOptions.Development
                | BuildOptions.AllowDebugging
                | BuildOptions.EnableDeepProfilingSupport
                | BuildOptions.DetailedBuildReport,
            extraScriptingDefines = new[] { "DEBUG" }
        };

        BuildPipeline.BuildPlayer(buildPlayerOptions);

        EditorUtility.DisplayDialog(
            "Build Complete",
            "Android Test Build Complete\n\nSaved to: "
                + Path.Join(
                    Directory.GetParent(Application.dataPath).ToString(),
                    RelativeSaveLocation
                ),
            "OK"
        );
    }

    [MenuItem("MyTools/Test Build/Android Local Test Build")]
    public static void AndroidLocalTestBuild()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(
            BuildTargetGroup.Android,
            BuildTarget.Android
        );
        EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.Android;
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
        /* EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.Generic; */
        PlayerSettings.SetArchitecture(NamedBuildTarget.Android, 2);

        SetBaseSettings();
        SetTestSettings();

        PlayerSettings.SetScriptingBackend(
            NamedBuildTarget.Android,
            ScriptingImplementation.Mono2x
        );
        PlayerSettings.SetApiCompatibilityLevel(
            NamedBuildTarget.Android,
            ApiCompatibilityLevel.NET_Unity_4_8
        );

        // Save path for the build relative to the Unity project root
        var RelativeSaveLocation = "Builds/Android/OkeyTest.apk";

        // Build the player
        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = GetEditorScenes().ToArray(),
            locationPathName = RelativeSaveLocation,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options =
                BuildOptions.Development
                | BuildOptions.AllowDebugging
                | BuildOptions.EnableDeepProfilingSupport
                | BuildOptions.DetailedBuildReport,
            extraScriptingDefines = new[] { "DEBUG", "LOCAL" }
        };

        BuildPipeline.BuildPlayer(buildPlayerOptions);

        EditorUtility.DisplayDialog(
            "Build Complete",
            "Android Test Build Complete\n\nSaved to: "
                + Path.Join(
                    Directory.GetParent(Application.dataPath).ToString(),
                    RelativeSaveLocation
                ),
            "OK"
        );
    }

    [MenuItem("MyTools/Release Build/Android Release Build")]
    public static void AndroidReleaseBuild()
    {
        SetBaseSettings();
        SetReleaseSettings();

        // Save path for the build relative to the Unity project root
        var RelativeSaveLocation = "Builds/Android/Okey";

        // Build the player
        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = GetEditorScenes().ToArray(),
            locationPathName = RelativeSaveLocation,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildPipeline.BuildPlayer(buildPlayerOptions);

        EditorUtility.DisplayDialog(
            "Build Complete",
            "Android Release Build Complete\n\nSaved to: "
                + Path.Join(
                    Directory.GetParent(Application.dataPath).ToString(),
                    RelativeSaveLocation
                ),
            "OK"
        );
    }

    [MenuItem("MyTools/Test Build/Linux x86_64 Test Build")]
    public static void LinuxTestBuild()
    {
        SetBaseSettings();
        SetTestSettings();

        // Save path for the build relative to the Unity project root
        var RelativeSaveLocation = "Builds/Linux/OkeyTest";

        // Build the player
        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = GetEditorScenes().ToArray(),
            locationPathName = RelativeSaveLocation,
            target = BuildTarget.StandaloneLinux64,
            options =
                BuildOptions.Development
                | BuildOptions.AllowDebugging
                | BuildOptions.EnableDeepProfilingSupport
                | BuildOptions.DetailedBuildReport,
            extraScriptingDefines = new[] { "DEBUG" }
        };

        BuildPipeline.BuildPlayer(buildPlayerOptions);

        EditorUtility.DisplayDialog(
            "Build Complete",
            "Linux x86_64 Test Build Complete\n\nSaved to: "
                + Path.Join(
                    Directory.GetParent(Application.dataPath).ToString(),
                    RelativeSaveLocation
                ),
            "OK"
        );
    }

    [MenuItem("MyTools/Test Build/Linux x86_64 Local Test Build")]
    public static void LinuxLocalTestBuild()
    {
        SetBaseSettings();
        SetTestSettings();

        // Save path for the build relative to the Unity project root
        var RelativeSaveLocation = "Builds/Linux/OkeyTest";

        // Build the player
        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = GetEditorScenes().ToArray(),
            locationPathName = RelativeSaveLocation,
            target = BuildTarget.StandaloneLinux64,
            options =
                BuildOptions.Development
                | BuildOptions.AllowDebugging
                | BuildOptions.EnableDeepProfilingSupport
                | BuildOptions.DetailedBuildReport,
            extraScriptingDefines = new[] { "DEBUG", "LOCAL" }
        };

        BuildPipeline.BuildPlayer(buildPlayerOptions);

        EditorUtility.DisplayDialog(
            "Build Complete",
            "Linux x86_64 Test Build Complete\n\nSaved to: "
                + Path.Join(
                    Directory.GetParent(Application.dataPath).ToString(),
                    RelativeSaveLocation
                ),
            "OK"
        );
    }

    [MenuItem("MyTools/Release Build/Linux x86_64 Release Build")]
    public static void LinuxReleaseBuild()
    {
        SetBaseSettings();
        SetReleaseSettings();

        // Save path for the build relative to the Unity project root
        var RelativeSaveLocation = "Builds/Linux/Okey";

        // Build the player
        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = GetEditorScenes().ToArray(),
            locationPathName = RelativeSaveLocation,
            target = BuildTarget.StandaloneLinux64,
            options = BuildOptions.None
        };

        BuildPipeline.BuildPlayer(buildPlayerOptions);

        EditorUtility.DisplayDialog(
            "Build Complete",
            "Linux x86_64 Release Build Complete\n\nSaved to: "
                + Path.Join(
                    Directory.GetParent(Application.dataPath).ToString(),
                    RelativeSaveLocation
                ),
            "OK"
        );
    }

    [MenuItem("MyTools/Test Build/Mac Universal Test Build")]
    public static void MacTestBuild()
    {
        SetBaseSettings();
        SetTestSettings();

        // Save path for the build relative to the Unity project root
        var RelativeSaveLocation = "Builds/Mac/OkeyTest.app";

        // Build the player
        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = GetEditorScenes().ToArray(),
            locationPathName = RelativeSaveLocation,
            target = BuildTarget.StandaloneOSX,
            options =
                BuildOptions.Development
                | BuildOptions.AllowDebugging
                | BuildOptions.EnableDeepProfilingSupport
                | BuildOptions.DetailedBuildReport,
            extraScriptingDefines = new[] { "DEBUG" }
        };

        BuildPipeline.BuildPlayer(buildPlayerOptions);

        EditorUtility.DisplayDialog(
            "Build Complete",
            "Mac Universal Test Build Complete\n\nSaved to: "
                + Path.Join(
                    Directory.GetParent(Application.dataPath).ToString(),
                    RelativeSaveLocation
                ),
            "OK"
        );
    }

    [MenuItem("MyTools/Test Build/Mac Universal Local Test Build")]
    public static void MacLocalTestBuild()
    {
        SetBaseSettings();
        SetTestSettings();

        // Save path for the build relative to the Unity project root
        var RelativeSaveLocation = "Builds/Mac/OkeyTest.app";

        // Build the player
        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = GetEditorScenes().ToArray(),
            locationPathName = RelativeSaveLocation,
            target = BuildTarget.StandaloneOSX,
            options =
                BuildOptions.Development
                | BuildOptions.AllowDebugging
                | BuildOptions.EnableDeepProfilingSupport
                | BuildOptions.DetailedBuildReport,
            extraScriptingDefines = new[] { "DEBUG", "LOCAL" }
        };

        BuildPipeline.BuildPlayer(buildPlayerOptions);

        EditorUtility.DisplayDialog(
            "Build Complete",
            "Mac Universal Test Build Complete\n\nSaved to: "
                + Path.Join(
                    Directory.GetParent(Application.dataPath).ToString(),
                    RelativeSaveLocation
                ),
            "OK"
        );
    }

    [MenuItem("MyTools/Release Build/Mac Universal Release Build")]
    public static void MacReleaseBuild()
    {
        SetBaseSettings();
        SetReleaseSettings();

        // Save path for the build relative to the Unity project root
        var RelativeSaveLocation = "Builds/Mac/Okey.app";

        // Build the player
        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = GetEditorScenes().ToArray(),
            locationPathName = RelativeSaveLocation,
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.None
        };

        BuildPipeline.BuildPlayer(buildPlayerOptions);

        EditorUtility.DisplayDialog(
            "Build Complete",
            "Mac Universal Release Build Complete\n\nSaved to: "
                + Path.Join(
                    Directory.GetParent(Application.dataPath).ToString(),
                    RelativeSaveLocation
                ),
            "OK"
        );
    }

    [MenuItem("MyTools/Test Build/Windows x86_64 Test Build")]
    public static void WindowsTestBuild()
    {
        SetBaseSettings();
        SetTestSettings();

        // Save path for the build relative to the Unity project root
        var RelativeSaveLocation = "Builds/Windows/OkeyTest.exe";

        // Build the player
        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = GetEditorScenes().ToArray(),
            locationPathName = RelativeSaveLocation,
            target = BuildTarget.StandaloneWindows64,
            options =
                BuildOptions.Development
                | BuildOptions.AllowDebugging
                | BuildOptions.EnableDeepProfilingSupport
                | BuildOptions.DetailedBuildReport,
            extraScriptingDefines = new[] { "DEBUG" }
        };

        BuildPipeline.BuildPlayer(buildPlayerOptions);

        EditorUtility.DisplayDialog(
            "Build Complete",
            "Windows x86_64 Test Build Complete\n\nSaved to: "
                + Path.Join(
                    Directory.GetParent(Application.dataPath).ToString(),
                    RelativeSaveLocation
                ),
            "OK"
        );
    }

    [MenuItem("MyTools/Test Build/Windows x86_64 Local Test Build")]
    public static void WindowsLocalTestBuild()
    {
        SetBaseSettings();
        SetTestSettings();

        // Save path for the build relative to the Unity project root
        var RelativeSaveLocation = "Builds/Windows/OkeyTest.exe";

        // Build the player
        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = GetEditorScenes().ToArray(),
            locationPathName = RelativeSaveLocation,
            target = BuildTarget.StandaloneWindows64,
            options =
                BuildOptions.Development
                | BuildOptions.AllowDebugging
                | BuildOptions.EnableDeepProfilingSupport
                | BuildOptions.DetailedBuildReport,
            extraScriptingDefines = new[] { "DEBUG", "LOCAL" }
        };

        BuildPipeline.BuildPlayer(buildPlayerOptions);

        EditorUtility.DisplayDialog(
            "Build Complete",
            "Windows x86_64 Test Build Complete\n\nSaved to: "
                + Path.Join(
                    Directory.GetParent(Application.dataPath).ToString(),
                    RelativeSaveLocation
                ),
            "OK"
        );
    }

    [MenuItem("MyTools/Release Build/Windows x86_64 Release Build")]
    public static void WindowsReleaseBuild()
    {
        SetBaseSettings();
        SetReleaseSettings();

        // Save path for the build relative to the Unity project root
        var RelativeSaveLocation = "Builds/Windows/Okey.exe";

        // Build the player
        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = GetEditorScenes().ToArray(),
            locationPathName = RelativeSaveLocation,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildPipeline.BuildPlayer(buildPlayerOptions);

        EditorUtility.DisplayDialog(
            "Build Complete",
            "Windows x86_64 Release Build Complete\n\nSaved to: "
                + Path.Join(
                    Directory.GetParent(Application.dataPath).ToString(),
                    RelativeSaveLocation
                ),
            "OK"
        );
    }
}
