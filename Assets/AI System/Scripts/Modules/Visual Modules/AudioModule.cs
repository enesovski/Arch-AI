using System.Collections;
using Archura.Aggro;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Archura
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI", sourceAssembly: "Assembly-CSharp")]
    public class AudioModule : NetworkBehaviour
    {
        [Title("References")]
        [SerializeField] private AggroModule aggroModule;

        [Title("Settings")]
        [SerializeField][MinMaxSlider(0, 20)] private Vector2 gruntTimerRange;

        [Title("Feedbacks")]
        [SerializeField] private MMF_Player passiveGruntFeedback;
        [SerializeField] private MMF_Player suspiciousGruntFeedback;
        [SerializeField] private MMF_Player alertedGruntFeedback;
        [SerializeField] private MMF_Player defaultGruntFeedback;

        [Space]
        [SerializeField] private MMF_Player onAlertedFeedback;
        [Space]
        [SerializeField] private MMF_Player footstepFeedbackPlayer;

        private Coroutine gruntCycleCoroutine;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                if (aggroModule)
                    aggroModule.OnEngagementChanged += HandleEngagementChanged;

                gruntCycleCoroutine = StartCoroutine(GruntSoundCycle());
            }
        }

        private void OnDestroy()
        {
            if (IsServer)
            {
                if (gruntCycleCoroutine != null)
                {
                    StopCoroutine(gruntCycleCoroutine);
                }

                if (aggroModule)
                    aggroModule.OnEngagementChanged -= HandleEngagementChanged;
            }
        }

        private void HandleEngagementChanged(EngagementChangeEventArgs args)
        {
            if (args.ChangesAwareness && args.Current.IsEngaged())
                PlayFeedback(onAlertedFeedback);
        }

        private IEnumerator GruntSoundCycle()
        {
            while (true)
            {
                float wait = Random.Range(gruntTimerRange.x, gruntTimerRange.y);
                yield return new WaitForSeconds(wait);

                EngagementState state = aggroModule ? aggroModule.CurrentState : EngagementState.Passive;

                PlayGruntClientRpc(state);
            }
        }

        [ClientRpc]
        private void PlayGruntClientRpc(EngagementState state)
        {
            switch (state)
            {
                case EngagementState.Passive:
                    PlayFeedback(passiveGruntFeedback);
                    break;
                case EngagementState.Alerted:
                case EngagementState.Searching:
                    PlayFeedback(suspiciousGruntFeedback);
                    break;
                case EngagementState.Engaged:
                    PlayFeedback(alertedGruntFeedback);
                    break;
                default:
                    PlayFeedback(defaultGruntFeedback);
                    break;
            }
        }

        public void RequestFootstepSound()
        {
            if (!IsServer) return;
            PlayFootstepClientRpc();
        }

        [ClientRpc]
        private void PlayFootstepClientRpc()
        {
            PlayFeedback(footstepFeedbackPlayer);
        }

        private static void PlayFeedback(MMF_Player feedback)
        {
            if (feedback)
                feedback.PlayFeedbacks();
        }
    }
}
