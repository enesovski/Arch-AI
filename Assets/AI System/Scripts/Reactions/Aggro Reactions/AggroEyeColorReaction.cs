using Artifika.AI.Aggro.Reactions;
using Artifika.AI.Attack;
using Artifika.AI.Stun.Reactions;
using DG.Tweening;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

namespace Artifika.AI.Visuals
{
    public class AggroEyeColorReaction : NetworkBehaviour, IAggroReaction, IStunReaction
    {
        private enum RobotEyeVisualState : byte
        {
            Passive,
            Suspicious,
            Alerted,
            Telegraphing,
            Stunned
        }

        private static readonly int emissionColor = Shader.PropertyToID(EmissionProperty);

        [Title("References")]
        [SerializeField] private Light eyeLight;
        [SerializeField] private LensFlareComponentSRP srpLensFlare;
        [SerializeField] private Renderer eyeRenderer;
        [SerializeField] private int materialIndex;

        [Title("Configuration")]
        [SerializeField] [LabelText("Robot Eye Configuration")] private RobotEyeConfiguration config;
        [SerializeField] [LabelText("Enable Attack Telegraph")] private bool attackTelegraphEnabled;

        private readonly NetworkVariable<RobotEyeVisualState> syncedVisualState = new(
            RobotEyeVisualState.Passive,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private Material eyeMat;
        private AttackModule attackModule;
        private AggroState aggroState = AggroState.Passive;
        private bool isStunned;
        private bool isAttackTelegraphing;
        private bool attackEventsSubscribed;
        private bool visualsInitialized;
        private bool hasAppliedVisualState;
        private RobotEyeVisualState appliedVisualState;

        private const string EmissionProperty = "_EmissionColor";

        public void Initialize(Blackboard blackboard)
        {
            if (blackboard != null && blackboard.aggroModule != null)
                aggroState = blackboard.aggroModule.CurrentAggroState;

            if (attackTelegraphEnabled)
                SubscribeToAttackEvents(blackboard != null ? blackboard.attackModule : null);

            EnsureVisualsInitialized();
            RefreshVisualState();
        }

        public override void OnNetworkSpawn()
        {
            syncedVisualState.OnValueChanged += HandleSyncedVisualStateChanged;
            EnsureVisualsInitialized();

            if (IsServer)
                RefreshVisualState();
            else
                ApplyVisualState(syncedVisualState.Value);
        }

        public override void OnNetworkDespawn()
        {
            syncedVisualState.OnValueChanged -= HandleSyncedVisualStateChanged;
            UnsubscribeFromAttackEvents();
            isAttackTelegraphing = false;
            hasAppliedVisualState = false;
            KillTweens();
        }

        public void OnAggroStateChanged(AggroStateChangeEventArgs args)
        {
            aggroState = args.NewState;
            RefreshVisualState();
        }

        public void OnStunnedChanged(bool isStunned)
        {
            this.isStunned = isStunned;
            RefreshVisualState();
        }

        private void HandleSyncedVisualStateChanged(
            RobotEyeVisualState previousState,
            RobotEyeVisualState newState)
        {
            if (!IsServer)
                ApplyVisualState(newState);
        }

        private void RefreshVisualState()
        {
            // Temporary overrides keep the underlying aggro state intact.
            RobotEyeVisualState visualState = isStunned
                ? RobotEyeVisualState.Stunned
                : isAttackTelegraphing
                    ? RobotEyeVisualState.Telegraphing
                    : ToVisualState(aggroState);

            if (IsServer && IsSpawned)
                syncedVisualState.Value = visualState;

            ApplyVisualState(visualState);
        }

        private void ApplyVisualState(RobotEyeVisualState state)
        {
            if (config == null)
            {
                GameLog.Error($"[{nameof(AggroEyeColorReaction)}] Robot eye configuration is missing on {name}.");
                return;
            }

            EnsureVisualsInitialized();

            if (hasAppliedVisualState && appliedVisualState == state)
                return;

            bool leavingTelegraph =
                hasAppliedVisualState &&
                appliedVisualState == RobotEyeVisualState.Telegraphing;

            if (state == RobotEyeVisualState.Telegraphing)
            {
                hasAppliedVisualState = true;
                appliedVisualState = state;
                ApplyTelegraphVisualState();
                return;
            }

            Color col;
            float eyeInt, flareInt, flareScale;

            switch (state)
            {
                case RobotEyeVisualState.Passive:
                    col = config.passiveColor;
                    eyeInt = config.passiveEyeLigthIntensity;
                    flareInt = config.passiveFlareIntensity;
                    flareScale = config.passiveFlareScale;
                    break;

                case RobotEyeVisualState.Suspicious:
                    col = config.threatenedColor;
                    eyeInt = config.threatenedEyeLightIntensity;
                    flareInt = config.threatenedFlareIntensity;
                    flareScale = config.threatenedFlareScale;
                    break;

                case RobotEyeVisualState.Alerted:
                    col = config.aggressiveColor;
                    eyeInt = config.aggressiveEyeLightIntensity;
                    flareInt = config.aggressiveFlareIntensity;
                    flareScale = config.aggressiveFlareScale;
                    break;

                case RobotEyeVisualState.Stunned:
                    col = config.stunnedColor;
                    eyeInt = config.stunnedEyeLightIntensity;
                    flareInt = config.stunnedFlareIntensity;
                    flareScale = config.stunnedFlareScale;
                    break;

                default:
                    return;
            }

            hasAppliedVisualState = true;
            appliedVisualState = state;
            float transitionDuration = leavingTelegraph
                ? config.telegraphReturnDuration
                : config.fadeDuration;

            if (eyeMat != null)
            {
                DOTween.Kill(eyeMat);
                DOTween.To(
                    () => eyeMat.GetColor(emissionColor).maxColorComponent,
                    v => eyeMat.SetColor(emissionColor, col * v),
                    eyeInt,
                    transitionDuration
                ).SetTarget(eyeMat).SetEase(config.easeMethod);
            }

            if (eyeLight != null)
            {
                DOTween.Kill(eyeLight);
                eyeLight.DOColor(col, transitionDuration).SetTarget(eyeLight).SetEase(config.easeMethod);
                eyeLight.DOIntensity(eyeInt, transitionDuration).SetTarget(eyeLight).SetEase(config.easeMethod);
            }

            if (srpLensFlare != null)
            {
                DOTween.Kill(srpLensFlare);
                DOTween.To(() => srpLensFlare.intensity, x => srpLensFlare.intensity = x, flareInt, transitionDuration)
                    .SetTarget(srpLensFlare).SetEase(config.easeMethod);
                DOTween.To(() => srpLensFlare.scale, x => srpLensFlare.scale = x, flareScale, transitionDuration)
                    .SetTarget(srpLensFlare).SetEase(config.easeMethod);
            }
        }

        private void ApplyTelegraphVisualState()
        {
            KillTweens();

            Color color = config.aggressiveColor;
            float pulseHalfDuration = Mathf.Max(0.01f, config.telegraphPulseHalfDuration);

            if (eyeMat != null)
            {
                eyeMat.SetColor(emissionColor, color * config.aggressiveEyeLightIntensity);
                DOTween.To(
                        () => eyeMat.GetColor(emissionColor).maxColorComponent,
                        value => eyeMat.SetColor(emissionColor, color * value),
                        config.telegraphEyeLightIntensity,
                        pulseHalfDuration)
                    .SetTarget(eyeMat)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }

            if (eyeLight != null)
            {
                eyeLight.color = color;
                eyeLight.intensity = config.aggressiveEyeLightIntensity;
                eyeLight.DOIntensity(config.telegraphEyeLightIntensity, pulseHalfDuration)
                    .SetTarget(eyeLight)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }

            if (srpLensFlare != null)
            {
                srpLensFlare.intensity = config.aggressiveFlareIntensity;
                srpLensFlare.scale = config.aggressiveFlareScale;

                DOTween.To(
                        () => srpLensFlare.intensity,
                        value => srpLensFlare.intensity = value,
                        config.telegraphFlareIntensity,
                        pulseHalfDuration)
                    .SetTarget(srpLensFlare)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);

                DOTween.To(
                        () => srpLensFlare.scale,
                        value => srpLensFlare.scale = value,
                        config.telegraphFlareScale,
                        pulseHalfDuration)
                    .SetTarget(srpLensFlare)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        }

        private void SubscribeToAttackEvents(AttackModule attackModule)
        {
            if (attackEventsSubscribed && this.attackModule == attackModule)
                return;

            UnsubscribeFromAttackEvents();
            this.attackModule = attackModule;

            if (this.attackModule == null)
                return;

            this.attackModule.OnAttackCommittedStarted += HandleAttackCommittedStarted;
            this.attackModule.OnAttackPerformed += HandleAttackPerformed;
            this.attackModule.OnAttackCommittedEnded += HandleAttackCommittedEnded;
            attackEventsSubscribed = true;
        }

        private void UnsubscribeFromAttackEvents()
        {
            if (attackModule != null && attackEventsSubscribed)
            {
                attackModule.OnAttackCommittedStarted -= HandleAttackCommittedStarted;
                attackModule.OnAttackPerformed -= HandleAttackPerformed;
                attackModule.OnAttackCommittedEnded -= HandleAttackCommittedEnded;
            }

            attackEventsSubscribed = false;
            attackModule = null;
        }

        private void HandleAttackCommittedStarted()
        {
            isAttackTelegraphing = true;
            RefreshVisualState();
        }

        private void HandleAttackPerformed(BaseAttackDefinition unused)
        {
            StopAttackTelegraph();
        }

        private void HandleAttackCommittedEnded()
        {
            StopAttackTelegraph();
        }

        private void StopAttackTelegraph()
        {
            if (!isAttackTelegraphing)
                return;

            isAttackTelegraphing = false;
            RefreshVisualState();
        }

        private void EnsureVisualsInitialized()
        {
            if (visualsInitialized)
                return;

            visualsInitialized = true;

            if (eyeRenderer == null)
                return;

            Material[] materials = eyeRenderer.materials;
            if (materialIndex < 0 || materialIndex >= materials.Length)
            {
                GameLog.Error($"[{nameof(AggroEyeColorReaction)}] Material index {materialIndex} is invalid on {name}.");
                return;
            }

            eyeMat = materials[materialIndex];
            eyeMat.EnableKeyword("_EMISSION");
        }

        private void KillTweens()
        {
            if (eyeMat != null)
                DOTween.Kill(eyeMat);

            if (eyeLight != null)
                DOTween.Kill(eyeLight);

            if (srpLensFlare != null)
                DOTween.Kill(srpLensFlare);
        }

        private static RobotEyeVisualState ToVisualState(AggroState state)
        {
            return state switch
            {
                AggroState.Suspicious => RobotEyeVisualState.Suspicious,
                AggroState.Alerted => RobotEyeVisualState.Alerted,
                _ => RobotEyeVisualState.Passive
            };
        }
    }
}
