using System.Collections.Generic;
using UnityEngine;

public class FloorManager : MonoBehaviour
{
    [SerializeField] private Transform floorPrefab;
    [SerializeField] private Transform waterPrefab;
    [SerializeField] private Transform robertPrefab;
    [SerializeField] private float speed = 5f;
    
    private List<Transform> _floor;
    private List<Vector3> _floorPreviousPosition;
    private List<Transform> _water;
    private List<Vector3> _waterPreviousPosition;
    
    private void Awake()
    {
        if (!floorPrefab || !waterPrefab)
            return;
        
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
        if (!floorPrefab || !waterPrefab)
            return;

        if (UpdateScrollingEnvironment(floorPrefab, speed * 1.0f, _floor, _floorPreviousPosition))
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
        
        UpdateScrollingEnvironment(waterPrefab, speed * 1.5f, _water, _waterPreviousPosition);
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
}
