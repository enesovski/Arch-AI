using Artifika.AI.Aggro.Reactions;
using Artifika.AI.Attack;
using Artifika.AI.Stun.Reactions;
using UnityEngine;
using DG.Tweening;
using Unity.Netcode;
using UnityEngine.Rendering;
using Sirenix.OdinInspector;

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

        private static readonly int EmissionColor = Shader.PropertyToID(EmissionProperty);

        [Title("References")] 
        [SerializeField] private Light eyeLight;             
        [SerializeField] private LensFlareComponentSRP srpLensFlare;
        [SerializeField] private Renderer eyeRenderer;
        [SerializeField] private int materialIndex;
    
        [Title("Configuration")]
        [SerializeField] [LabelText("Robot Eye Configuration")] private RobotEyeConfiguration config;
        [SerializeField] [LabelText("Enable Attack Telegraph")] private bool attackTelegraphEnabled;
        
        private readonly NetworkVariable<RobotEyeVisualState> _syncedVisualState = new(
            RobotEyeVisualState.Passive,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private Material _eyeMat;
        private AttackModule _attackModule;
        private AggroState _aggroState = AggroState.Passive;
        private bool _isStunned;
        private bool _isAttackTelegraphing;
        private bool _attackEventsSubscribed;
        private bool _visualsInitialized;
        private bool _hasAppliedVisualState;
        private RobotEyeVisualState _appliedVisualState;

        private const string EmissionProperty = "_EmissionColor";

        public void Initialize(Blackboard blackboard)
        {
            if (blackboard != null && blackboard.aggroModule != null)
                _aggroState = blackboard.aggroModule.CurrentAggroState;

            if (attackTelegraphEnabled)
                SubscribeToAttackEvents(blackboard != null ? blackboard.attackModule : null);

            EnsureVisualsInitialized();
            RefreshVisualState();
        }

        public override void OnNetworkSpawn()
        {
            _syncedVisualState.OnValueChanged += HandleSyncedVisualStateChanged;
            EnsureVisualsInitialized();

            if (IsServer)
                RefreshVisualState();
            else
                ApplyVisualState(_syncedVisualState.Value);
        }

        public override void OnNetworkDespawn()
        {
            _syncedVisualState.OnValueChanged -= HandleSyncedVisualStateChanged;
            UnsubscribeFromAttackEvents();
            _isAttackTelegraphing = false;
            _hasAppliedVisualState = false;
            KillTweens();
        }

        public void OnAggroStateChanged(AggroStateChangeEventArgs args)
        {
            _aggroState = args.NewState;
            RefreshVisualState();
        }

        public void OnStunnedChanged(bool isStunned)
        {
            _isStunned = isStunned;
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
            RobotEyeVisualState visualState = _isStunned
                ? RobotEyeVisualState.Stunned
                : _isAttackTelegraphing
                    ? RobotEyeVisualState.Telegraphing
                    : ToVisualState(_aggroState);

            if (IsServer && IsSpawned)
                _syncedVisualState.Value = visualState;

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

            if (_hasAppliedVisualState && _appliedVisualState == state)
                return;

            bool leavingTelegraph =
                _hasAppliedVisualState &&
                _appliedVisualState == RobotEyeVisualState.Telegraphing;

            if (state == RobotEyeVisualState.Telegraphing)
            {
                _hasAppliedVisualState = true;
                _appliedVisualState = state;
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

            _hasAppliedVisualState = true;
            _appliedVisualState = state;
            float transitionDuration = leavingTelegraph
                ? config.telegraphReturnDuration
                : config.fadeDuration;

            if (_eyeMat != null)
            {
                DOTween.Kill(_eyeMat);
                DOTween.To(
                    () => _eyeMat.GetColor(EmissionColor).maxColorComponent,
                    v => _eyeMat.SetColor(EmissionColor, col * v),
                    eyeInt,
                    transitionDuration
                ).SetTarget(_eyeMat).SetEase(config.easeMethod);
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

            if (_eyeMat != null)
            {
                _eyeMat.SetColor(EmissionColor, color * config.aggressiveEyeLightIntensity);
                DOTween.To(
                        () => _eyeMat.GetColor(EmissionColor).maxColorComponent,
                        value => _eyeMat.SetColor(EmissionColor, color * value),
                        config.telegraphEyeLightIntensity,
                        pulseHalfDuration)
                    .SetTarget(_eyeMat)
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
            if (_attackEventsSubscribed && _attackModule == attackModule)
                return;

            UnsubscribeFromAttackEvents();
            _attackModule = attackModule;

            if (_attackModule == null)
                return;

            _attackModule.OnAttackCommittedStarted += HandleAttackCommittedStarted;
            _attackModule.OnAttackPerformed += HandleAttackPerformed;
            _attackModule.OnAttackCommittedEnded += HandleAttackCommittedEnded;
            _attackEventsSubscribed = true;
        }

        private void UnsubscribeFromAttackEvents()
        {
            if (_attackModule != null && _attackEventsSubscribed)
            {
                _attackModule.OnAttackCommittedStarted -= HandleAttackCommittedStarted;
                _attackModule.OnAttackPerformed -= HandleAttackPerformed;
                _attackModule.OnAttackCommittedEnded -= HandleAttackCommittedEnded;
            }

            _attackEventsSubscribed = false;
            _attackModule = null;
        }

        private void HandleAttackCommittedStarted()
        {
            _isAttackTelegraphing = true;
            RefreshVisualState();
        }

        private void HandleAttackPerformed(BaseAttackDefinition _)
        {
            StopAttackTelegraph();
        }

        private void HandleAttackCommittedEnded()
        {
            StopAttackTelegraph();
        }

        private void StopAttackTelegraph()
        {
            if (!_isAttackTelegraphing)
                return;

            _isAttackTelegraphing = false;
            RefreshVisualState();
        }

        private void EnsureVisualsInitialized()
        {
            if (_visualsInitialized)
                return;

            _visualsInitialized = true;

            if (eyeRenderer == null)
                return;

            Material[] materials = eyeRenderer.materials;
            if (materialIndex < 0 || materialIndex >= materials.Length)
            {
                GameLog.Error($"[{nameof(AggroEyeColorReaction)}] Material index {materialIndex} is invalid on {name}.");
                return;
            }

            _eyeMat = materials[materialIndex];
            _eyeMat.EnableKeyword("_EMISSION");
        }

        private void KillTweens()
        {
            if (_eyeMat != null)
                DOTween.Kill(_eyeMat);

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
