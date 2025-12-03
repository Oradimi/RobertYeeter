using System;
using TriInspector;
using UnityEngine;

namespace So
{
    [CreateAssetMenu(fileName = "SkinData", menuName = "ScriptableObjects/SkinData", order = 1)]
    public class Skin : ScriptableObject
    {
        [ListDrawerSettings(Draggable = true,
            HideAddButton = false,
            HideRemoveButton = false,
            AlwaysExpanded = true)]
        public Category[] data;
    
        [Serializable]
        public class Category
        {
            public string name;
            [TableList(Draggable = true,
                HideAddButton = false,
                HideRemoveButton = false,
                AlwaysExpanded = false)]
            public Data[] skins;
        
            [Serializable]
            public class Data
            {
                public string name;
                public Sprite sprite;
                public string nameInScene;
                public int scoreRequired;
            }
        }
    }
}