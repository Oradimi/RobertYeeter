using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UINavigation
{
    public class SetUiElementToSelectOnInteraction : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private EventSystem eventSystem;
        [SerializeField] private Selectable elementToSelect;

        [Header("Visualization")] 
        [SerializeField] private bool showVisualization;
        [SerializeField] private Color navigationColour = Color.cyan;
        
        private void OnDrawGizmos()
        {
            if (!showVisualization)
                return;
            
            if (!elementToSelect)
                return;
            
            Gizmos.color = navigationColour;
            Gizmos.DrawLine(gameObject.transform.position, elementToSelect.gameObject.transform.position);
        }

        private void Reset()
        {
            eventSystem = FindAnyObjectByType<EventSystem>();
            
            if (!eventSystem)
                Debug.Log("Did not find an Event System in your Scene.", this);
        }
        
        public void JumpToElement()
        {
            if (!eventSystem)
                Debug.Log("This item has no event system referenced yet.", this);
            
            if (!elementToSelect)
                Debug.Log("This should jump where?", this);
            
            eventSystem.SetSelectedGameObject(elementToSelect.gameObject);
        }
    }
}
