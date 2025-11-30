using UnityEngine;

public class UnlocksManager : MonoBehaviour
{
    private static UnlocksManager _instance;
    public static bool Unlocked;

    [SerializeField] private AudioClip caveAmbience;
    [SerializeField] private AudioClip mainMusic;

    private GameObject baseSkin;
    private GameObject yukataSkin;
    private GameObject baseHair;
    private GameObject yukataHair;

    private bool _clothes;
    private bool _hair;
    private bool _nameDisplay;
    
    public static AudioSource audioSource;
    public static bool soundEffectsMute;
    
    private void Awake()
    {
        if (_instance)
        {
            Destroy(this);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(_instance);
        
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = _instance.caveAmbience;
        audioSource.Play();
    }

    public static void PlayMainMusic()
    {
        if (audioSource.clip == _instance.mainMusic)
            return;
        audioSource.clip = _instance.mainMusic;
        audioSource.Play();
    }

    public static void ChangeClothes()
    {
        _instance._clothes ^= true;
        _instance.baseSkin.SetActive(!_instance._clothes);
        _instance.yukataSkin.SetActive(_instance._clothes);
    }
    
    public static void ChangeHair()
    {
        _instance._hair ^= true;
        _instance.baseHair.SetActive(!_instance._hair);
        _instance.yukataHair.SetActive(_instance._hair);
    }

    public static void InitAndApplyClothesAndHair()
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
        
        _instance.baseSkin.SetActive(!_instance._clothes);
        _instance.yukataSkin.SetActive(_instance._clothes);
        _instance.baseHair.SetActive(!_instance._hair);
        _instance.yukataHair.SetActive(_instance._hair);
    }

    public static void ToggleNameDisplay()
    {
        _instance._nameDisplay ^= true;
    }

    public static void NameDisplay(SceneLabelAttribute attr, SceneLabelOverlay.SceneLabelOverlayData data)
    {
        if (attr.ID != SceneLabelID.EnemyName)
            return;
        
        if (!_instance._nameDisplay)
            attr.Value = "";
    }

    public static bool GetNameDisplay()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            return false;
#endif
        return _instance._nameDisplay;
    }

    public static bool IsAudioSourceMute()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            return false;
#endif
        return audioSource.mute;
    }
}
