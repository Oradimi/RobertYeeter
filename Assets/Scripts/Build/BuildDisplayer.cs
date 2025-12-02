using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class BuildDisplayer : MonoBehaviour
{
    private TextMeshProUGUI _text;

    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
#if UNITY_EDITOR
        var obj = Resources.Load("Build", typeof(BuildScriptableObject));
        var buildScriptableObject = obj as BuildScriptableObject;

        if (buildScriptableObject == null)
        {
            Debug.LogError("Build scriptable object not found in resources directory! Check build log for errors!");
        }
        else
        {
            buildScriptableObject.developmentPhase = BuildScriptableObject.DevelopmentPhase.Dev;
            _text.SetText($"v{Application.version}_{buildScriptableObject.buildNumber:000}_{buildScriptableObject.developmentPhase.ToString().ToLowerInvariant()}");
        }
#endif
        var request = Resources.LoadAsync("Build", typeof(BuildScriptableObject));
        request.completed += Request_completed;
    }

    private void Request_completed(AsyncOperation obj)
    {
        var buildScriptableObject = ((ResourceRequest)obj).asset as BuildScriptableObject;

        if (buildScriptableObject == null)
        {
            Debug.LogError("Build scriptable object not found in resources directory! Check build log for errors!");
        }
        else
        {
            if (buildScriptableObject.developmentPhase == BuildScriptableObject.DevelopmentPhase.Release)
                _text.SetText($"v{Application.version}_{buildScriptableObject.buildNumber:000}");
            else
                _text.SetText($"v{Application.version}_{buildScriptableObject.buildNumber:000}_{buildScriptableObject.developmentPhase.ToString().ToLowerInvariant()}");
        }
    }
}
