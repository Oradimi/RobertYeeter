using System;
using System.Collections.Generic;
using TriInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        
        [Flags]
        public enum Type
        {
            None = 0,
            Standard = 1,
            Narrow = 2,
            Bridge = 4,
            Exit = 8,
        }

        public enum Zone
        {
            Lush,
            Dry,
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
    
    [TableList(Draggable = true,
        HideAddButton = false,
        HideRemoveButton = false,
        AlwaysExpanded = true)]
    [SerializeField] private FloorData[] floorData;
    [SerializeField] private ZoneTextures[] zoneTextures;
    [SerializeField] private FloorData.Zone startingZone;
    [ShowInInspector]
    private FloorData.Zone _currentZone;
    [SerializeField] private Transform robertPrefab;
    [SerializeField] private Transform jeanPierrePrefab;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float cameraSpeed = 10f;
    
    [SerializeField] private Transform cameraStartPosition;
    [SerializeField] private Transform cameraEndPosition;
    [SerializeField] private Transform cameraHighPosition;

    private float _introTimer = 3f;
    private Camera _camera;
    private Vector3 _cameraTarget;
    private float _cameraSpeedBoost;
    [SerializeField] private Texture2D button;
    [SerializeField] private Texture2D buttonHover;
    private bool _started;
    private bool _gameOver;
    
    [ShowInInspector]
    private List<int> _prefabPreselection;
    private List<Transform> _floor;
    private List<Vector3> _floorPreviousPosition;
    private int _levelCount;
    private float _totalInstantiatedFloors;
    private bool _enemyInstantiationReady;
    private FloorData _lastInstantiatedFloor;
    
    [SerializeField] private Font interFont;
    
    private void Awake()
    {
        if (_instance)
        {
            Destroy(this);
            return;
        }

        _instance = this;
        
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
        _prefabPreselection = FloorGeneration.Init();
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

        if (_gameOver)
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
            _prefabPreselection = FloorGeneration.Init(_lastInstantiatedFloor.type);
        GameObject newFloor;
        if (floorData[_prefabPreselection[0]].exitPrefab && GameManager.IsDistanceThresholdCrossed() && Random.Range(0, 2) == 0)
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
        for (var i = 0; i < 25; i++)
        {
            int randomX;
            Vector3 randomPosition;
            Ray ray;
            RaycastHit hit;
            
            var pattern = Random.Range(0, 6);
            switch (pattern)
            {
                case 0:
                    randomX = Random.Range(-3, 3);
                    if (randomX >= 0)
                        randomX++;
                    randomPosition = new Vector3(randomX, 0f,
                        _floor[^1].localPosition.z - (Random.Range(0, 7) + 3 * i));
                    ray = new Ray(randomPosition + Vector3.up * 4f, Vector3.down);
                    if (Physics.Raycast(ray, out hit))
                    {
                        randomPosition += Vector3.up * hit.point.y;
                        Instantiate(robertPrefab, randomPosition, Quaternion.identity, _floor[^1]);
                    }
                    break;
                case 1:
                    for (var j = 0; j < 3; j++)
                    {
                        randomX = Random.Range(-3, 3);
                        if (randomX >= 0)
                            randomX++;
                        randomPosition = new Vector3(randomX, 0f,
                            _floor[^1].localPosition.z - (Random.Range(j * 3f, 3f + j * 3f) + 9 * i));
                        ray = new Ray(randomPosition + Vector3.up * 4f, Vector3.down);
                        if (Physics.Raycast(ray, out hit))
                        {
                            randomPosition += Vector3.up * hit.point.y;
                            Instantiate(robertPrefab, randomPosition, Quaternion.identity, _floor[^1]);
                        }
                    }
                    break;
                case 2:
                case 5:
                    for (var j = 0; j < 3; j++)
                    {
                        randomX = Random.Range(-3, 3);
                        if (randomX >= 0)
                            randomX++;
                        for (var k = 0; k < 3; k++)
                        {
                            randomPosition = new Vector3(randomX, 0f,
                                _floor[^1].localPosition.z - (j * 3f + 9 * i) - k);
                            ray = new Ray(randomPosition + Vector3.up * 4f, Vector3.down);
                            if (Physics.Raycast(ray, out hit))
                            {
                                randomPosition += Vector3.up * hit.point.y;
                                Instantiate(robertPrefab, randomPosition, Quaternion.identity, _floor[^1]);
                            }
                        }
                    }
                    break;
                case 3:
                case 4:
                    randomX = Random.Range(-3, 3);
                    if (randomX >= 0)
                        randomX++;
                    randomPosition = new Vector3(randomX, 0f,
                        _floor[^1].localPosition.z - (Random.Range(0, 7) + 9 * i));
                    ray = new Ray(randomPosition + Vector3.up * 4f, Vector3.down);
                    if (Physics.Raycast(ray, out hit))
                    {
                        randomPosition += Vector3.up * hit.point.y;
                        Instantiate(jeanPierrePrefab, randomPosition, Quaternion.identity, _floor[^1]);
                    }
                    break;
            }
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

    public void GameOver(GameManager.GameOverCase gameOverCase, Transform deathCause = null)
    {
        switch (gameOverCase)
        {
            case GameManager.GameOverCase.Caught:
                _gameOver = true;
                var playerPosition = GameManager.GetPlayer().transform.position;
                var positionNoZ = new Vector3(playerPosition.x, playerPosition.y, 0f);
                _cameraTarget = positionNoZ + new Vector3(0.3f, 1.5f, -6f);
                cameraEndPosition.eulerAngles = new Vector3(20f, -10f, 0f);
                break;
            case GameManager.GameOverCase.Drowned:
                _gameOver = true;
                _cameraTarget = new Vector3(0.1f, 0f, -0.7f);
                cameraEndPosition.eulerAngles = new Vector3(0f, -5f, 0f);
                break;
        }
    }

    private void ChangeZone()
    {
        _floor = new List<Transform>();
        _floorPreviousPosition = new List<Vector3>();

        var children = GetComponentsInChildren<Transform>();
        foreach (var child in children)
            if (child != transform)
                Destroy(child.gameObject);
        
        var zoneEnumValues = Enum.GetValues(typeof(FloorData.Zone));
        var newZone = (FloorData.Zone)zoneEnumValues.GetValue(Random.Range(0, zoneEnumValues.Length));
        while (_currentZone == newZone)
            newZone = (FloorData.Zone)zoneEnumValues.GetValue(Random.Range(0, zoneEnumValues.Length));
        
        GameManager.GetPlayer().transform.position = new Vector3(GameManager.GetPlayer().transform.position.x, 30f, GameManager.GetPlayer().transform.position.z);
        _camera.transform.position = GameManager.GetPlayer().transform.position;
        _currentZone = newZone;
        _prefabPreselection = FloorGeneration.Init();
        _levelCount++;
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
        var currentZoneTextures = zoneTextures[0].textureData;
        foreach (var zoneTexturesSet in zoneTextures)
        {
            if (zoneTexturesSet.zone != _currentZone || currentZoneTextures == zoneTexturesSet.textureData)
                continue;
            
            floorMaterial.SetTexture(FloorMain, zoneTexturesSet.textureData.floorMain);
            floorMaterial.SetTexture(FloorDetail, zoneTexturesSet.textureData.floorDetail);
            floorMaterial.SetTexture(WallMain, zoneTexturesSet.textureData.wallMain);
            floorMaterial.SetTexture(WallDetail, zoneTexturesSet.textureData.wallDetail);
            break;
        }
    }

    public static bool GetStarted()
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

    private void UpdateCamera()
    {
        _camera.transform.position = Vector3.MoveTowards(_camera.transform.position, _cameraTarget,
            _cameraSpeedBoost * cameraSpeed * Time.fixedDeltaTime);
        _camera.transform.forward = Vector3.Slerp(_camera.transform.forward, cameraEndPosition.forward, 
            cameraSpeed * Time.fixedDeltaTime);
    }

    private void OnGUI()
    {
        if (_started && !_gameOver)
            return;
        
        var gameViewScale = Screen.height / 640f;
        var style = new GUIStyle(GUI.skin.label)
        {
            font = interFont ? interFont : null,
            fontSize = Mathf.FloorToInt(48 * gameViewScale),
            alignment = TextAnchor.MiddleCenter,
            normal =
            {
                textColor = Color.white,
                background = button
            },
            hover =
            {
                textColor = Color.black,
                background = buttonHover
            },
        };
        
        var styleLabels = new GUIStyle(GUI.skin.label)
        {
            font = interFont ? interFont : null,
            fontSize = Mathf.FloorToInt(24 * gameViewScale),
            alignment = TextAnchor.MiddleLeft,
            normal =
            {
                textColor = Color.white
            },
            hover =
            {
                textColor = Color.black
            },
        };

        var worldPos = new Vector3(0f, 0.5f, 1f);
        var screenPos = _camera.WorldToScreenPoint(worldPos);

        if (screenPos.z < 0)
            return;

        var invertedY = Screen.height - screenPos.y;
        
        var labelWidth = 240f * 1.675f;
        var labelHeight = style.fontSize * 1.675f + 20f;
        
        var position = new Vector2(screenPos.x, invertedY);
        
        var shadowOffset = 2f * gameViewScale;
        
        styleLabels.normal.textColor = Color.black;
        styleLabels.hover.textColor = Color.black;
        DrawScaledLabel(new Rect(position.x + shadowOffset, position.y + shadowOffset - 480, labelWidth, labelHeight),
            "Robert Yeeter", styleLabels, 3f);

        styleLabels.normal.textColor = Color.goldenRod;
        styleLabels.hover.textColor = Color.goldenRod;
        DrawScaledLabel(new Rect(position.x, position.y - 480, labelWidth, labelHeight),
            "Robert Yeeter", styleLabels, 3f);
        
        styleLabels.normal.textColor = Color.black;
        styleLabels.hover.textColor = Color.black;
        DrawScaledLabel(new Rect(position.x + shadowOffset, position.y + shadowOffset + 120, labelWidth, labelHeight),
            "Meina needs to escape!\nRun into the miners named Robert.", styleLabels, 1f);

        styleLabels.normal.textColor = Color.white;
        styleLabels.hover.textColor = Color.white;
        DrawScaledLabel(new Rect(position.x, position.y + 120, labelWidth, labelHeight),
            "Meina needs to escape!\nRun into the miners named Robert.", styleLabels, 1f);
        
        // Label shadow
        style.normal.textColor = Color.black;
        DrawScaledButton(new Rect(position.x + shadowOffset, position.y + shadowOffset, labelWidth - 80f, labelHeight - 40f),
            _gameOver ? "Restart" : "Start", style, 1f);

        // Label
        style.normal.textColor = Color.white;
        style.hover.textColor = Color.white;
        style.normal.background = null;
        style.hover.background = null;
        DrawScaledButton(new Rect(position.x, position.y, labelWidth - 80f, labelHeight - 40f),
            _gameOver ? "Restart" : "Start", style, 1f);

        if (UnlocksManager.Unlocked)
        {
            // Label shadow
            style.normal.textColor = Color.black;
            style.normal.background = button;
            style.hover.background = buttonHover;
            DrawScaledButton2(new Rect(position.x + shadowOffset, position.y + shadowOffset + 300, labelWidth - 120f, labelHeight + 40f),
                "Change Clothes", style, 0.5f, true);

            // Label
            style.normal.textColor = Color.white;
            style.hover.textColor = Color.white;
            style.normal.background = null;
            style.hover.background = null;
            DrawScaledButton2(new Rect(position.x, position.y + 300, labelWidth - 80f, labelHeight + 40f),
                "Change Clothes", style, 0.5f, true);
        
            // Label shadow
            style.normal.textColor = Color.black;
            style.normal.background = button;
            style.hover.background = buttonHover;
            DrawScaledButton2(new Rect(position.x + shadowOffset + 400, position.y + shadowOffset + 300, labelWidth - 120f, labelHeight + 40f),
                "Change Hair", style, 0.5f, false);

            // Label
            style.normal.textColor = Color.white;
            style.hover.textColor = Color.white;
            style.normal.background = null;
            style.hover.background = null;
            DrawScaledButton2(new Rect(position.x + 400, position.y + 300, labelWidth - 80f, labelHeight + 40f),
                "Change Hair", style, 0.5f, false);
        }
    }
    
    private void DrawScaledButton(Rect rect, string text, GUIStyle style, float scale)
    {
        var matrixBackup = GUI.matrix;
        GUIUtility.ScaleAroundPivot(Vector2.one * scale, rect.position);
        if (GUI.Button(rect, text, style))
        {
            if (!_gameOver)
            {
                if (!UnlocksManager.audioSource.isPlaying)
                    UnlocksManager.audioSource.Play();
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            
            _started = true;
            _gameOver = false;
            GameManager.ResetScore();
        }
        GUI.matrix = matrixBackup;
    }
    
    private void DrawScaledButton2(Rect rect, string text, GUIStyle style, float scale, bool what)
    {
        var matrixBackup = GUI.matrix;
        GUIUtility.ScaleAroundPivot(Vector2.one * scale, rect.position);
        if (GUI.Button(rect, text, style))
        {
            if (what)
                UnlocksManager.ChangeClothes();
            else
                UnlocksManager.ChangeHair();
        }
        GUI.matrix = matrixBackup;
    }
    
    private void DrawScaledLabel(Rect rect, string text, GUIStyle style, float scale)
    {
        var matrixBackup = GUI.matrix;
        GUIUtility.ScaleAroundPivot(Vector2.one * scale, rect.position);
        GUI.Label(rect, text, style);
        GUI.matrix = matrixBackup;
    }
}
