// ============================================================
//  Sağlık360 – AnimatorSetup.cs  |  Editor Only
// ============================================================
//  Tools → Saglik360 → Create Exercise Animator Controller
//
//  Creates an AnimatorController asset with one state per
//  exercise animation clip. Reads clips from your
//  ExerciseDefinition assets in Resources/Exercises.
//
//  After running this:
//    1. Assign the generated controller to your character's
//       Animator component
//    2. The ExerciseAnimatorController script handles
//       switching states at runtime
// ============================================================

using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using Saglik360.Data;

namespace Saglik360.Editor
{
    public static class AnimatorSetup
    {
        private const string OutputPath = "Assets/Animations/ExerciseAnimatorController.controller";

        [MenuItem("Tools/Saglik360/Create Exercise Animator Controller")]
        public static void CreateAnimatorController()
        {
            // Load all exercise assets
            ExerciseDefinition[] exercises =
                Resources.LoadAll<ExerciseDefinition>("Exercises");

            if (exercises.Length == 0)
            {
                EditorUtility.DisplayDialog("Hata",
                    "Resources/Exercises klasöründe ExerciseDefinition bulunamadı.\n" +
                    "Önce 'Create All Exercise Assets' menüsünü çalıştırın.",
                    "Tamam");
                return;
            }

            // Ensure Animations folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");

            // Create the controller
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(OutputPath);
            AnimatorStateMachine rootSM   = controller.layers[0].stateMachine;

            int clipsAdded = 0;
            int clipsMissing = 0;

            foreach (var exercise in exercises)
            {
                if (exercise.ReferenceAnimation == null)
                {
                    Debug.LogWarning($"[AnimatorSetup] '{exercise.ExerciseName}' için animasyon klibi atanmamış — atlandı.");
                    clipsMissing++;
                    continue;
                }

                // Create a state for this exercise
                AnimatorState state = rootSM.AddState(exercise.ReferenceAnimation.name);
                state.motion = exercise.ReferenceAnimation;
                state.speed  = 0.7f; // demo speed

                clipsAdded++;
                Debug.Log($"[AnimatorSetup] State eklendi: {exercise.ReferenceAnimation.name}");
            }

            // Set the first state as default
            if (rootSM.states.Length > 0)
                rootSM.defaultState = rootSM.states[0].state;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Select it in the Project window
            Selection.activeObject = controller;

            string message = $"{clipsAdded} animasyon state'i oluşturuldu.\n";
            if (clipsMissing > 0)
                message += $"{clipsMissing} egzersizde animasyon klibi eksik.\n\n" +
                           "Eksik klipleri ExerciseDefinition asset'lerinde 'Reference Animation' alanına atayın.";
            else
                message += "\nTüm animasyonlar başarıyla eklendi!";

            message += $"\n\nKonum: {OutputPath}";

            EditorUtility.DisplayDialog("Animator Controller Oluşturuldu", message, "Tamam");
        }
    }
}
