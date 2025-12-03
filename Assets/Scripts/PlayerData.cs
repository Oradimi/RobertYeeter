using System.Collections.Generic;
using MessagePack;

[MessagePackObject]
public class PlayerData
{
    [Key(00)] public int Build;
    [Key(01)] public int MusicVolume;
    [Key(02)] public int SoundVolume;
    [Key(03)] public bool EnemyNameDisplay;
    [Key(07)] public Dictionary<string, string> SelectedSkins;
    [Key(11)] public int MaxScore;
    [Key(12)] public float MaxDistanceTraveled;
}