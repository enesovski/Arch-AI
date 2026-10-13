using UnityEngine;

namespace Artifika.UI.Override
{
    [DisallowMultipleComponent]
    public sealed class OverrideSkillCheckAudioFeedback : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private AudioSource audioSource;

        [Header("Assign Audio Clips")]
        [SerializeField] private AudioClip checkSuccessClip;
        [SerializeField] private AudioClip checkFailureClip;
        [SerializeField] private AudioClip overrideCompletedClip;
        [SerializeField] private AudioClip overrideLostClip;

        [Header("Success Combo Pitch")]
        [SerializeField, Range(0.5f, 2f)] private float firstSuccessPitch = 1f;
        [SerializeField, Range(0f, 0.5f)] private float pitchIncreasePerSuccess = 0.08f;
        [SerializeField, Range(0.5f, 3f)] private float maximumSuccessPitch = 1.4f;
        [SerializeField] private bool resetComboOnFailedCheck = true;

        [Header("Volume")]
        [SerializeField, Range(0f, 1f)] private float checkVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float terminalVolume = 1f;

        private int _successCombo;

        public void SetAudioSource(AudioSource value)
        {
            audioSource = value;
            ConfigureSource();
        }

        public void BeginSession()
        {
            _successCombo = 0;
            ConfigureSource();
        }

        public void PlayCheckResult(OverrideSkillCheckOutcome outcome)
        {
            if (outcome == OverrideSkillCheckOutcome.Success)
            {
                _successCombo++;
                float pitch = Mathf.Min(
                    maximumSuccessPitch,
                    firstSuccessPitch + (_successCombo - 1) * pitchIncreasePerSuccess);
                Play(checkSuccessClip, pitch, checkVolume);
                return;
            }

            Play(checkFailureClip, 1f, checkVolume);
            if (resetComboOnFailedCheck)
                _successCombo = 0;
        }

        public void PlaySessionEnd(bool completed)
        {
            Play(completed ? overrideCompletedClip : overrideLostClip, 1f, terminalVolume);
            _successCombo = 0;
        }

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            ConfigureSource();
        }

        private void ConfigureSource()
        {
            if (audioSource == null)
                return;

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            audioSource.ignoreListenerPause = true;
        }

        private void Play(AudioClip clip, float pitch, float volume)
        {
            if (audioSource == null || clip == null)
                return;

            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip, volume);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            firstSuccessPitch = Mathf.Clamp(firstSuccessPitch, 0.5f, 2f);
            pitchIncreasePerSuccess = Mathf.Clamp(pitchIncreasePerSuccess, 0f, 0.5f);
            maximumSuccessPitch = Mathf.Max(firstSuccessPitch, maximumSuccessPitch);
            checkVolume = Mathf.Clamp01(checkVolume);
            terminalVolume = Mathf.Clamp01(terminalVolume);
            ConfigureSource();
        }
#endif
    }
}
