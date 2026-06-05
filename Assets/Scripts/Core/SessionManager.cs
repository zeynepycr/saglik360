// ============================================================
//  Sağlık360 – SessionManager.cs  |  Core
// ============================================================
//  Tracks the lifecycle of a single therapy session:
//  start time, exercise results, total duration, etc.
//  Produces a SessionData object that FirebaseManager uploads.
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using Saglik360.Movement;

namespace Saglik360.Core
{
    public class SessionManager : MonoBehaviour
    {
        // ── Current session ──────────────────────────────────
        public SessionData CurrentSession { get; private set; }
        public bool IsSessionActive       { get; private set; }

        // ── Events ───────────────────────────────────────────
        public event Action<SessionData> OnSessionEnded;

        // ────────────────────────────────────────────────────
        #region Session Lifecycle
        // ────────────────────────────────────────────────────

        /// <summary>Initialize a fresh SessionData record.</summary>
        public void BeginSession()
        {
            if (IsSessionActive)
            {
                Debug.LogWarning("[SessionManager] BeginSession called while session already active.");
                return;
            }

            CurrentSession = new SessionData
            {
                SessionId     = Guid.NewGuid().ToString(),
                PatientId     = PlayerPrefs.GetString("PatientId", "unknown"),
                StartTime     = DateTime.UtcNow,
                ExerciseResults = new List<ExerciseResult>()
            };

            IsSessionActive = true;
            Debug.Log($"[SessionManager] Oturum başladı: {CurrentSession.SessionId}");
        }

        /// <summary>Record the result of one completed exercise.</summary>
        public void RecordExerciseResult(ExerciseResult result)
        {
            if (!IsSessionActive)
            {
                Debug.LogError("[SessionManager] RecordExerciseResult called outside of an active session.");
                return;
            }

            result.Timestamp = DateTime.UtcNow;
            CurrentSession.ExerciseResults.Add(result);
            Debug.Log($"[SessionManager] Egzersiz kaydedildi: {result.ExerciseName} | Doğruluk: {result.AccuracyPercent:F1}%");
        }

        /// <summary>Close the session; returns finalized SessionData.</summary>
        public SessionData EndSession()
        {
            if (!IsSessionActive)
            {
                Debug.LogWarning("[SessionManager] EndSession called with no active session.");
                return null;
            }

            CurrentSession.EndTime = DateTime.UtcNow;
            CurrentSession.TotalDurationSeconds = (float)(CurrentSession.EndTime - CurrentSession.StartTime).TotalSeconds;
            CurrentSession.OverallAccuracy      = CalculateOverallAccuracy();

            IsSessionActive = false;
            OnSessionEnded?.Invoke(CurrentSession);
            Debug.Log($"[SessionManager] Oturum tamamlandı. Süre: {CurrentSession.TotalDurationSeconds:F0}s | Ortalama doğruluk: {CurrentSession.OverallAccuracy:F1}%");

            return CurrentSession;
        }

        #endregion

        // ────────────────────────────────────────────────────
        #region Private Helpers
        // ────────────────────────────────────────────────────

        private float CalculateOverallAccuracy()
        {
            if (CurrentSession.ExerciseResults == null || CurrentSession.ExerciseResults.Count == 0)
                return 0f;

            float total = 0f;
            foreach (ExerciseResult r in CurrentSession.ExerciseResults)
                total += r.AccuracyPercent;

            return total / CurrentSession.ExerciseResults.Count;
        }

        #endregion
    }

    // ────────────────────────────────────────────────────────
    // Data Models (nested here for simplicity; move to
    // Data/SessionData.cs in a larger project)
    // ────────────────────────────────────────────────────────

    [Serializable]
    public class SessionData
    {
        public string          SessionId;
        public string          PatientId;
        public DateTime        StartTime;
        public DateTime        EndTime;
        public float           TotalDurationSeconds;
        public float           OverallAccuracy;           // 0-100
        public List<ExerciseResult> ExerciseResults;
    }

    [Serializable]
    public class ExerciseResult
    {
        public string   ExerciseName;
        public int      CompletedReps;
        public int      TargetReps;
        public float    AccuracyPercent;                  // 0-100
        public float    DurationSeconds;
        public int      PointsEarned;
        public DateTime Timestamp;
        public List<MovementSample> Movements;            // raw movement log
    }
}
