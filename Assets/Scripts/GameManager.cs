using System.Collections.Generic;
using System.Linq;
using SceneLabel;
using So;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum GameOverCase
    {
        Caught,
        Bonked,
        Drowned,
    }
    
    private static GameManager _instance;
    
    public static int MaxScore;
    public static float MaxDistanceTraveled;
    
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

    private Dictionary<string, string> _skins;

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
        
        AudioSource = GetComponent<AudioSource>();
        AudioSource.clip = _instance.caveAmbience;
        AudioSource.Play();
        
        SoundEffectsVolume = _instance.player.GetComponent<AudioSource>().volume;
        
        GlobalSpeed = 1f;
        
        _targetPosition = transform.position;
        
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

        if (_instance._score > MaxScore)
            MaxScore = _instance._score;
        if (_instance._distanceTraveled > MaxDistanceTraveled)
            MaxDistanceTraveled = _instance._distanceTraveled;
        
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

    private static void InitSkin()
    {
        _instance._skins = new Dictionary<string, string>();
        foreach (var category in _instance.soSkin.data)
        {
            _instance._skins.Add(category.name, category.skins[0].nameInScene);
            foreach (var skin in category.skins)
                _instance.player.transform.Find(skin.nameInScene).gameObject.SetActive(_instance._skins[category.name] == skin.nameInScene);
        }
    }

    public static string GetSkin(string category)
    {
        return _instance._skins[category];
    }

    public static void SetSkin(string category, string skinName)
    {
        _instance._skins[category] = skinName;
    }

    public static void ApplySkin()
    {
        var objects = _instance.player.GetComponentsInChildren<Transform>(true).ToDictionary(o => o.name, o => o);
        foreach (var category in _instance.soSkin.data)
            foreach (var skin in category.skins)
                objects[skin.nameInScene].gameObject.SetActive(_instance._skins[category.name] == skin.nameInScene);
    }
    
    public static Skin GetSoSkin()
    {
        return _instance.soSkin;
    }
}
