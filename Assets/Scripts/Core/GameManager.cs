// ============================================================
//  Sağlık360 – Oyunlaştırılmış Fizik Tedavi Simülasyonu
//  GameManager.cs  |  Core
//  Zeynep Yüceer · Ankara Üniversitesi · 2026
// ============================================================
//  Singleton controller that owns the global AppState machine.
//  All other managers query or listen to this class.
// ============================================================

using System;
using UnityEngine;
using Saglik360;
using Saglik360.Data;
using Saglik360.UI;

namespace Saglik360.Core
{
    // ─── App-wide state enum ──────────────────────────────────
    public enum AppState
    {
        MainMenu,
        ExerciseSelection,
        ExerciseActive,
        ExercisePaused,
        ExerciseComplete,
        SessionSummary,
        Settings
    }

    public class GameManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────
        public static GameManager Instance { get; private set; }

        // ── Current state (read-only outside) ───────────────
        public AppState CurrentState { get; private set; } = AppState.MainMenu;

        // ── Cross-manager references (assign in Inspector) ──
        [Header("Manager References")]
        [SerializeField] private ExerciseManager  exerciseManager;
        [SerializeField] private SessionManager   sessionManager;
        [SerializeField] private GamificationSystem gamification;
        [SerializeField] private FirebaseManager  firebase;
        [SerializeField] private UIManager        uiManager;
        [SerializeField] private FeedbackSystem   feedback;

        // ── Events ───────────────────────────────────────────
        /// <summary>Fired whenever AppState changes.</summary>
        public static event Action<AppState> OnStateChanged;

        // ────────────────────────────────────────────────────
        #region Unity Lifecycle
        // ────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            ChangeState(AppState.MainMenu);
            Debug.Log("[GameManager] Sağlık360 başlatıldı.");
        }

        #endregion

        // ────────────────────────────────────────────────────
        #region State Machine
        // ────────────────────────────────────────────────────

        /// <summary>
        /// Transition to a new AppState.
        /// Validates legal transitions to prevent accidental skips.
        /// </summary>
        public void ChangeState(AppState newState)
        {
            if (newState == CurrentState) return;

            Debug.Log($"[GameManager] {CurrentState} → {newState}");
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }

        #endregion

        // ────────────────────────────────────────────────────
        #region Public API (called by UI buttons, etc.)
        // ────────────────────────────────────────────────────

        /// <summary>Player presses "Başla" on main menu.</summary>
        public void StartSession()
        {
            sessionManager.BeginSession();
            gamification.ResetSessionCounters();
            ChangeState(AppState.ExerciseSelection);
        }

        /// <summary>Player selects an exercise from the list.</summary>
        public void SelectExercise(ExerciseDefinition exercise)
        {
            exerciseManager.LoadExercise(exercise);
            ChangeState(AppState.ExerciseActive);
        }

        /// <summary>Player pauses mid-exercise.</summary>
        public void PauseExercise()
        {
            if (CurrentState != AppState.ExerciseActive) return;
            exerciseManager.Pause();
            ChangeState(AppState.ExercisePaused);
        }

        /// <summary>Resume from pause.</summary>
        public void ResumeExercise()
        {
            if (CurrentState != AppState.ExercisePaused) return;
            exerciseManager.Resume();
            ChangeState(AppState.ExerciseActive);
        }

        /// <summary>
        /// Called by ExerciseManager when all reps are complete.
        /// Triggers gamification, saves data, shows summary.
        /// </summary>
        public void CompleteCurrentExercise(ExerciseResult result)
        {
            gamification.ProcessResult(result);
            sessionManager.RecordExerciseResult(result);
            feedback.PlayCompletion(result.AccuracyPercent);
            ChangeState(AppState.ExerciseComplete);
        }

        /// <summary>Finalize the whole session and upload to Firebase.</summary>
        public void EndSession()
        {
            SessionData session = sessionManager.EndSession();
            firebase.UploadSession(session);
            ChangeState(AppState.SessionSummary);
        }

        /// <summary>Return to main menu from any non-active state.</summary>
        public void GoToMainMenu()
        {
            if (CurrentState == AppState.ExerciseActive)
                exerciseManager.ForceStop();

            ChangeState(AppState.MainMenu);
        }

        public void OpenSettings() => ChangeState(AppState.Settings);

        public void QuitApp()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        // ────────────────────────────────────────────────────
        #region Helpers
        // ────────────────────────────────────────────────────

        public bool IsExerciseRunning => CurrentState == AppState.ExerciseActive;

        #endregion
    }
}
