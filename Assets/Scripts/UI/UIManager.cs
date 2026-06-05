// ============================================================
//  Sağlık360 – UIManager.cs  |  UI  (REWRITTEN v2)
// ============================================================
//  Self-wiring version — finds all panels by name at Start().
//  No Inspector references needed. Works even when [Managers]
//  moves to DontDestroyOnLoad.
// ============================================================

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Saglik360.Core;

namespace Saglik360.UI
{
    public class UIManager : MonoBehaviour
    {
        // ── Found at runtime ─────────────────────────────────
        private GameObject mainMenuPanel;
        private GameObject exerciseSelectionPanel;
        private GameObject exerciseActivePanel;    
        private GameObject exerciseCompletePanel;
        private GameObject sessionSummaryPanel;
        private GameObject pausePanel;

        // Complete panel fields
        private TextMeshProUGUI completeExerciseName;
        private TextMeshProUGUI completeAccuracyText;
        private TextMeshProUGUI completePointsText;
        private Slider          completeAccuracyBar;

        // Summary panel fields
        private TextMeshProUGUI summaryDurationText;
        private TextMeshProUGUI summaryOverallAccuracyText;
        private TextMeshProUGUI summaryTotalPointsText;
        private TextMeshProUGUI summaryStreakText;
        private TextMeshProUGUI summaryLevelText;

        private Core.ExerciseManager _exerciseMgr;
        private bool _panelsFound = false;

        // ────────────────────────────────────────────────────
        #region Unity Lifecycle
        // ────────────────────────────────────────────────────

        private void Start()
        {
            FindAllPanels();

            _exerciseMgr = FindObjectOfType<Core.ExerciseManager>();
            if (_exerciseMgr != null)
                _exerciseMgr.OnExerciseDone += PopulateExerciseCompletePanel;

            GameManager.OnStateChanged += HandleStateChange;
            HandleStateChange(AppState.MainMenu);
        }

        private void OnDestroy()
        {
            if (_exerciseMgr != null)
                _exerciseMgr.OnExerciseDone -= PopulateExerciseCompletePanel;
            GameManager.OnStateChanged -= HandleStateChange;
        }

        #endregion

        // ────────────────────────────────────────────────────
        #region Panel Discovery
        // ────────────────────────────────────────────────────

        private void FindAllPanels()
        {
            // Find the canvas anywhere in the scene
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[UIManager] Sahnede Canvas bulunamadı!");
                return;
            }

            GameObject root = canvas.gameObject;

            mainMenuPanel          = FindChildByName(root, "MainMenuPanel");
            exerciseSelectionPanel = FindChildByName(root, "ExerciseSelectPanel");
            exerciseActivePanel    = FindChildByName(root, "ExerciseActivePanel");
            exerciseCompletePanel  = FindChildByName(root, "ExerciseCompletePanel");
            sessionSummaryPanel    = FindChildByName(root, "SessionSummaryPanel");
            pausePanel             = FindChildByName(root, "PausePanel");

            // Complete panel TMP fields
            completeExerciseName  = FindTMP(root, "CompleteExerciseName");
            completeAccuracyText  = FindTMP(root, "CompleteAccuracyText");
            completePointsText    = FindTMP(root, "CompletePointsText");
            completeAccuracyBar   = FindSlider(root, "CompleteAccuracyBar");

            // Summary panel TMP fields
            summaryDurationText        = FindTMP(root, "SummaryDurationText");
            summaryOverallAccuracyText = FindTMP(root, "SummaryAccuracyText");
            summaryTotalPointsText     = FindTMP(root, "SummaryPointsText");
            summaryStreakText          = FindTMP(root, "SummaryStreakText");
            summaryLevelText           = FindTMP(root, "SummaryLevelText");

            _panelsFound = mainMenuPanel != null;

            if (_panelsFound)
                Debug.Log("[UIManager] Tüm paneller başarıyla bulundu.");
            else
                Debug.LogError("[UIManager] MainMenuPanel bulunamadı — canvas adlarını kontrol edin.");
        }

        private static GameObject FindChildByName(GameObject root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.gameObject.name == name) return t.gameObject;
            Debug.LogWarning($"[UIManager] '{name}' bulunamadı.");
            return null;
        }

        private static TextMeshProUGUI FindTMP(GameObject root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (t.gameObject.name == name) return t;
            return null;
        }

        private static Slider FindSlider(GameObject root, string name)
        {
            foreach (var s in root.GetComponentsInChildren<Slider>(true))
                if (s.gameObject.name == name) return s;
            return null;
        }

        #endregion

        // ────────────────────────────────────────────────────
        #region State → Panel Mapping
        // ────────────────────────────────────────────────────

        private void HandleStateChange(AppState state)
        {
            if (!_panelsFound) FindAllPanels();

            SetAllPanelsActive(false);

            switch (state)
        {
            case AppState.MainMenu:
                mainMenuPanel?.SetActive(true);
                break;
            case AppState.ExerciseSelection:
                exerciseSelectionPanel?.SetActive(true);
                break;
            case AppState.ExerciseActive:
                exerciseActivePanel?.SetActive(true);
                break;
            case AppState.ExercisePaused:
                pausePanel?.SetActive(true);
                break;
            case AppState.ExerciseComplete:
                exerciseCompletePanel?.SetActive(true);
                break;
            case AppState.SessionSummary:
                sessionSummaryPanel?.SetActive(true);
                PopulateSessionSummary();
                break;
        }
        }

        private void SetAllPanelsActive(bool active)
        {
            mainMenuPanel?.SetActive(active);
            exerciseSelectionPanel?.SetActive(active);
            exerciseActivePanel?.SetActive(active);
            exerciseCompletePanel?.SetActive(active);
            sessionSummaryPanel?.SetActive(active);
            pausePanel?.SetActive(active);
        }

        #endregion

        // ────────────────────────────────────────────────────
        #region Panel Population
        // ────────────────────────────────────────────────────

        private void PopulateExerciseCompletePanel(Core.ExerciseResult result)
        {
            if (completeExerciseName != null)  completeExerciseName.text  = result.ExerciseName;
            if (completeAccuracyText != null)  completeAccuracyText.text  = $"%{result.AccuracyPercent:F0}";
            if (completePointsText   != null)  completePointsText.text    = $"+{result.PointsEarned}";
            if (completeAccuracyBar  != null)  completeAccuracyBar.value  = result.AccuracyPercent / 100f;
        }

        private void PopulateSessionSummary()
        {
            var session = FindObjectOfType<Core.SessionManager>()?.CurrentSession;
            var gamif   = GamificationSystem.Instance;

            if (session != null)
            {
                if (summaryDurationText        != null)
                    summaryDurationText.text        = FormatDuration(session.TotalDurationSeconds);
                if (summaryOverallAccuracyText  != null)
                    summaryOverallAccuracyText.text = $"%{session.OverallAccuracy:F0}";
            }

            if (gamif != null)
            {
                if (summaryTotalPointsText != null)
                    summaryTotalPointsText.text = $"{gamif.SessionPoints} puan";
                if (summaryStreakText      != null)
                    summaryStreakText.text      = $"🔥 {gamif.CurrentStreak} günlük seri";
                if (summaryLevelText       != null)
                    summaryLevelText.text       = $"Seviye {gamif.PlayerLevel}";
            }
        }

        #endregion

        // ────────────────────────────────────────────────────
        #region Button Handlers
        // ────────────────────────────────────────────────────

        public void OnStartSessionButton()
        {
            Debug.Log("[UIManager] Başla butonuna basıldı.");
            GameManager.Instance.StartSession();
        }

        public void OnMainMenuButton()        => GameManager.Instance.GoToMainMenu();
        public void OnPauseButton()           => GameManager.Instance.PauseExercise();
        public void OnResumeButton()          => GameManager.Instance.ResumeExercise();
        public void OnEndSessionButton()      => GameManager.Instance.EndSession();
        public void OnContinueButton()        => GameManager.Instance.ChangeState(AppState.ExerciseSelection);
        public void OnQuitButton()            => GameManager.Instance.QuitApp();

        public void OnExerciseSelected(Data.ExerciseDefinition exercise)
            => GameManager.Instance.SelectExercise(exercise);

        #endregion

        // ────────────────────────────────────────────────────
        #region Helpers
        // ────────────────────────────────────────────────────

        private static string FormatDuration(float seconds)
        {
            int m = Mathf.FloorToInt(seconds / 60f);
            int s = Mathf.FloorToInt(seconds % 60f);
            return $"{m:D2}:{s:D2}";
        }

        #endregion
    }
}
