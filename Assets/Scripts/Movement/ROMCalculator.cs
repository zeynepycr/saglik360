// ============================================================
//  Sağlık360 – ROMCalculator.cs  |  Movement
// ============================================================
//  Pure static math library for joint angle computation.
//  No MonoBehaviour – call from MovementTracker.
//
//  Coordinate assumptions (Unity world-space, Y-up):
//    - Shoulder is the origin of the arm segment.
//    - Flexion/Extension is measured in the sagittal plane (XY).
//    - Abduction/Adduction is measured in the frontal plane (YZ).
//    - Rotation is measured from forward reference.
// ============================================================

using UnityEngine;

namespace Saglik360.Movement
{
    public static class ROMCalculator
    {
        // ────────────────────────────────────────────────────
        #region Shoulder
        // ────────────────────────────────────────────────────

        /// <summary>
        /// Computes shoulder FLEXION angle (sagittal plane, 0-180°).
        /// Angle between the resting arm direction (down) and current arm vector.
        /// </summary>
        public static float ComputeShoulderFlexionAngle(Vector3 shoulderPos, Vector3 controllerPos, Transform referenceTransform = null)
        {
            Vector3 armVec = controllerPos - shoulderPos;
            
            if (referenceTransform != null)
            {
                // Kameraya (kullanıcıya) göre lokal vektöre çevir ki kullanıcı odada nereye bakarsa baksın doğru hesaplansın.
                armVec = referenceTransform.InverseTransformDirection(armVec);
            }

            // Sagittal plane (YZ düzlemi). X'i (sağa/sola açılmayı) yoksayıyoruz.
            Vector3 sagittal = new Vector3(0f, armVec.y, armVec.z).normalized;
            return Vector3.Angle(Vector3.down, sagittal);
        }

        /// <summary>
        /// Computes shoulder ABDUCTION angle (frontal plane, 0-180°).
        /// </summary>
        public static float ComputeShoulderAbductionAngle(Vector3 shoulderPos, Vector3 controllerPos, Transform referenceTransform = null)
        {
            Vector3 armVec = controllerPos - shoulderPos;
            
            if (referenceTransform != null)
            {
                armVec = referenceTransform.InverseTransformDirection(armVec);
            }

            // Frontal plane (XY düzlemi). Z'yi (öne/arkaya gitmeyi) yoksayıyoruz.
            Vector3 frontal = new Vector3(armVec.x, armVec.y, 0f).normalized;
            return Vector3.Angle(Vector3.down, frontal);
        }

        /// <summary>
        /// Computes shoulder ROTATION angle using controller orientation.
        /// </summary>
        /// <param name="controllerRot">Quaternion rotation of the controller.</param>
        /// <param name="referenceForward">The forward direction at rest (usually Vector3.forward).</param>
        public static float ComputeShoulderRotationAngle(Quaternion controllerRot, Vector3 referenceForward)
        {
            Vector3 controllerForward = controllerRot * Vector3.forward;
            // Project onto horizontal plane (remove Y)
            Vector3 projRef  = Vector3.ProjectOnPlane(referenceForward,  Vector3.up).normalized;
            Vector3 projCtrl = Vector3.ProjectOnPlane(controllerForward, Vector3.up).normalized;
            float angle = Vector3.SignedAngle(projRef, projCtrl, Vector3.up);
            return angle;
        }

        #endregion

        // ────────────────────────────────────────────────────
        #region Elbow
        // ────────────────────────────────────────────────────

        /// <summary>
        /// Computes elbow FLEXION angle (0 = fully extended, 145 = full flex).
        /// Requires three joints: shoulder, elbow, wrist.
        /// </summary>
        public static float ComputeElbowFlexionAngle(
            Vector3 shoulderPos,
            Vector3 elbowPos,
            Vector3 wristPos)
        {
            Vector3 upperArm = (elbowPos - shoulderPos).normalized;
            Vector3 forearm  = (wristPos  - elbowPos).normalized;
            // 0° = fully extended (vectors align), 145° = full flex
            return 180f - Vector3.Angle(upperArm, forearm);
        }

        #endregion

        // ────────────────────────────────────────────────────
        #region Hand / Finger (pinch proxy)
        // ────────────────────────────────────────────────────

        /// <summary>
        /// Uses Quest 2 trigger value as a proxy for pinch / grip ROM.
        /// Returns 0 (open) to 100 (full closure).
        /// </summary>
        /// <param name="triggerValue">Raw trigger float from OVRInput (0-1).</param>
        public static float TriggerToGripPercent(float triggerValue)
        {
            return Mathf.Clamp01(triggerValue) * 100f;
        }

        #endregion

        // ────────────────────────────────────────────────────
        #region Accuracy Scoring
        // ────────────────────────────────────────────────────

        /// <summary>
        /// Converts a deviation (degrees) to an accuracy score (0-100%).
        /// Perfect = 0° deviation → 100. Beyond 2x tolerance → 0.
        /// Uses a linear falloff inside the tolerance window and a cosine
        /// taper outside to give partial credit.
        /// </summary>
        /// <param name="deviationDeg">Absolute angular error in degrees.</param>
        /// <param name="toleranceDeg">Tolerance window (from ExerciseDefinition).</param>
        public static float DeviationToAccuracy(float deviationDeg, float toleranceDeg)
        {
            if (deviationDeg <= 0f) return 100f;

            float ratio = deviationDeg / (toleranceDeg * 2f);   // 2x tolerance = 0%
            return Mathf.Clamp01(1f - ratio) * 100f;
        }

        /// <summary>
        /// Classify accuracy into qualitative feedback tiers.
        /// </summary>
        public static string AccuracyLabel(float accuracyPercent)
        {
            return accuracyPercent switch
            {
                >= 90f => "Mükemmel! 🏆",
                >= 75f => "Çok İyi! ⭐",
                >= 60f => "İyi 👍",
                >= 40f => "Gelişiyor 💪",
                _      => "Tekrar Dene 🔄"
            };
        }

        #endregion
    }
}
