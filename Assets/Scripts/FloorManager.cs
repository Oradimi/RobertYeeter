using System;
using System.Collections.Generic;
using SceneLabel;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Floor = So.Floor;
using Random = UnityEngine.Random;

public class FloorManager : MonoBehaviour
{
    private static FloorManager _instance;
    private static readonly int ZShift = Shader.PropertyToID("_ZShift");
    private static readonly int FloorMain = Shader.PropertyToID("_FloorMain");
    private static readonly int FloorDetail = Shader.PropertyToID("_FloorDetail");
    private static readonly int WallMain = Shader.PropertyToID("_WallMain");
    private static readonly int WallDetail = Shader.PropertyToID("_WallDetail");
    
    [SerializeField] private Floor.Data.Zone startingZone;
    [SerializeField] private Volume volume;
    [SerializeField] private Transform robertPrefab;
    [SerializeField] private Transform jeanPierrePrefab;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float cameraSpeed = 10f;

    [SerializeField] private Floor soFloor;
    
    [SerializeField] private Transform cameraStartPosition;
    [SerializeField] private Transform cameraTarget;

    private Vignette _vignette;
    private float _introTimer = 3f;
    private Camera _camera;
    private Vector3 _cameraTarget;
    private float _cameraSpeedBoost;
    private bool _started;
    private bool _gameOver;
    private GameManager.GameOverCase _gameOverCase;
    private bool _paused;
    private bool _loadingTrigger;
    
    private List<int> _prefabPreselection;
    private List<Transform> _floor;
    private List<Vector3> _floorPreviousPosition;
    private int _levelCount;
    private Floor.Data.Zone _currentZone;
    private float _totalInstantiatedFloors;
    private bool _enemyInstantiationReady;
    private Floor.Data _lastInstantiatedFloor;
    
    private void Awake()
    {
        if (_instance)
        {
            Destroy(this);
            return;
        }

        _instance = this;
        
        volume.profile.TryGet(out _vignette);
        
        _camera = Camera.main;
        
        if (soFloor.data.Length < 1 || !robertPrefab
            || !_camera || !cameraStartPosition || !cameraTarget)
            return;
        
        _camera.transform.position = cameraStartPosition.position;
        _camera.transform.rotation = cameraStartPosition.rotation;
        _cameraTarget = cameraTarget.position;
        SetAntiAliasing(GameManager.AntiAliasing);
        
        _floor = new List<Transform>();
        _floorPreviousPosition = new List<Vector3>();

        var children = GetComponentsInChildren<Transform>();
        foreach (var child in children)
            if (child != transform)
                Destroy(child.gameObject);

        _currentZone = startingZone;
        FloorGeneration.Init(soFloor.settings);
        _prefabPreselection = FloorGeneration.Run(soFloor.settings);
        _levelCount = 1;
        InstantiateFloor(Vector3.forward * 30f);
    }
    
    private void OnEnable()
    {
        SceneLabelOverlay.OnSetSpecialAttribute += GameManager.GameManagerLabelEffect;
    }
    
    private void OnDisable()
    {
        SceneLabelOverlay.OnSetSpecialAttribute -= GameManager.GameManagerLabelEffect;
    }
    
    private void FixedUpdate()
    {
        if (!_started || soFloor.data.Length < 1 || !robertPrefab
            || !_camera || !cameraStartPosition || !cameraTarget)
            return;

        _introTimer -= Time.fixedDeltaTime;
        if (_introTimer < 0)
            UpdateCamera();

        if (_gameOver || _paused)
        {
            if (_gameOverCase == GameManager.GameOverCase.Drowned)
                UpdateScrollingEnvironment(GetFloorScrollSpeed(), _floor, _floorPreviousPosition);
            return;
        }

        if (_enemyInstantiationReady)
            InstantiateEnemies();

        var playerAltitude = GameManager.GetPlayer().transform.position.y;
        switch (playerAltitude)
        {
            case > 10f:
                _cameraTarget = cameraTarget.position + Vector3.up * 2f;
                _cameraSpeedBoost = 4f;
                break;
            case > 1f:
                _cameraTarget = cameraTarget.position + Vector3.up;
                _cameraSpeedBoost = 1f;
                GameManager.SetTargetPosition(Vector3.up);
                break;
            case > -10f:
                _cameraTarget = cameraTarget.position;
                _cameraSpeedBoost = 1f;
                GameManager.SetTargetPosition(Vector3.zero);
                break;
            default:
                _cameraTarget = cameraTarget.position + Vector3.down * cameraTarget.position.y;
                _cameraSpeedBoost = 0.25f;
                GameManager.SetTargetPosition(cameraTarget.position + Vector3.up * 3f);
                if (!_loadingTrigger)
                {
                    _loadingTrigger = true;
                    LoadingManager.LoadZoneEffect();
                }
                break;
        }
        
        if (_camera.transform.position.y < 0.1f)
            ChangeZone();

        _enemyInstantiationReady = UpdateScrollingEnvironment(GetFloorScrollSpeed(), _floor, _floorPreviousPosition);
    }

    private Floor.Data InstantiateFloor(Vector3 position)
    {
        if (_prefabPreselection.Count < 1)
            _prefabPreselection = FloorGeneration.Run(soFloor.settings, _lastInstantiatedFloor.type);
        GameObject newFloor;
        if (soFloor.data[_prefabPreselection[0]].exitPrefab && Random.Range(0f, 1f) < GameManager.NextLevelStairsChance())
            newFloor = Instantiate(soFloor.data[_prefabPreselection[0]].exitPrefab, position, Quaternion.identity, transform);
        else
            newFloor = Instantiate(soFloor.data[_prefabPreselection[0]].prefab, position, Quaternion.identity, transform);
        newFloor.AddComponent<MeshCollider>();
        var newFloorRenderer = newFloor.GetComponent<Renderer>();
        UpdateFloorMaterialShift(newFloorRenderer);
        UpdateFloorTextures(newFloorRenderer);
        var prefabData = soFloor.data[_prefabPreselection[0]];
        _prefabPreselection.RemoveAt(0);
        _floor.Add(newFloor.transform);
        _floorPreviousPosition.Add(position);
        _totalInstantiatedFloors++;
        return prefabData;
    }

    private void InstantiateEnemies()
    {
        _enemyInstantiationReady = false;
        var chunk = 17f + 4f * _levelCount;
        var chunkInt = Mathf.FloorToInt(chunk);
        const float corridorLength = 84f;
        var sectionLength = corridorLength / chunk;
        for (var i = 0; i < chunkInt; i++)
        {
            var section = Mathf.FloorToInt(i * sectionLength);
            var pattern = Random.Range(0, 5 + _levelCount);
            switch (pattern)
            {
                case 0:
                case 1:
                case 2:
                    EnemyGenerationPattern(Random.Range(-3, 4),
                        _floor[^1].localPosition.z - (Random.Range(0, sectionLength) + section), v => {
                            Instantiate(robertPrefab, v, Quaternion.identity, _floor[^1]);
                        });
                    break;
                case 3:
                    for (var j = 0; j < 2; j++)
                    {
                        EnemyGenerationPattern(Random.Range(-3, 4),
                            _floor[^1].localPosition.z - (j + section), v => {
                                Instantiate(robertPrefab, v, Quaternion.identity, _floor[^1]);
                            });
                    }
                    break;
                case 4:
                case 6:
                    for (var j = 0; j < sectionLength; j++)
                    {
                        EnemyGenerationPattern(Random.Range(-3, 4),
                            _floor[^1].localPosition.z - (j + section), v => {
                                Instantiate(robertPrefab, v, Quaternion.identity, _floor[^1]);
                            });
                    }
                    break;
                case 5:
                case 7:
                    EnemyGenerationPattern(Random.Range(-3, 4),
                        _floor[^1].localPosition.z - (Random.Range(0, sectionLength) + section), v => {
                            Instantiate(jeanPierrePrefab, v, Quaternion.identity, _floor[^1]);
                        });
                    break;
                case 8:
                    for (var j = 0; j < sectionLength; j++)
                    {
                        EnemyGenerationPattern(Random.Range(-3, 4),
                            _floor[^1].localPosition.z - (j + section), v =>
                            {
                                Instantiate(j != sectionLength - 1 ? robertPrefab : jeanPierrePrefab, v, Quaternion.identity,
                                    _floor[^1]);
                            });
                    }
                    break;
            }
        }
    }

    private void EnemyGenerationPattern(float x, float z, Action<Vector3> pattern)
    {
        var randomPosition = new Vector3(x, 0f, z);
        var ray = new Ray(randomPosition + Vector3.up * 4f, Vector3.down);
        var rayFront = new Ray(randomPosition + Vector3.up * 4f + Vector3.forward * 5f, Vector3.down);
        Physics.Raycast(rayFront, out var hitFront);
        if (Physics.Raycast(ray, out var hit) && hit.point.y > -0.1f && hitFront.point.y - hit.point.y <= 0.1f)
        {
            randomPosition += Vector3.up * hit.point.y;
            pattern(randomPosition);
        }
    }

    private void DestroyFirstFloor()
    {
        Destroy(_floor[0].gameObject);
        _floor.RemoveAt(0);
        _floorPreviousPosition.RemoveAt(0);
    }

    public static bool IsGameOver()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            return false;
#endif
        return _instance._gameOver;
    }

    public static GameManager.GameOverCase GetGameOverCase()
    {
        return _instance._gameOverCase;
    }
    
    public static bool IsStarted()
    {
        return _instance._started;
    }

    public static Floor.Data[] GetFloorData()
    {
        return _instance.soFloor.data;
    }

    public static int GetLevelCount()
    {
        return _instance._levelCount;
    }

    public static bool IsPaused()
    {
        return _instance._paused;
    }

    public static void TogglePause()
    {
        _instance._paused = !_instance._paused;
        if (_instance._paused)
        {
            GameManager.GetPlayer().DisableActionMap();
            GameManager.GlobalSpeedStored = GameManager.GlobalSpeed;
            GameManager.AffectsAnimationsStored = GameManager.AffectsAnimations;
            GameManager.GlobalSpeed = 0f;
            GameManager.AffectsAnimations = true;
        }
        else
        {
            GameManager.GetPlayer().EnableActionMap();
            GameManager.GlobalSpeed = GameManager.GlobalSpeedStored;
            GameManager.AffectsAnimations = GameManager.AffectsAnimationsStored;
        }
    }

    public static Vector3 WorldToScreenPoint(Vector3 position)
    {
        return _instance._camera.WorldToScreenPoint(position);
    }

    public static Floor.Data.Zone GetCurrentZone()
    {
        return _instance._currentZone;
    }

    public static Transform GetCurrentFloor()
    {
        return _instance._floor[0];
    }

    public static float GetFloorScrollSpeed()
    {
        return GameManager.GlobalSpeed * _instance.speed;
    }

    public static void SetAntiAliasing(bool value)
    {
        _instance._camera.GetComponent<UniversalAdditionalCameraData>().antialiasing = value ? AntialiasingMode.FastApproximateAntialiasing : AntialiasingMode.None;
    }

    public static void StartGame()
    {
        _instance._started = true;
        _instance._gameOver = false;
        GameManager.GlobalSpeed = 1f;
        GameManager.AffectsAnimations = true;
        UIManager.DisplayMenu(false);
    }

    public static void GameOver(GameManager.GameOverCase gameOverCase, Transform deathCause = null)
    {
        GameManager.GetPlayer().DisableUIMap();
        GameManager.SaveData();
        _instance._gameOver = true;
        _instance._gameOverCase = gameOverCase;
        Vector3 playerPosition;
        switch (gameOverCase)
        {
            case GameManager.GameOverCase.Caught:
                playerPosition = GameManager.GetPlayer().transform.position;
                _instance._cameraTarget = playerPosition + new Vector3(0.3f, 0.8f, -1.5f);
                _instance.cameraTarget.eulerAngles = new Vector3(20f, -10f, 0f);
                UIManager.SetGameOverText("You got caught...");
                break;
            case GameManager.GameOverCase.Bonked:
                playerPosition = GameManager.GetPlayer().transform.position;
                _instance._cameraTarget = playerPosition + new Vector3(0.3f, 0.8f, -1.5f);
                _instance.cameraTarget.eulerAngles = new Vector3(20f, -10f, 0f);
                UIManager.SetGameOverText("You bonked...");
                break;
            case GameManager.GameOverCase.Drowned:
                playerPosition = GameManager.GetPlayer().transform.position;
                _instance._cameraTarget = new Vector3(playerPosition.x, 0f, -0.7f);
                _instance.cameraTarget.eulerAngles = new Vector3(0f, -5f, 0f);
                UIManager.SetGameOverText("You're drenched...");
                break;
        }
        UIManager.DisplayMenu(true, true);
    }

    private void ChangeZone()
    {
        _levelCount++;
        _floor = new List<Transform>();
        _floorPreviousPosition = new List<Vector3>();
        _totalInstantiatedFloors = 0;

        var children = GetComponentsInChildren<Transform>();
        foreach (var child in children)
            if (child != transform)
                Destroy(child.gameObject);
        
        var zoneEnumValues = Enum.GetValues(typeof(Floor.Data.Zone));
        var newZone = (Floor.Data.Zone)zoneEnumValues.GetValue((_levelCount - 1) % zoneEnumValues.Length);
        GameManager.GetPlayer().transform.position = new Vector3(GameManager.GetPlayer().transform.position.x, 30f, GameManager.GetPlayer().transform.position.z);
        _camera.transform.position = cameraTarget.position + Vector3.up * 2f;
        _currentZone = newZone;
        _prefabPreselection = FloorGeneration.Run(soFloor.settings);
        _loadingTrigger = false;
        InstantiateFloor(Vector3.forward * 30f);
    }

    private bool UpdateScrollingEnvironment(float elementSpeed, List<Transform> element, List<Vector3> elementPreviousPosition)
    {
        var elementToAdd = false;
        var elementToRemove = false;
        for (var i = 0; i < element.Count; i++)
        {
            element[i].localPosition += Vector3.forward * (elementSpeed * Time.fixedDeltaTime);
            
            if (elementPreviousPosition[i].z <= 42f && element[i].localPosition.z > 42f)
                elementToAdd = true;

            if (elementPreviousPosition[i].z <= 84f && element[i].localPosition.z > 84f)
                elementToRemove = true;
            
            elementPreviousPosition[i] = element[i].localPosition;
        }
        
        if (elementToAdd)
            _lastInstantiatedFloor = InstantiateFloor(elementPreviousPosition[0] + Vector3.back * 84f);

        if (elementToRemove)
            DestroyFirstFloor();

        return elementToAdd;
    }
    
    private void UpdateFloorMaterialShift(Renderer r)
    {
        var floorMaterial = r.material;
        floorMaterial.SetFloat(ZShift, _totalInstantiatedFloors);
    }

    private void UpdateFloorTextures(Renderer r)
    {
        var floorMaterial = r.material;
        foreach (var zoneTexturesSet in soFloor.zoneTextures)
        {
            if (zoneTexturesSet.zone != _currentZone)
                continue;
            
            floorMaterial.SetTexture(FloorMain, zoneTexturesSet.data.floorMain);
            floorMaterial.SetTexture(FloorDetail, zoneTexturesSet.data.floorDetail);
            floorMaterial.SetTexture(WallMain, zoneTexturesSet.data.wallMain);
            floorMaterial.SetTexture(WallDetail, zoneTexturesSet.data.wallDetail);
            _vignette.color.Override(zoneTexturesSet.data.fogColor);
            _camera.backgroundColor = zoneTexturesSet.data.fogColor;
            RenderSettings.fogColor = zoneTexturesSet.data.fogColor;
            RenderSettings.ambientLight = zoneTexturesSet.data.ambientColor;
            break;
        }
    }

    private void UpdateCamera()
    {
        _camera.transform.position = Vector3.MoveTowards(_camera.transform.position, _cameraTarget,
            _cameraSpeedBoost * cameraSpeed * Time.fixedDeltaTime);
        _camera.transform.forward = Vector3.Slerp(_camera.transform.forward, cameraTarget.forward, 
            cameraSpeed * Time.fixedDeltaTime);
    }
}
