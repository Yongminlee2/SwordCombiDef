#if UNITY_IOS
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace Swordstellation.Editor
{
    /// Fills Info.plist and the per-language home-screen name after Unity writes
    /// the Xcode project. iOS picks the home-screen name by the phone language.
    public sealed class IosPostBuild : IPostprocessBuildWithReport
    {
        /// Keys are .lproj folder names. The game itself speaks Korean and English.
        private static readonly IReadOnlyDictionary<string, string> AppName =
            new Dictionary<string, string>
            {
                ["en"] = "Swordstellation",
                ["ko"] = "검별공방"
            };

        public int callbackOrder => 200;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS)
            {
                return;
            }

            string root = report.summary.outputPath;
            UpdateInfoPlist(Path.Combine(root, "Info.plist"));
            AddLocalizations(root);
            Debug.Log("[IosPostBuild] Info.plist and localized names applied");
        }

        private static void UpdateInfoPlist(string path)
        {
            PlistDocument plist = new PlistDocument();
            plist.ReadFromFile(path);
            PlistElementDict dict = plist.root;

            // English default so phones in other languages do not show the Korean name.
            dict.SetString("CFBundleDisplayName", AppName["en"]);
            dict.SetString("CFBundleDevelopmentRegion", "en");

            // iOS only uses the languages listed here.
            PlistElementArray languages = dict.CreateArray("CFBundleLocalizations");
            foreach (string code in AppName.Keys)
            {
                languages.AddString(code);
            }

            // No encryption besides the system's; without this key every upload
            // stops at the export-compliance question.
            dict.SetBoolean("ITSAppUsesNonExemptEncryption", false);

            plist.WriteToFile(path);
        }

        private static void AddLocalizations(string root)
        {
            string projectPath = PBXProject.GetPBXProjectPath(root);
            PBXProject project = new PBXProject();
            project.ReadFromFile(projectPath);
            string target = project.GetUnityMainTargetGuid();

            foreach (KeyValuePair<string, string> pair in AppName)
            {
                string folder = $"Localization/{pair.Key}.lproj";
                Directory.CreateDirectory(Path.Combine(root, folder));
                StringBuilder lines = new StringBuilder();
                lines.Append(Entry("CFBundleDisplayName", pair.Value));
                lines.Append(Entry("CFBundleName", pair.Value));
                File.WriteAllText(
                    Path.Combine(root, folder, "InfoPlist.strings"),
                    lines.ToString(),
                    new UTF8Encoding(false));

                // Rebuilding into the same folder must not register it twice.
                if (project.FindFileGuidByProjectPath(folder) != null)
                {
                    continue;
                }
                string guid = project.AddFile(folder, folder, PBXSourceTree.Source);
                project.AddFileToBuild(target, guid);
            }
            project.WriteToFile(projectPath);
        }

        private static string Entry(string key, string value) =>
            $"\"{key}\" = \"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\";\n";
    }
}
#endif
