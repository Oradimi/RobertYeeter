using System.Collections.Generic;
using UnityEngine;
using Floor = So.Floor;
using Random = UnityEngine.Random;

public static class FloorGeneration
{
    public static void Init(Floor.Settings[] settings)
    {
        foreach (var setting in settings)
            setting.ComputeTotalWeight();
    }
    
    public static List<int> Run(Floor.Settings[] settings, Floor.Data.Type carryOver = Floor.Data.Type.Standard)
    {
        var indexList = new List<int>();
        var previousType = carryOver;

        for (var i = 0; i < 100; i++)
        {
            var selectedType = previousType;

            foreach (var setting in settings)
            {
                if (setting.type != previousType)
                    continue;
                var randomValue = Random.Range(0, setting.GetTotalWeight());
                var accumulatedWeight = 0;
                foreach (var outcome in setting.possibleOutcomes)
                {
                    accumulatedWeight += outcome.weight;
                    if (randomValue >= accumulatedWeight)
                        continue;
                    selectedType = outcome.type;
                    break;
                }
                break;
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