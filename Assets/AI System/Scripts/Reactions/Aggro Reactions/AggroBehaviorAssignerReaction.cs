using Unity.Behavior;
using UnityEngine;

namespace Artifika.AI.Aggro.Reactions
{
    public class AggroBehaviorAssignerReaction : MonoBehaviour, IAggroReaction
    {
        private const string AggroStatusVar = "Aggro Status";
        private const string TargetVar = "Target";
        private const string SuspiciousStateVar = "Suspicious State";

        private AggroModule aggroModule;
        private BehaviorGraphAgent behaviorGraphAgent;
        private bool isSubscribed;

        public void Initialize(Blackboard blackboard)
        {
            aggroModule = blackboard.aggroModule;
            behaviorGraphAgent = blackboard.behaviorGraphAgent;

            TrySubscribe();

            SetTarget(null);
            SetAggroStatus(AggroState.Passive);
            SetSuspiciousState(SuspiciousState.None);
        }

        private void OnEnable() => TrySubscribe();
        private void OnDisable() => Unsubscribe();

        private void TrySubscribe()
        {
            if (!isActiveAndEnabled) return;
            if (isSubscribed) return;
            if (aggroModule == null) return;

            aggroModule.OnTargetChanged += OnTargetChanged;
            aggroModule.OnStateChanged += OnAggroStateChanged;
            aggroModule.OnSuspiciousStateChanged += OnSuspiciousStateChanged;
            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!isSubscribed) return;
            if (aggroModule == null) return;

            aggroModule.OnTargetChanged -= OnTargetChanged;
            aggroModule.OnStateChanged -= OnAggroStateChanged;
            aggroModule.OnSuspiciousStateChanged -= OnSuspiciousStateChanged;
            isSubscribed = false;
        }

        public void OnTargetChanged(TargetChangeEventArgs args) => SetTarget(args.NewTarget);
        public void OnAggroStateChanged(AggroStateChangeEventArgs args) => SetAggroStatus(args.NewState);
        public void OnSuspiciousStateChanged(SuspiciousStateChangeEventArgs args) => SetSuspiciousState(args.NewState);
        

        private void SetTarget(GameEntity target)
        {
            if (behaviorGraphAgent == null) return;
            if (behaviorGraphAgent.BlackboardReference == null) return;

            behaviorGraphAgent.BlackboardReference.SetVariableValue(TargetVar, target);
        }

        private void SetAggroStatus(AggroState newState)
        {
            if (behaviorGraphAgent == null) return;
            if (behaviorGraphAgent.BlackboardReference == null) return;

            behaviorGraphAgent.BlackboardReference
                .SetVariableValue(AggroStatusVar, MapAggroStatus(newState));
        }

        private void SetSuspiciousState(SuspiciousState newState)
        {
            if (behaviorGraphAgent == null) return;
            if (behaviorGraphAgent.BlackboardReference == null) return;

            behaviorGraphAgent.BlackboardReference
                .SetVariableValue(SuspiciousStateVar, MapSuspiciousStatus(newState));
        }


        private static AggroStatus MapAggroStatus(AggroState state) => state switch
        {
            AggroState.Passive => AggroStatus.Passive,
            AggroState.Suspicious => AggroStatus.Suspicious,
            AggroState.Alerted => AggroStatus.Aggressive,
            _ => AggroStatus.Passive
        };

        private static SuspiciousStatus MapSuspiciousStatus(SuspiciousState state) => state switch
        {
            SuspiciousState.Alerted => SuspiciousStatus.Alerted,
            SuspiciousState.Searching => SuspiciousStatus.Searching,
            _ => SuspiciousStatus.None
        };
    }
}
