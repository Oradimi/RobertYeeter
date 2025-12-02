using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnlocksManager : MonoBehaviour
{
    private static UnlocksManager _instance;
    
    public static AudioSource AudioSource;
    public static float SoundEffectsVolume;
    
    public static bool Unlocked;
    
    public static int MaxScore;
    public static float MaxDistanceTraveled;

    [SerializeField] private AudioClip caveAmbience;
    [SerializeField] private AudioClip mainMusic;
    [SerializeField] private SkinMenuData skinMenuData;

    private Dictionary<string, string> _skins;
    private bool _nameDisplay;
    
    private void Awake()
    {
        if (_instance)
        {
            Destroy(this);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(_instance);
        
        AudioSource = GetComponent<AudioSource>();
        AudioSource.clip = _instance.caveAmbience;
        AudioSource.Play();
        
        SoundEffectsVolume = GameManager.GetPlayer().GetComponent<AudioSource>().volume;
        
        InitSkin();
    }

    public static void PlayMainMusic()
    {
        if (AudioSource.clip == _instance.mainMusic)
            return;
        AudioSource.clip = _instance.mainMusic;
        AudioSource.Play();
    }

    public static void ChangeEnemyNameDisplay(bool value)
    {
        _instance._nameDisplay = value;
    }

    public static bool GetNameDisplay()
    {
        return _instance._nameDisplay;
    }

    public static void NameDisplay(SceneLabelAttribute attr, SceneLabelOverlay.SceneLabelOverlayData data)
    {
        if (attr.ID != SceneLabelID.EnemyName)
            return;
        
        if (!_instance._nameDisplay)
            attr.Value = "";
    }

    public static void InitSkin()
    {
        _instance._skins = new Dictionary<string, string>();
        foreach (var category in _instance.skinMenuData.categories)
        {
            _instance._skins.Add(category.name, category.skins[0].nameInScene);
            foreach (var skin in category.skins)
                GameManager.GetPlayer().transform.Find(skin.nameInScene).gameObject.SetActive(_instance._skins[category.name] == skin.nameInScene);
        }
    }

    public static void SetSkin(string category, string skinName)
    {
        _instance._skins[category] = skinName;
    }

    public static void ApplySkin()
    {
        var objects = GameManager.GetPlayer().GetComponentsInChildren<Transform>(true).ToDictionary(o => o.name, o => o);
        foreach (var category in _instance.skinMenuData.categories)
            foreach (var skin in category.skins)
                objects[skin.nameInScene].gameObject.SetActive(_instance._skins[category.name] == skin.nameInScene);
    }
    
    public static SkinMenuData GetSkinMenuData()
    {
        return _instance.skinMenuData;
    }
}
