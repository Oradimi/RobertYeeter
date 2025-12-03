using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Utilities
{
    public class BuildIncrementor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 1;

        public void OnPreprocessBuild(BuildReport report)
        {
            var buildScriptableObject = AssetDatabase.LoadAssetAtPath<BuildScriptableObject>("Assets/Resources/Build.asset");

            switch(report.summary.platform)
            {
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneLinux64:
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneOSX:
                    PlayerSettings.macOS.buildNumber = buildScriptableObject.buildNumber.ToString();
                    break;
                case BuildTarget.Android:
                    PlayerSettings.Android.bundleVersionCode = buildScriptableObject.buildNumber;
                    break;
            }

            buildScriptableObject.buildNumber++;
            AssetDatabase.SaveAssets();
        }
    }
}
