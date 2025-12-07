using UnityEngine;

public class LoadingManager : MonoBehaviour
{
    private GameObject _child;
    
    private void Awake()
    {
        _child = transform.GetChild(0).gameObject;
        _child.gameObject.SetActive(!GameManager.InitialLoadingFinished);
    }
    
    public void OnInitialLoadingFinished()
    {
        _child.gameObject.SetActive(false);
        GameManager.InitialLoadingFinished = true;
    }
}
