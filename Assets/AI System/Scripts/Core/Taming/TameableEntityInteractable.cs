using HighlightPlus;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    [DisallowMultipleComponent]
    public class TameableEntityInteractable : MonoBehaviour, IInteractionTarget
    {
        [SerializeField] private string interactionText = "Tame";
        [SerializeField] private string interactionLocKey = "Interact_Tame";
        [SerializeField] private GameEntity target;
        [SerializeField] private Blackboard blackboard;
        [SerializeField] private HighlightEffect highlightEffect;

        private InteractionHighlight highlight;

        public bool CanInteract => isActiveAndEnabled &&
                                   target != null &&
                                   blackboard != null &&
                                   blackboard.CanBeTamed();

        private void Awake()
        {
            highlight = InteractionHighlight.Ensure(gameObject, highlightEffect, alwaysOn: true);
        }

        private void OnDisable()
        {
            if (highlight != null) highlight.SetTargeted(false);
        }

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
}
