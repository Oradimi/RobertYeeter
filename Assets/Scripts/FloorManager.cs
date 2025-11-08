using System.Collections.Generic;
using UnityEngine;

public class FloorManager : MonoBehaviour
{
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private float speed = 5f;
    
    private List<GameObject> _floors;
    private List<Vector3> _floorsPreviousPosition;
    
    private void Awake()
    {
        if (!floorPrefab)
            return;
        
        _floors = new List<GameObject>();
        _floorsPreviousPosition = new List<Vector3>();

        var children = GetComponentsInChildren<Transform>();
        foreach (var child in children)
            if (child != transform)
                Destroy(child.gameObject);
        
        _floors.Add(Instantiate(floorPrefab, transform));
        _floorsPreviousPosition.Add(transform.localPosition);
    }

    private void Update()
    {
        if (!floorPrefab)
            return;
        
        var floorToAdd = false;
        var floorToRemove = false;
        for (var i = 0; i < _floors.Count; i++)
        {
            _floors[i].transform.localPosition += Vector3.forward * (speed * Time.deltaTime);
            
            if (_floorsPreviousPosition[i].z <= 42f && _floors[i].transform.localPosition.z > 42f)
                floorToAdd = true;

            if (_floorsPreviousPosition[i].z <= 84f && _floors[i].transform.localPosition.z > 84f)
                floorToRemove = true;
            
            _floorsPreviousPosition[i] = _floors[i].transform.localPosition;
        }

        if (floorToAdd)
        {
            _floors.Add(Instantiate(floorPrefab, _floorsPreviousPosition[0] + Vector3.back * 84f, Quaternion.identity, transform));
            _floorsPreviousPosition.Add(_floorsPreviousPosition[0] + Vector3.back * 84f);
        }

        if (floorToRemove)
        {
            Destroy(_floors[0]);
            _floors.RemoveAt(0);
            _floorsPreviousPosition.RemoveAt(0);
        }
    }
}
