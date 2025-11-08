using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Poi.Tools
{
    [CustomEditor(typeof(PoiExportPackageObject))]
    public class PoiExportPackage : Editor
    {
        SerializedProperty title;
        SerializedProperty filesToRemove;
        SerializedProperty filesToAdd;

        ReorderableList listRemove;
        ReorderableList listAdd;
        private void OnEnable()
        {
            title = serializedObject.FindProperty(nameof(PoiExportPackageObject.title));

            filesToRemove = serializedObject.FindProperty(nameof(PoiExportPackageObject.filesToRemove));
            listRemove = new ReorderableList(serializedObject, filesToRemove, true, true, true, true);
            listRemove.elementHeight = EditorGUIUtility.singleLineHeight * 1.4f;

            filesToAdd = serializedObject.FindProperty(nameof(PoiExportPackageObject.filesToAdd));
            listAdd = new ReorderableList(serializedObject, filesToAdd, true, true, true, true);
            listAdd.elementHeight = EditorGUIUtility.singleLineHeight * 1.4f;

            listRemove.drawHeaderCallback = (Rect rect) => EditorGUI.LabelField(rect, "Files to Remove");
            listRemove.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                DrawListItems(listRemove, rect, index, isActive, isFocused);
            };

            listAdd.drawHeaderCallback = (Rect rect) => EditorGUI.LabelField(rect, "Files to Add");
            listAdd.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                DrawListItems(listAdd, rect, index, isActive, isFocused);
            };
        }
        void DrawListItems(ReorderableList list, Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
            rect.height = 21f;
            rect.y += 2f;
            GUIContent label = GUIContent.none;
            if (element.objectReferenceValue != null)
            {
                label = new GUIContent(AssetDatabase.GetAssetPath(element.objectReferenceValue).Replace("Assets/_PoiyomiShaders/", ""));
                label.tooltip = label.text;
            }
            EditorGUI.PropertyField(rect, element, label);
        }
        GUIStyle headerTextFieldGUIStyle;
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (headerTextFieldGUIStyle == null)
            {
                headerTextFieldGUIStyle = new GUIStyle(GUI.skin.label);
                headerTextFieldGUIStyle.alignment = TextAnchor.MiddleCenter;
                headerTextFieldGUIStyle.fontSize = 16;
                headerTextFieldGUIStyle.fontStyle = FontStyle.Bold;
            }
            EditorGUI.BeginChangeCheck();
            string tempstr = EditorGUILayout.DelayedTextField(title.stringValue, headerTextFieldGUIStyle, GUILayout.ExpandWidth(true));
            if (EditorGUI.EndChangeCheck())
            {
                title.stringValue = tempstr;
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Export"))
            {
                ThirdPartyIncluder.mi_ThirdPartyIncluderCleanDestAssets(); // Only used for Pro but we'll do it for all anyway ;)
                ExportPackage(title.stringValue, SerialisedPropListToPathList(filesToAdd), SerialisedPropListToPathList(filesToRemove));
            }

            EditorGUILayout.Space();

            var oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = (EditorGUIUtility.currentViewWidth / 3.5f) * 2.0f;
            listRemove.DoLayoutList();
            listAdd.DoLayoutList();
            EditorGUIUtility.labelWidth = oldWidth;

            serializedObject.ApplyModifiedProperties();
        }

        private IEnumerable<string> SerialisedPropListToPathList(SerializedProperty prop)
        {
            List<string> list = new List<string>(prop.arraySize);
            for (int index = 0; index < prop.arraySize; index++)
            {
                var element = prop.GetArrayElementAtIndex(index);
                if (element.objectReferenceValue != null)
                {
                    list.Add(AssetDatabase.GetAssetPath(element.objectReferenceValue));
                }
            }
            return list;
        }
        static readonly Regex rich = new Regex(@"<[^>]*>");
        private static void ExportPackage(string _title, IEnumerable<string> _filesToAdd, IEnumerable<string> _filesToRemove, bool skipDialog = false)
        {
            string firstLine = File.ReadLines("Assets/_PoiyomiShaders/ModularShader/Editor/Poi_UIBoilerPlate/VRLT_PropsUIBoilerPlate.poiTemplate").First();
            firstLine = rich.Replace(firstLine, string.Empty);
            int start = firstLine.IndexOf("Poiyomi ") + "Poiyomi ".Length;
            int end = firstLine.LastIndexOf("\",");
            string version = firstLine.Substring(start, end - start);
            string fileName = $"poi_{_title}_{version}.unitypackage";
            if (File.Exists(fileName))
            {
                bool result = EditorUtility.DisplayDialog("Poi Export, File already exists", $"{fileName} already exists, did you forget to change the version number.\nDo you want to override it?", "Yes", "No");
                if (!result) return;
            }
            try
            {
                EditorUtility.DisplayProgressBar("Package Export", "Searching for assets", 1 / 4.0f);
                var guids = AssetDatabase.FindAssets("t:Object", new[] { "Assets/_PoiyomiShaders" });
                EditorUtility.DisplayProgressBar("Package Export", "Getting their paths", 2 / 4.0f);
                var filesFound = guids.Select(x => AssetDatabase.GUIDToAssetPath(x)).ToList();
                EditorUtility.DisplayProgressBar("Package Export", "Applying denylisted and allowlisted files", 3 / 4.0f);
                List<string> filesToExport = new List<string>();
                foreach (var fileToAdd in _filesToAdd)
                {
                    filesToExport.AddRange(filesFound.Where(x => x.StartsWith(fileToAdd)));
                }
                foreach (var fileToRemove in _filesToRemove)
                {
                    filesToExport.RemoveAll(x => x.StartsWith(fileToRemove));
                }
                EditorUtility.DisplayProgressBar("Package Export", "Exporting Package!", 4 / 4.0f);
                AssetDatabase.ExportPackage(filesToExport.ToArray(), fileName);
                EditorUtility.ClearProgressBar();
                if (!skipDialog) EditorUtility.DisplayDialog("Poi Export, Package exported", $"Package exported to {Path.GetFullPath(fileName)} with {filesToExport.Count} files!", "Continue");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
        private static void CleanUp()
        {
            PoiModulesToggles.Init();
            if (PoiModulesToggles.IsModified()) PoiModulesToggles.Revert(skipDialog: true);
            ThirdPartyIncluder.mi_ThirdPartyIncluderCleanDestAssets();
        }
        [MenuItem("Poi/Tools/Export Pro Package")]
        private static void ExportProPackage()
        {
            CleanUp();

            var pro = AssetDatabase.LoadAssetAtPath<PoiExportPackageObject>(AssetDatabase.GUIDToAssetPath("98b8329fcee9eb3438336c6a5679a354"));
            ExportPackage(pro.title, pro.filesToAdd.Select(x => AssetDatabase.GetAssetPath(x)), pro.filesToRemove.Select(x => AssetDatabase.GetAssetPath(x)));
        }
        [MenuItem("Poi/Tools/Export ProSlim Package")]
        private static void ExportProSlimPackage()
        {
            CleanUp();

            var proSlim = AssetDatabase.LoadAssetAtPath<PoiExportPackageObject>(AssetDatabase.GUIDToAssetPath("5771311c7021b9540a371715cf9885a8"));
            ExportPackage(proSlim.title, proSlim.filesToAdd.Select(x => AssetDatabase.GetAssetPath(x)), proSlim.filesToRemove.Select(x => AssetDatabase.GetAssetPath(x)));
        }
        [MenuItem("Poi/Tools/Export Pro (Both) Package")]
        private static void ExportProBothPackage()
        {
            CleanUp();

            var pro = AssetDatabase.LoadAssetAtPath<PoiExportPackageObject>(AssetDatabase.GUIDToAssetPath("98b8329fcee9eb3438336c6a5679a354"));
            ExportPackage(pro.title, pro.filesToAdd.Select(x => AssetDatabase.GetAssetPath(x)), pro.filesToRemove.Select(x => AssetDatabase.GetAssetPath(x)), skipDialog: true);
            var proSlim = AssetDatabase.LoadAssetAtPath<PoiExportPackageObject>(AssetDatabase.GUIDToAssetPath("5771311c7021b9540a371715cf9885a8"));
            ExportPackage(proSlim.title, proSlim.filesToAdd.Select(x => AssetDatabase.GetAssetPath(x)), proSlim.filesToRemove.Select(x => AssetDatabase.GetAssetPath(x)));
        }
        [MenuItem("Poi/Tools/Export Toon Package")]
        private static void ExportToonPackage()
        {
            PoiModulesToggles.Init();
            if (PoiModulesToggles.IsModified()) PoiModulesToggles.Revert(skipDialog: true);

            var toon = AssetDatabase.LoadAssetAtPath<PoiExportPackageObject>(AssetDatabase.GUIDToAssetPath("7b0de107479224743a84bdf7e6202815"));
            ExportPackage(toon.title, toon.filesToAdd.Select(x => AssetDatabase.GetAssetPath(x)), toon.filesToRemove.Select(x => AssetDatabase.GetAssetPath(x)));
        }
    }
}