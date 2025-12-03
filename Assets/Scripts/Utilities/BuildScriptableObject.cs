using UnityEngine;

namespace Utilities
{
    [CreateAssetMenu(fileName = "Build", menuName = "ScriptableObjects/BuildScriptableObject", order = 1)]
    public class BuildScriptableObject : ScriptableObject
    {
        public enum DevelopmentPhase
        {
            Release,
            Beta,
            Alpha,
            PreAlpha,
            Dev,
        }
    
        public int buildNumber;
        public DevelopmentPhase developmentPhase;
    }
}
