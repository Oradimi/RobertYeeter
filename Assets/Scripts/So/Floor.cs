using System;
using TriInspector;
using UnityEngine;

namespace So
{
    [CreateAssetMenu(fileName = "FloorData", menuName = "ScriptableObjects/FloorData", order = 1)]
    public class Floor : ScriptableObject
    {
        [TableList(Draggable = true,
            HideAddButton = false,
            HideRemoveButton = false,
            AlwaysExpanded = false)]
        public Data[] data;
        public Settings[] settings;
        public ZoneTextures[] zoneTextures;
    
        [Serializable]
        [DeclareVerticalGroup("Prefabs")]
        [DeclareVerticalGroup("Data")]
        public class Data
        {
            [Required, HideLabel]
            [Group("Prefabs")]
            public GameObject prefab;
            [Group("Prefabs")]
            public GameObject exitPrefab;
            [Group("Data")]
            public Type type;
            [Group("Data")]
            public Zone zone;
        
            public enum Type
            {
                None,
                Standard,
                Narrow,
                Bridge,
                Exit,
            }

            public enum Zone
            {
                Lush,
                Dry,
                Cold,
            }
        }

        [Serializable]
        public class ZoneTextures
        {
            public Data.Zone zone;
            public ZoneTexture data;
        }
    
        [Serializable]
        public class Settings
        {
            [Serializable]
            [DeclareHorizontalGroup("Outcome")]
            public class Outcome
            {
                [Group("Outcome"), HideLabel]
                public Data.Type type;
                [Group("Outcome"), HideLabel, Unit("weight")]
                public int weight;
            }
        
            public Data.Type type;
            [ListDrawerSettings(Draggable = true,
                HideAddButton = false,
                HideRemoveButton = false,
                AlwaysExpanded = true)]
            public Outcome[] possibleOutcomes;
            private int _totalWeight;

            public void ComputeTotalWeight()
            {
                foreach (var outcome in possibleOutcomes)
                    _totalWeight += outcome.weight;
            }

            public int GetTotalWeight()
            {
                return _totalWeight;
            }
        }
    }
}