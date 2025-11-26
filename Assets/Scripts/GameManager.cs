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
    
    private static GameManager _instance;
    private static FloorManager _floorManager;

    [SceneLabel(SceneLabelID.Score)]
    private int _score;

    private Vector3 _targetPosition;
    private float _effectTime;
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
        SceneLabelOverlay.OnSetSpecialAttribute = ScoreLabelEffect;
    }

    private void Update()
    {
        _instance._effectTime -= Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, _targetPosition, 
            Time.fixedDeltaTime);
    }

    public static void SetTargetPosition(Vector3 position)
    {
        _instance._targetPosition = position + new Vector3(0.08f, 0f, 1.25f);
    }

    public static void AddScore(int score)
    {
        _instance._gain = score;
        _instance._score += score;
        _instance._effectTime = 2f;
    }
    
    public static void ResetScore()
    {
        _instance._score = 0;
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

    private static void ScoreLabelEffect(SceneLabelAttribute attr, SceneLabelOverlay.SceneLabelOverlayData data)
    {
        if (attr.ID != SceneLabelID.Score)
            return;

        var prefix = FloorManager.GetStarted() ? "" : "Score: ";
        
        attr.Value = $"{prefix}{_instance._score}";
        
        if (_instance._effectTime <= 0f)
        {
            attr.FormatValue = null;
            attr.RichValue = null;
            return;
        }

        var fontSize = attr.FontSize * data.GameViewScale + _instance._effectTime * 8f * data.GameViewScale;
        var gainFontSize = _instance._effectTime * attr.FontSize * 0.6 * data.GameViewScale;
        var color = Color.Lerp(Color.white, Color.chocolate, _instance._effectTime);
        attr.FormatValue = $"<size={fontSize}>{_instance._score}</size><size={gainFontSize}>+{_instance._gain}</size>";
        attr.RichValue = $"<color=#{color.ToHexString()}>{attr.FormatValue}</color>";
    }
}