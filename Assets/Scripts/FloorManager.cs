using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FloorManager : MonoBehaviour
{
    private static FloorManager _instance;
    private static readonly int ZShift = Shader.PropertyToID("_ZShift");

    [SerializeField] private Transform[] floorPrefab;
    [SerializeField] private Transform robertPrefab;
    [SerializeField] private float speed = 5f;
    
    [SerializeField] private Transform cameraStartPosition;
    [SerializeField] private Transform cameraEndPosition;

    private float _introTimer = 3f;
    private Camera _camera;
    private Vector3 _cameraTarget;
    [SerializeField] private Texture2D button;
    [SerializeField] private Texture2D buttonHover;
    private bool _started;
    private bool _gameOver;
    
    private List<Transform> _floor;
    private List<Vector3> _floorPreviousPosition;
    private float _totalInstantiatedFloors;
    
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
        
        if (floorPrefab.Length < 1 || !robertPrefab
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

        InstantiateFloor(0, Vector3.forward * 10f);
    }
    
    private void FixedUpdate()
    {
        if (!_started || floorPrefab.Length < 1 || !robertPrefab
            || !_camera || !cameraStartPosition || !cameraEndPosition)
            return;

        _introTimer -= Time.deltaTime;
        if (_introTimer < 0)
            UpdateCamera();

        if (_gameOver)
            return;

        var floorScrollSpeed = GameManager.GlobalSpeed * speed;
        if (UpdateScrollingEnvironment(floorPrefab, floorScrollSpeed, _floor, _floorPreviousPosition))
        {
            UpdateFloorMaterialShift(_floor[^1]);
            for (var i = 0; i < 9; i++)
            {
                var randomX = Random.Range(-2, 2);
                if (randomX >= 0)
                    randomX++;
                var randomPosition = new Vector3(randomX, 0f, _floor[^1].localPosition.z - (Random.Range(0, 7) + 9 * i));
                Instantiate(robertPrefab, randomPosition, Quaternion.identity, _floor[^1]);
            }
        }
    }

    private Transform InstantiateFloor(int index, Vector3 position)
    {
        var floor = Instantiate(floorPrefab[index], position, Quaternion.identity, transform);
        _floor.Add(floor);
        _floorPreviousPosition.Add(position);
        _floor[^1].AddComponent<MeshCollider>();
        UpdateFloorMaterialShift(_floor[^1]);
        return floor;
    }

    private void DestroyFirstFloor()
    {
        Destroy(_floor[0].gameObject);
        _floor.RemoveAt(0);
        _floorPreviousPosition.RemoveAt(0);
    }

    public void GameOver(GameManager.GameOverCase gameOverCase, Transform deathCause = null)
    {
        switch (gameOverCase)
        {
            case GameManager.GameOverCase.Caught:
                _gameOver = true;
                _cameraTarget = GameManager.GetPlayer().transform.position + new Vector3(0.3f, 1.5f, -2f);
                cameraEndPosition.eulerAngles = new Vector3(20f, -10f, 0f);
                break;
            case GameManager.GameOverCase.Drowned:
                _gameOver = true;
                _cameraTarget = new Vector3(0.1f, 0f, -0.7f);
                cameraEndPosition.eulerAngles = new Vector3(0f, -5f, 0f);
                break;
        }
    }

    private bool UpdateScrollingEnvironment(Transform[] prefab, float elementSpeed, List<Transform> element, List<Vector3> elementPreviousPosition)
    {
        var elementToAdd = false;
        var elementToRemove = false;
        for (var i = 0; i < element.Count; i++)
        {
            element[i].localPosition += Vector3.forward * (elementSpeed * Time.deltaTime);
            
            if (elementPreviousPosition[i].z <= 42f && element[i].localPosition.z > 42f)
                elementToAdd = true;

            if (elementPreviousPosition[i].z <= 84f && element[i].localPosition.z > 84f)
                elementToRemove = true;
            
            elementPreviousPosition[i] = element[i].localPosition;
        }
        
        if (elementToAdd)
            InstantiateFloor(Random.Range(0, floorPrefab.Length), elementPreviousPosition[0] + Vector3.back * 84f);

        if (elementToRemove)
            DestroyFirstFloor();

        return elementToAdd;
    }
    
    private void UpdateFloorMaterialShift(Transform floor)
    {
        var floorMaterial = floor.GetComponent<Renderer>().material;
        floorMaterial.SetFloat(ZShift, _totalInstantiatedFloors);
        _totalInstantiatedFloors++;
    }

    public static bool GetStarted()
    {
        return _instance._started;
    }

    private void UpdateCamera()
    {
        _camera.transform.position = Vector3.MoveTowards(_camera.transform.position, _cameraTarget, speed * Time.fixedDeltaTime);
        _camera.transform.forward = Vector3.Slerp(_camera.transform.forward, cameraEndPosition.forward, speed * Time.deltaTime);
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
