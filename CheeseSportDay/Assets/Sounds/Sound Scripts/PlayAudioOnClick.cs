using UdonSharp;
using UnityEngine;
namespace CheeseSportDay.Interactions
{
    [AddComponentMenu("Cheese Sport Day/Interactions/Animator Bool Toggle Button")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(BoxCollider))]
    public class PlayAudioOnClick : UdonSharpBehaviour
    {
        [Tooltip("AudioSource controlled by this button. The same GameObject's AudioSource is used when empty.")]
        public AudioSource audioSource;

        [Tooltip("Playback start position in seconds. Negative values use the subclass default.")]
        public float startTime = -1f;

        [Tooltip("Playback duration in seconds. Zero plays to the end. Negative values use the subclass default.")]
        public float duration = -1f;

        private float stopAtTime = -1f;

        protected virtual float GetDefaultStartTime()
        {
            return 0f;
        }

        protected virtual float GetDefaultDuration()
        {
            return 0f;
        }

        protected virtual void Start()
        {
            Initialize();
        }

        private void Update()
        {
            if (stopAtTime >= 0f && Time.time >= stopAtTime)
            {
                StopAudio();
            }
        }

        public override void Interact()
        {
            Initialize();

            if (audioSource != null && audioSource.isPlaying)
            {
                StopAudio();
                return;
            }

            PlayAudio();
        }

        public void PlayAudio()
        {
            Initialize();

            if (audioSource == null)
            {
                Debug.LogWarning(name + " has no AudioSource assigned.", this);
                return;
            }

            if (audioSource.clip == null)
            {
                Debug.LogWarning(name + " has no AudioClip assigned.", this);
                return;
            }

            float lastPlayableTime = Mathf.Max(0f, audioSource.clip.length - 0.01f);
            audioSource.time = Mathf.Clamp(startTime, 0f, lastPlayableTime);
            audioSource.Play();
            stopAtTime = duration > 0f ? Time.time + duration : -1f;
        }

        public void StopAudio()
        {
            stopAtTime = -1f;

            if (audioSource != null)
            {
                audioSource.Stop();
            }
        }

        private void Initialize()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (startTime < 0f)
            {
                startTime = GetDefaultStartTime();
            }

            if (duration < 0f)
            {
                duration = GetDefaultDuration();
            }
        }
    }
}
