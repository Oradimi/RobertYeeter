using System;
using System.Collections.Generic;
using TriInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Random = UnityEngine.Random;

public class FloorManager : MonoBehaviour
{
    [Serializable]
    [DeclareVerticalGroup("Prefabs")]
    [DeclareVerticalGroup("Data")]
    public class FloorData
    {
        [Required, HideLabel]
        [Group("Prefabs")]
        public GameObject prefab;
        [Group("Prefabs")]
        public GameObject exitPrefab;
        [Group("Data")]
        public Type type;
        [Group("Data")]
        public Zone zone;
        
        public enum Type
        {
            None,
            Standard,
            Narrow,
            Bridge,
            Exit,
        }

        public enum Zone
        {
            Lush,
            Dry,
            Cold,
        }
    }

    [Serializable]
    public class ZoneTextures
    {
        public FloorData.Zone zone;
        public ZoneTextureData textureData;
    }
    
    private static FloorManager _instance;
    private static readonly int ZShift = Shader.PropertyToID("_ZShift");
    private static readonly int FloorMain = Shader.PropertyToID("_FloorMain");
    private static readonly int FloorDetail = Shader.PropertyToID("_FloorDetail");
    private static readonly int WallMain = Shader.PropertyToID("_WallMain");
    private static readonly int WallDetail = Shader.PropertyToID("_WallDetail");
    
    
    [SerializeField] private FloorData.Zone startingZone;
    [SerializeField, Min(1f)] private int exitSpawnWeight;
    [SerializeField] private Volume volume;
    [SerializeField] private Transform robertPrefab;
    [SerializeField] private Transform jeanPierrePrefab;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float cameraSpeed = 10f;
    
    [TableList(Draggable = true,
        HideAddButton = false,
        HideRemoveButton = false,
        AlwaysExpanded = false)]
    [SerializeField] private FloorData[] floorData;
    [SerializeField] private FloorGeneration.Settings[] settings;
    [SerializeField] private ZoneTextures[] zoneTextures;
    
    [SerializeField] private Transform cameraStartPosition;
    [SerializeField] private Transform cameraEndPosition;
    [SerializeField] private Transform cameraHighPosition;

    private Vignette _vignette;
    private float _introTimer = 3f;
    private Camera _camera;
    private Vector3 _cameraTarget;
    private float _cameraSpeedBoost;
    private bool _started;
    private bool _gameOver;
    private bool _paused;
    
    [ShowInInspector]
    private List<int> _prefabPreselection;
    private List<Transform> _floor;
    private List<Vector3> _floorPreviousPosition;
    private int _levelCount;
    private FloorData.Zone _currentZone;
    private float _totalInstantiatedFloors;
    private bool _enemyInstantiationReady;
    private FloorData _lastInstantiatedFloor;
    
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
        
        if (floorData.Length < 1 || !robertPrefab
            || !_camera || !cameraStartPosition || !cameraEndPosition)
            return;
        
        _camera.transform.position = cameraStartPosition.position;
        _camera.transform.rotation = cameraStartPosition.rotation;
        _cameraTarget = cameraEndPosition.position;
        
        _floor = new List<Transform>();
        _floorPreviousPosition = new List<Vector3>();

        var children = GetComponentsInChildren<Transform>();
        foreach (var child in children)
            if (child != transform)
                Destroy(child.gameObject);

        _currentZone = startingZone;
        FloorGeneration.Init(settings);
        _prefabPreselection = FloorGeneration.Run(settings);
        _levelCount = 1;
        InstantiateFloor(Vector3.forward * 30f);
    }
    
    private void FixedUpdate()
    {
        if (!_started || floorData.Length < 1 || !robertPrefab
            || !_camera || !cameraStartPosition || !cameraEndPosition)
            return;

        _introTimer -= Time.fixedDeltaTime;
        if (_introTimer < 0)
            UpdateCamera();

        if (_gameOver || _paused)
            return;

        if (_enemyInstantiationReady)
            InstantiateEnemies();

        var playerAltitude = GameManager.GetPlayer().transform.position.y;
        switch (playerAltitude)
        {
            case > 10f:
                _cameraTarget = cameraHighPosition.position + GameManager.GetPlayer().transform.position;
                _cameraSpeedBoost = 4f;
                GameManager.SetTargetPosition(cameraEndPosition.position + GameManager.GetPlayer().transform.position);
                break;
            case > 1f:
                _cameraTarget = cameraHighPosition.position;
                _cameraSpeedBoost = 1f;
                GameManager.SetTargetPosition(Vector3.up);
                break;
            case > -10f:
                _cameraTarget = cameraEndPosition.position;
                _cameraSpeedBoost = 1f;
                GameManager.SetTargetPosition(Vector3.zero);
                break;
            default:
                _cameraTarget = cameraEndPosition.position + GameManager.GetPlayer().transform.position;
                _cameraSpeedBoost = 4f;
                GameManager.SetTargetPosition(cameraEndPosition.position + GameManager.GetPlayer().transform.position);
                break;
        }
        
        if (_camera.transform.position.y < -15f)
            ChangeZone();

        _enemyInstantiationReady = UpdateScrollingEnvironment(GetFloorScrollSpeed(), _floor, _floorPreviousPosition);
    }

    private FloorData InstantiateFloor(Vector3 position)
    {
        if (_prefabPreselection.Count < 1)
            _prefabPreselection = FloorGeneration.Run(settings, _lastInstantiatedFloor.type);
        GameObject newFloor;
        if (floorData[_prefabPreselection[0]].exitPrefab && GameManager.IsDistanceThresholdCrossed() && Random.Range(0, exitSpawnWeight) == 0)
            newFloor = Instantiate(floorData[_prefabPreselection[0]].exitPrefab, position, Quaternion.identity, transform);
        else
            newFloor = Instantiate(floorData[_prefabPreselection[0]].prefab, position, Quaternion.identity, transform);
        newFloor.AddComponent<MeshCollider>();
        var newFloorRenderer = newFloor.GetComponent<Renderer>();
        UpdateFloorMaterialShift(newFloorRenderer);
        UpdateFloorTextures(newFloorRenderer);
        var prefabData = floorData[_prefabPreselection[0]];
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
        if (Physics.Raycast(ray, out var hit) && hit.point.y > -0.1f)
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

    public static FloorData.Zone GetCurrentZone()
    {
        return _instance._currentZone;
    }

    public static float GetFloorScrollSpeed()
    {
        return GameManager.GlobalSpeed * _instance.speed;
    }

    public static void StartGame()
    {
        _instance._started = true;
        _instance._gameOver = false;
        UIManager.DisplayMenu(false);
    }

    public void GameOver(GameManager.GameOverCase gameOverCase, Transform deathCause = null)
    {
        GameManager.GetPlayer().DisableUIMap();
        Vector3 playerPosition;
        switch (gameOverCase)
        {
            case GameManager.GameOverCase.Caught:
                _gameOver = true;
                playerPosition = GameManager.GetPlayer().transform.position;
                _cameraTarget = playerPosition + new Vector3(0.3f, 0.8f, -1.5f);
                cameraEndPosition.eulerAngles = new Vector3(20f, -10f, 0f);
                UIManager.SetGameOverText("You got caught...");
                break;
            case GameManager.GameOverCase.Bonked:
                _gameOver = true;
                playerPosition = GameManager.GetPlayer().transform.position;
                _cameraTarget = playerPosition + new Vector3(0.3f, 0.8f, -1.5f);
                cameraEndPosition.eulerAngles = new Vector3(20f, -10f, 0f);
                UIManager.SetGameOverText("You bonked...");
                break;
            case GameManager.GameOverCase.Drowned:
                _gameOver = true;
                _cameraTarget = new Vector3(0.1f, 0f, -0.7f);
                cameraEndPosition.eulerAngles = new Vector3(0f, -5f, 0f);
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
        
        var zoneEnumValues = Enum.GetValues(typeof(FloorData.Zone));
        var newZone = (FloorData.Zone)zoneEnumValues.GetValue((_levelCount - 1) % zoneEnumValues.Length);
        GameManager.GetPlayer().transform.position = new Vector3(GameManager.GetPlayer().transform.position.x, 30f, GameManager.GetPlayer().transform.position.z);
        _camera.transform.position = GameManager.GetPlayer().transform.position;
        _currentZone = newZone;
        _prefabPreselection = FloorGeneration.Run(settings);
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
        foreach (var zoneTexturesSet in zoneTextures)
        {
            if (zoneTexturesSet.zone != _currentZone)
                continue;
            
            floorMaterial.SetTexture(FloorMain, zoneTexturesSet.textureData.floorMain);
            floorMaterial.SetTexture(FloorDetail, zoneTexturesSet.textureData.floorDetail);
            floorMaterial.SetTexture(WallMain, zoneTexturesSet.textureData.wallMain);
            floorMaterial.SetTexture(WallDetail, zoneTexturesSet.textureData.wallDetail);
            _vignette.color.Override(zoneTexturesSet.textureData.fogColor);
            _camera.backgroundColor = zoneTexturesSet.textureData.fogColor;
            RenderSettings.fogColor = zoneTexturesSet.textureData.fogColor;
            RenderSettings.ambientLight = zoneTexturesSet.textureData.ambientColor;
            break;
        }
    }

    public static bool IsStarted()
    {
        return _instance._started;
    }

    public static FloorData[] GetFloorData()
    {
        return _instance.floorData;
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

    private void UpdateCamera()
    {
        _camera.transform.position = Vector3.MoveTowards(_camera.transform.position, _cameraTarget,
            _cameraSpeedBoost * cameraSpeed * Time.fixedDeltaTime);
        _camera.transform.forward = Vector3.Slerp(_camera.transform.forward, cameraEndPosition.forward, 
            cameraSpeed * Time.fixedDeltaTime);
    }
}
