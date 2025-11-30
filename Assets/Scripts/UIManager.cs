using UnityEngine;
using UnityEngine.SceneManagement;

[ExecuteAlways]
public class UIManager : MonoBehaviour
{
    [Header("Editor")]
    [SerializeField] private bool showUiInEditMode;
    [Header("Main")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform uiObject;
    [SerializeField] private Font interFont;
    [Header("Resources")]
    [SerializeField] private Texture2D button;
    [SerializeField] private Texture2D buttonHover;
    [SerializeField] private Texture2D buttonDisabled;

    private void OnGUI()
    {
        if (!showUiInEditMode || Application.isPlaying && FloorManager.IsStarted() && !FloorManager.IsGameOver())
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

        var worldPos = uiObject.position;
        var screenPos = mainCamera.WorldToScreenPoint(worldPos);

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
        DrawScaledLabel(new Rect(position.x + shadowOffset, position.y + shadowOffset + 120, labelWidth + 200, labelHeight),
            "Meina needs to escape!\nRun into the miners named Robert.", styleLabels, 1f);

        styleLabels.normal.textColor = Color.white;
        styleLabels.hover.textColor = Color.white;
        DrawScaledLabel(new Rect(position.x, position.y + 120, labelWidth + 200, labelHeight),
            "Meina needs to escape!\nRun into the miners named Robert.", styleLabels, 1f);
        
        styleLabels.normal.textColor = Color.black;
        styleLabels.hover.textColor = Color.black;
        DrawScaledLabel(new Rect(position.x + shadowOffset + 500, position.y + shadowOffset + 120, labelWidth + 200, labelHeight),
            "Escape to pause.\nLeft click to charge.\nSpace to Jump.", styleLabels, 0.5f);

        styleLabels.normal.textColor = Color.white;
        styleLabels.hover.textColor = Color.white;
        DrawScaledLabel(new Rect(position.x + 500, position.y + 120, labelWidth + 200, labelHeight),
            "Escape to pause.\nLeft click to charge.\nSpace to Jump.", styleLabels, 0.5f);
        
        // Label shadow
        style.normal.textColor = Color.black;
        style.hover.textColor = Color.black;
        DrawScaledButton(new Rect(position.x + shadowOffset, position.y + shadowOffset, labelWidth - 80f, labelHeight - 40f),
           FloorManager.IsGameOver() ? "Restart" : "Start", style, 1f);

        // Label
        style.normal.textColor = Color.white;
        style.hover.textColor = Color.white;
        style.normal.background = null;
        style.hover.background = null;
        DrawScaledButton(new Rect(position.x, position.y, labelWidth - 80f, labelHeight - 40f),
            FloorManager.IsGameOver() ? "Restart" : "Start", style, 1f);
        
        // Label shadow
        style.normal.textColor = Color.black;
        style.hover.textColor = Color.black;
        style.normal.background = button;
        style.hover.background = buttonHover;
        DrawScaledButton3(new Rect(position.x + shadowOffset, position.y + shadowOffset + 500, labelWidth + 300, labelHeight + 40f),
            UnlocksManager.GetNameDisplay() ? "Disable Enemy Names" : "Enable Enemy Names", style, 0.5f, true);

        // Label
        style.normal.textColor = Color.white;
        style.hover.textColor = Color.white;
        style.normal.background = null;
        style.hover.background = null;
        DrawScaledButton3(new Rect(position.x, position.y + 500, labelWidth + 300, labelHeight + 40f),
            UnlocksManager.GetNameDisplay() ? "Disable Enemy Names" : "Enable Enemy Names", style, 0.5f, true);
        
        // Label shadow
        style.normal.textColor = Color.black;
        style.hover.textColor = Color.black;
        style.normal.background = UnlocksManager.IsAudioSourceMute() ? buttonDisabled : button;
        style.hover.background = UnlocksManager.IsAudioSourceMute() ? buttonDisabled : button;
        DrawScaledToggle(new Rect(position.x + shadowOffset + 500, position.y + shadowOffset + 500, labelWidth, labelHeight + 40f),
            "Music", style, 0.5f, true);

        // Label
        style.normal.textColor = Color.white;
        style.hover.textColor = Color.white;
        style.normal.background = null;
        style.hover.background = null;
        DrawScaledToggle(new Rect(position.x + 500, position.y + 500, labelWidth, labelHeight + 40f),
            "Music", style, 0.5f, true);
        
        // Label shadow
        style.normal.textColor = Color.black;
        style.hover.textColor = Color.black;
        style.normal.background = GameManager.IsPlayerAudioSourceMute() ? buttonDisabled : button;
        style.hover.background = GameManager.IsPlayerAudioSourceMute() ? buttonDisabled : button;
        DrawScaledToggle(new Rect(position.x + shadowOffset + 700, position.y + shadowOffset + 500, labelWidth, labelHeight + 40f),
            "Sounds", style, 0.5f, false);

        // Label
        style.normal.textColor = Color.white;
        style.hover.textColor = Color.white;
        style.normal.background = null;
        style.hover.background = null;
        DrawScaledToggle(new Rect(position.x + 700, position.y + 500, labelWidth, labelHeight + 40f),
            "Sounds", style, 0.5f, false);

        if (UnlocksManager.Unlocked)
        {
            // Label shadow
            style.normal.textColor = Color.black;
            style.hover.textColor = Color.black;
            style.normal.background = button;
            style.hover.background = buttonHover;
            DrawScaledButton2(new Rect(position.x + shadowOffset, position.y + shadowOffset + 300, labelWidth, labelHeight + 40f),
                "Change Clothes", style, 0.5f, true);

            // Label
            style.normal.textColor = Color.white;
            style.hover.textColor = Color.white;
            style.normal.background = null;
            style.hover.background = null;
            DrawScaledButton2(new Rect(position.x, position.y + 300, labelWidth, labelHeight + 40f),
                "Change Clothes", style, 0.5f, true);
        
            // Label shadow
            style.normal.textColor = Color.black;
            style.hover.textColor = Color.black;
            style.normal.background = button;
            style.hover.background = buttonHover;
            DrawScaledButton2(new Rect(position.x + shadowOffset + 400, position.y + shadowOffset + 300, labelWidth, labelHeight + 40f),
                "Change Hair", style, 0.5f, false);

            // Label
            style.normal.textColor = Color.white;
            style.hover.textColor = Color.white;
            style.normal.background = null;
            style.hover.background = null;
            DrawScaledButton2(new Rect(position.x + 400, position.y + 300, labelWidth, labelHeight + 40f),
                "Change Hair", style, 0.5f, false);
        }
    }
    
    private void DrawScaledButton(Rect rect, string text, GUIStyle style, float scale)
    {
        var matrixBackup = GUI.matrix;
        GUIUtility.ScaleAroundPivot(Vector2.one * scale, rect.position);
        if (GUI.Button(rect, text, style) && Application.isPlaying)
        {
            if (!FloorManager.IsGameOver())
            {
                GameManager.GetPlayer().EnableUIMap();
                UnlocksManager.PlayMainMusic();
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }

            FloorManager.StartGame();
            GameManager.ResetScore();
        }
        GUI.matrix = matrixBackup;
    }
    
    private void DrawScaledButton2(Rect rect, string text, GUIStyle style, float scale, bool what)
    {
        var matrixBackup = GUI.matrix;
        GUIUtility.ScaleAroundPivot(Vector2.one * scale, rect.position);
        if (GUI.Button(rect, text, style) && Application.isPlaying)
        {
            if (what)
                UnlocksManager.ChangeClothes();
            else
                UnlocksManager.ChangeHair();
        }
        GUI.matrix = matrixBackup;
    }
    
    private void DrawScaledButton3(Rect rect, string text, GUIStyle style, float scale, bool what)
    {
        var matrixBackup = GUI.matrix;
        GUIUtility.ScaleAroundPivot(Vector2.one * scale, rect.position);
        if (GUI.Button(rect, text, style) && Application.isPlaying)
        {
            UnlocksManager.ToggleNameDisplay();
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
    
    private void DrawScaledToggle(Rect rect, string text, GUIStyle style, float scale, bool what)
    {
        var matrixBackup = GUI.matrix;
        GUIUtility.ScaleAroundPivot(Vector2.one * scale, rect.position);
        if (GUI.Button(rect, text, style) && Application.isPlaying)
        {
            if (what)
                UnlocksManager.audioSource.mute ^= true;
            else
                GameManager.GetPlayer().ToggleMuteSoundEffects();
        }
        GUI.matrix = matrixBackup;
    }
}
