#if UNITY_EDITOR
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

namespace Utilities
{
    public static class ShaderGraphExporter
    {
        private static readonly Regex ExtraPasses = new Regex(
            @"HLSL\s*}\s*Pass\s*{.+END",
            RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.Singleline
        );

        [MenuItem("Assets/Export as single-pass Shader", false)]
        public static void ExportSinglePassShader()
        {
            const string tempDirectory = "Temp";
        
            var obj = Selection.activeObject;
            var path = AssetDatabase.GetAssetPath(obj);

            var directory = Path.GetDirectoryName(path);
            var name = Path.GetFileNameWithoutExtension(path);

            if (directory == null)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    $"Directory not found.",
                    "OK"
                );
                return;
            }

            var shaderPath = Path.Combine(tempDirectory, "GeneratedFromGraph-" + name + ".shader");

            if (!File.Exists(shaderPath))
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    $"Generated shader not found:\n{shaderPath}\n\nView the generated shader graph first.\nMake sure to regenerate it after each change.",
                    "OK"
                );
                return;
            }

            var shaderText = File.ReadAllText(shaderPath);
            var processed = ExtraPasses.Replace(shaderText, "");
            var outPath = Path.Combine(directory, name + ".shader");

            File.WriteAllText(outPath, processed);
            AssetDatabase.Refresh();
        }
    
        [MenuItem("Assets/Export as single-pass Shader", true)]
        public static bool ExportFromContext_Validate()
        {
            var obj = Selection.activeObject;
            if (!obj)
                return false;

            var path = AssetDatabase.GetAssetPath(obj);
            return path != null && path.EndsWith(".shadergraph");
        }
    }
}
#endif
