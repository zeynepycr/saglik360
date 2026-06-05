// ============================================================
//  Sağlık360 – ExerciseCardWirer.cs  |  Editor Only
// ============================================================
//  Tools → Saglik360 → Wire Exercise Cards
//
//  Finds all ExCard_0 … ExCard_8 GameObjects in the scene,
//  adds ExerciseCardButton to each one, and assigns the
//  matching ExerciseDefinition asset from Resources/Exercises.
// ============================================================

using UnityEngine;
using UnityEditor;
using Saglik360.UI;
using Saglik360.Data;

namespace Saglik360.Editor
{
    public static class ExerciseCardWirer
    {
        // Must match the order cards were created in UIAutoBuilder
        private static readonly string[] ExerciseNames =
        {
            "Omuz Fleksiyonu",
            "Omuz Ekstansiyonu",
            "Omuz Abduksiyonu",
            "Omuz Adduksiyonu",
            "Dairesel Hareket",
            "Uzanma ve Hedefleme",
            "Pinch Hareketi",
            "Yumruk Yapma",
            "El İşaretleri"
        };

        [MenuItem("Tools/Saglik360/Wire Exercise Cards")]
        public static void WireCards()
        {
            // Load all ExerciseDefinition assets
            ExerciseDefinition[] assets =
                Resources.LoadAll<ExerciseDefinition>("Exercises");

            if (assets.Length == 0)
            {
                EditorUtility.DisplayDialog("Hata",
                    "Resources/Exercises klasöründe ExerciseDefinition asset bulunamadı.\n\n" +
                    "Önce 'Create All Exercise Assets' menüsünü çalıştırın.",
                    "Tamam");
                return;
            }

            int wired = 0;
            int missing = 0;

            for (int i = 0; i < 9; i++)
            {
                string cardName = $"ExCard_{i}";
                GameObject cardGO = FindInScene(cardName);

                if (cardGO == null)
                {
                    Debug.LogWarning($"[ExerciseCardWirer] '{cardName}' sahnede bulunamadı — atlandı.");
                    missing++;
                    continue;
                }

                // Add or get ExerciseCardButton
                ExerciseCardButton btn = cardGO.GetComponent<ExerciseCardButton>();
                if (btn == null)
                    btn = cardGO.AddComponent<ExerciseCardButton>();

                // Find matching asset by name
                string targetName = ExerciseNames[i];
                ExerciseDefinition match = null;

                foreach (var asset in assets)
                {
                    if (asset.ExerciseName == targetName)
                    {
                        match = asset;
                        break;
                    }
                }

                if (match != null)
                {
                    btn.Exercise = match;
                    EditorUtility.SetDirty(cardGO);
                    wired++;
                    Debug.Log($"[ExerciseCardWirer] {cardName} → {match.ExerciseName}");
                }
                else
                {
                    Debug.LogWarning($"[ExerciseCardWirer] '{targetName}' için asset bulunamadı.");
                    missing++;
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();

            EditorUtility.DisplayDialog("Tamamlandı",
                $"{wired} kart başarıyla bağlandı.\n" +
                $"{missing} kart bulunamadı veya asset eksik.\n\n" +
                "Artık egzersiz kartlarına tıklamak çalışacak!",
                "Tamam");
        }

        private static GameObject FindInScene(string name)
        {
            foreach (var t in Object.FindObjectsOfType<Transform>(true))
                if (t.gameObject.name == name) return t.gameObject;
            return null;
        }
    }
}
