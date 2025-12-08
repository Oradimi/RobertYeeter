using UnityEngine;

public class LoadingManager : MonoBehaviour
{
    private static LoadingManager _instance;

    private GameObject _child;
    private Animator _animator;
    
    private void Awake()
    {
        if (_instance)
        {
            Destroy(this);
            return;
        }

        _instance = this;
        
        _animator = GetComponent<Animator>();
        _child = transform.GetChild(0).gameObject;
        _child.gameObject.SetActive(!GameManager.InitialLoadingFinished);
    }

    public static void LoadZoneEffect()
    {
        _instance._child.gameObject.SetActive(true);
        _instance._animator.Play("Selected");
    }
    
    public void OnInitialLoadingFinished()
    {
        _child.gameObject.SetActive(false);
        GameManager.InitialLoadingFinished = true;
    }
}
