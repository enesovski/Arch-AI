using Unity.Behavior;
using UnityEngine;

namespace Artifika.AI.Aggro.Reactions
{
    public class AggroBehaviorAssignerReaction : MonoBehaviour, IAggroReaction
    {
        private const string AggroStatusVar = "Aggro Status";
        private const string TargetVar = "Target";

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
            isSubscribed = true;

            Debug.Log("[Aggro Assigner] Subscribed to aggro events.");
        }

        private void Unsubscribe()
        {
            if (!isSubscribed) return;
            if (aggroModule == null) return;

            aggroModule.OnTargetChanged -= OnTargetChanged;
            aggroModule.OnStateChanged -= OnAggroStateChanged;
            isSubscribed = false;

            Debug.Log("[Aggro Assigner] Unsubscribed from aggro events.");
        }

        public void OnTargetChanged(TargetChangeEventArgs args) => SetTarget(args.NewTarget);
        public void OnAggroStateChanged(AggroStateChangeEventArgs args) => SetAggroStatus(args.NewState);

        private void SetTarget(GameEntity target)
        {
            if (behaviorGraphAgent == null)
            {
                Debug.LogWarning("[Aggro Assigner] behaviorGraphAgent is null (Initialize order issue).");
                return;
            }

            bool ok = behaviorGraphAgent.BlackboardReference.SetVariableValue(TargetVar, target);
            Debug.Log($"[Aggro Assigner] Set {TargetVar}={target} ok={ok}");
        }

        private void SetAggroStatus(AggroState newState)
        {
            if (behaviorGraphAgent == null)
            {
                Debug.LogWarning("[Aggro Assigner] behaviorGraphAgent is null (Initialize order issue).");
                return;
            }

            var mappedStatus = MapAggroStatus(newState);
            bool ok = behaviorGraphAgent.BlackboardReference.SetVariableValue(AggroStatusVar, mappedStatus);
            Debug.Log($"[Aggro Assigner] Set {AggroStatusVar}={mappedStatus} (from {newState}) ok={ok}");
        }

        private static AggroStatus MapAggroStatus(AggroState state) => state switch
        {
            AggroState.Passive => AggroStatus.Passive,
            AggroState.Suspicious => AggroStatus.Suspicious,
            AggroState.Alerted => AggroStatus.Aggressive,
            _ => AggroStatus.Passive
        };
    }
}
