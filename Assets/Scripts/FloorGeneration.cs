using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public static class FloorGeneration
{
    [Serializable]
    public class Settings
    {
        public FloorManager.FloorData.Type type;
        public int count;

        public static int CompareByType(Settings s1, Settings s2)
        {
            return s1.type.CompareTo(s2.type);
        }

        public Settings Clone()
        {
            return new Settings()
            {
                type = this.type,
                count = this.count,
            };
        }
    }
    
    public static List<int> Init(FloorManager.FloorData.Type carryOver = FloorManager.FloorData.Type.Standard)
    {
        var indexList = new List<int>();
        var previousType = carryOver;

        for (var i = 0; i < 100; i++)
        {
            var selectedType = previousType;
            var randomChance = 0;
            if (previousType.HasFlag(FloorManager.FloorData.Type.Standard))
            {
                randomChance = Random.Range(0, 3);
                if (randomChance == 0)
                    selectedType = FloorManager.FloorData.Type.Standard;
                else
                    selectedType = FloorManager.FloorData.Type.Bridge;
            }
            else if (previousType.HasFlag(FloorManager.FloorData.Type.Narrow))
            {
                randomChance = Random.Range(0, 3);
                if (randomChance == 0)
                    selectedType = FloorManager.FloorData.Type.Narrow;
                else
                    selectedType = FloorManager.FloorData.Type.Standard;
            }
            else if (previousType.HasFlag(FloorManager.FloorData.Type.Bridge))
            {
                randomChance = Random.Range(0, 3);
                if (randomChance == 0)
                    selectedType = FloorManager.FloorData.Type.Standard;
                else
                    selectedType = FloorManager.FloorData.Type.Narrow;
            }
            
            previousType = selectedType;

            var floorData = FloorManager.GetFloorData();
            var currentZone = FloorManager.GetCurrentZone();
            var typeIndices = new List<int>();
            for (var j = 0; j < floorData.Length; j++)
            {
                if (floorData[j].type == selectedType && floorData[j].zone == currentZone)
                    typeIndices.Add(j);
            }

            if (typeIndices.Count <= 0)
            {
                Debug.LogWarning($"The zone {currentZone} has no prefab of type {selectedType}!");
                if (indexList.Count <= 0)
                    indexList.Add(0);
                return indexList;
            }
                
            var selectedIndex = typeIndices[typeIndices.Count <= 0 ? 0 : Random.Range(0, typeIndices.Count)];
            indexList.Add(selectedIndex);
        }
        
        return indexList;
    }
}