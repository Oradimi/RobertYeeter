using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SceneLabel;
using So;
using UnityEngine;
using Utilities;

public class GameManager : MonoBehaviour
{
    public enum GameOverCase
    {
        Caught,
        Bonked,
        Drowned,
    }
    
    private static GameManager _instance;
    
    public static PlayerData PlayerData;
    public static readonly string PlayerDataPath = "/saveData";
    
    public static AudioSource AudioSource;
    public static float SoundEffectsVolume;
    public static bool EnemyNameDisplay;
    
    public static float GlobalSpeed;
    public static bool AffectsAnimations;
    
    public static float GlobalSpeedStored;
    public static bool AffectsAnimationsStored;
    
    [Header("Data")]
    [SerializeField] private Skin soSkin;
    [SerializeField] private PlayerController player;
    [SerializeField] private float nextLevelDistance;
    
    [Header("Music")]
    [SerializeField] private AudioClip caveAmbience;
    [SerializeField] private AudioClip mainMusic;

    [SceneLabel(SceneLabelID.Score)]
    private int _score;
    
    [SceneLabel(SceneLabelID.DistanceTraveled, suffix: "m", fontSize: 12)]
    private float _distanceTraveled;

    private Vector3 _targetPosition;
    private float _effectTime;
    private int _combo;
    private int _gain;
    
    private void Awake()
    {
        if (_instance)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(_instance);
        
        GlobalSpeed = 1f;
        _targetPosition = transform.position;
        
        InitPlayerData();
        InitAudio();
        InitSettings();
        InitSkin();
    }

    private void FixedUpdate()
    {
        if (FloorManager.IsPaused() || FloorManager.IsGameOver())
            return;
        
        _instance._effectTime -= Time.fixedDeltaTime;
        transform.position = Vector3.MoveTowards(transform.position, _targetPosition, 
            Time.fixedDeltaTime);
        
        if (FloorManager.IsStarted())
            _instance._distanceTraveled += FloorManager.GetFloorScrollSpeed() * Time.fixedDeltaTime;
    }

    public static void SetTargetPosition(Vector3 position)
    {
        _instance._targetPosition = position + new Vector3(0.08f, 0f, 1.25f);
    }

    public static void AddScore(int score)
    {
        if (_instance._effectTime > 0f)
            _instance._combo++;
        else
            _instance._combo = 1;
        
        _instance._gain = _instance._combo * score;
        _instance._score += _instance._combo * score;
        _instance._effectTime = 2f;
    }
    
    public static void ResetScore()
    {
        _instance._score = 0;
        _instance._distanceTraveled = 0;
    }

    public static float NextLevelStairsChance()
    {
        return (_instance._distanceTraveled - _instance.nextLevelDistance * (FloorManager.GetLevelCount() - 1)) / _instance.nextLevelDistance;
    }

    public static void GameOver(GameOverCase gameOverCase, Transform deathCause = null)
    {
        _instance.player.DisableActionMap();
        GlobalSpeed = 0f;
        AffectsAnimations = false;

        if (_instance._score > PlayerData.MaxScore)
            PlayerData.MaxScore = _instance._score;
        if (_instance._distanceTraveled > PlayerData.MaxDistanceTraveled)
            PlayerData.MaxDistanceTraveled = _instance._distanceTraveled;
        
        FloorManager.GameOver(gameOverCase, deathCause);
    }

    public static void SetPlayer(PlayerController player)
    {
        _instance.player = player;
    }

    public static PlayerController GetPlayer()
    {
        return _instance.player;
    }

    public static void GameManagerLabelEffect(SceneLabelAttribute attr, SceneLabelOverlay.SceneLabelOverlayData data)
    {
        string prefix;
        switch (attr.ID)
        {
            case SceneLabelID.Score:
                prefix = FloorManager.IsGameOver() ? "Score — " : "";
                attr.Value = FloorManager.IsPaused() ? "Paused" : FloorManager.IsStarted() ? $"{prefix}{_instance._score}" : "";
        
                if (_instance._effectTime <= 0f || FloorManager.IsPaused() || FloorManager.IsGameOver() || !FloorManager.IsStarted())
                {
                    attr.FormatValue = null;
                    attr.RichValue = null;
                    return;
                }

                var fontSize = attr.FontSize * data.GameViewScale + _instance._effectTime * 8f * data.GameViewScale;
                var gainFontSize = _instance._effectTime * attr.FontSize * 0.6 * data.GameViewScale;
                var comboColor = Color.Lerp(Color.chocolate, Color.crimson, (_instance._combo - 1) / 3f);
                var color = Color.Lerp(Color.white, comboColor, _instance._effectTime);
                attr.FormatValue = $"<size={fontSize}>{_instance._score}</size><size={gainFontSize}>+{_instance._gain}</size>";
                attr.RichValue = $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{attr.FormatValue}</color>";
                break;
            case SceneLabelID.DistanceTraveled:
                prefix = FloorManager.IsGameOver() ? "Reached — " : "";
                attr.Value = FloorManager.IsPaused() ? "Escape key to resume" : FloorManager.IsStarted() ? $"{prefix}{_instance._distanceTraveled:F1}" : "";
                attr.Suffix = FloorManager.IsPaused() || !FloorManager.IsStarted() ? "" : "m";
                break;
        }
    }

    public static void PlayMainMusic()
    {
        if (AudioSource.clip == _instance.mainMusic)
            return;
        AudioSource.clip = _instance.mainMusic;
        AudioSource.Play();
    }

    public static void EnemyNameDisplayLabelEffect(SceneLabelAttribute attr, SceneLabelOverlay.SceneLabelOverlayData data)
    {
        if (attr.ID != SceneLabelID.EnemyName)
            return;
        
        if (!EnemyNameDisplay)
            attr.Value = "";

        attr.RichValue = attr.Value.ToString() == "Jean-Pierre" ? $"<color=red>{attr.Value}</color>" : null;
    }
    
    private static void InitPlayerData()
    {
        try
        {
            PlayerData = DataService.LoadData<PlayerData>(PlayerDataPath);
        }
        catch (Exception e)
        {
            if (e is not FileNotFoundException)
            {
                var path = Application.persistentDataPath + PlayerDataPath;
                var bytes = File.ReadAllBytes(path);
                File.WriteAllBytes(path + "Corrupted", bytes);
                Debug.LogError("Invalid save file. Created backup.");
            }
            
            PlayerData = new PlayerData
            {
                MusicVolume = 40,
                SoundVolume = 40,
                SelectedSkins = new Dictionary<string, string>()
            };

            var obj = Resources.Load("Build", typeof(BuildScriptableObject));
            var buildScriptableObject = obj as BuildScriptableObject;

            if (buildScriptableObject == null)
                Debug.LogError("Build scriptable object not found in resources directory.");
            else
                PlayerData.Build = buildScriptableObject.buildNumber;

            SaveData();
        }
    }

    private static void InitAudio()
    {
        AudioSource = _instance.GetComponent<AudioSource>();
        AudioSource.clip = _instance.caveAmbience;
        AudioSource.Play();
        AudioSource.volume = PlayerData.MusicVolume * 0.01f;

        var playerAudioSource = _instance.player.GetComponent<AudioSource>();
        playerAudioSource.volume = PlayerData.SoundVolume * 0.01f;
        SoundEffectsVolume = playerAudioSource.volume;
    }

    private static void InitSettings()
    {
        EnemyNameDisplay = PlayerData.EnemyNameDisplay;
    }

    private static void InitSkin()
    {
        foreach (var category in _instance.soSkin.data)
        {
            if (!PlayerData.SelectedSkins.TryGetValue(category.name, out _))
                PlayerData.SelectedSkins.Add(category.name, category.skins[0].nameInScene);
            
            foreach (var skin in category.skins)
                _instance.player.transform.Find(skin.nameInScene).gameObject.SetActive(PlayerData.SelectedSkins[category.name] == skin.nameInScene);
        }
    }
    
    public static void SaveData()
    {
        DataService.SaveData(PlayerDataPath, PlayerData);
    }

    public static string GetSkin(string category)
    {
        return PlayerData.SelectedSkins[category];
    }

    public static void SetSkin(string category, string skinName)
    {
        PlayerData.SelectedSkins[category] = skinName;
    }

    public static void ApplySkin()
    {
        var objects = _instance.player.GetComponentsInChildren<Transform>(true).ToDictionary(o => o.name, o => o);
        foreach (var category in _instance.soSkin.data)
            foreach (var skin in category.skins)
                objects[skin.nameInScene].gameObject.SetActive(PlayerData.SelectedSkins[category.name] == skin.nameInScene);
    }
    
    public static Skin GetSoSkin()
    {
        return _instance.soSkin;
    }
}
