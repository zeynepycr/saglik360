// ============================================================
//  Sağlık360 – GamificationSystem.cs  |  Gamification
// ============================================================
//  Central hub for all gamification mechanics:
//    • Point accumulation (session + lifetime)
//    • Level progression
//    • Achievement / badge unlocks
//    • Daily streak tracking
//    • Leaderboard data prep
//
//  Persists data via PlayerPrefs (lightweight local cache).
//  Full persistence is handled by FirebaseManager.
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Saglik360
{
    // ── Achievement definition ───────────────────────────────
    [Serializable]
    public class Achievement
    {
        public string Id;
        public string Name;
        public string Description;
        public string BadgeIcon;        // sprite name in Resources
        public bool   IsUnlocked;
        public DateTime UnlockedAt;
    }

    public class GamificationSystem : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────
        public static GamificationSystem Instance { get; private set; }

        // ── Points ───────────────────────────────────────────
        public int SessionPoints  { get; private set; }
        public int LifetimePoints { get; private set; }

        // ── Level ────────────────────────────────────────────
        public int   PlayerLevel        { get; private set; } = 1;
        public int   PointsToNextLevel  { get; private set; }
        public float LevelProgress      { get; private set; }   // 0-1

        // Levels: points required for each (index = level-1)
        private static readonly int[] LevelThresholds =
            { 0, 100, 250, 500, 900, 1400, 2100, 3000, 4200, 6000 };

        // ── Streak ───────────────────────────────────────────
        public int CurrentStreak { get; private set; }  // consecutive days

        // ── Achievements ─────────────────────────────────────
        private List<Achievement> _achievements = new();
        public IReadOnlyList<Achievement> Achievements => _achievements;

        // ── Events ───────────────────────────────────────────
        public event Action<int>         OnPointsEarned;
        public event Action<int>         OnLevelUp;
        public event Action<Achievement> OnAchievementUnlocked;

        // ────────────────────────────────────────────────────
        #region Unity Lifecycle
        // ────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadPersistentData();
            InitializeAchievements();
        }

        #endregion

        // ────────────────────────────────────────────────────
        #region Public API
        // ────────────────────────────────────────────────────

        public void ResetSessionCounters()
        {
            SessionPoints = 0;
        }

        /// <summary>Call after each exercise completes.</summary>
        public void ProcessResult(Core.ExerciseResult result)
        {
            AddPoints(result.PointsEarned);
            CheckAchievements(result);
            UpdateStreak();
        }

        public void AddPoints(int amount)
        {
            if (amount <= 0) return;

            SessionPoints  += amount;
            LifetimePoints += amount;
            OnPointsEarned?.Invoke(amount);

            int oldLevel = PlayerLevel;
            RecalculateLevel();
            if (PlayerLevel > oldLevel)
            {
                OnLevelUp?.Invoke(PlayerLevel);
                Debug.Log($"[GamificationSystem] SEVİYE ATLANDI! Yeni seviye: {PlayerLevel}");
            }

            SavePersistentData();
        }

        #endregion

        // ────────────────────────────────────────────────────
        #region Level System
        // ────────────────────────────────────────────────────

        private void RecalculateLevel()
        {
            for (int i = LevelThresholds.Length - 1; i >= 0; i--)
            {
                if (LifetimePoints >= LevelThresholds[i])
                {
                    PlayerLevel = i + 1;
                    int currentThreshold = LevelThresholds[i];
                    int nextThreshold    = (i + 1 < LevelThresholds.Length)
                                         ? LevelThresholds[i + 1]
                                         : LevelThresholds[i] + 2000;

                    PointsToNextLevel = nextThreshold - LifetimePoints;
                    LevelProgress     = (float)(LifetimePoints - currentThreshold)
                                       / (nextThreshold - currentThreshold);
                    return;
                }
            }
        }

        #endregion

        // ────────────────────────────────────────────────────
        #region Achievement System
        // ────────────────────────────────────────────────────

        private void InitializeAchievements()
        {
            _achievements = new List<Achievement>
            {
                new() { Id = "first_exercise",   Name = "İlk Adım 👣",        Description = "İlk egzersizini tamamla." },
                new() { Id = "perfect_rep",      Name = "Mükemmel! 🏆",        Description = "Tek bir tekrarda %100 doğruluk al." },
                new() { Id = "streak_3",         Name = "3 Günlük Seri 🔥",    Description = "3 gün üst üste egzersiz yap." },
                new() { Id = "streak_7",         Name = "Bir Hafta 💪",         Description = "7 gün üst üste egzersiz yap." },
                new() { Id = "streak_30",        Name = "Aylık Şampiyon 🥇",   Description = "30 gün üst üste egzersiz yap." },
                new() { Id = "points_100",       Name = "100 Puan ⭐",          Description = "100 puan biriktir." },
                new() { Id = "points_1000",      Name = "1000 Puan 🌟",         Description = "1000 puan biriktir." },
                new() { Id = "level_5",          Name = "Seviye 5 🎯",          Description = "5. seviyeye ulaş." },
                new() { Id = "reps_100",         Name = "100 Tekrar 🔄",        Description = "Toplam 100 tekrar tamamla." },
                new() { Id = "accuracy_90_5",   Name = "Hassas El 🎖️",         Description = "5 egzersizi %90+ doğrulukla tamamla." },
            };

            // Restore unlock states from PlayerPrefs
            foreach (var a in _achievements)
            {
                a.IsUnlocked = PlayerPrefs.GetInt($"ach_{a.Id}", 0) == 1;
            }
        }

        private void CheckAchievements(Core.ExerciseResult result)
        {
            TryUnlock("first_exercise");

            if (result.AccuracyPercent >= 100f)
                TryUnlock("perfect_rep");

            if (CurrentStreak >= 3)  TryUnlock("streak_3");
            if (CurrentStreak >= 7)  TryUnlock("streak_7");
            if (CurrentStreak >= 30) TryUnlock("streak_30");

            if (LifetimePoints >= 100)  TryUnlock("points_100");
            if (LifetimePoints >= 1000) TryUnlock("points_1000");

            if (PlayerLevel >= 5) TryUnlock("level_5");

            int totalReps = PlayerPrefs.GetInt("totalReps", 0) + result.CompletedReps;
            PlayerPrefs.SetInt("totalReps", totalReps);
            if (totalReps >= 100) TryUnlock("reps_100");
        }

        private void TryUnlock(string id)
        {
            Achievement a = _achievements.Find(x => x.Id == id);
            if (a == null || a.IsUnlocked) return;

            a.IsUnlocked  = true;
            a.UnlockedAt  = DateTime.UtcNow;
            PlayerPrefs.SetInt($"ach_{a.Id}", 1);

            OnAchievementUnlocked?.Invoke(a);
            Debug.Log($"[GamificationSystem] ROZET KAZANILDI: {a.Name}");
        }

        #endregion

        // ────────────────────────────────────────────────────
        #region Streak
        // ────────────────────────────────────────────────────

        private void UpdateStreak()
        {
            string lastDateStr = PlayerPrefs.GetString("lastExerciseDate", "");
            DateTime today     = DateTime.UtcNow.Date;

            if (string.IsNullOrEmpty(lastDateStr))
            {
                CurrentStreak = 1;
            }
            else
            {
                DateTime lastDate = DateTime.Parse(lastDateStr);
                if (lastDate.Date == today) return;                      // already exercised today
                if ((today - lastDate.Date).Days == 1) CurrentStreak++;  // consecutive day
                else                                   CurrentStreak = 1; // streak broken
            }

            PlayerPrefs.SetString("lastExerciseDate", today.ToString("o"));
            PlayerPrefs.SetInt("currentStreak", CurrentStreak);
            Debug.Log($"[GamificationSystem] Seri: {CurrentStreak} gün");
        }

        #endregion

        // ────────────────────────────────────────────────────
        #region Persistence
        // ────────────────────────────────────────────────────

        private void LoadPersistentData()
        {
            LifetimePoints = PlayerPrefs.GetInt("lifetimePoints", 0);
            CurrentStreak  = PlayerPrefs.GetInt("currentStreak",  0);
            RecalculateLevel();
        }

        private void SavePersistentData()
        {
            PlayerPrefs.SetInt("lifetimePoints", LifetimePoints);
            PlayerPrefs.Save();
        }

        #endregion
    }
}
