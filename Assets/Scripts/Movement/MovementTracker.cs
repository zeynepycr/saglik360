// ============================================================
//  Sağlık360 – MovementTracker.cs  |  Movement
// ============================================================
//  Core 6DoF tracking engine.
//  Runs every frame while an exercise is active:
//    1. Reads Quest 2 controller position + rotation
//    2. Computes joint angle via ROMCalculator
//    3. Compares angle to therapeutic target
//    4. Fires events consumed by FeedbackSystem & HUDController
//    5. Logs MovementSample every N frames for Firebase upload
//
//  Attach to: [XR Rig] or a dedicated TrackerManager object.
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Saglik360.Data;

namespace Saglik360.Movement
{
    // ── Per-frame movement record ────────────────────────────
    [Serializable]
    public class MovementSample
    {
        public float   Timestamp;
        public float   JointAngleDeg;
        public float   TargetAngleDeg;
        public float   DeviationDeg;
        public bool    WithinTolerance;
        public Vector3 ControllerPosition;
    }

    public class MovementTracker : MonoBehaviour
    {
        // ── Inspector config ─────────────────────────────────
        [Header("Quest 2 Controller Transforms")]
        [Tooltip("Attach the XR Right Hand Controller transform here.")]
        [SerializeField] private Transform rightControllerTransform;
        [Tooltip("Attach the XR Left Hand Controller transform here.")]
        [SerializeField] private Transform leftControllerTransform;
        [Tooltip("Shoulder anchor (used as rotation origin for arm angle calc).")]
        [SerializeField] private Transform shoulderAnchor;

        [Header("Logging")]
        [Tooltip("Log a sample every N frames (72 FPS device → 6 = ~12 samples/sec).")]
        [SerializeField] [Range(1, 20)] private int sampleEveryNFrames = 6;

        // ── Runtime state ────────────────────────────────────
        private ExerciseDefinition _currentExercise;        private bool               _isTracking;
        private int                _frameCounter;
        private List<MovementSample> _samples = new();

        // Current computed values (polled by HUDController)
        public float  CurrentAngleDeg       { get; private set; }
        public float  CurrentDeviationDeg   { get; private set; }
        public bool   IsWithinTolerance     { get; private set; }
        public float  AccuracyPercent       { get; private set; }   // running avg

        // Accumulated accuracy bookkeeping
        private float _accuracySum;
        private int   _accuracySampleCount;

        // ── Events ───────────────────────────────────────────
        /// <summary>Fired every frame tracking is active.</summary>
        public event Action<MovementSample> OnSampleRecorded;

        /// <summary>Fired when patient enters/exits therapeutic window.</summary>
        public event Action<bool> OnToleranceChanged;

        // ────────────────────────────────────────────────────
        #region Public API
        // ────────────────────────────────────────────────────

        /// <summary>Start tracking for the specified exercise.</summary>
        public void StartTracking(ExerciseDefinition exercise)        {
            _currentExercise     = exercise;
            _isTracking          = true;
            _frameCounter        = 0;
            _samples             = new List<MovementSample>();
            _accuracySum         = 0f;
            _accuracySampleCount = 0;
            IsWithinTolerance    = false;

            Debug.Log($"[MovementTracker] İzleme başladı: {exercise.ExerciseName}");
        }

        /// <summary>Stop tracking and return the recorded sample list.</summary>
        public List<MovementSample> StopTracking()
        {
            _isTracking = false;
            Debug.Log($"[MovementTracker] İzleme durdu. Toplam örnek: {_samples.Count}");
            return _samples;
        }

        /// <summary>Returns a snapshot of collected samples (for result building).</summary>
        public List<MovementSample> GetSamples() => new List<MovementSample>(_samples);

        #endregion

        // ────────────────────────────────────────────────────
        #region Unity Lifecycle
        // ────────────────────────────────────────────────────

        private void Update()
        {
            if (!_isTracking || _currentExercise == null) return;

            // ── 1. Compute joint angle ────────────────────────
            Vector3 controllerPos = GetActiveControllerPosition();
            
            if (shoulderAnchor == null)
            {
                Debug.LogError("[MovementTracker] ShoulderAnchor is NOT assigned! Angle cannot be calculated.");
                return;
            }

            Transform cam = Camera.main != null ? Camera.main.transform : null;

            CurrentAngleDeg = ROMCalculator.ComputeShoulderFlexionAngle(
                shoulderAnchor.position, controllerPos, cam);

            // ── 2. Compare to target ──────────────────────────
            CurrentDeviationDeg = Mathf.Abs(CurrentAngleDeg - _currentExercise.TargetAngleDeg);
            bool wasWithin      = IsWithinTolerance;
            IsWithinTolerance   = CurrentDeviationDeg <= _currentExercise.ToleranceDeg;

            // Fire tolerance-change event
            if (IsWithinTolerance != wasWithin)
                OnToleranceChanged?.Invoke(IsWithinTolerance);

            // ── 3. Running accuracy ───────────────────────────
            float frameAccuracy = Mathf.Clamp01(
                1f - (CurrentDeviationDeg / _currentExercise.TotalROMDeg)) * 100f;
            _accuracySum         += frameAccuracy;
            _accuracySampleCount++;
            AccuracyPercent       = _accuracySum / _accuracySampleCount;

            // ── 4. Log sample every N frames ──────────────────
            _frameCounter++;
            if (_frameCounter >= sampleEveryNFrames)
            {
                _frameCounter = 0;
                var sample = new MovementSample
                {
                    Timestamp         = Time.time,
                    JointAngleDeg     = CurrentAngleDeg,
                    TargetAngleDeg    = _currentExercise.TargetAngleDeg,
                    DeviationDeg      = CurrentDeviationDeg,
                    WithinTolerance   = IsWithinTolerance,
                    ControllerPosition = controllerPos
                };
                _samples.Add(sample);
                OnSampleRecorded?.Invoke(sample);
            }
        }

        #endregion

        // ────────────────────────────────────────────────────
        #region Private Helpers
        // ────────────────────────────────────────────────────

        /// <summary>
        /// Returns the position of whichever controller is relevant.
        /// For shoulder exercises, the right controller is default.
        /// Extend this to support bilateral exercises.
        /// </summary>
        private Vector3 GetActiveControllerPosition()
        {
            // If XR transforms are assigned use them directly
            if (rightControllerTransform != null)
                return rightControllerTransform.position;

            // Fallback: query XR InputDevice API
            InputDevice rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (rightDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos))
                return pos;

            Debug.LogWarning("[MovementTracker] Sağ kontrolör pozisyonu alınamadı.");
            return Vector3.zero;
        }

        #endregion
    }
}
