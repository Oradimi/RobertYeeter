using System;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum GameOverCase
    {
        Caught,
        Drowned,
    }
    
    public static float GlobalSpeed;
    public static bool AffectsAnimations;
    
    [SerializeField] private PlayerController player;
    [SerializeField] private float nextLevelDistance;
    
    private static GameManager _instance;
    private static FloorManager _floorManager;

    [SceneLabel(SceneLabelID.Score)]
    private int _score;
    
    [SceneLabel(SceneLabelID.DistanceTraveled, suffix: "m", fontSize: 12)]
    private float _distanceTraveled;

    private Vector3 _targetPosition;
    private float _effectTime;
    private int _combo;
    private int _gain;
    private bool _isGameOver;
    
    private void Awake()
    {
        if (!player)
            return;
        
        if (_instance)
        {
            Destroy(this);
            return;
        }

        _instance = this;
        
        _floorManager = FindAnyObjectByType<FloorManager>();

        GlobalSpeed = 1f;
        
        _targetPosition = transform.position;
    }
    
    private void OnEnable()
    {
        SceneLabelOverlay.OnSetSpecialAttribute += GameManagerLabelEffect;
    }
    
    private void OnDisable()
    {
        SceneLabelOverlay.OnSetSpecialAttribute -= GameManagerLabelEffect;
    }

    private void FixedUpdate()
    {
        _instance._effectTime -= Time.fixedDeltaTime;
        _instance._effectTime = Mathf.Max(_instance._effectTime, 0f);
        transform.position = Vector3.MoveTowards(transform.position, _targetPosition, 
            Time.fixedDeltaTime);
        
        if (FloorManager.GetStarted() && !_isGameOver)
            _instance._distanceTraveled += GetPlayer().Speed() * Time.fixedDeltaTime;
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

    public static bool IsDistanceThresholdCrossed()
    {
        return _instance._distanceTraveled > _instance.nextLevelDistance * FloorManager.GetLevelCount();
    }

    public static void GameOver(GameOverCase gameOverCase, Transform deathCause = null)
    {
        if (_instance._score > 30)
            UnlocksManager.Unlocked = true;
        
        if (_instance._isGameOver)
            return;
        _instance._isGameOver = true;
        if (!_floorManager)
            return;
        _instance.player.DisableActionMap();
        GlobalSpeed = 0f;
        AffectsAnimations = false;
        _floorManager.GameOver(gameOverCase, deathCause);
    }

    public static PlayerController GetPlayer()
    {
        return _instance.player;
    }

    private static void GameManagerLabelEffect(SceneLabelAttribute attr, SceneLabelOverlay.SceneLabelOverlayData data)
    {
        var prefix = "";
        switch (attr.ID)
        {
            case SceneLabelID.Score:
                prefix = FloorManager.GetStarted() ? "" : "Score: ";
                attr.Value = $"{prefix}{_instance._score}";
        
                if (_instance._effectTime <= 0f)
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
                attr.RichValue = $"<color=#{color.ToHexString()}>{attr.FormatValue}</color>";
                break;
            case SceneLabelID.DistanceTraveled:
                prefix = FloorManager.GetStarted() ? "" : "Distance traveled: ";
                attr.Value = $"{prefix}{_instance._distanceTraveled:F1}";
                break;
        }
    }
}