using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Swordstellation.Editor
{
    /// iPhone Xcode project builds on the Mac (Tools/ios-build.sh).
    ///
    ///   -buildTarget iOS -executeMethod Swordstellation.Editor.IosBuild.BuildIos           device
    ///   -buildTarget iOS -executeMethod Swordstellation.Editor.IosBuild.BuildIosSimulator  simulator
    ///
    /// The game code is shared with Android; only player settings differ.
    public static class IosBuild
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        // 1024 square without alpha. iOS rounds the corners itself.
        private const string IosIconPath = "Assets/Art/Generated/AppIcon.png";
        // Same identifier as the Android package.
        private const string BundleId = "com.ymlee.swordstellation";
        private const string TeamId = "3276G27R59";

        [MenuItem("Swordstellation/Configure iOS Project")]
        public static void ConfigureIos()
        {
            PlayerSettings.companyName = "YMLEE Games";
            PlayerSettings.productName = "Swordstellation";
            PlayerSettings.bundleVersion = "0.18.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.use32BitDisplayBuffer = true;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.iOS, ApiCompatibilityLevel.NET_Standard);

            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneOnly;
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.appleDeveloperTeamID = TeamId;
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            PlayerSettings.iOS.requiresFullScreen = true;
            PlayerSettings.iOS.hideHomeButton = false;
            PlayerSettings.iOS.appInBackgroundBehavior = iOSAppInBackgroundBehavior.Suspend;
            // The build number lives in ProjectSettings and is raised per upload,
            // never written here.
            if (string.IsNullOrWhiteSpace(PlayerSettings.iOS.buildNumber) ||
                PlayerSettings.iOS.buildNumber == "0")
            {
                PlayerSettings.iOS.buildNumber = "1";
            }

            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.statusBarHidden = true;

            ApplyIcons();
            ProjectSetup.EnsureMainScene();
            AssetDatabase.SaveAssets();
            Debug.Log($"[IosBuild] configured {BundleId} {PlayerSettings.bundleVersion} ({PlayerSettings.iOS.buildNumber})");
        }

        public static void BuildIos() => Build("Build/ios", simulator: false);
        public static void BuildIosSimulator() => Build("Build/ios-sim", simulator: true);

        private static void Build(string outputPath, bool simulator)
        {
            ConfigureIos();
            Directory.CreateDirectory("Build");

            PlayerSettings.iOS.sdkVersion = simulator ? iOSSdkVersion.SimulatorSDK : iOSSdkVersion.DeviceSDK;
            // The x86_64 default does not install on Apple-silicon iOS 26 simulators.
            PlayerSettings.iOS.simulatorSdkArchitecture = AppleMobileArchitectureSimulator.ARM64;

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { MainScenePath },
                locationPathName = outputPath,
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = BuildOptions.None
            });
            Debug.Log($"[IosBuild] {report.summary.result} / {outputPath} / {report.summary.totalTime}");

            // A simulator SDK left in ProjectSettings would make the next device build a simulator build.
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
            AssetDatabase.SaveAssets();
            if (report.summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
        }

        /// Fills every iOS icon slot every build. An empty slot still builds and
        /// runs, and only the App Store upload rejects it ("Missing app icon").
        private static void ApplyIcons()
        {
            AssetDatabase.ImportAsset(IosIconPath, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(IosIconPath) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false;
                importer.alphaSource = TextureImporterAlphaSource.None;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 1024;
                importer.SaveAndReimport();
            }

            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IosIconPath);
            if (icon == null)
            {
                throw new FileNotFoundException("iOS icon is missing", IosIconPath);
            }

            foreach (IconKind kind in new[]
                     {
                         IconKind.Application, IconKind.Spotlight,
                         IconKind.Settings, IconKind.Notification, IconKind.Store
                     })
            {
                int count = PlayerSettings.GetIconSizes(NamedBuildTarget.iOS, kind).Length;
                if (count == 0)
                {
                    continue;
                }

                Texture2D[] icons = new Texture2D[count];
                for (int i = 0; i < count; i++)
                {
                    icons[i] = icon;
                }
                PlayerSettings.SetIcons(NamedBuildTarget.iOS, icons, kind);
            }
        }
    }
}
