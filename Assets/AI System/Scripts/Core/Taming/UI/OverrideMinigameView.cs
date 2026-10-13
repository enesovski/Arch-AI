using UnityEngine;

namespace Artifika.UI.Override
{
    public enum OverrideMinigameType : byte
    {
        TimingRing,
        PulseStabilization,
    }

    public abstract class OverrideMinigameView : MonoBehaviour
    {
        [SerializeField] protected CanvasGroup moduleCanvasGroup;

        protected OverrideSkillCheckUIConfigSO Config { get; private set; }

        public abstract OverrideMinigameType Type { get; }

        public virtual void Configure(OverrideSkillCheckUIConfigSO config)
        {
            Config = config;
        }

        public virtual void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);

            if (moduleCanvasGroup == null)
                return;

            moduleCanvasGroup.alpha = visible ? 1f : 0f;
            moduleCanvasGroup.interactable = false;
            moduleCanvasGroup.blocksRaycasts = false;
        }

        public abstract void ResetImmediate();

        public abstract void ShowOutcome(OverrideSkillCheckOutcome outcome, bool animated);
    }
}
