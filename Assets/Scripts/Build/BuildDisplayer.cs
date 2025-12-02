using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class BuildDisplayer : MonoBehaviour
{
    private TextMeshProUGUI _text;

    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
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
#if UNITY_EDITOR
            buildScriptableObject.developmentPhase = BuildScriptableObject.DevelopmentPhase.Dev;
#endif
            if (buildScriptableObject.developmentPhase == BuildScriptableObject.DevelopmentPhase.Release)
                _text.SetText($"v{Application.version}_{buildScriptableObject.buildNumber:000}");
            else
                _text.SetText($"v{Application.version}_{buildScriptableObject.buildNumber:000}_{buildScriptableObject.developmentPhase.ToString().ToLowerInvariant()}");
        }
    }
}
