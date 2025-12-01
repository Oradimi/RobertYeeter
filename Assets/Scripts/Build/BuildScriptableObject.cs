using UnityEngine;

[CreateAssetMenu(fileName = "Build", menuName = "ScriptableObjects/BuildScriptableObject", order = 1)]
public class BuildScriptableObject : ScriptableObject
{
    public enum DevelopmentPhase
    {
        Release,
        Beta,
        Alpha,
        PreAlpha,
    }
    
    public int buildNumber = 0;
    public DevelopmentPhase developmentPhase;
}
