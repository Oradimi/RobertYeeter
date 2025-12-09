using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
[RequireComponent(typeof(Animator))]
public class SelectableAnimation : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler
{
    private static readonly int NormalTrigger = Animator.StringToHash("Normal");
    private static readonly int HoverTrigger = Animator.StringToHash("Highlighted");
    
    [SerializeField] private Animator animator;
    [SerializeField] private Selectable selectable;

    protected void Awake()
    {
        if (!animator)
            animator = GetComponent<Animator>();
        
        if (!selectable)
            selectable = GetComponent<Selectable>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        EvaluateAndTransitionToSelectionState(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        EvaluateAndTransitionToSelectionState(false);
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (eventData is not PointerEventData)
            UIManager.PlaySelectElement();
        EvaluateAndTransitionToSelectionState(true);
    }
    
    public void OnDeselect(BaseEventData eventData)
    {
        EvaluateAndTransitionToSelectionState(false);
    }
    
    private void EvaluateAndTransitionToSelectionState(bool isHovered)
    {
        if (!selectable.IsActive() || !selectable.IsInteractable())
            return;

        TriggerAnimation(isHovered ? HoverTrigger : NormalTrigger);
    }
    
    private void TriggerAnimation(int triggerHash)
    {
        if (!gameObject.activeInHierarchy || !animator.isActiveAndEnabled || !animator.hasBoundPlayables)
            return;

        animator.ResetTrigger(NormalTrigger);
        animator.ResetTrigger(HoverTrigger);

        animator.SetTrigger(triggerHash);
    }
}