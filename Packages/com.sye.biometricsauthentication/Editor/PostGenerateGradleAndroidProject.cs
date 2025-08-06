#if UNITY_EDITOR && UNITY_ANDROID
using System.IO;
using UnityEngine;
using UnityEditor.Android;
namespace SyE.BiometricsAuthentication.Editor
{
    public class PostGenerateGradleAndroidProject : IPostGenerateGradleAndroidProject {
        public int callbackOrder => 0;

        public void OnPostGenerateGradleAndroidProject (string path) {
            string gradleFilePath = Path.Join (path, "build.gradle");
            if (File.Exists (gradleFilePath)) {
                string gradleContent = File.ReadAllText (gradleFilePath);
                if (!gradleContent.Contains ("implementation \"androidx.biometric:biometric:1.1.0\"")) {
                    string dependenciesBlock = "dependencies {";
                    int dependenciesIndex = gradleContent.IndexOf (dependenciesBlock);
                    if (dependenciesIndex >= 0) {
                        int insertIndex = dependenciesIndex + dependenciesBlock.Length;
                        string dependency = "\n    implementation \"androidx.biometric:biometric:1.1.0\"";
                        gradleContent = gradleContent.Insert (insertIndex, dependency);
                        File.WriteAllText (gradleFilePath, gradleContent);
                        Debug.Log ("Added implementation \"androidx.biometric:biometric:1.1.0\" to mainTemplate.gradle");
                    } else {
                        Debug.LogError ("dependencies block not found in build.gradle");
                    }
                } else {
                    Debug.Log ("biometric dependency already exists in build.gradle");
                }
            } else {
                Debug.LogError ("build.gradle file not found: " + gradleFilePath);
            }

            string propertiesFilePath = Path.Combine (path, "../gradle.properties");
            if (File.Exists (propertiesFilePath)) {
                string propertiesContent = File.ReadAllText (propertiesFilePath);
                if (!propertiesContent.Contains ("android.useAndroidX=true")) {
                    propertiesContent += "\nandroid.useAndroidX=true";
                    File.WriteAllText (propertiesFilePath, propertiesContent);
                    Debug.Log ("Added android.useAndroidX=true to gradleTemplate.properties");
                }
            } else {
                Debug.LogError ("gradle.properties file not found: " + propertiesFilePath);
            }

            string manifestFilePath = Path.Combine (path, "src", "main" ,"AndroidManifest.xml");

            if (File.Exists (manifestFilePath)) {
                string manifestContent = File.ReadAllText (manifestFilePath);
                if (!manifestContent.Contains ("sye.biometricsauthentication.EmptyFragmentActivity")) {
                    string activityTag = "\n        <activity android:name=\"sye.biometricsauthentication.EmptyFragmentActivity\"\n                  android:theme=\"@android:style/Theme.Translucent.NoTitleBar.Fullscreen\"></activity>";
                    manifestContent = manifestContent.Replace ("</application>", activityTag + "\n    </application>");
                    File.WriteAllText (manifestFilePath, manifestContent);
                    Debug.Log ("Added EmptyFragmentActivity to AndroidManifest.xml");
                } else {
                    Debug.Log ("EmptyFragmentActivity already exists in AndroidManifest.xml");
                }
            } else {
                Debug.LogError ("AndroidManifest.xml not found: " + manifestFilePath);
            }
        }
    }
}
#endif