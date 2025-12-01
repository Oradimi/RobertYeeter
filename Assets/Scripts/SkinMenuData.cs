using System;
using TriInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "SkinData", menuName = "ScriptableObjects/SkinData", order = 1)]
public class SkinMenuData : ScriptableObject
{
    [Serializable]
    public class CategoryData
    {
        public string name;
        [TableList(Draggable = true,
            HideAddButton = false,
            HideRemoveButton = false,
            AlwaysExpanded = false)]
        public SkinData[] skins;
    }
    
    [Serializable]
    public class SkinData
    {
        public string name;
        public Sprite sprite;
        public string nameInScene;
    }
    
    [ListDrawerSettings(Draggable = true,
        HideAddButton = false,
        HideRemoveButton = false,
        AlwaysExpanded = true)]
    public CategoryData[] categories;
}