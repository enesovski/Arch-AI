using Sirenix.OdinInspector;
using UnityEngine;

namespace Archura.Aggro.Reactions
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

        public void OnEngagementChanged(EngagementChangeEventArgs args)
        {
            if (!args.ChangesAwareness || !rotationModule)
                return;

            RotationMode mode = passive;
            if (args.Current.IsEngaged())
                mode = alerted;
            else if (args.Current.IsSuspicious())
                mode = suspicious;

            rotationModule.SetMode(mode);
        }
    }
}
