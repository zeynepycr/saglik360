// ============================================================
//  Sağlık360 – FeedbackSystem.cs  |  Feedback
// ============================================================
//  Multi-modal feedback controller:
//    • Visual  – color-coded holographic indicators in VR
//    • Haptic  – Quest 2 controller vibration patterns
//    • Audio   – sound effects for events
//
//  HUDController calls PlayToleranceFeedback() every frame.
//  ExerciseManager calls PlayRepComplete() / PlayCompletion().
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.XR;

namespace Saglik360
{
    public class FeedbackSystem : MonoBehaviour
    {
        // ── Inspector refs ───────────────────────────────────
        [Header("Visual References")]
        [Tooltip("Renderer of the in-world ROM arc indicator.")]
        [SerializeField] private Renderer arcIndicatorRenderer;
        [Tooltip("Particle system that plays on rep completion.")]
        [SerializeField] private ParticleSystem repCompleteParticles;
        [Tooltip("Particle system that plays on exercise completion.")]
        [SerializeField] private ParticleSystem exerciseCompleteParticles;

        [Header("Materials")]
        [SerializeField] private Material correctMaterial;    // Green
        [SerializeField] private Material warningMaterial;    // Yellow
        [SerializeField] private Material errorMaterial;      // Red

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip   repCompleteClip;
        [SerializeField] private AudioClip   exerciseCompleteClip;
        [SerializeField] private AudioClip   errorClip;
        [SerializeField] private AudioClip   countdownClip;
        [SerializeField] private AudioClip   levelUpClip;
        [SerializeField] private AudioClip   achievementClip;

        [Header("Haptic Settings")]
        [SerializeField] [Range(0f, 1f)] private float hapticAmplitude = 0.5f;

        // ── Private state ────────────────────────────────────
        private bool   _lastToleranceState = true;
        private Coroutine _feedbackCoroutine;

        // ────────────────────────────────────────────────────
        #region Real-Time Tolerance Feedback  (called every frame)
        // ────────────────────────────────────────────────────

        /// <summary>
        /// Call every Update() while an exercise is active.
        /// Changes arc indicator color and triggers one-shot haptic on state change.
        /// </summary>
        public void PlayToleranceFeedback(bool withinTolerance, float deviationDeg)
        {
            // ── Visual ───────────────────────────────────────
            if (arcIndicatorRenderer != null && correctMaterial != null && warningMaterial != null && errorMaterial != null)
            {
                arcIndicatorRenderer.material = withinTolerance ? correctMaterial
                    : deviationDeg < 15f       ? warningMaterial
                    :                            errorMaterial;
            }

            // ── Haptic on tolerance change ───────────────────
            if (withinTolerance != _lastToleranceState)
            {
                _lastToleranceState = withinTolerance;
                if (withinTolerance)
                    SendHaptic(XRNode.RightHand, 0.3f, 0.2f);  // short confirm buzz
                else
                    SendHaptic(XRNode.RightHand, 0.15f, 0.1f); // soft warning
            }
        }

        #endregion

        // ────────────────────────────────────────────────────
        #region Event-Triggered Feedback
        // ────────────────────────────────────────────────────

        /// <summary>Called when a single rep is completed.</summary>
        public void PlayRepComplete(float accuracyPercent)
        {
            if (repCompleteClip != null)
                PlayAudio(repCompleteClip);
            if (repCompleteParticles != null)
                repCompleteParticles.Play();
            // Strong buzz proportional to accuracy
            float amplitude = Mathf.Lerp(0.2f, 0.8f, accuracyPercent / 100f);
            SendHaptic(XRNode.RightHand, amplitude, 0.3f);
        }

        /// <summary>Called when the full exercise is complete.</summary>
        public void PlayCompletion(float accuracyPercent)
        {
            if (exerciseCompleteClip != null)
                PlayAudio(exerciseCompleteClip);
            if (exerciseCompleteParticles != null)
                exerciseCompleteParticles.Play();
            StartCoroutine(CelebrationHaptics());
        }

        /// <summary>Countdown numbers (3-2-1) before exercise starts.</summary>
        public void ShowCountdown(int number)
        {
            if (countdownClip != null)
                PlayAudio(countdownClip);
            // The HUDController listens to CountdownVisible event for the UI part
            Debug.Log($"[FeedbackSystem] Geri sayım: {number}");
        }

        public void PlayLevelUp()
        {
            if (levelUpClip != null)
                PlayAudio(levelUpClip);
            StartCoroutine(CelebrationHaptics());
        }

        public void PlayAchievementUnlocked()
        {
            if (achievementClip != null)
                PlayAudio(achievementClip);
            SendHaptic(XRNode.RightHand, 0.6f, 0.5f);
            SendHaptic(XRNode.LeftHand,  0.6f, 0.5f);
        }

        public void PlayError()
        {
            if (errorClip != null)
                PlayAudio(errorClip);
            SendHaptic(XRNode.RightHand, 0.4f, 0.15f);
        }

        #endregion

        // ────────────────────────────────────────────────────
        #region Haptic Helpers
        // ────────────────────────────────────────────────────

        /// <summary>Send a haptic impulse to one controller via XR API.</summary>
        private static void SendHaptic(XRNode node, float amplitude, float duration)
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(node);
            if (device.TryGetHapticCapabilities(out HapticCapabilities caps) && caps.supportsImpulse)
                device.SendHapticImpulse(0, amplitude, duration);
        }

        private IEnumerator CelebrationHaptics()
        {
            for (int i = 0; i < 3; i++)
            {
                SendHaptic(XRNode.RightHand, hapticAmplitude, 0.1f);
                SendHaptic(XRNode.LeftHand,  hapticAmplitude, 0.1f);
                yield return new WaitForSeconds(0.2f);
            }
        }

        #endregion

        // ────────────────────────────────────────────────────
        #region Audio Helper
        // ────────────────────────────────────────────────────

        private void PlayAudio(AudioClip clip)
        {
            if (audioSource == null)
            {
                Debug.LogWarning("[FeedbackSystem] AudioSource not assigned!");
                return;
            }
            if (clip == null)
            {
                Debug.LogWarning("[FeedbackSystem] AudioClip is null!");
                return;
            }
            audioSource.PlayOneShot(clip);
        }

        #endregion
    }
}
