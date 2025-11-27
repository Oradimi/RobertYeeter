using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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
    
    public static List<int> Init(Settings[] settings)
    {
        var generationCount = 0;
        foreach (var setting in settings)
            generationCount += setting.count;
        
        var tempSettings = new List<Settings>();
        tempSettings = settings.ToList();
        tempSettings.Sort(Settings.CompareByType);
        
        var indexList = new List<int>();
        var previousType = FloorManager.FloorData.Type.Standard;

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
            
            if (GameManager.GetDistanceTraveled() > 1000f && Random.Range(0, 3) == 0)
                selectedType |= FloorManager.FloorData.Type.Exit;
            
            previousType = selectedType;
            // tempSettings[(int)selectedType].count++;

            var floorData = FloorManager.GetFloorData();
            var currentZone = FloorManager.GetCurrentZone();
            var typeIndices = new List<int>();
            for (var j = 0; j < floorData.Length; j++)
            {
                if (floorData[j].type == selectedType && floorData[j].zone == currentZone)
                    typeIndices.Add(j);
            }

            if (typeIndices.Count <= 0 && selectedType.HasFlag(FloorManager.FloorData.Type.Exit))
            {
                selectedType ^= FloorManager.FloorData.Type.Exit;
                for (var j = 0; j < floorData.Length; j++)
                {
                    if (floorData[j].type == selectedType && floorData[j].zone == currentZone)
                        typeIndices.Add(j);
                }
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