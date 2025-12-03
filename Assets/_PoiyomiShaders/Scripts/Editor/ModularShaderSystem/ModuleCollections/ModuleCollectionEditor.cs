using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Poiyomi.ModularShaderSystem.CibbiExtensions.UI
{
    [CustomEditor(typeof(ModuleCollection))]
    public class ModuleCollectionEditor : Editor
    {
        private VisualElement _root;
        private ModuleCollection _collection;

        public override VisualElement CreateInspectorGUI()
        {
            _root = new VisualElement();
            _collection = (ModuleCollection)target;

            var visualTree = Resources.Load<VisualTreeAsset>(MSSConstants.RESOURCES_FOLDER + "/MSSUIElements/ModuleCollectionEditor");
            VisualElement template = visualTree.CloneTree();
            _root.Add(template);
            
            if (_collection.ModulesEnabled == null || _collection.ModulesEnabled.Count != _collection.Modules.Count)
            {
                if (_collection.ModulesEnabled == null)
                    _collection.ModulesEnabled = new List<bool>();
                    
                _collection.ModulesEnabled.Clear();
                for (int i = 0; i < _collection.Modules.Count; i++)
                {
                    _collection.ModulesEnabled.Add(true);
                }
                EditorUtility.SetDirty(_collection);
            }

            return _root;
        }
    }
}