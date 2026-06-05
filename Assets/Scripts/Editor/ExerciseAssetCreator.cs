// ============================================================
//  Sağlık360 – ExerciseAssetCreator.cs  |  Editor Only
// ============================================================
//  Adds a menu item:
//    Tools → Saglik360 → Create All Exercise Assets
//
//  One click creates all 9 ExerciseDefinition .asset files
//  inside  Assets/Resources/Exercises/
//  with pre-filled therapeutic values from your report.
//
//  ⚠️  This file MUST live in an "Editor" folder:
//      Assets/Scripts/Editor/ExerciseAssetCreator.cs
// ============================================================

using UnityEngine;
using UnityEditor;
using System.IO;

namespace Saglik360.Editor
{
    public static class ExerciseAssetCreator
    {
        private const string OutputFolder = "Assets/Resources/Exercises";

        // ── Menu entry ────────────────────────────────────────
        [MenuItem("Tools/Saglik360/Create All Exercise Assets")]
        public static void CreateAll()
        {
            EnsureFolder(OutputFolder);

            // ── Define all 9 exercises from your Blender animations ──
            var definitions = new ExerciseSetup[]
            {
                // ── 1. Omuz Fleksiyonu (Shoulder Flexion) ────────────
                new ExerciseSetup
                {
                    FileName        = "OmuzFleksiyonu",
                    ExerciseName    = "Omuz Fleksiyonu",
                    Description     = "Kolu öne doğru yavaşça kaldırın.",
                    TargetBodyPart  = Saglik360.Data.BodyPart.Shoulder,
                    Difficulty      = Saglik360.Data.DifficultyLevel.Başlangıç,
                    StartAngleDeg   = 0f,
                    TargetAngleDeg  = 180f,
                    ToleranceDeg    = 5f,
                    TargetReps      = 10,
                    TargetSets      = 3,
                    RestBetweenReps = 1.5f,
                    RestBetweenSets = 30f,
                    BasePoints      = 10,
                    Bonus           = 50,
                    Instructions    = "Kolunuzu yavaşça öne kaldırın. Omuz hizasına gelince 2 saniye tutun ve indirin.",
                    SafetyNote      = "Ağrı hissederseniz hareketi durdurun. Donuk omuz durumunda zorlamayın."
                },

                // ── 2. Omuz Ekstansiyonu (Shoulder Extension) ────────
                new ExerciseSetup
                {
                    FileName        = "OmuzEkstansiyonu",
                    ExerciseName    = "Omuz Ekstansiyonu",
                    Description     = "Kolu arkaya doğru kontrollü şekilde açın.",
                    TargetBodyPart  = Saglik360.Data.BodyPart.Shoulder,
                    Difficulty      = Saglik360.Data.DifficultyLevel.Başlangıç,
                    StartAngleDeg   = 0f,
                    TargetAngleDeg  = 60f,
                    ToleranceDeg    = 5f,
                    TargetReps      = 10,
                    TargetSets      = 3,
                    RestBetweenReps = 1.5f,
                    RestBetweenSets = 30f,
                    BasePoints      = 10,
                    Bonus           = 50,
                    Instructions    = "Gövdeyi sabit tutarak kolu yavaşça arkaya açın. Hedef açıya ulaşınca 1 saniye tutun.",
                    SafetyNote      = "Bel veya omuz ağrısında durun."
                },

                // ── 3. Omuz Abduksiyonu (Shoulder Abduction) ─────────
                new ExerciseSetup
                {
                    FileName        = "OmuzAbduksiyonu",
                    ExerciseName    = "Omuz Abduksiyonu",
                    Description     = "Kolu yana doğru kaldırın.",
                    TargetBodyPart  = Saglik360.Data.BodyPart.Shoulder,
                    Difficulty      = Saglik360.Data.DifficultyLevel.Başlangıç,
                    StartAngleDeg   = 0f,
                    TargetAngleDeg  = 180f,
                    ToleranceDeg    = 5f,
                    TargetReps      = 10,
                    TargetSets      = 3,
                    RestBetweenReps = 1.5f,
                    RestBetweenSets = 30f,
                    BasePoints      = 10,
                    Bonus           = 50,
                    Instructions    = "Kolu yavaşça yana açın. Baş parmak yukarıya baksın. Hedef açıya ulaşınca indirin.",
                    SafetyNote      = "Omuz üzerinden zorlamayın; ağrıda durun."
                },

                // ── 4. Omuz Adduksiyonu (Shoulder Adduction) ─────────
                new ExerciseSetup
                {
                    FileName        = "OmuzAdduksiyonu",
                    ExerciseName    = "Omuz Adduksiyonu",
                    Description     = "Açık kolu gövdeye doğru kontrollü kapatın.",
                    TargetBodyPart  = Saglik360.Data.BodyPart.Shoulder,
                    Difficulty      = Saglik360.Data.DifficultyLevel.Başlangıç,
                    StartAngleDeg   = 90f,
                    TargetAngleDeg  = 10f,
                    ToleranceDeg    = 5f,
                    TargetReps      = 10,
                    TargetSets      = 3,
                    RestBetweenReps = 1.5f,
                    RestBetweenSets = 30f,
                    BasePoints      = 10,
                    Bonus           = 50,
                    Instructions    = "Kolunuzu yana açık başlatın. Yavaşça gövdeye doğru kapatın, dirsek bükülmeden.",
                    SafetyNote      = "Hareketi zorlamayın."
                },

                // ── 5. Dairesel Hareket / Teker Çevirme ──────────────
                new ExerciseSetup
                {
                    FileName        = "DaireselHareket",
                    ExerciseName    = "Dairesel Hareket",
                    Description     = "Kolu dairesel olarak döndürün (sirkumdüksiyon).",
                    TargetBodyPart  = Saglik360.Data.BodyPart.Shoulder,
                    Difficulty      = Saglik360.Data.DifficultyLevel.Orta,
                    StartAngleDeg   = 0f,
                    TargetAngleDeg  = 360f,
                    ToleranceDeg    = 15f,
                    TargetReps      = 8,
                    TargetSets      = 3,
                    RestBetweenReps = 2f,
                    RestBetweenSets = 40f,
                    BasePoints      = 15,
                    Bonus           = 60,
                    Instructions    = "Kolunuzu büyük daireler çizerek döndürün. İlk 4 tekrar saat yönünde, sonraki 4 ters yönde.",
                    SafetyNote      = "Başlangıçta küçük daireler yapın, kademeli olarak büyütün."
                },

                // ── 6. Uzanma ve Hedefleme ────────────────────────────
                new ExerciseSetup
                {
                    FileName        = "UzanmaHedefleme",
                    ExerciseName    = "Uzanma ve Hedefleme",
                    Description     = "Farklı yönlerdeki hedeflere kontrollü uzanın.",
                    TargetBodyPart  = Saglik360.Data.BodyPart.FullArm,
                    Difficulty      = Saglik360.Data.DifficultyLevel.Orta,
                    StartAngleDeg   = 0f,
                    TargetAngleDeg  = 120f,
                    ToleranceDeg    = 10f,
                    TargetReps      = 8,
                    TargetSets      = 3,
                    RestBetweenReps = 2f,
                    RestBetweenSets = 35f,
                    BasePoints      = 15,
                    Bonus           = 70,
                    Instructions    = "VR ortamındaki hedefe doğru kolunuzu uzatın. Parmak uçlarınız hedefe değince geri alın.",
                    SafetyNote      = "Gövdenizi döndürmeyin, sadece kol hareketine odaklanın."
                },

                // ── 7. Pinch Hareketi ─────────────────────────────────
                new ExerciseSetup
                {
                    FileName        = "PinchHareketi",
                    ExerciseName    = "Pinch Hareketi",
                    Description     = "Başparmak ve işaret parmağıyla tutma egzersizi.",
                    TargetBodyPart  = Saglik360.Data.BodyPart.Hand,
                    Difficulty      = Saglik360.Data.DifficultyLevel.Orta,
                    StartAngleDeg   = 0f,
                    TargetAngleDeg  = 100f,   // trigger proxy: 100% = full pinch
                    ToleranceDeg    = 8f,
                    TargetReps      = 15,
                    TargetSets      = 2,
                    RestBetweenReps = 1f,
                    RestBetweenSets = 25f,
                    BasePoints      = 8,
                    Bonus           = 40,
                    Instructions    = "Başparmak ucu ile işaret parmağı ucunu birleştirin. Sıkıca tutun, sayın, bırakın.",
                    SafetyNote      = "Artrit durumunda zorlamayın. Ağrıda durun."
                },

                // ── 8. Yumruk Yapma (Fist) ────────────────────────────
                new ExerciseSetup
                {
                    FileName        = "YumrukYapma",
                    ExerciseName    = "Yumruk Yapma",
                    Description     = "Eli yavaşça yumruk yapın ve açın.",
                    TargetBodyPart  = Saglik360.Data.BodyPart.Hand,
                    Difficulty      = Saglik360.Data.DifficultyLevel.Başlangıç,
                    StartAngleDeg   = 0f,
                    TargetAngleDeg  = 100f,   // trigger proxy: 100% = full fist
                    ToleranceDeg    = 8f,
                    TargetReps      = 12,
                    TargetSets      = 3,
                    RestBetweenReps = 1f,
                    RestBetweenSets = 25f,
                    BasePoints      = 8,
                    Bonus           = 45,
                    Instructions    = "Ellerinizi açık başlatın. Tüm parmakları avuç içine doğru yavaşça kapatın, 2 saniye tutun, açın.",
                    SafetyNote      = "Tendon yaralanmasında tam kapama yapmayın."
                },

                // ── 9. El İşaretleri (Hand Gestures) ─────────────────
                new ExerciseSetup
                {
                    FileName        = "ElIsaretleri",
                    ExerciseName    = "El İşaretleri",
                    Description     = "Farklı parmak kombinasyonlarını yapın.",
                    TargetBodyPart  = Saglik360.Data.BodyPart.Hand,
                    Difficulty      = Saglik360.Data.DifficultyLevel.Orta,
                    StartAngleDeg   = 0f,
                    TargetAngleDeg  = 100f,
                    ToleranceDeg    = 12f,
                    TargetReps      = 12,
                    TargetSets      = 2,
                    RestBetweenReps = 1.5f,
                    RestBetweenSets = 30f,
                    BasePoints      = 12,
                    Bonus           = 55,
                    Instructions    = "VR'daki sembole bakın. İşaret parmağı kaldırma, V işareti veya OK işaretini yapın.",
                    SafetyNote      = "Nörolojik rehabilitasyonda zorlamayın — kısmi hareket de kabul edilir."
                },
            };

            int created = 0;
            foreach (var def in definitions)
            {
                string path = $"{OutputFolder}/{def.FileName}.asset";

                // Skip if already exists to avoid overwrite
                if (File.Exists(path))
                {
                    Debug.Log($"[ExerciseAssetCreator] Zaten mevcut, atlandı: {path}");
                    continue;
                }

                var asset = ScriptableObject.CreateInstance<Saglik360.Data.ExerciseDefinition>();
                ApplySetup(asset, def);
                AssetDatabase.CreateAsset(asset, path);
                created++;
                Debug.Log($"[ExerciseAssetCreator] Oluşturuldu: {path}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Saglik360 — Egzersiz Assetleri",
                $"{created} yeni asset oluşturuldu.\n{definitions.Length - created} mevcut asset atlandı.\n\nKonum: {OutputFolder}",
                "Tamam");
        }

        // ── Force-recreate (overwrite) ────────────────────────
        [MenuItem("Tools/Saglik360/Recreate All Exercise Assets (Overwrite)")]
        private static void RecreateAll()
        {
            if (!EditorUtility.DisplayDialog(
                    "Tüm Egzersiz Assetlerini Yeniden Oluştur",
                    "Mevcut asset'lar silinip yeniden oluşturulacak. Devam edilsin mi?",
                    "Evet, Devam Et", "İptal"))
                return;

            // Delete existing
            if (AssetDatabase.IsValidFolder(OutputFolder))
            {
                string[] guids = AssetDatabase.FindAssets("t:ExerciseDefinition", new[] { OutputFolder });
                foreach (string guid in guids)
                    AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));
            }

            CreateAll();
        }

        // ────────────────────────────────────────────────────
        #region Helpers
        // ────────────────────────────────────────────────────

        private static void ApplySetup(Saglik360.Data.ExerciseDefinition asset, ExerciseSetup s)
        {
            asset.ExerciseName      = s.ExerciseName;
            asset.Description       = s.Description;
            asset.TargetBodyPart    = s.TargetBodyPart;
            asset.Difficulty        = s.Difficulty;
            asset.StartAngleDeg     = s.StartAngleDeg;
            asset.TargetAngleDeg    = s.TargetAngleDeg;
            asset.ToleranceDeg      = s.ToleranceDeg;
            asset.TargetReps        = s.TargetReps;
            asset.TargetSets        = s.TargetSets;
            asset.RestBetweenRepsSec = s.RestBetweenReps;
            asset.RestBetweenSetsSec = s.RestBetweenSets;
            asset.BasePointsPerRep  = s.BasePoints;
            asset.CompletionBonus   = s.Bonus;
            asset.Instructions      = s.Instructions;
            asset.SafetyNote        = s.SafetyNote;
            // AnimationClip & ThumbnailSprite left null — assign manually after import
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace('\\', '/');
                string folder = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        #endregion

        // ── Inner data struct (no MonoBehaviour needed) ───────
        private struct ExerciseSetup
        {
            public string                          FileName;
            public string                          ExerciseName;
            public string                          Description;
            public Saglik360.Data.BodyPart         TargetBodyPart;
            public Saglik360.Data.DifficultyLevel  Difficulty;
            public float         StartAngleDeg;
            public float         TargetAngleDeg;
            public float         ToleranceDeg;
            public int           TargetReps;
            public int           TargetSets;
            public float         RestBetweenReps;
            public float         RestBetweenSets;
            public int           BasePoints;
            public int           Bonus;
            public string        Instructions;
            public string        SafetyNote;
        }
    }
}
