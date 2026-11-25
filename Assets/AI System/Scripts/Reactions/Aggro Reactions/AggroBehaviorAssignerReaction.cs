using Unity.Behavior;
using UnityEngine;

namespace Archura.Aggro.Reactions
{
    public class AggroBehaviorAssignerReaction : MonoBehaviour, IAggroReaction
    {
        private const string TargetVar = "Target";
        private const string EngagementStateVar = "Engagement State";

        private AggroModule aggroModule;
        private BehaviorGraphAgent behaviorGraphAgent;
        private bool isSubscribed;

        public void Initialize(Blackboard blackboard)
        {
            aggroModule = blackboard.aggroModule;
            behaviorGraphAgent = blackboard.behaviorGraphAgent;

            TrySubscribe();

            SetVariable(TargetVar, (GameEntity)null);
            SetVariable(EngagementStateVar, EngagementState.Passive);
        }

        private void OnEnable() => TrySubscribe();
        private void OnDisable() => Unsubscribe();

        private void TrySubscribe()
        {
            if (!isActiveAndEnabled) return;
            if (isSubscribed) return;
            if (aggroModule == null) return;

            aggroModule.OnTargetChanged += OnTargetChanged;
            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!isSubscribed) return;
            if (aggroModule == null) return;

            aggroModule.OnTargetChanged -= OnTargetChanged;
            isSubscribed = false;
        }

        public void OnTargetChanged(TargetChangeEventArgs args) => SetVariable(TargetVar, args.NewTarget);

        public void OnEngagementChanged(EngagementChangeEventArgs args) => SetVariable(EngagementStateVar, args.Current);

        private void SetVariable<T>(string variableName, T value)
        {
            if (behaviorGraphAgent == null) return;
            if (behaviorGraphAgent.BlackboardReference == null) return;

            behaviorGraphAgent.BlackboardReference.SetVariableValue(variableName, value);
        }
    }
}
