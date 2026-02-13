using UnityEngine;
using Sirenix.OdinInspector;

namespace Artifika.AI.Aggro.Reactions
{
    [DisallowMultipleComponent]
    [Title("Aggro Rotation Reaction")]
    public sealed class AggroRotationReaction : MonoBehaviour, IAggroReaction
    {
        [LabelText("Passive")]
        [SerializeField] private RotationMode passive = RotationMode.Agent;

        [LabelText("Suspicious")]
        [SerializeField] private RotationMode suspicious = RotationMode.Agent;

        [LabelText("Alerted")]
        [Tooltip("During Alerted, AttackModule may override this to LookAt when actually attacking.")]
        [SerializeField] private RotationMode alerted = RotationMode.Agent;

        private RotationModule rotationModule;

        public void Initialize(Blackboard blackboard)
        {
            rotationModule = blackboard.rotationModule;
        }

        public void OnAggroStateChanged(AggroStateChangeEventArgs args)
        {
            if (rotationModule == null)
                return;

            RotationMode mode = args.NewState switch
            {
                AggroState.Passive    => passive,
                AggroState.Suspicious => suspicious,
                AggroState.Alerted    => alerted,
                _                     => passive
            };

            rotationModule.SetMode(mode);
        }
    }
}