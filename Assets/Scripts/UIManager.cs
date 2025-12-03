using System.Collections.Generic;
using System.Linq;
using So;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private static UIManager _instance;

    [Header("Main Content")]
    [SerializeField] private RectTransform ui;
    [SerializeField] private RectTransform mainContent;
    [SerializeField] private Button backButton;
    
    [Header("Game Over Content")]
    [SerializeField] private RectTransform gameOverMenu;
    [SerializeField] private TextMeshProUGUI gameOverText;
    
    [Header("Main Menu")]
    [SerializeField] private RectTransform mainMenu;
    [SerializeField] private TextMeshProUGUI startText;
    [SerializeField] private TextMeshProUGUI highestScoreText;
    
    [Header("Skins Menu")]
    [SerializeField] private RectTransform skinsMenu;
    [SerializeField] private Button skinsButton;
    [SerializeField] private GameObject skinCategoryPrefab;
    [SerializeField] private GameObject skinPrefab;
    
    [Header("Settings Menus")]
    [SerializeField] private RectTransform settingsMenu;
    [SerializeField] private Slider musicVolume;
    [SerializeField] private TextMeshProUGUI musicVolumeLabel;
    [SerializeField] private Slider soundVolume;
    [SerializeField] private TextMeshProUGUI soundVolumeLabel;
    [SerializeField] private Toggle enemyNameDisplay;
    
    [Header("Credits Menus")]
    [SerializeField] private RectTransform creditsMenu;
    
    [Header("Sound Effects")]
    [SerializeField] private AudioClip openMenu;
    [SerializeField] private AudioClip closeMenu;
    [SerializeField] private AudioClip enableSetting;
    [SerializeField] private AudioClip disableSetting;
    [SerializeField] private AudioClip cantSelect;
    
    [Header("Colors")]
    [SerializeField] private Color normalColor;
    [SerializeField] private Color selectedColor;
    [SerializeField] private Color normalTextColor;
    [SerializeField] private Color selectedTextColor;
    
    private void Awake()
    {
        if (_instance)
        {
            Destroy(this);
            return;
        }

        _instance = this;
    }

    private void Start()
    {
        DisplayMenu(true);
    }

    public static void DisplayMenu(bool value, bool gameOver = false)
    {
        _instance.ui.gameObject.SetActive(value);
        
        if (value)
        {
            _instance.mainContent.gameObject.SetActive(!gameOver);
            _instance.skinsButton.gameObject.SetActive(GameManager.PlayerData.MaxScore >= 10);
            if (gameOver)
                DisplayGameOverMenu(true);
            else
                DisplayMainMenu(true);
        }
    }
    
    public static void DisplayMainMenu(bool value)
    {
        if (!_instance.mainMenu)
            return;
        
        _instance.mainMenu.gameObject.SetActive(value);
        _instance.backButton.gameObject.SetActive(!value);
        _instance.highestScoreText.text = $"Best — {GameManager.PlayerData.MaxScore}\tReached — {GameManager.PlayerData.MaxDistanceTraveled:F1}m";

        if (value)
        {
            _instance.skinsMenu.gameObject.SetActive(false);
            _instance.settingsMenu.gameObject.SetActive(false);
            _instance.creditsMenu.gameObject.SetActive(false);
            _instance.gameOverMenu.gameObject.SetActive(false);
        }
    }
    
    public static void DisplaySkinsMenu(bool value)
    {
        if (!_instance.skinsMenu)
            return;
        
        _instance.skinsMenu.gameObject.SetActive(value);
        DisplayMainMenu(!value);

        if (!value)
            return;
            
        var children = _instance.skinsMenu.GetComponentsInChildren<Transform>();
        foreach (var child in children)
            if (child != _instance.skinsMenu)
                Destroy(child.gameObject);

        var soSkin = GameManager.GetSoSkin();
        foreach (var categoryUi in soSkin.data)
        {
            var categoryPrefabUi = Instantiate(_instance.skinCategoryPrefab, _instance.skinsMenu);
            categoryPrefabUi.transform.Find("CategoryLabel").GetComponent<TextMeshProUGUI>().text = categoryUi.name;
            var skinPrefabsUi = new Dictionary<GameObject, Skin.Category.Data>();
            
            foreach (var skinUi in categoryUi.skins)
            {
                var skinPrefabUi = Instantiate(_instance.skinPrefab, categoryPrefabUi.transform);

                GameManager.PlayerData.SelectedSkins.TryGetValue(categoryUi.name, out var skinName);
                if (GameManager.PlayerData.MaxScore < skinUi.scoreRequired && skinName != skinUi.nameInScene)
                {
                    var textMesh = skinPrefabUi.transform.Find("SkinName").GetComponent<TextMeshProUGUI>();
                    textMesh.text = $"{skinUi.scoreRequired}+ points";
                    var image = skinPrefabUi.transform.Find("SkinImageCanvas/SkinImage").GetComponent<Image>();
                    image.sprite = skinUi.sprite;
                    image.color = new Color(image.color.r, image.color.g, image.color.b, 0.4f);
                }
                else
                {
                    skinPrefabUi.transform.Find("SkinName").GetComponent<TextMeshProUGUI>().text = skinUi.name;
                    skinPrefabUi.transform.Find("SkinImageCanvas/SkinImage").GetComponent<Image>().sprite = skinUi.sprite;
                }
                
                skinPrefabsUi.Add(skinPrefabUi, skinUi);
            }

            foreach (var skinPrefabUi in skinPrefabsUi)
            {
                var button = skinPrefabUi.Key.GetComponent<Button>();
                
                var objects = GameManager.GetPlayer().GetComponentsInChildren<Transform>(true).ToDictionary(o => o.name, o => o);
                var currentSkin = GameManager.GetSkin(categoryUi.name);
                var textMesh = skinPrefabUi.Key.transform.Find("SkinName").GetComponent<TextMeshProUGUI>();
                if (currentSkin == skinPrefabUi.Value.nameInScene)
                {
                    textMesh.color = _instance.selectedTextColor;
                    button.interactable = false;
                    var block = button.colors;
                    block.disabledColor = _instance.selectedColor;
                    button.colors = block;
                }
                else
                {
                    button.interactable = GameManager.PlayerData.MaxScore >= skinPrefabUi.Value.scoreRequired;
                }
                
                button.onClick.AddListener(() =>
                {
                    GameManager.SetSkin(categoryUi.name, skinPrefabUi.Value.nameInScene);
                    GameManager.SaveData();
                    foreach (var skin in skinPrefabsUi)
                    {
                        var obj = objects[skin.Value.nameInScene];
                        var match = obj.name == skinPrefabUi.Value.nameInScene;
                        obj.gameObject.SetActive(match);
                        var localTextMesh = skin.Key.transform.Find("SkinName").GetComponent<TextMeshProUGUI>();
                        var localButton = skin.Key.GetComponent<Button>();
                        if (match)
                        {
                            localTextMesh.color = _instance.selectedTextColor;
                            localButton.interactable = false;
                            var block = localButton.colors;
                            block.disabledColor = _instance.selectedColor;
                            localButton.colors = block;
                        }
                        else
                        {
                            localTextMesh.color = _instance.normalTextColor;
                            localButton.interactable = true;
                            var block = localButton.colors;
                            block.disabledColor = _instance.normalColor;
                            localButton.colors = block;
                        }
                    }
                });
                
                button.onClick.AddListener(() =>
                {
                    PlayToggleSetting(true);
                });
            }
        }
    }
    
    public static void DisplaySettingsMenu(bool value)
    {
        if (!_instance.settingsMenu)
            return;
        
        _instance.settingsMenu.gameObject.SetActive(value);
        _instance.musicVolume.SetValueWithoutNotify(GameManager.AudioSource.volume * 100f);
        _instance.musicVolumeLabel.text = GameManager.PlayerData.MusicVolume.ToString();
        _instance.soundVolume.SetValueWithoutNotify(GameManager.SoundEffectsVolume * 100f);
        _instance.soundVolumeLabel.text = GameManager.PlayerData.SoundVolume.ToString();
        _instance.enemyNameDisplay.SetIsOnWithoutNotify(GameManager.EnemyNameDisplay);
        DisplayMainMenu(!value);
    }
    
    public static void DisplayCreditsMenu(bool value)
    {
        if (!_instance.creditsMenu)
            return;
        
        _instance.creditsMenu.gameObject.SetActive(value);
        DisplayMainMenu(!value);
    }
    
    public static void DisplayGameOverMenu(bool value)
    {
        if (!_instance.gameOverMenu)
            return;
        
        _instance.gameOverMenu.gameObject.SetActive(value);
        DisplayMainMenu(!value);
    }
    
    public static void StartGame()
    {
        if (!FloorManager.IsGameOver())
        {
            GameManager.GetPlayer().EnableUIMap();
            GameManager.GetPlayer().PerformCharging();
            GameManager.PlayMainMusic();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        FloorManager.StartGame();
        GameManager.ResetScore();
    }

    public static void QuitGame()
    {
        Application.Quit();
    }

    public static void OpenExternalLink(string url)
    {
        Application.OpenURL(url);
    }

    public static void ChangeMusicVolume(float value)
    {
        GameManager.AudioSource.volume = value * 0.01f;
        GameManager.PlayerData.MusicVolume = Mathf.FloorToInt(value);
        GameManager.SaveData();
        _instance.musicVolumeLabel.text = GameManager.PlayerData.MusicVolume.ToString();
    }

    public static void ChangeSoundEffectsVolume(float value)
    {
        GameManager.GetPlayer().ChangeSoundEffectsVolume(value);
        GameManager.PlayerData.SoundVolume = Mathf.FloorToInt(value);
        GameManager.SaveData();
        _instance.soundVolumeLabel.text = GameManager.PlayerData.SoundVolume.ToString();
    }
    
    public static void ChangeEnemyNameDisplay(bool value)
    {
        GameManager.EnemyNameDisplay = value;
        GameManager.PlayerData.EnemyNameDisplay = value;
        GameManager.SaveData();
    }

    public static void SetGameOverText(string text)
    {
        _instance.gameOverText.text = text;
    }

    public static void PlayOpenMenu()
    {
        GameManager.GetPlayer().PlaySound(_instance.openMenu);
    }

    public static void PlayCloseMenu()
    {
        GameManager.GetPlayer().PlaySound(_instance.closeMenu);
    }
    
    public static void PlayToggleSetting(bool value)
    {
        GameManager.GetPlayer().PlaySound(value ? _instance.enableSetting : _instance.disableSetting);
    }
    
    public static void PlayCantSelect()
    {
        GameManager.GetPlayer().PlaySound(_instance.cantSelect);
    }
}
