using UnityEngine;

public class UnlocksManager : MonoBehaviour
{
    private static UnlocksManager _instance;
    public static bool Unlocked;

    private GameObject baseSkin;
    private GameObject yukataSkin;
    private GameObject baseHair;
    private GameObject yukataHair;

    private bool _clothes;
    private bool _hair;
    
    private void Awake()
    {
        if (_instance)
        {
            Destroy(this);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(_instance);
    }

    public static void ChangeClothes()
    {
        if (!_instance.baseSkin || !_instance.baseHair || !_instance.yukataSkin || !_instance.yukataSkin)
        {
            var objects = GameManager.GetPlayer().GetComponentsInChildren<Transform>(true);
            foreach (var obj in objects)
            {
                switch (obj.name)
                {
                    case "Clothes_base":
                        _instance.baseSkin = obj.gameObject;
                        break;
                    case "Clothes_yukata":
                        _instance.yukataSkin = obj.gameObject;
                        break;
                    case "Hair_base":
                        _instance.baseHair = obj.gameObject;
                        break;
                    case "Hair_bun":
                        _instance.yukataHair = obj.gameObject;
                        break;
                }
            }
        }
        
        _instance._clothes ^= true;
        _instance.baseSkin.SetActive(!_instance._clothes);
        _instance.yukataSkin.SetActive(_instance._clothes);
    }

    public static void ChangeHair()
    {
        if (!_instance.baseSkin || !_instance.baseHair || !_instance.yukataSkin || !_instance.yukataSkin)
        {
            var objects = GameManager.GetPlayer().GetComponentsInChildren<Transform>(true);
            foreach (var obj in objects)
            {
                switch (obj.name)
                {
                    case "Clothes_base":
                        _instance.baseSkin = obj.gameObject;
                        break;
                    case "Clothes_yukata":
                        _instance.yukataSkin = obj.gameObject;
                        break;
                    case "Hair_base":
                        _instance.baseHair = obj.gameObject;
                        break;
                    case "Hair_bun":
                        _instance.yukataHair = obj.gameObject;
                        break;
                }
            }
        }
        
        _instance._hair ^= true;
        _instance.baseHair.SetActive(!_instance._hair);
        _instance.yukataHair.SetActive(_instance._hair);
    }
}
