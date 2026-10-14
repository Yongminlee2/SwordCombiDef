using System;
using System.IO;
using System.Linq;
using Swordstellation.Runtime;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#if UNITY_ANDROID || UNITY_EDITOR_WIN
using UnityEditor.Android;
#endif
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Swordstellation.Editor
{
    // UnityEditor.Android exists only in editors with the Android module. The
    // Mac editor used for iPhone builds has the iOS module only, so the
    // Android-only parts are compiled on Windows or when Android is active.
#if UNITY_ANDROID || UNITY_EDITOR_WIN
    public sealed class AndroidGradlePathFix :
        IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => -100;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string gradleRoot = Directory.GetParent(path)?.FullName;
            if (string.IsNullOrWhiteSpace(gradleRoot))
            {
                throw new InvalidOperationException(
                    "Could not resolve the generated Gradle root.");
            }

            string propertiesPath = Path.Combine(
                gradleRoot,
                "gradle.properties");
            const string setting = "android.overridePathCheck=true";
            string existing = File.Exists(propertiesPath)
                ? File.ReadAllText(propertiesPath)
                : string.Empty;
            if (!existing.Contains(setting, StringComparison.Ordinal))
            {
                File.AppendAllText(
                    propertiesPath,
                    Environment.NewLine + setting + Environment.NewLine);
            }
        }
    }
#endif

    public static class ProjectSetup
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        private const string AppIconPath = "Assets/Art/Generated/AppIcon.png";
        private const string ApplicationId = "com.ymlee.swordstellation";

        [MenuItem("Swordstellation/Configure Android Project")]
        public static void ConfigureProject()
        {
            PlayerSettings.companyName = "YMLEE Games";
            PlayerSettings.productName = "Swordstellation";
            PlayerSettings.bundleVersion = "0.18.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.use32BitDisplayBuffer = true;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ApplicationId);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Android, ApiCompatibilityLevel.NET_Standard);

#if UNITY_ANDROID || UNITY_EDITOR_WIN
            PlayerSettings.Android.bundleVersionCode = 23;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.buildApkPerCpuArchitecture = false;
#endif
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

#if UNITY_ANDROID || UNITY_EDITOR_WIN
            ConfigureIcon();
#endif
            EnsureMainScene();
            AssetDatabase.SaveAssets();
            Debug.Log($"Swordstellation configured: {ApplicationId}, portrait, ARM64, API 36.");
        }

        [MenuItem("Swordstellation/Build/Development APK")]
        public static void BuildDevelopmentApk()
        {
            ConfigureProject();
            EditorUserBuildSettings.buildAppBundle = false;
            Directory.CreateDirectory("Builds/Android");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { MainScenePath },
                locationPathName = "Builds/Android/Swordstellation-development.apk",
                target = BuildTarget.Android,
                options = BuildOptions.Development |
                          BuildOptions.CleanBuildCache
            });
            EnsureBuildSucceeded(report);
        }

        [MenuItem("Swordstellation/Build/Release AAB")]
        public static void BuildReleaseBundle()
        {
            ConfigureProject();
            ConfigureSigningFromEnvironment();
            EditorUserBuildSettings.buildAppBundle = true;
            Directory.CreateDirectory("Builds/Android");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { MainScenePath },
                locationPathName = "Builds/Android/Swordstellation-release.aab",
                target = BuildTarget.Android,
                options = BuildOptions.CleanBuildCache
            });
            EnsureBuildSucceeded(report);
        }

        internal static void EnsureMainScene()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            if (!File.Exists(MainScenePath))
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                GameObject bootstrap = new GameObject("SwordstellationBootstrap");
                bootstrap.AddComponent<GameBootstrap>();
                EditorSceneManager.SaveScene(scene, MainScenePath);
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainScenePath, true)
            };
        }

#if UNITY_ANDROID || UNITY_EDITOR_WIN
        private static void ConfigureIcon()
        {
            AssetDatabase.ImportAsset(AppIconPath, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(AppIconPath) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 1024;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath);
            if (icon != null)
            {
#pragma warning disable CS0618
                ClearPlatformIcons(
                    AndroidPlatformIconKind.Legacy);
                ClearPlatformIcons(
                    AndroidPlatformIconKind.Round);
#pragma warning restore CS0618
                ConfigurePlatformIcons(
                    icon,
                    AndroidPlatformIconKind.Adaptive);
            }
        }

        private static void ClearPlatformIcons(
            PlatformIconKind kind)
        {
            PlatformIcon[] icons =
                PlayerSettings.GetPlatformIcons(
                    NamedBuildTarget.Android,
                    kind);
            foreach (PlatformIcon icon in icons)
            {
                icon.SetTextures(
                    new Texture2D[
                        icon.maxLayerCount]);
            }
            PlayerSettings.SetPlatformIcons(
                NamedBuildTarget.Android,
                kind,
                icons);
        }

        private static void ConfigurePlatformIcons(
            Texture2D texture,
            PlatformIconKind kind)
        {
            PlatformIcon[] icons =
                PlayerSettings.GetPlatformIcons(
                    NamedBuildTarget.Android,
                    kind);
            foreach (PlatformIcon icon in icons)
            {
                Texture2D[] layers =
                    Enumerable
                        .Repeat(
                            texture,
                            icon.maxLayerCount)
                        .ToArray();
                icon.SetTextures(layers);
            }
            PlayerSettings.SetPlatformIcons(
                NamedBuildTarget.Android,
                kind,
                icons);
        }

#endif

        private static void ConfigureSigningFromEnvironment()
        {
            string keystorePath = Environment.GetEnvironmentVariable("SWORDSTELLATION_KEYSTORE_PATH");
            string keystorePassword = Environment.GetEnvironmentVariable("SWORDSTELLATION_KEYSTORE_PASSWORD");
            string alias = Environment.GetEnvironmentVariable("SWORDSTELLATION_KEY_ALIAS");
            string aliasPassword = Environment.GetEnvironmentVariable("SWORDSTELLATION_KEY_PASSWORD");

            if (string.IsNullOrWhiteSpace(keystorePath) ||
                string.IsNullOrWhiteSpace(keystorePassword) ||
                string.IsNullOrWhiteSpace(alias) ||
                string.IsNullOrWhiteSpace(aliasPassword))
            {
                throw new InvalidOperationException(
                    "Release signing requires SWORDSTELLATION_KEYSTORE_PATH, " +
                    "SWORDSTELLATION_KEYSTORE_PASSWORD, SWORDSTELLATION_KEY_ALIAS, " +
                    "and SWORDSTELLATION_KEY_PASSWORD.");
            }

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = Path.GetFullPath(keystorePath);
            PlayerSettings.Android.keystorePass = keystorePassword;
            PlayerSettings.Android.keyaliasName = alias;
            PlayerSettings.Android.keyaliasPass = aliasPassword;
        }

        private static void EnsureBuildSucceeded(BuildReport report)
        {
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Android build failed: {report.summary.result}, errors: {report.summary.totalErrors}");
            }

            Debug.Log(
                $"Android build succeeded: {report.summary.outputPath} " +
                $"({report.summary.totalSize / 1024f / 1024f:0.0} MB)");
        }
    }
}
