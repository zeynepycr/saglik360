// ============================================================
//  Sağlık360 – HUDController.cs  |  UI
// ============================================================
//  Controls the in-VR HUD (world-space Canvas attached to rig).
//  Updates every frame with:
//    • Current joint angle & target
//    • Accuracy percentage bar
//    • Rep / set counter
//    • Points earned this session
//    • Rest countdown timer
//    • Achievement toast notifications
//
//  Relies on Unity's TextMeshPro and UI Image components.
//  Canvas should be set to World Space and parented to [XR Camera Offset].
// ============================================================

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Saglik360;
using Saglik360.Core;
using Saglik360.Movement;

namespace Saglik360.UI
{
    public class HUDController : MonoBehaviour
    {
        // ── Panel references ─────────────────────────────────
        [Header("Exercise Info")]
        [SerializeField] private TextMeshProUGUI exerciseNameText;
        [SerializeField] private TextMeshProUGUI repCounterText;       // "Rep 3 / 10"
        [SerializeField] private TextMeshProUGUI setCounterText;       // "Set 1 / 3"
        [SerializeField] private TextMeshProUGUI angleText;            // "72° / 180°"
        [SerializeField] private Slider          accuracySlider;
        [SerializeField] private TextMeshProUGUI accuracyLabel;        // "Mükemmel!"

        [Header("Points & Level")]
        [SerializeField] private TextMeshProUGUI pointsText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Slider          levelProgressSlider;

        [Header("Rest Timer")]
        [SerializeField] private GameObject      restPanel;
        [SerializeField] private TextMeshProUGUI restTimerText;
        [SerializeField] private TextMeshProUGUI restMessageText;

        [Header("Countdown")]
        [SerializeField] private GameObject      countdownPanel;
        [SerializeField] private TextMeshProUGUI countdownText;

        [Header("Achievement Toast")]
        [SerializeField] private GameObject      achievementToast;
        [SerializeField] private TextMeshProUGUI achievementTitle;
        [SerializeField] private TextMeshProUGUI achievementDesc;
        [SerializeField] private float           toastDuration = 4f;

        [Header("Feedback Colors")]
        [SerializeField] private Color colorCorrect  = Color.green;
        [SerializeField] private Color colorWarning  = new Color(1f, 0.6f, 0f);  // orange
        [SerializeField] private Color colorError    = Color.red;

        // ── References resolved at runtime ───────────────────
        private Core.ExerciseManager  _exerciseMgr;
        private Movement.MovementTracker _tracker;
        private GamificationSystem     _gamification;

        // ────────────────────────────────────────────────────
        #region Unity Lifecycle
        // ────────────────────────────────────────────────────

        private void Start()
        {
            _exerciseMgr  = FindObjectOfType<Core.ExerciseManager>();
            _tracker      = FindObjectOfType<Movement.MovementTracker>();
            _gamification = GamificationSystem.Instance;

            AutoBindUI();

            // Subscribe to events
            if (_exerciseMgr != null)
            {
                _exerciseMgr.OnRepCompleted     += HandleRepCompleted;
                _exerciseMgr.OnSetCompleted     += HandleSetCompleted;
            }

            if (_gamification != null)
            {
                _gamification.OnPointsEarned          += HandlePointsEarned;
                _gamification.OnLevelUp               += HandleLevelUp;
                _gamification.OnAchievementUnlocked   += HandleAchievement;
            }

            GameManager.OnStateChanged += HandleStateChanged;

            // Start hidden
            HideAll();
        }

        private void Update()
        {
            // Ekrana hata logu yansıtmak için:
            if (angleText != null)
            {
                if (_tracker == null) { angleText.text = "HATA: Tracker Yok"; return; }
                if (_exerciseMgr == null) { angleText.text = "HATA: ExMgr Yok"; return; }
                
                // Rep içinde değilsek bile anlık açıyı görelim (Debug için)
                if (_exerciseMgr.CurrentRepState != Core.RepState.InRep)
                {
                    angleText.text = $"Bekleniyor ({_exerciseMgr.CurrentRepState})\nAçı: {_tracker.CurrentAngleDeg:F0}°";
                    return;
                }

                // ── Live angle ───────────────────────────────────
                float target = _exerciseMgr.CurrentExercise.TargetAngleDeg;
                angleText.text  = $"{_tracker.CurrentAngleDeg:F0}°  /  {target:F0}°";
                angleText.color = _tracker.IsWithinTolerance ? colorCorrect
                    : _tracker.CurrentDeviationDeg < 15f      ? colorWarning
                    :                                            colorError;
            }
            else
            {
                // Eğer angleText null ise ve bir şekilde AutoBindUI çalışmamışsa, her frame denesin
                AutoBindUI();
                return;
            }

            // ── Accuracy slider ──────────────────────────────
            if (accuracySlider != null)
                accuracySlider.value = _tracker.AccuracyPercent / 100f;

            if (accuracyLabel != null)
                accuracyLabel.text = Movement.ROMCalculator.AccuracyLabel(_tracker.AccuracyPercent);

            // ── Rest panel ───────────────────────────────────
            bool inRest = _exerciseMgr.CurrentRepState == Core.RepState.RestBetweenReps
                       || _exerciseMgr.CurrentRepState == Core.RepState.RestBetweenSets;
            if (restPanel != null)
            {
                restPanel.SetActive(inRest);
                if (inRest && restTimerText != null)
                    restTimerText.text = $"{_exerciseMgr.RestTimeRemaining:F0}";
            }
        }

        private void OnDestroy()
        {
            if (_exerciseMgr != null)
            {
                _exerciseMgr.OnRepCompleted -= HandleRepCompleted;
                _exerciseMgr.OnSetCompleted -= HandleSetCompleted;
            }
            if (_gamification != null)
            {
                _gamification.OnPointsEarned        -= HandlePointsEarned;
                _gamification.OnLevelUp             -= HandleLevelUp;
                _gamification.OnAchievementUnlocked -= HandleAchievement;
            }
            GameManager.OnStateChanged -= HandleStateChanged;
        }

        #endregion

        // ────────────────────────────────────────────────────
        #region Event Handlers
        // ────────────────────────────────────────────────────

        private void HandleStateChanged(AppState state)
        {
            bool inExercise = state == AppState.ExerciseActive || state == AppState.ExercisePaused;
            
            // Eğer HUDController'ı Canvas'a veya Kamera'ya eklerseniz, gameObject.SetActive tüm sistemi kapatıyordu! 
            // Bu yüzden gameObject'i komple kapatmak yerine, sadece yazıları güncelleyip güncellememe kararı vereceğiz.
            // (UIManager zaten doğru paneli açıp kapatıyor).

            if (state == AppState.ExerciseActive && _exerciseMgr != null && _exerciseMgr.CurrentExercise != null)
            {
                if (exerciseNameText != null)
                    exerciseNameText.text = _exerciseMgr.CurrentExercise.ExerciseName;
            }
        }

        private void HandleRepCompleted(int currentRep, int targetReps)
        {
            if (repCounterText != null)
                repCounterText.text = $"Tekrar  {currentRep} / {targetReps}";
        }

        private void HandleSetCompleted(int currentSet, int targetSets)
        {
            if (setCounterText != null)
                setCounterText.text = $"Set  {currentSet} / {targetSets}";
        }

        private void HandlePointsEarned(int amount)
        {
            if (pointsText != null && _gamification != null)
                pointsText.text = $"⭐ {_gamification.SessionPoints}";

            if (levelProgressSlider != null && _gamification != null)
                levelProgressSlider.value = _gamification.LevelProgress;
        }

        private void HandleLevelUp(int newLevel)
        {
            if (levelText != null)
                levelText.text = $"Seviye {newLevel}";
            // A brief particle burst could be triggered here via FeedbackSystem
        }

        private void HandleAchievement(Achievement achievement)
        {
            StartCoroutine(ShowAchievementToast(achievement));
        }

        #endregion

        // ────────────────────────────────────────────────────
        #region Countdown (called by FeedbackSystem)
        // ────────────────────────────────────────────────────

        public void ShowCountdown(int number)
        {
            if (countdownPanel == null) return;
            StopCoroutine(nameof(HideCountdownAfterDelay));
            countdownPanel.SetActive(true);
            if (countdownText != null)
                countdownText.text = number > 0 ? number.ToString() : "Başla!";
            StartCoroutine(HideCountdownAfterDelay(1.1f));
        }

        private IEnumerator HideCountdownAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            countdownPanel?.SetActive(false);
        }

        #endregion

        // ────────────────────────────────────────────────────
        #region Achievement Toast
        // ────────────────────────────────────────────────────

        private IEnumerator ShowAchievementToast(Achievement achievement)
        {
            if (achievementToast == null) yield break;

            if (achievementTitle != null) achievementTitle.text = achievement.Name;
            if (achievementDesc  != null) achievementDesc.text  = achievement.Description;

            achievementToast.SetActive(true);
            yield return new WaitForSeconds(toastDuration);
            achievementToast.SetActive(false);
        }

        #endregion

        // ────────────────────────────────────────────────────
        #region Helpers
        // ────────────────────────────────────────────────────

        private void HideAll()
        {
            restPanel?.SetActive(false);
            countdownPanel?.SetActive(false);
            achievementToast?.SetActive(false);
        }

        private void AutoBindUI()
        {
            if (angleText != null) return; // Zaten atanmış

            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            var texts = canvas.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in texts)
            {
                switch (t.name)
                {
                    case "ExerciseNameText": exerciseNameText = t; break;
                    case "RepCounterText":   repCounterText = t; break;
                    case "SetCounterText":   setCounterText = t; break;
                    case "AngleText":        angleText = t; break;
                    case "AccuracyLabel":    accuracyLabel = t; break;
                    case "PointsText":       pointsText = t; break;
                    case "RestTimerText":    restTimerText = t; break;
                    case "RestMessage":      restMessageText = t; break;
                    case "CountdownText":    countdownText = t; break;
                }
            }

            var sliders = canvas.GetComponentsInChildren<Slider>(true);
            foreach (var s in sliders)
            {
                if (s.name == "AccuracySlider") accuracySlider = s;
            }

            var transforms = canvas.GetComponentsInChildren<Transform>(true);
            foreach (var tr in transforms)
            {
                if (tr.name == "RestPanel") restPanel = tr.gameObject;
                else if (tr.name == "CountdownPanel") countdownPanel = tr.gameObject;
            }

            Debug.Log("[HUDController] UI referansları otomatik bağlandı.");
        }

        #endregion
    }
}
