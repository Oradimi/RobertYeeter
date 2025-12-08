#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Utilities
{
    public class BuildIncrementor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 1;
        
        private readonly string _buildAssetPath = "Assets/Resources/Build.asset";

        public void OnPreprocessBuild(BuildReport report)
        {
            var buildScriptableObject = AssetDatabase.LoadAssetAtPath<BuildScriptableObject>(_buildAssetPath);
            
            if (buildScriptableObject == null)
            {
                Debug.LogError($"BuildIncrementor: Could not find Build.asset at path '{_buildAssetPath}'.");
                return;
            }

            switch (report.summary.platform)
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
            EditorUtility.SetDirty(buildScriptableObject);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Game build number successfully incremented to v{Application.version}_{buildScriptableObject.buildNumber:000}_{buildScriptableObject.developmentPhase.ToString().ToLowerInvariant()}");
        }
    }
}
#endif
