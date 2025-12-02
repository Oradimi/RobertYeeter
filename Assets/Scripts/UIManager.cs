using System.Collections.Generic;
using System.Linq;
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
    
    [Header("Other Menus")]
    [SerializeField] private RectTransform settingsMenu;
    [SerializeField] private RectTransform creditsMenu;
    
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
            _instance.skinsButton.gameObject.SetActive(UnlocksManager.Unlocked);
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
        _instance.highestScoreText.text = $"Best — {UnlocksManager.MaxScore}\tReached — {UnlocksManager.MaxDistanceTraveled:F1}m";

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

        var skinMenuData = UnlocksManager.GetSkinMenuData();
        foreach (var categoryUi in skinMenuData.categories)
        {
            var categoryPrefabUi = Instantiate(_instance.skinCategoryPrefab, _instance.skinsMenu);
            categoryPrefabUi.transform.Find("CategoryLabel").GetComponent<TextMeshProUGUI>().text = categoryUi.name;
            var skinPrefabsUi = new Dictionary<GameObject, SkinMenuData.SkinData>();
            
            foreach (var skinUi in categoryUi.skins)
            {
                var skinPrefabUi = Instantiate(_instance.skinPrefab, categoryPrefabUi.transform);
                skinPrefabUi.transform.Find("SkinName").GetComponent<TextMeshProUGUI>().text = skinUi.name;
                skinPrefabUi.transform.Find("SkinImageCanvas/SkinImage").GetComponent<Image>().sprite = skinUi.sprite;
                skinPrefabsUi.Add(skinPrefabUi, skinUi);
            }

            foreach (var skinPrefabUi in skinPrefabsUi)
            {
                skinPrefabUi.Key.GetComponent<Button>().onClick.AddListener(() =>
                {
                    var objects = GameManager.GetPlayer().GetComponentsInChildren<Transform>(true).ToDictionary(o => o.name, o => o);
                    UnlocksManager.SetSkin(categoryUi.name, skinPrefabUi.Value.nameInScene);
                    foreach (var skin in skinPrefabsUi.Values)
                    {
                        var obj = objects[skin.nameInScene];
                        obj.gameObject.SetActive(obj.name == skinPrefabUi.Value.nameInScene);
                    }
                });
            }
        }
    }
    
    public static void DisplaySettingsMenu(bool value)
    {
        if (!_instance.settingsMenu)
            return;
        
        _instance.settingsMenu.gameObject.SetActive(value);
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
            UnlocksManager.PlayMainMusic();
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

    public static void ToggleEnemyNameDisplay(TextMeshProUGUI textMeshPro)
    {
        textMeshPro.text = UnlocksManager.ToggleNameDisplay() ? "Disable Enemy Name Display" : "Enable Enemy Name Display";
    }

    public static void ToggleMusic(TextMeshProUGUI textMeshPro)
    {
        UnlocksManager.AudioSource.mute ^= true;
        textMeshPro.text = UnlocksManager.AudioSource.mute ? "Enable Music" : "Disable Music";
    }

    public static void ToggleSounds(TextMeshProUGUI textMeshPro)
    {
        textMeshPro.text = GameManager.GetPlayer().ToggleMuteSoundEffects() ? "Enable Sounds" : "Disable Sounds";
    }

    public static void SetGameOverText(string text)
    {
        _instance.gameOverText.text = text;
    }
}
