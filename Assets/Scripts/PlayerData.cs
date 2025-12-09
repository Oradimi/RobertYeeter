using System.Collections.Generic;
using MessagePack;

[MessagePackObject]
public class PlayerData
{
    [Key(00)] public int Build;
    [Key(01)] public int MusicVolume;
    [Key(02)] public int SoundVolume;
    [Key(03)] public bool EnemyNameDisplay;
    [Key(04)] public bool AntiAliasing;
    [Key(05)] public Dictionary<string, string> SelectedSkins;
    [Key(06)] public int MaxScore;
    [Key(07)] public float MaxDistanceTraveled;
}