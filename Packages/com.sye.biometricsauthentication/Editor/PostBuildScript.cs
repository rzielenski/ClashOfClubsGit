#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using UnityEngine;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif
namespace SyE.BiometricsAuthentication.Editor
{
    public class PostBuildScript
    {
        [PostProcessBuild]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target == BuildTarget.iOS)
            {
                AddFaceIDUsageDescription(pathToBuiltProject);
                Debug.Log("NSFaceIDUsageDescription added to Info.plist");
            }
        }

        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                string destDir = Path.Combine(destinationDir, Path.GetFileName(dir));
                CopyDirectory(dir, destDir);
            }
        }

        private static void AddFaceIDUsageDescription(string pathToBuiltProject)
        {
    #if UNITY_IOS
            string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
            PlistDocument plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            PlistElementDict rootDict = plist.root;
            rootDict.SetString("NSFaceIDUsageDescription", "This app uses Face ID to unlock features securely.");
            File.WriteAllText(plistPath, plist.WriteToString());
    #endif
        }
    }
}
#endif