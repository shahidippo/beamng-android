using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ImpactDrive.Editor
{
    public static class BuildAndroid
    {
        [MenuItem("ImpactDrive/Build development APK")]
        public static void Development()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new BuildFailedException("Install Android Build Support, SDK/NDK and OpenJDK for Unity 6000.0.62f1.");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new BuildFailedException("Select Android in Build Profiles, or start the Editor with -buildTarget Android.");
            ProjectSetup.Configure();
            string path = Environment.GetEnvironmentVariable("IMPACTDRIVE_APK_PATH");
            if (string.IsNullOrWhiteSpace(path)) path = "Artifacts/Android/ImpactDrive-development.apk";
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.ScenePath },
                locationPathName = path,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded || !File.Exists(path))
                throw new BuildFailedException("APK build failed: " + report.summary.result + " (" + report.summary.totalErrors + " errors).");
            Debug.Log("Development APK generated: " + Path.GetFullPath(path) + ". Install and test on a real ARM64 device before claiming readiness.");
        }
    }
}
