using System.Collections.Generic;
using UnityEngine;

public class FloorManager : MonoBehaviour
{
    private static FloorManager _instance;
    
    [SerializeField] private Transform floorPrefab;
    [SerializeField] private Transform waterPrefab;
    [SerializeField] private Transform robertPrefab;
    [SerializeField] private float speed = 5f;
    
    [SerializeField] private Transform cameraStartPosition;
    [SerializeField] private Transform cameraEndPosition;

    private float _introTimer = 3f;
    private Camera _camera;
    
    private List<Transform> _floor;
    private List<Vector3> _floorPreviousPosition;
    private List<Transform> _water;
    private List<Vector3> _waterPreviousPosition;
    
    private void Awake()
    {
        if (_instance)
        {
            Destroy(this);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(_instance);
        
        _camera = Camera.main;
        
        if (!floorPrefab || !waterPrefab || !robertPrefab
            || !_camera || !cameraStartPosition || !cameraEndPosition)
            return;
        
        cameraEndPosition.position = _camera.transform.position;
        cameraEndPosition.rotation = _camera.transform.rotation;
        _camera.transform.position = cameraStartPosition.position;
        _camera.transform.rotation = cameraStartPosition.rotation;
        
        _floor = new List<Transform>();
        _floorPreviousPosition = new List<Vector3>();
        _water = new List<Transform>();
        _waterPreviousPosition = new List<Vector3>();

        var children = GetComponentsInChildren<Transform>();
        foreach (var child in children)
            if (child != transform)
                Destroy(child.gameObject);
        
        _floor.Add(Instantiate(floorPrefab, transform));
        _floorPreviousPosition.Add(transform.localPosition);
        _water.Add(Instantiate(waterPrefab, transform));
        _waterPreviousPosition.Add(transform.localPosition);
        _floor[0].localPosition += Vector3.forward * 30f;
        _water[0].localPosition += Vector3.forward * 30f;
    }

    private void FixedUpdate()
    {
        if (!floorPrefab || !waterPrefab || !robertPrefab
            || !_camera || !cameraStartPosition || !cameraEndPosition)
            return;

        _introTimer -= Time.deltaTime;
        if (_introTimer < 0)
            UpdateCamera();

        var floorScrollSpeed = GameManager.GlobalSpeed * speed;
        if (UpdateScrollingEnvironment(floorPrefab, floorScrollSpeed, _floor, _floorPreviousPosition))
        {
            for (var i = 0; i < 9; i++)
            {
                var randomX = Random.Range(-2, 2);
                if (randomX >= 0)
                    randomX++;
                var randomPosition = new Vector3(randomX, 0f, _floor[^1].localPosition.z - (Random.Range(0, 7) + 9 * i));
                Instantiate(robertPrefab, randomPosition, Quaternion.identity, _floor[^1]);
            }
        }

        var waterScrollSpeed = floorScrollSpeed == 0f ? speed * 0.5f : GameManager.GlobalSpeed * speed * 1.5f;
        UpdateScrollingEnvironment(waterPrefab, waterScrollSpeed, _water, _waterPreviousPosition);
    }

    public void GameOver(GameManager.GameOverCase gameOverCase, Transform deathCause = null)
    {
        switch (gameOverCase)
        {
            case GameManager.GameOverCase.Caught:
                if (deathCause)
                    cameraEndPosition.position = deathCause.transform.position + Vector3.forward;
                break;
            case GameManager.GameOverCase.Drowned:
                cameraEndPosition.position = new Vector3(0, 0f, -0.7f);
                cameraEndPosition.eulerAngles = new Vector3(0f, 0.2f, 0);
                break;
        }
    }

    private bool UpdateScrollingEnvironment(Transform prefab, float elementSpeed, List<Transform> element, List<Vector3> elementPreviousPosition)
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
        {
            element.Add(Instantiate(prefab, elementPreviousPosition[0] + Vector3.back * 84f, Quaternion.identity, transform));
            elementPreviousPosition.Add(elementPreviousPosition[0] + Vector3.back * 84f);
        }

        if (elementToRemove)
        {
            Destroy(element[0].gameObject);
            element.RemoveAt(0);
            elementPreviousPosition.RemoveAt(0);
        }

        return elementToAdd;
    }

    private void UpdateCamera()
    {
        _camera.transform.position = Vector3.MoveTowards(_camera.transform.position, cameraEndPosition.position, speed * Time.fixedDeltaTime);
        _camera.transform.forward = Vector3.Slerp(_camera.transform.forward, cameraEndPosition.forward, speed * Time.deltaTime);
    }
}
