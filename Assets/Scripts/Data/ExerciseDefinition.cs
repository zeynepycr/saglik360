// ============================================================
//  Sağlık360 – ExerciseDefinition.cs  |  Data
// ============================================================
//  ScriptableObject that defines one physiotherapy exercise.
//  Create assets via:
//    Assets → Create → Saglik360 → Exercise Definition
//
//  Each exercise asset configures:
//    • Animation clip reference
//    • Therapeutic ROM targets (angles)
//    • Rep / set counts
//    • Difficulty level
//    • Body part targeted
// ============================================================

using UnityEngine;

namespace Saglik360.Data
{
    public enum BodyPart
    {
        Shoulder,
        Elbow,
        Wrist,
        Hand,
        FullArm
    }

    public enum DifficultyLevel
    {
        Başlangıç  = 0,   // Beginner
        Orta       = 1,   // Intermediate
        İleri      = 2    // Advanced
    }

    [CreateAssetMenu(
        fileName = "NewExercise",
        menuName  = "Saglik360/Exercise Definition")]
    public class ExerciseDefinition : ScriptableObject
    {
        // ── Identity ─────────────────────────────────────────
        [Header("Kimlik / Identity")]
        [Tooltip("Türkçe egzersiz adı (UI'da gösterilir).")]
        public string ExerciseName = "Omuz Fleksiyonu";

        [TextArea(2, 4)]
        [Tooltip("Kısa açıklama – başlangıç ekranında gösterilir.")]
        public string Description  = "Kolu öne doğru yavaşça kaldırın.";

        public Sprite ThumbnailSprite;

        // ── Anatomy ──────────────────────────────────────────
        [Header("Anatomi / Anatomy")]
        public BodyPart      TargetBodyPart  = BodyPart.Shoulder;
        public DifficultyLevel Difficulty    = DifficultyLevel.Başlangıç;

        // ── Therapeutic Parameters ────────────────────────────
        [Header("Terapötik Parametreler")]
        [Tooltip("Eklem hareket başlangıç açısı (derece).")]
        public float StartAngleDeg   =   0f;

        [Tooltip("Hedef eklem hareket açısı (derece).")]
        public float TargetAngleDeg  = 180f;

        [Tooltip("Kabul edilen tolerans (±derece).")]
        public float ToleranceDeg    =   5f;

        // ── Rep / Set Config ──────────────────────────────────
        [Header("Tekrar / Set")]
        [Min(1)] public int TargetReps  = 10;
        [Min(1)] public int TargetSets  =  3;
        [Tooltip("Her tekrar arası dinlenme süresi (saniye).")]
        public float RestBetweenRepsSec = 1.5f;
        [Tooltip("Her set arası dinlenme süresi (saniye).")]
        public float RestBetweenSetsSec = 30f;

        // ── Animation ────────────────────────────────────────
        [Header("Animasyon")]
        [Tooltip("Referans animasyon klibi (Blender'dan export edilmiş FBX).")]
        public AnimationClip ReferenceAnimation;
        [Tooltip("Oynatım hızı çarpanı (1 = normal).")]
        [Range(0.25f, 2f)]
        public float AnimationSpeed = 1f;

        // ── Scoring ──────────────────────────────────────────
        [Header("Puanlama")]
        [Tooltip("Mükemmel tekrar için baz puan.")]
        public int BasePointsPerRep = 10;
        [Tooltip("Bu egzersizi tamamlama bonusu.")]
        public int CompletionBonus  = 50;

        // ── Instructions ─────────────────────────────────────
        [Header("Talimatlar")]
        [TextArea(3, 6)]
        [Tooltip("Sesli/yazılı talimat metni.")]
        public string Instructions =
            "Kolunuzu yavaşça öne kaldırın. Omuz hizasına gelince 2 saniye tutun ve indirin.";

        [Tooltip("Opsiyonel uyarı mesajı (örn. ağrı durumunda).")]
        [TextArea(2, 3)]
        public string SafetyNote = "Ağrı hissederseniz egzersizi durdurun.";

        // ────────────────────────────────────────────────────
        // Computed Helpers
        // ────────────────────────────────────────────────────

        /// <summary>Total ROM range this exercise expects.</summary>
        public float TotalROMDeg => Mathf.Abs(TargetAngleDeg - StartAngleDeg);

        /// <summary>Total reps across all sets.</summary>
        public int TotalReps => TargetReps * TargetSets;
    }
}
