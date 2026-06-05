// ============================================================
//  Sağlık360 – SceneAutoWirer.cs  |  Editor Only
// ============================================================
//  Adds a menu item:
//    Tools → Saglik360 → Auto-Wire Scene References
//
//  Finds every manager in the scene and fills in all the
//  serialized Inspector fields automatically — so you never
//  have to drag and drop anything manually.
//
//  Safe to run multiple times (idempotent).
//
//  ⚠️  This file MUST live in an "Editor" folder:
//      Assets/Scripts/Editor/SceneAutoWirer.cs
// ============================================================

using UnityEngine;
using UnityEditor;
using Saglik360.Core;
using Saglik360.Movement;
using Saglik360.UI;

namespace Saglik360.Editor
{
    public static class SceneAutoWirer
    {
        [MenuItem("Tools/Saglik360/Auto-Wire Scene References")]
        public static void AutoWire()
        {
            int fixes = 0;

            // ── Locate every manager ──────────────────────────
            var gameManager      = Find<GameManager>("GameManager");
            var exerciseManager  = Find<ExerciseManager>("ExerciseManager");
            var sessionManager   = Find<SessionManager>("SessionManager");
            var gamification     = Find<GamificationSystem>("GamificationSystem");
            var feedbackSystem   = Find<FeedbackSystem>("FeedbackSystem");
            var firebaseManager  = Find<FirebaseManager>("FirebaseManager");
            var uiManager        = Find<UIManager>("UIManager");
            var movementTracker  = Find<MovementTracker>("MovementTracker");
            var hudController    = Find<HUDController>("HUDController");

            if (gameManager == null)
            {
                EditorUtility.DisplayDialog("Hata",
                    "GameManager sahnede bulunamadı.\n\nLütfen önce [Managers] GameObject'ini ve tüm manager script'lerini sahneye ekleyin.",
                    "Tamam");
                return;
            }

            // ────────────────────────────────────────────────
            // GameManager → wire all sub-managers
            // ────────────────────────────────────────────────
            var gmSO = new SerializedObject(gameManager);

            fixes += SetRef(gmSO, "exerciseManager",  exerciseManager);
            fixes += SetRef(gmSO, "sessionManager",   sessionManager);
            fixes += SetRef(gmSO, "gamification",     gamification);
            fixes += SetRef(gmSO, "firebase",         firebaseManager);
            fixes += SetRef(gmSO, "uiManager",        uiManager);
            fixes += SetRef(gmSO, "feedback",         feedbackSystem);

            gmSO.ApplyModifiedProperties();

            // ────────────────────────────────────────────────
            // ExerciseManager → needs MovementTracker + FeedbackSystem
            // ────────────────────────────────────────────────
            if (exerciseManager != null)
            {
                var emSO = new SerializedObject(exerciseManager);
                fixes += SetRef(emSO, "movementTracker", movementTracker);
                fixes += SetRef(emSO, "feedback",        feedbackSystem);
                emSO.ApplyModifiedProperties();
            }

            // ────────────────────────────────────────────────
            // MovementTracker → wire controller transforms from XR Rig
            // ────────────────────────────────────────────────
            if (movementTracker != null)
            {
                var mtSO = new SerializedObject(movementTracker);

                // Try to find the standard XR Rig controller transforms
                Transform rightCtrl = FindTransformByName("RightHand Controller");
                Transform leftCtrl  = FindTransformByName("LeftHand Controller");
                Transform shoulder  = FindTransformByName("ShoulderAnchor");

                if (rightCtrl != null) fixes += SetRef(mtSO, "rightControllerTransform", rightCtrl);
                if (leftCtrl  != null) fixes += SetRef(mtSO, "leftControllerTransform",  leftCtrl);
                if (shoulder  != null) fixes += SetRef(mtSO, "shoulderAnchor",           shoulder);

                mtSO.ApplyModifiedProperties();
            }

            // ────────────────────────────────────────────────
            // Mark scene dirty so Unity saves the changes
            // ────────────────────────────────────────────────
            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();

            string report = BuildReport(
                gameManager, exerciseManager, sessionManager,
                gamification, feedbackSystem, firebaseManager,
                uiManager, movementTracker, hudController, fixes);

            Debug.Log("[SceneAutoWirer] " + report);
            EditorUtility.DisplayDialog("Saglik360— Auto-Wire Tamamlandı", report, "Tamam");
        }

        // ────────────────────────────────────────────────────
        // Validate: checks which fields are still empty
        // ────────────────────────────────────────────────────
        [MenuItem("Tools/Saglik360/Validate Scene References")]
        public static void ValidateScene()
        {
            var issues = new System.Text.StringBuilder();
            int ok = 0, missing = 0;

            CheckComponent<GameManager>(ref ok, ref missing, issues);
            CheckComponent<ExerciseManager>(ref ok, ref missing, issues);
            CheckComponent<SessionManager>(ref ok, ref missing, issues);
            CheckComponent<GamificationSystem>(ref ok, ref missing, issues);
            CheckComponent<FeedbackSystem>(ref ok, ref missing, issues);
            CheckComponent<FirebaseManager>(ref ok, ref missing, issues);
            CheckComponent<UIManager>(ref ok, ref missing, issues);
            CheckComponent<MovementTracker>(ref ok, ref missing, issues);
            CheckComponent<HUDController>(ref ok, ref missing, issues);

            string result = missing == 0
                ? $"Tüm referanslar tamam ({ok} bileşen doğrulandı)."
                : $"UYARI: {missing} bileşen sahnede bulunamadı.\n\n{issues}";

            EditorUtility.DisplayDialog("Saglik360 — Sahne Doğrulama", result, "Tamam");
        }

        // ── Setup helper: create the [Managers] GameObject ───
        [MenuItem("Tools/Saglik360/Setup — Create Manager GameObjects")]
        public static void CreateManagerObjects()
        {
            if (!EditorUtility.DisplayDialog("Manager GameObject'leri Oluştur",
                "Sahnede [Managers] adlı bir GameObject ve tüm manager script'leri eklenecek. Devam edilsin mi?",
                "Evet", "İptal")) return;

            // Root managers object
            GameObject root = new GameObject("[Managers]");
            Undo.RegisterCreatedObjectUndo(root, "Create Saglik360 Managers");

            AddManager<GameManager>(root);
            AddManager<ExerciseManager>(root);
            AddManager<SessionManager>(root);
            AddManager<GamificationSystem>(root);
            AddManager<FeedbackSystem>(root);
            AddManager<FirebaseManager>(root);
            AddManager<UIManager>(root);

            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();

            EditorUtility.DisplayDialog("Tamamlandı",
                "[Managers] GameObject oluşturuldu ve tüm manager script'leri eklendi.\n\nŞimdi:\n1. XR Rig'inizi sahneye ekleyin\n2. MovementTracker'ı XR Rig'e ekleyin\n3. HUDController'ı Camera Offset'e ekleyin\n4. 'Auto-Wire Scene References' menüsünü çalıştırın.",
                "Tamam");
        }

        // ────────────────────────────────────────────────────
        #region Helpers
        // ────────────────────────────────────────────────────

        private static T Find<T>(string label) where T : Component
        {
            T obj = Object.FindObjectOfType<T>();
            if (obj == null)
                Debug.LogWarning($"[SceneAutoWirer] '{label}' sahnede bulunamadı — ilgili alan atlandı.");
            return obj;
        }

        private static Transform FindTransformByName(string name)
        {
            // Search entire scene hierarchy
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
                if (go.name == name) return go.transform;
            return null;
        }

        /// <summary>
        /// Sets a serialized reference field on the target object.
        /// Returns 1 if a change was made, 0 if already set.
        /// </summary>
        private static int SetRef(SerializedObject so, string fieldName, Object value)
        {
            if (value == null) return 0;

            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[SceneAutoWirer] '{fieldName}' alanı bulunamadı. Field adı scriptde farklı mı?");
                return 0;
            }

            if (prop.objectReferenceValue == value) return 0;  // already set

            prop.objectReferenceValue = value;
            return 1;
        }

        private static void AddManager<T>(GameObject parent) where T : Component
        {
            if (Object.FindObjectOfType<T>() == null)
                parent.AddComponent<T>();
        }

        private static void CheckComponent<T>(
            ref int ok, ref int missing,
            System.Text.StringBuilder issues) where T : Component
        {
            if (Object.FindObjectOfType<T>() != null) ok++;
            else { missing++; issues.AppendLine($"✗ {typeof(T).Name} sahnede yok"); }
        }

        private static string BuildReport(
            GameManager gm, ExerciseManager em, SessionManager sm,
            GamificationSystem gs, FeedbackSystem fs, FirebaseManager fb,
            UIManager ui, MovementTracker mt, HUDController hud,
            int fixes)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"{fixes} referans güncellendi.\n");
            sb.AppendLine("Bileşen Durumu:");
            sb.AppendLine(gm  != null ? "✓ GameManager"      : "✗ GameManager EKSİK");
            sb.AppendLine(em  != null ? "✓ ExerciseManager"  : "✗ ExerciseManager EKSİK");
            sb.AppendLine(sm  != null ? "✓ SessionManager"   : "✗ SessionManager EKSİK");
            sb.AppendLine(gs  != null ? "✓ GamificationSystem" : "✗ GamificationSystem EKSİK");
            sb.AppendLine(fs  != null ? "✓ FeedbackSystem"   : "✗ FeedbackSystem EKSİK");
            sb.AppendLine(fb  != null ? "✓ FirebaseManager"  : "✗ FirebaseManager EKSİK");
            sb.AppendLine(ui  != null ? "✓ UIManager"        : "✗ UIManager EKSİK");
            sb.AppendLine(mt  != null ? "✓ MovementTracker"  : "✗ MovementTracker EKSİK");
            sb.AppendLine(hud != null ? "✓ HUDController"    : "✗ HUDController EKSİK");

            if (mt != null && FindTransformByName("RightHand Controller") == null)
                sb.AppendLine("\n⚠ 'RightHand Controller' transform bulunamadı — XR Rig'de manuel atayın.");
            if (mt != null && FindTransformByName("ShoulderAnchor") == null)
                sb.AppendLine("⚠ 'ShoulderAnchor' transform bulunamadı — boş bir GameObject oluşturup atayın.");

            return sb.ToString();
        }

        #endregion
    }
}
