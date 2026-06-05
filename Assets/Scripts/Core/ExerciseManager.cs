// ============================================================
//  Sağlık360 – ExerciseManager.cs  |  Core
// ============================================================
//  Drives the rep/set loop for one loaded ExerciseDefinition.
//  State machine:  Idle → WaitingForStart → InRep → RestBetweenReps
//                         → RestBetweenSets → Complete
//
//  Communicates with:
//    • MovementTracker  – reads real-time angle / accuracy
//    • FeedbackSystem   – triggers visual/haptic/audio cues
//    • GameManager      – notifies on completion
// ============================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Saglik360.Data;
using Saglik360.Movement;

namespace Saglik360.Core
{
    public enum RepState
    {
        Idle,
        WaitingForStart,
        InRep,
        RestBetweenReps,
        RestBetweenSets,
        Complete
    }

    public class ExerciseManager : MonoBehaviour
    {
        // ── References ───────────────────────────────────────
        [Header("References")]
        [SerializeField] private Movement.MovementTracker movementTracker;
        [SerializeField] private FeedbackSystem           feedback;

        // ── Runtime state ────────────────────────────────────
        public ExerciseDefinition CurrentExercise { get; private set; }        public RepState            CurrentRepState { get; private set; } = RepState.Idle;
        public int                 CurrentSet      { get; private set; }
        public int                 CurrentRep      { get; private set; }
        public float               RestTimeRemaining { get; private set; }

        // Accumulated accuracy for current exercise
        private float _repAccuracySum;
        private int   _repAccuracyCount;

        // Per-rep tracking
        private float               _repStartTime;
        private bool                _isPaused;
        private Coroutine           _activeCoroutine;

        // ── Events (HUDController listens) ───────────────────
        public event Action<int, int>       OnRepCompleted;   // (currentRep, targetReps)
        public event Action<int, int>       OnSetCompleted;   // (currentSet, targetSets)
        public event Action<ExerciseResult> OnExerciseDone;

        // ────────────────────────────────────────────────────
        #region Public API
        // ────────────────────────────────────────────────────

        public void LoadExercise(ExerciseDefinition exercise)        {
            if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);

            CurrentExercise    = exercise;
            CurrentSet         = 1;
            CurrentRep         = 0;
            _repAccuracySum    = 0f;
            _repAccuracyCount  = 0;

            movementTracker.StartTracking(exercise);
            SetRepState(RepState.WaitingForStart);

            _activeCoroutine = StartCoroutine(ExerciseLoop());
            Debug.Log($"[ExerciseManager] Egzersiz yüklendi: {exercise.ExerciseName}");
        }

        public void Pause()
        {
            _isPaused = true;
            movementTracker.StopTracking();
            Debug.Log("[ExerciseManager] Duraklatıldı.");
        }

        public void Resume()
        {
            _isPaused = false;
            movementTracker.StartTracking(CurrentExercise);
            Debug.Log("[ExerciseManager] Devam edildi.");
        }

        public void ForceStop()
        {
            if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);
            movementTracker.StopTracking();
            SetRepState(RepState.Idle);
        }

        #endregion

        // ────────────────────────────────────────────────────
        #region Exercise Coroutine Loop
        // ────────────────────────────────────────────────────

        private IEnumerator ExerciseLoop()
        {
            // Brief countdown before starting
            yield return StartCoroutine(Countdown(3));

            for (CurrentSet = 1; CurrentSet <= CurrentExercise.TargetSets; CurrentSet++)
            {
                for (CurrentRep = 1; CurrentRep <= CurrentExercise.TargetReps; CurrentRep++)
                {
                    // ── Perform one rep ──────────────────────
                    yield return StartCoroutine(PerformRep());

                    // ── Record rep accuracy ──────────────────
                    float repAcc = movementTracker.AccuracyPercent;
                    _repAccuracySum   += repAcc;
                    _repAccuracyCount++;

                    feedback.PlayRepComplete(repAcc);
                    OnRepCompleted?.Invoke(CurrentRep, CurrentExercise.TargetReps);

                    // ── Rest between reps (except last rep of set) ─
                    if (CurrentRep < CurrentExercise.TargetReps)
                        yield return StartCoroutine(Rest(CurrentExercise.RestBetweenRepsSec, RepState.RestBetweenReps));
                }

                OnSetCompleted?.Invoke(CurrentSet, CurrentExercise.TargetSets);

                // ── Rest between sets ────────────────────────
                if (CurrentSet < CurrentExercise.TargetSets)
                    yield return StartCoroutine(Rest(CurrentExercise.RestBetweenSetsSec, RepState.RestBetweenSets));
            }

            // ── Exercise complete ────────────────────────────
            SetRepState(RepState.Complete);
            ExerciseResult result = BuildResult();
            OnExerciseDone?.Invoke(result);
            GameManager.Instance.CompleteCurrentExercise(result);
        }

        private IEnumerator PerformRep()
        {
            SetRepState(RepState.InRep);
            _repStartTime = Time.time;

            // Wait until patient reaches target angle within tolerance
            yield return new WaitUntil(() =>
            {
                if (_isPaused) return false;
                return movementTracker.IsWithinTolerance;
            });

            // Hold in position briefly (confirms intentional reach)
            float holdStart = Time.time;
            while (Time.time - holdStart < 0.5f)
            {
                if (!movementTracker.IsWithinTolerance)
                    holdStart = Time.time;   // reset if patient moves away
                yield return null;
            }
        }

        private IEnumerator Countdown(int seconds)
        {
            for (int i = seconds; i > 0; i--)
            {
                feedback.ShowCountdown(i);
                yield return new WaitForSeconds(1f);
            }
        }

        private IEnumerator Rest(float duration, RepState restState)
        {
            SetRepState(restState);
            RestTimeRemaining = duration;

            while (RestTimeRemaining > 0f)
            {
                if (!_isPaused)
                    RestTimeRemaining -= Time.deltaTime;
                yield return null;
            }
        }

        #endregion

        // ────────────────────────────────────────────────────
        #region Helpers
        // ────────────────────────────────────────────────────

        private void SetRepState(RepState state)
        {
            CurrentRepState = state;
            Debug.Log($"[ExerciseManager] RepState → {state}  (Set {CurrentSet}/{CurrentExercise?.TargetSets}, Rep {CurrentRep}/{CurrentExercise?.TargetReps})");
        }

        private ExerciseResult BuildResult()
        {
            var samples = movementTracker.GetSamples();
            float overallAccuracy = _repAccuracyCount > 0
                ? _repAccuracySum / _repAccuracyCount
                : 0f;

            int points = Mathf.RoundToInt(
                CurrentExercise.BasePointsPerRep * CurrentExercise.TotalReps * (overallAccuracy / 100f)
                + CurrentExercise.CompletionBonus);

            return new ExerciseResult
            {
                ExerciseName    = CurrentExercise.ExerciseName,
                CompletedReps   = CurrentExercise.TotalReps,
                TargetReps      = CurrentExercise.TotalReps,
                AccuracyPercent = overallAccuracy,
                DurationSeconds = Time.time - _repStartTime,
                PointsEarned    = points,
                Movements       = samples
            };
        }

        #endregion
    }
}
