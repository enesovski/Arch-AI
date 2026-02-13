using Unity.Netcode;       
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;

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
            aggroModule.OnStateChanged += state =>
            {
                if (state.NewState == AggroState.Alerted)
                    onAlertedFeedback?.PlayFeedbacks();
            };

            gruntCycleCoroutine = StartCoroutine(GruntSoundCycle());
        }
    }

    private void OnDestroy()
    {
        if (IsServer)
        {
            StopCoroutine(gruntCycleCoroutine);
            aggroModule.OnStateChanged -= _ => { };
        }
    }

    private IEnumerator GruntSoundCycle()
    {
        while (true)
        {
            float wait = Random.Range(gruntTimerRange.x, gruntTimerRange.y);
            yield return new WaitForSeconds(wait);

            AggroState state = aggroModule.CurrentAggroState;

            PlayGruntClientRpc(state);
        }
    }

    [ClientRpc]
    private void PlayGruntClientRpc(AggroState state)
    {
        switch (state)
        {
            case AggroState.Passive:
                passiveGruntFeedback?.PlayFeedbacks();
                break;
            case AggroState.Suspicious:
                suspiciousGruntFeedback?.PlayFeedbacks();
                break;
            case AggroState.Alerted:
                alertedGruntFeedback?.PlayFeedbacks();
                break;
            default:
                defaultGruntFeedback?.PlayFeedbacks();
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
        footstepFeedbackPlayer?.PlayFeedbacks();
    }
}
