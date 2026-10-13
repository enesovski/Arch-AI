using System;
using Artifika.AI.Utility;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Blackboard = Artifika.AI.Blackboard;
using Composite = Unity.Behavior.Composite;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Utility Action Selector",
        story: "Evaluate children using [Blackboard]",
        category: "Flow",
        description: "Scores all UtilityNode children each decision interval and ticks the winner.",
        id: "88c0415cc7ae3b98419fdb2d9b2050ae")]
    public partial class UtilityActionSelectorSequence : Composite
    {
        [SerializeReference] public BlackboardVariable<Blackboard> Blackboard = new BlackboardVariable<Blackboard>();

        [SerializeReference] public BlackboardVariable<float> updateInterval = new BlackboardVariable<float>(0.35f);

        [SerializeReference] public BlackboardVariable<float> HysteresisBias = new BlackboardVariable<float>(0.15f);

        [SerializeReference] public BlackboardVariable<float> MinimumScoreToRun = new BlackboardVariable<float>(0.3f);

        [SerializeReference] public BlackboardVariable<bool> EnableDebug = new BlackboardVariable<bool>(false);

        private int currentIndex = -1;
        private float decisionTimer;

        protected override Status OnStart()
        {
            decisionTimer = 0f;
            currentIndex = -1;
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            var blackboard = Blackboard.Value;
            if (blackboard == null || blackboard.attackModule == null)
                return Status.Failure;

            if (blackboard.attackModule.IsAttackCommitted)
            {
                StopCurrentUtilityAction();
                return Status.Running;
            }

            if (!blackboard.attackModule.ShouldUseAttackBranch())
            {
                StopCurrentUtilityAction();
                return Status.Failure;
            }

            decisionTimer -= Time.deltaTime;

            if (decisionTimer <= 0f)
            {
                decisionTimer = updateInterval.Value;
                int best = SelectBestIndex();

                if (best < 0)
                {
                    StopCurrentUtilityAction();
                    return Status.Running;
                }

                if (currentIndex != best)
                {
                    if (currentIndex >= 0 && Children[currentIndex].CurrentStatus == Status.Running)
                    {
                        EndNode(Children[currentIndex]);
                    }

                    currentIndex = best;
                    StartNode(Children[currentIndex]);
                }
                else if (Children[currentIndex].CurrentStatus != Status.Running)
                {
                    StartNode(Children[currentIndex]);
                }
            }
            else
            {
                if (currentIndex >= 0 && Children[currentIndex].CurrentStatus != Status.Running)
                {
                    decisionTimer = 0f;
                }
            }

            return Status.Running;
        }

        protected override void OnEnd()
        {
            if (currentIndex >= 0 && Children[currentIndex].CurrentStatus == Status.Running)
            {
                EndNode(Children[currentIndex]);
            }
        }

        private void StopCurrentUtilityAction()
        {
            if (currentIndex >= 0 && Children[currentIndex].CurrentStatus == Status.Running)
                EndNode(Children[currentIndex]);

            currentIndex = -1;
            decisionTimer = 0f;
            Blackboard.Value?.movementModule?.Stop();
        }

        private int SelectBestIndex()
        {
            if (!Blackboard.Value) return -1;

            TacticalContext ctx = Blackboard.Value.GetTacticalContext();

            float bestScore = float.MinValue;
            int bestIndex = -1;

            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i] is not IUtilityActionNode node)
                    continue;

                float baseScore = node.EvaluateScore(ctx);

                if (baseScore <= 0f)
                    continue;

                float score = baseScore;

                if (i == currentIndex)
                    score += HysteresisBias.Value;

                if (EnableDebug.Value)
                {
                    Debug.Log($"[Utility AI] Evaluated {node.GetActionName()}: Score = {score:F2}");
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            if (EnableDebug.Value && bestIndex >= 0)
            {
                if (Children[bestIndex] is IUtilityActionNode bestNode)
                {
                    Debug.Log($"[Utility AI] Selected Winner: {bestNode.GetActionName()} with Score {bestScore:F2}");
                }
            }

            return bestScore >= MinimumScoreToRun.Value ? bestIndex : -1;
        }
    }
}
