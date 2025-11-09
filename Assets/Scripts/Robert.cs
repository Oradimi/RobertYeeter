using UnityEngine;

public class Robert : MonoBehaviour
{
    [SceneLabel]
    [SerializeField] private string robertName = "Robert";
    
    public void SetRobertName(string newName)
    {
        robertName = newName;
    }
}
