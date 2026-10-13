using Artifika.AI;
using HighlightPlus;
using UnityEngine;

[DisallowMultipleComponent]
public class TameableEntityInteractable : MonoBehaviour, IInteractionTarget
{
    [SerializeField] private string interactionText = "Hold Attack to Override";
    [SerializeField] private string interactionLocKey = "Interact_Override_Hold";
    [SerializeField] private GameEntity target;
    [SerializeField] private Blackboard blackboard;
    [SerializeField] private HighlightEffect highlightEffect;

    private InteractionHighlight _highlight;

    public bool CanInteract => isActiveAndEnabled &&
                               target != null &&
                               blackboard != null &&
                               blackboard.CanBeTamed();

    private void Awake()
    {
        // The effect stays on; only "highlighted" toggles (as before).
        _highlight = InteractionHighlight.Ensure(gameObject, highlightEffect, alwaysOn: true);
    }

    private void OnDisable()
    {
        if (_highlight != null) _highlight.SetTargeted(false);
    }

    // Override sessions start from an override-weapon hit, not an interaction input.
    // This component provides availability, highlighting and the hint only.
    public bool BuildPrompt(in InteractorContext ctx, InteractionPrompt prompt)
    {
        if (!CanInteract) return false;

        prompt.Title = target.entityConfiguration != null ? target.entityConfiguration.entityName : null;
        prompt.Hint = InteractionUtility.TextWithParams(interactionLocKey, interactionText,
            ("Creature_Name", prompt.Title ?? string.Empty));
        return true;
    }

    public void Execute(in InteractorContext ctx, InteractionInput input) { }

}
