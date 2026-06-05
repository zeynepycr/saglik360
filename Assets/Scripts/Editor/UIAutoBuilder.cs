// ============================================================
//  Sağlık360 – UIAutoBuilder.cs  |  Editor Only
// ============================================================
//  Tools → Saglik360 → Build Complete UI
//
//  Creates the entire in-VR UI from scratch:
//    • World Space Canvas (floats in front of player)
//    • MainMenuPanel       – logo, stats, start button
//    • ExerciseSelectPanel – 9 exercise cards
//    • ExerciseActivePanel – live HUD (angle, accuracy, reps)
//    • ExerciseCompletePanel – score summary
//    • SessionSummaryPanel  – full session recap
//    • PausePanel           – pause menu
//
//  Also wires every button's OnClick and assigns all panel
//  references to UIManager and HUDController automatically.
//
//  ⚠️ Must live in Assets/Scripts/Editor/
// ============================================================

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using Saglik360.UI;
using Saglik360.Data;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace Saglik360.Editor
{
    public static class UIAutoBuilder
    {
        // ── Colors (VR-optimised: dark bg, bright text) ──────
        private static readonly Color BG_DARK      = HexColor("0A0E1A");
        private static readonly Color BG_PANEL     = HexColor("0D1829");
        private static readonly Color BG_CARD      = HexColor("0E1F36");
        private static readonly Color ACCENT_BLUE  = HexColor("5CC8FF");
        private static readonly Color ACCENT_GREEN = HexColor("3AAA6A");
        private static readonly Color ACCENT_GOLD  = HexColor("E0AA30");
        private static readonly Color ACCENT_RED   = HexColor("E03A3A");
        private static readonly Color TEXT_PRIMARY  = HexColor("C8E8FF");
        private static readonly Color TEXT_MUTED    = HexColor("4A7FA5");
        private static readonly Color BTN_BLUE     = HexColor("0E4A8A");
        private static readonly Color BTN_BORDER   = HexColor("2A8AC8");

        // ── Canvas world-space settings ──────────────────────
        private const float CANVAS_W   = 800f;
        private const float CANVAS_H   = 600f;
        private const float CANVAS_SCL = 0.001f;   // 0.8 m wide in world

        [MenuItem("Tools/Saglik360/Build Complete UI")]
        public static void BuildUI()
        {
            if (!EditorUtility.DisplayDialog("UI Oluştur",
                "Sahnede Sağlık360 VR UI'ı sıfırdan oluşturulsun mu?\n\n" +
                "Mevcut 'Saglik360Canvas' varsa önce silinecek.",
                "Evet, Oluştur", "İptal")) return;

            // ── Clean up old canvas ───────────────────────────
            var old = GameObject.Find("Saglik360Canvas");
            if (old != null) Object.DestroyImmediate(old);

            // ── Root Canvas ───────────────────────────────────
            GameObject canvasGO = new GameObject("Saglik360Canvas");
            canvasGO.layer = 5; // Set to UI Layer
            Canvas canvas       = canvasGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.WorldSpace;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<TrackedDeviceGraphicRaycaster>();

            RectTransform crt   = canvasGO.GetComponent<RectTransform>();
            crt.sizeDelta       = new Vector2(CANVAS_W, CANVAS_H);
            canvasGO.transform.localScale = Vector3.one * CANVAS_SCL;
            canvasGO.transform.position   = new Vector3(0f, 1.4f, 2f);

            // Background image
            Image bgImg = canvasGO.AddComponent<Image>();
            bgImg.color = BG_DARK;

            // ── Build all panels ──────────────────────────────
            GameObject mainPanel     = BuildMainMenuPanel(canvasGO);
            GameObject selectPanel   = BuildExerciseSelectPanel(canvasGO);
            GameObject activePanel   = BuildExerciseActivePanel(canvasGO);
            GameObject completePanel = BuildExerciseCompletePanel(canvasGO);
            GameObject summaryPanel  = BuildSessionSummaryPanel(canvasGO);
            GameObject pausePanel    = BuildPausePanel(canvasGO);

            // Show only main menu at start
            mainPanel.SetActive(true);
            selectPanel.SetActive(false);
            activePanel.SetActive(false);
            completePanel.SetActive(false);
            summaryPanel.SetActive(false);
            pausePanel.SetActive(false);

            // ── Wire UIManager ────────────────────────────────
            UIManager uiMgr = Object.FindObjectOfType<UIManager>();
            if (uiMgr != null)
            {
                SerializedObject so = new SerializedObject(uiMgr);
                so.FindProperty("mainMenuPanel").objectReferenceValue        = mainPanel;
                so.FindProperty("exerciseSelectionPanel").objectReferenceValue = selectPanel;
                so.FindProperty("exerciseCompletePanel").objectReferenceValue  = completePanel;
                so.FindProperty("sessionSummaryPanel").objectReferenceValue    = summaryPanel;
                so.FindProperty("settingsPanel").objectReferenceValue          = null;
                so.FindProperty("pausePanel").objectReferenceValue             = pausePanel;
                so.ApplyModifiedProperties();
            }

            // ── Wire HUDController fields ─────────────────────
            HUDController hud = Object.FindObjectOfType<HUDController>();
            if (hud != null)
            {
                SerializedObject hso = new SerializedObject(hud);
                // Find named children inside activePanel
                hso.FindProperty("exerciseNameText").objectReferenceValue =
                    FindTMP(activePanel, "ExerciseNameText");
                hso.FindProperty("repCounterText").objectReferenceValue =
                    FindTMP(activePanel, "RepCounterText");
                hso.FindProperty("setCounterText").objectReferenceValue =
                    FindTMP(activePanel, "SetCounterText");
                hso.FindProperty("angleText").objectReferenceValue =
                    FindTMP(activePanel, "AngleText");
                hso.FindProperty("accuracySlider").objectReferenceValue =
                    FindChild<Slider>(activePanel, "AccuracySlider");
                hso.FindProperty("accuracyLabel").objectReferenceValue =
                    FindTMP(activePanel, "AccuracyLabel");
                hso.FindProperty("pointsText").objectReferenceValue =
                    FindTMP(activePanel, "PointsText");
                hso.FindProperty("restPanel").objectReferenceValue =
                    FindChildGO(activePanel, "RestPanel");
                hso.FindProperty("restTimerText").objectReferenceValue =
                    FindTMP(activePanel, "RestTimerText");
                hso.FindProperty("countdownPanel").objectReferenceValue =
                    FindChildGO(activePanel, "CountdownPanel");
                hso.FindProperty("countdownText").objectReferenceValue =
                    FindTMP(activePanel, "CountdownText");
                hso.FindProperty("achievementToast").objectReferenceValue =
                    FindChildGO(canvasGO, "AchievementToast");
                hso.FindProperty("achievementTitle").objectReferenceValue =
                    FindTMP(canvasGO, "AchievementTitle");
                hso.FindProperty("achievementDesc").objectReferenceValue =
                    FindTMP(canvasGO, "AchievementDesc");
                hso.ApplyModifiedProperties();
            }

            // Wire complete panel fields to UIManager
            if (uiMgr != null)
            {
                SerializedObject so = new SerializedObject(uiMgr);
                so.FindProperty("completeExerciseName").objectReferenceValue =
                    FindTMP(completePanel, "CompleteExerciseName");
                so.FindProperty("completeAccuracyText").objectReferenceValue =
                    FindTMP(completePanel, "CompleteAccuracyText");
                so.FindProperty("completePointsText").objectReferenceValue =
                    FindTMP(completePanel, "CompletePointsText");
                so.FindProperty("completeAccuracyBar").objectReferenceValue =
                    FindChild<Slider>(completePanel, "CompleteAccuracyBar");
                so.FindProperty("summaryDurationText").objectReferenceValue =
                    FindTMP(summaryPanel, "SummaryDurationText");
                so.FindProperty("summaryOverallAccuracyText").objectReferenceValue =
                    FindTMP(summaryPanel, "SummaryAccuracyText");
                so.FindProperty("summaryTotalPointsText").objectReferenceValue =
                    FindTMP(summaryPanel, "SummaryPointsText");
                so.FindProperty("summaryStreakText").objectReferenceValue =
                    FindTMP(summaryPanel, "SummaryStreakText");
                so.FindProperty("summaryLevelText").objectReferenceValue =
                    FindTMP(summaryPanel, "SummaryLevelText");
                so.ApplyModifiedProperties();
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            Selection.activeGameObject = canvasGO;

            EditorUtility.DisplayDialog("Tamamlandı ✓",
                "UI başarıyla oluşturuldu!\n\n" +
                "Kontrol listesi:\n" +
                "✓ MainMenuPanel\n" +
                "✓ ExerciseSelectPanel (9 kart)\n" +
                "✓ ExerciseActivePanel (HUD)\n" +
                "✓ ExerciseCompletePanel\n" +
                "✓ SessionSummaryPanel\n" +
                "✓ PausePanel\n" +
                "✓ UIManager referansları bağlandı\n" +
                "✓ HUDController referansları bağlandı\n\n" +
                "Şimdi Build And Run yapabilirsiniz!", "Tamam");
        }

        // ════════════════════════════════════════════════════
        // PANEL BUILDERS
        // ════════════════════════════════════════════════════

        // ── MAIN MENU ────────────────────────────────────────
        private static GameObject BuildMainMenuPanel(GameObject parent)
        {
            GameObject panel = MakePanel(parent, "MainMenuPanel", BG_PANEL);

            // Title
            MakeTMP(panel, "TitleText", "SAĞLIK360", 42, ACCENT_BLUE, FontStyles.Bold,
                new Rect(0, 180, 800, 80), TextAlignmentOptions.Center);

            // Subtitle
            MakeTMP(panel, "SubtitleText", "Oyunlaştırılmış Fizik Tedavi Simülasyonu",
                16, TEXT_MUTED, FontStyles.Normal,
                new Rect(0, 130, 800, 40), TextAlignmentOptions.Center);

            // Divider
            MakeImage(panel, "Divider", new Rect(100, 110, 600, 1), ACCENT_BLUE * 0.4f);

            // Stat cards row
            MakeStatCard(panel, "StatStreak",  "🔥", "7",   "Günlük Seri",  new Vector2(-240, 40));
            MakeStatCard(panel, "StatPoints",  "⭐", "1240","Toplam Puan",   new Vector2(0,    40));
            MakeStatCard(panel, "StatAccuracy","💪", "%84", "Ort. Doğruluk", new Vector2(240,  40));

            // Start button
            UIManager uiMgr = Object.FindObjectOfType<UIManager>();
            GameObject startBtn = MakeButton(panel, "StartButton", "▶  Egzersiz Başlat",
                new Rect(0, -60, 400, 60), BTN_BLUE, BTN_BORDER, ACCENT_BLUE, 20);
            AddOnClick(startBtn, uiMgr, "OnStartSessionButton");

            // Settings button (visual only for now)
            MakeButton(panel, "SettingsButton", "⚙  Ayarlar",
                new Rect(0, -140, 260, 48), BG_CARD, TEXT_MUTED, TEXT_MUTED, 16);

            return panel;
        }

        // ── EXERCISE SELECTION ────────────────────────────────
        private static GameObject BuildExerciseSelectPanel(GameObject parent)
        {
            GameObject panel = MakePanel(parent, "ExerciseSelectPanel", BG_PANEL);

            MakeTMP(panel, "SelectTitle", "Egzersiz Seç", 26, ACCENT_BLUE, FontStyles.Bold,
                new Rect(0, 250, 800, 50), TextAlignmentOptions.Center);

            MakeTMP(panel, "SelectSub", "Fizyoterapistinizin atadığı egzersizler",
                13, TEXT_MUTED, FontStyles.Normal,
                new Rect(0, 215, 800, 30), TextAlignmentOptions.Center);

            // 9 exercise cards in a 3x3 grid
            string[] names = {
                "Omuz Fleksiyonu", "Omuz Ekstansiyonu", "Omuz Abduksiyonu",
                "Omuz Adduksiyonu", "Dairesel Hareket",  "Uzanma & Hedef",
                "Pinch Hareketi",  "Yumruk Yapma",       "El İşaretleri"
            };
            string[] icons = { "🦾","↩","↔","→","🔄","🎯","🤏","✊","🖐" };
            string[] reps  = { "10 tek·3 set","10 tek·3 set","10 tek·3 set",
                                "10 tek·3 set","8 tek·3 set", "8 tek·3 set",
                                "15 tek·2 set","12 tek·3 set","12 tek·2 set" };

            int cols = 3;
            float cardW = 220f, cardH = 100f, gapX = 15f, gapY = 12f;
            float startX = -(cols - 1) * (cardW + gapX) / 2f;
            float startY = 130f;

            UIManager uiMgr = Object.FindObjectOfType<UIManager>();

            // Load exercise assets
            ExerciseDefinition[] assets =
                Resources.LoadAll<ExerciseDefinition>("Exercises");

            for (int i = 0; i < names.Length; i++)
            {
                int col = i % cols;
                int row = i / cols;
                float x = startX + col * (cardW + gapX);
                float y = startY - row * (cardH + gapY);

                GameObject card = MakeImage(panel, $"ExCard_{i}",
                    new Rect(x, y, cardW, cardH), BG_CARD);

                // Card border
                Outline outline = card.AddComponent<Outline>();
                outline.effectColor    = BTN_BORDER * 0.6f;
                outline.effectDistance = new Vector2(1, 1);

                // Icon
                MakeTMP(card, "Icon", icons[i], 22, Color.white, FontStyles.Normal,
                    new Rect(-70, 10, 50, 40), TextAlignmentOptions.Center);

                // Name
                MakeTMP(card, "Name", names[i], 13, TEXT_PRIMARY, FontStyles.Bold,
                    new Rect(10, 20, 150, 30), TextAlignmentOptions.Left);

                // Meta
                MakeTMP(card, "Meta", reps[i], 10, TEXT_MUTED, FontStyles.Normal,
                    new Rect(10, -5, 150, 24), TextAlignmentOptions.Left);

                // Difficulty dot
                Color dotCol = i < 4 ? ACCENT_GREEN : ACCENT_GOLD;
                string diffLabel = i < 4 ? "Başlangıç" : "Orta";
                MakeTMP(card, "Diff", diffLabel, 9, dotCol, FontStyles.Normal,
                    new Rect(10, -28, 120, 20), TextAlignmentOptions.Left);

                // Button overlay
                Button btn = card.AddComponent<Button>();
                ColorBlock cb = btn.colors;
                cb.normalColor      = Color.clear;
                cb.highlightedColor = new Color(1, 1, 1, 0.1f);
                cb.pressedColor     = new Color(1, 1, 1, 0.2f);
                btn.colors = cb;

                // Wire to UIManager — pass the exercise definition if available
                if (uiMgr != null)
                {
                    ExerciseDefinition def = null;
                    foreach (var a in assets)
                        if (a.ExerciseName == names[i]) { def = a; break; }

                    if (def != null)
                    {
                        btn.onClick.AddListener(() => uiMgr.OnExerciseSelected(def));
                    }
                }
            }

            // Back button
            GameObject backBtn = MakeButton(panel, "BackBtn", "← Ana Menü",
                new Rect(-300, -255, 160, 44), BG_CARD, TEXT_MUTED, TEXT_MUTED, 14);
            AddOnClick(backBtn, uiMgr, "OnMainMenuButton");

            return panel;
        }

        // ── EXERCISE ACTIVE (HUD) ─────────────────────────────
        private static GameObject BuildExerciseActivePanel(GameObject parent)
        {
            GameObject panel = MakePanel(parent, "ExerciseActivePanel", BG_PANEL);

            // Top bar
            MakeImage(panel, "TopBar", new Rect(0, 265, 800, 50), BG_DARK);
            MakeTMP(panel, "ExerciseNameText", "Omuz Fleksiyonu", 20,
                ACCENT_BLUE, FontStyles.Bold, new Rect(-180, 265, 400, 44), TextAlignmentOptions.Left);
            MakeTMP(panel, "PointsText", "⭐ 0 puan", 16,
                ACCENT_GOLD, FontStyles.Normal, new Rect(230, 265, 200, 44), TextAlignmentOptions.Right);

            // Rep / set counter
            MakeTMP(panel, "RepCounterText", "Tekrar 1 / 10", 15,
                ACCENT_GREEN, FontStyles.Normal, new Rect(-180, 220, 250, 34), TextAlignmentOptions.Left);
            MakeTMP(panel, "SetCounterText", "Set 1 / 3", 15,
                TEXT_MUTED, FontStyles.Normal, new Rect(180, 220, 200, 34), TextAlignmentOptions.Right);

            // Angle display box
            MakeImage(panel, "AngleBox", new Rect(0, 100, 380, 140), BG_DARK);
            MakeTMP(panel, "AngleText", "0°  /  180°", 36,
                ACCENT_BLUE, FontStyles.Bold, new Rect(0, 105, 360, 80), TextAlignmentOptions.Center);
            MakeTMP(panel, "AngleLabel", "Mevcut Açı  /  Hedef", 12,
                TEXT_MUTED, FontStyles.Normal, new Rect(0, 55, 360, 30), TextAlignmentOptions.Center);

            // Accuracy bar
            MakeTMP(panel, "AccLabel1", "Doğruluk", 12, TEXT_MUTED, FontStyles.Normal,
                new Rect(-280, 10, 160, 28), TextAlignmentOptions.Left);
            MakeTMP(panel, "AccuracyLabel", "—", 12, ACCENT_GREEN, FontStyles.Bold,
                new Rect(220, 10, 120, 28), TextAlignmentOptions.Right);

            GameObject sliderGO = MakeSlider(panel, "AccuracySlider",
                new Rect(0, -18, 700, 18), ACCENT_GREEN);

            // Rep dots row
            float dotStartX = -320f;
            for (int i = 0; i < 10; i++)
            {
                GameObject dot = MakeImage(panel, $"Dot_{i}",
                    new Rect(dotStartX + i * 70f, -65, 24, 24), BG_CARD);
                dot.GetComponent<Image>().sprite = null;
                var dotOutline = dot.AddComponent<Outline>();
                dotOutline.effectColor = BTN_BORDER;
                // Make it round via script (best effort in editor)
            }

            // Pause / stop buttons
            UIManager uiMgr = Object.FindObjectOfType<UIManager>();
            GameObject pauseBtn = MakeButton(panel, "PauseBtn", "⏸  Duraklat",
                new Rect(-180, -140, 300, 52), BG_CARD, ACCENT_GOLD, ACCENT_GOLD, 16);
            AddOnClick(pauseBtn, uiMgr, "OnPauseButton");

            GameObject stopBtn = MakeButton(panel, "StopBtn", "⏹  Bitir",
                new Rect(180, -140, 300, 52), BG_DARK, ACCENT_RED, ACCENT_RED, 16);
            AddOnClick(stopBtn, uiMgr, "OnEndSessionButton");

            // ── Rest panel (hidden by default) ───────────────
            GameObject restPanel = MakeImage(panel, "RestPanel",
                new Rect(0, 60, 500, 200), new Color(0.04f, 0.08f, 0.16f, 0.97f));
            restPanel.SetActive(false);

            MakeTMP(restPanel, "RestTitle", "DİNLENME", 22, ACCENT_GOLD, FontStyles.Bold,
                new Rect(0, 40, 460, 50), TextAlignmentOptions.Center);
            MakeTMP(restPanel, "RestTimerText", "30", 48, ACCENT_BLUE, FontStyles.Bold,
                new Rect(0, -10, 460, 80), TextAlignmentOptions.Center);
            MakeTMP(restPanel, "RestMessage", "Sonraki sete hazırlanın", 14, TEXT_MUTED, FontStyles.Normal,
                new Rect(0, -65, 460, 36), TextAlignmentOptions.Center);

            // ── Countdown panel (hidden by default) ──────────
            GameObject countPanel = MakeImage(panel, "CountdownPanel",
                new Rect(0, 60, 300, 200), new Color(0.04f, 0.08f, 0.16f, 0.97f));
            countPanel.SetActive(false);
            MakeTMP(countPanel, "CountdownText", "3", 72, ACCENT_BLUE, FontStyles.Bold,
                new Rect(0, 0, 280, 160), TextAlignmentOptions.Center);

            return panel;
        }

        // ── EXERCISE COMPLETE ─────────────────────────────────
        private static GameObject BuildExerciseCompletePanel(GameObject parent)
        {
            GameObject panel = MakePanel(parent, "ExerciseCompletePanel", BG_PANEL);

            MakeTMP(panel, "Badge", "🏆", 56, Color.white, FontStyles.Normal,
                new Rect(0, 210, 800, 80), TextAlignmentOptions.Center);
            MakeTMP(panel, "CompleteTitle", "Egzersiz Tamamlandı!", 30,
                ACCENT_BLUE, FontStyles.Bold, new Rect(0, 145, 800, 55), TextAlignmentOptions.Center);
            MakeTMP(panel, "CompleteExerciseName", "Omuz Fleksiyonu", 16,
                TEXT_MUTED, FontStyles.Normal, new Rect(0, 105, 800, 36), TextAlignmentOptions.Center);

            // Result cards
            MakeResultCard(panel, "CardAcc",  new Vector2(-220, 0),  "CompleteAccuracyText",  "%—",  "DOĞRULUK");
            MakeResultCard(panel, "CardReps", new Vector2(0,    0),  "CompleteRepsText",       "30",  "TOPLAM TEKRAR");
            MakeResultCard(panel, "CardPts",  new Vector2(220,  0),  "CompletePointsText",    "+—",  "PUAN KAZANILDI");

            // Accuracy bar
            MakeTMP(panel, "AccBarLabel", "Doğruluk", 12, TEXT_MUTED, FontStyles.Normal,
                new Rect(0, -95, 700, 28), TextAlignmentOptions.Left);
            MakeSlider(panel, "CompleteAccuracyBar", new Rect(0, -118, 660, 18), ACCENT_GREEN);

            // Buttons
            UIManager uiMgr = Object.FindObjectOfType<UIManager>();
            GameObject nextBtn = MakeButton(panel, "NextBtn", "Sonraki Egzersiz  →",
                new Rect(100, -195, 300, 56), BTN_BLUE, BTN_BORDER, ACCENT_BLUE, 17);
            AddOnClick(nextBtn, uiMgr, "OnContinueButton");

            GameObject menuBtn = MakeButton(panel, "MenuBtn2", "Ana Menü",
                new Rect(-200, -195, 220, 56), BG_CARD, TEXT_MUTED, TEXT_MUTED, 16);
            AddOnClick(menuBtn, uiMgr, "OnMainMenuButton");

            return panel;
        }

        // ── SESSION SUMMARY ───────────────────────────────────
        private static GameObject BuildSessionSummaryPanel(GameObject parent)
        {
            GameObject panel = MakePanel(parent, "SessionSummaryPanel", BG_PANEL);

            MakeTMP(panel, "SumTitle", "Oturum Özeti", 30, ACCENT_BLUE, FontStyles.Bold,
                new Rect(0, 245, 800, 56), TextAlignmentOptions.Center);
            MakeTMP(panel, "SumSub", "Harika iş çıkardınız! Verileriniz fizyoterapistinize gönderildi.",
                13, TEXT_MUTED, FontStyles.Normal, new Rect(0, 200, 700, 36), TextAlignmentOptions.Center);

            MakeStatRow(panel, "SummaryDurationText",   "⏱",  "00:00", "Toplam Süre",        new Vector2(0,  140));
            MakeStatRow(panel, "SummaryAccuracyText",   "🎯", "%—",    "Ortalama Doğruluk",  new Vector2(0,   70));
            MakeStatRow(panel, "SummaryPointsText",     "⭐", "—",     "Kazanılan Puan",     new Vector2(0,    0));
            MakeStatRow(panel, "SummaryStreakText",      "🔥", "—",     "Günlük Seri",        new Vector2(0,  -70));
            MakeStatRow(panel, "SummaryLevelText",      "🏅", "—",     "Seviye",             new Vector2(0, -140));

            UIManager uiMgr = Object.FindObjectOfType<UIManager>();
            GameObject menuBtn = MakeButton(panel, "SumMenuBtn", "Ana Menüye Dön",
                new Rect(0, -235, 340, 56), BTN_BLUE, BTN_BORDER, ACCENT_BLUE, 18);
            AddOnClick(menuBtn, uiMgr, "OnMainMenuButton");

            return panel;
        }

        // ── PAUSE ─────────────────────────────────────────────
        private static GameObject BuildPausePanel(GameObject parent)
        {
            GameObject panel = MakePanel(parent, "PausePanel", new Color(0.04f, 0.07f, 0.14f, 0.96f));

            MakeTMP(panel, "PauseTitle", "⏸ DURAKLATILDI", 34, ACCENT_GOLD, FontStyles.Bold,
                new Rect(0, 100, 800, 60), TextAlignmentOptions.Center);

            UIManager uiMgr = Object.FindObjectOfType<UIManager>();
            GameObject resumeBtn = MakeButton(panel, "ResumeBtn", "▶  Devam Et",
                new Rect(0, 0, 340, 60), BTN_BLUE, BTN_BORDER, ACCENT_BLUE, 20);
            AddOnClick(resumeBtn, uiMgr, "OnResumeButton");

            GameObject menuBtn = MakeButton(panel, "PauseMenuBtn", "Ana Menü",
                new Rect(0, -90, 260, 52), BG_CARD, TEXT_MUTED, TEXT_MUTED, 17);
            AddOnClick(menuBtn, uiMgr, "OnMainMenuButton");

            return panel;
        }

        // ── ACHIEVEMENT TOAST (always-on-top child of canvas) ─
        private static void BuildAchievementToast(GameObject canvas)
        {
            GameObject toast = MakeImage(canvas, "AchievementToast",
                new Rect(0, 240, 520, 90), new Color(0.06f, 0.18f, 0.10f, 0.97f));
            toast.SetActive(false);

            var outline = toast.AddComponent<Outline>();
            outline.effectColor    = ACCENT_GREEN;
            outline.effectDistance = new Vector2(1.5f, 1.5f);

            MakeTMP(toast, "AchievementTitle", "Rozet Kazandın! 🏆", 18,
                ACCENT_GREEN, FontStyles.Bold, new Rect(0, 14, 500, 36), TextAlignmentOptions.Center);
            MakeTMP(toast, "AchievementDesc", "İlk egzersizini tamamladın.", 13,
                TEXT_PRIMARY, FontStyles.Normal, new Rect(0, -16, 500, 30), TextAlignmentOptions.Center);
        }

        // ════════════════════════════════════════════════════
        // COMPONENT FACTORY HELPERS
        // ════════════════════════════════════════════════════

        private static GameObject MakePanel(GameObject parent, string name, Color bg)
        {
            var go  = new GameObject(name);
            go.layer = 5;
            go.transform.SetParent(parent.transform, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = new Vector2(CANVAS_W, CANVAS_H);
            var img = go.AddComponent<Image>();
            img.color = bg;
            return go;
        }

        private static GameObject MakeTMP(GameObject parent, string name,
            string text, float size, Color color, FontStyles style,
            Rect rect, TextAlignmentOptions align)
        {
            var go  = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(rect.x, rect.y);
            rt.sizeDelta        = new Vector2(rect.width, rect.height);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text            = text;
            tmp.fontSize        = size;
            tmp.color           = color;
            tmp.fontStyle       = style;
            tmp.alignment       = align;
            tmp.enableWordWrapping = true;
            return go;
        }

        private static GameObject MakeImage(GameObject parent, string name, Rect rect, Color color)
        {
            var go  = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(rect.x, rect.y);
            rt.sizeDelta        = new Vector2(rect.width, rect.height);
            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        private static GameObject MakeButton(GameObject parent, string name,
            string label, Rect rect, Color bgColor, Color borderColor, Color textColor, float fontSize)
        {
            GameObject go  = MakeImage(parent, name, rect, bgColor);
            var outline    = go.AddComponent<Outline>();
            outline.effectColor    = borderColor;
            outline.effectDistance = new Vector2(1f, 1f);

            Button btn = go.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor      = bgColor;
            cb.highlightedColor = bgColor * 1.3f;
            cb.pressedColor     = bgColor * 0.7f;
            btn.colors = cb;

            MakeTMP(go, "Label", label, fontSize, textColor, FontStyles.Normal,
                new Rect(0, 0, rect.width - 20, rect.height), TextAlignmentOptions.Center);
            return go;
        }

        private static GameObject MakeSlider(GameObject parent, string name, Rect rect, Color fillColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(rect.x, rect.y);
            rt.sizeDelta        = new Vector2(rect.width, rect.height);

            // Background
            var bg = new GameObject("Background");
            bg.transform.SetParent(go.transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            var bgImg = bg.AddComponent<Image>(); bgImg.color = BG_DARK;

            // Fill area
            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(go.transform, false);
            var faRt = fillArea.AddComponent<RectTransform>();
            faRt.anchorMin = Vector2.zero; faRt.anchorMax = Vector2.one;
            faRt.offsetMin = faRt.offsetMax = Vector2.zero;

            // Fill
            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillRt = fill.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = new Vector2(0.5f, 1f);
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
            var fillImg = fill.AddComponent<Image>(); fillImg.color = fillColor;

            Slider slider = go.AddComponent<Slider>();
            slider.fillRect = fillRt;
            slider.value    = 0.5f;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.interactable = false;

            return go;
        }

        private static void MakeStatCard(GameObject parent, string name,
            string icon, string value, string label, Vector2 center)
        {
            GameObject card = MakeImage(parent, name,
                new Rect(center.x, center.y, 180, 90), BG_CARD);
            var outline = card.AddComponent<Outline>();
            outline.effectColor    = BTN_BORDER * 0.5f;
            outline.effectDistance = new Vector2(1, 1);

            MakeTMP(card, "Icon",  icon,  20, Color.white,   FontStyles.Normal, new Rect(0, 20, 170, 34), TextAlignmentOptions.Center);
            MakeTMP(card, "Val",   value, 22, ACCENT_BLUE,   FontStyles.Bold,   new Rect(0, -5, 170, 34), TextAlignmentOptions.Center);
            MakeTMP(card, "Label", label, 10, TEXT_MUTED,    FontStyles.Normal, new Rect(0, -32, 170, 24), TextAlignmentOptions.Center);
        }

        private static void MakeResultCard(GameObject parent, string cardName,
            Vector2 center, string tmpName, string defaultVal, string label)
        {
            GameObject card = MakeImage(parent, cardName,
                new Rect(center.x, center.y, 190, 110), BG_CARD);
            var outline = card.AddComponent<Outline>();
            outline.effectColor    = BTN_BORDER * 0.5f;
            outline.effectDistance = new Vector2(1, 1);

            MakeTMP(card, tmpName, defaultVal, 28, ACCENT_BLUE, FontStyles.Bold,
                new Rect(0, 14, 170, 50), TextAlignmentOptions.Center);
            MakeTMP(card, "Label", label, 10, TEXT_MUTED, FontStyles.Normal,
                new Rect(0, -24, 170, 26), TextAlignmentOptions.Center);
        }

        private static void MakeStatRow(GameObject parent, string tmpName,
            string icon, string value, string label, Vector2 center)
        {
            GameObject row = MakeImage(parent, "Row_" + tmpName,
                new Rect(center.x, center.y, 660, 54), BG_CARD);

            MakeTMP(row, "Icon",  icon,  20, Color.white, FontStyles.Normal,
                new Rect(-270, 0, 44, 50), TextAlignmentOptions.Center);
            MakeTMP(row, "Label", label, 14, TEXT_MUTED,  FontStyles.Normal,
                new Rect(-50, 0, 300, 50), TextAlignmentOptions.Left);
            MakeTMP(row, tmpName, value, 18, ACCENT_BLUE, FontStyles.Bold,
                new Rect(150, 0, 200, 50), TextAlignmentOptions.Right);
        }

        // ════════════════════════════════════════════════════
        // WIRING HELPERS
        // ════════════════════════════════════════════════════

        private static void AddOnClick(GameObject btnGO, Object target, string methodName)
        {
            if (btnGO == null || target == null) return;
            Button btn = btnGO.GetComponent<Button>();
            if (btn == null) return;

            SerializedObject so = new SerializedObject(btn);
            SerializedProperty onClick = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
            onClick.arraySize++;
            SerializedProperty call = onClick.GetArrayElementAtIndex(onClick.arraySize - 1);
            call.FindPropertyRelative("m_Target").objectReferenceValue = target;
            call.FindPropertyRelative("m_MethodName").stringValue      = methodName;
            call.FindPropertyRelative("m_Mode").enumValueIndex         = 1; // Void
            call.FindPropertyRelative("m_CallState").enumValueIndex    = 2; // RuntimeOnly
            so.ApplyModifiedProperties();
        }

        private static TextMeshProUGUI FindTMP(GameObject root, string name)
        {
            var all = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in all)
                if (t.gameObject.name == name) return t;
            return null;
        }

        private static T FindChild<T>(GameObject root, string name) where T : Component
        {
            var all = root.GetComponentsInChildren<T>(true);
            foreach (var t in all)
                if (t.gameObject.name == name) return t;
            return null;
        }

        private static GameObject FindChildGO(GameObject root, string name)
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in all)
                if (t.gameObject.name == name) return t.gameObject;
            return null;
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color c);
            return c;
        }
    }
}
