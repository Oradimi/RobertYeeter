using System;
using System.Collections.Generic;
using System.Linq;
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
        foreach (var setting in settings)
            tempSettings.Add(setting.Clone());
        tempSettings.Sort(Settings.CompareByType);
        
        var indexList = new List<int>();
        var previousType = FloorManager.FloorData.Type.Standard;

        for (var i = 0; i < generationCount; i++)
        {
            var selectedType = previousType;
            var randomChance = 0;
            switch (previousType)
            {
                case FloorManager.FloorData.Type.Standard:
                    randomChance = Random.Range(0, 3);
                    if (randomChance == 0)
                        selectedType = FloorManager.FloorData.Type.Standard;
                    else
                        selectedType = FloorManager.FloorData.Type.Bridge;
                    break;
                case FloorManager.FloorData.Type.Narrow:
                    randomChance = Random.Range(0, 3);
                    if (randomChance == 0)
                        selectedType = FloorManager.FloorData.Type.Narrow;
                    else
                        selectedType = FloorManager.FloorData.Type.Standard;
                    break;
                case FloorManager.FloorData.Type.Bridge:
                    randomChance = Random.Range(0, 3);
                    if (randomChance == 0)
                        selectedType = FloorManager.FloorData.Type.Standard;
                    else
                        selectedType = FloorManager.FloorData.Type.Narrow;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            previousType = selectedType;
            tempSettings[(int)selectedType].count--;

            var floorData = FloorManager.GetFloorData();
            var typeIndices = new List<int>();
            for (var j = 0; j < floorData.Length; j++)
            {
                if (floorData[j].type == selectedType)
                    typeIndices.Add(j);
            }
            
            var selectedIndex = typeIndices[Random.Range(0, typeIndices.Count)];
            indexList.Add(selectedIndex);
            if (tempSettings[(int)selectedType].count <= 0)
                return indexList;
        }
        
        return indexList;
    }
}