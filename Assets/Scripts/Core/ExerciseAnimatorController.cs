// ============================================================
//  Sağlık360 – ExerciseAnimatorController.cs  |  Core  v3
//  Simple polling version — no events, no timing issues.
// ============================================================

using UnityEngine;
using Saglik360.Core;
using Saglik360.Data;

namespace Saglik360
{
    public class ExerciseAnimatorController : MonoBehaviour
    {
        [Header("Character Setup")]
        public Animator CharacterAnimator;

        [Header("Position")]
        public float DistanceFromPlayer = 2.0f;
        public float SideOffset         = 1.5f; // UI'ı kapatmaması için daha sağa alındı

        private ExerciseDefinition _lastExercise;
        private Transform          _cam;
        private Renderer[]         _renderers;

        private void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            _cam       = Camera.main?.transform;
            
            Debug.Log($"[ExerciseAnimatorController] Awake: Found {_renderers.Length} renderers, Camera: {(_cam != null ? "Found" : "NOT FOUND")}");
            
            // Log all renderers found
            for (int i = 0; i < _renderers.Length; i++)
                Debug.Log($"  - Renderer {i}: {_renderers[i].gameObject.name}");
            
            //SetVisible(false);
            
        }

        private void Update()
        {
            if (GameManager.Instance == null) return;

            AppState state = GameManager.Instance.CurrentState;

            if (state == AppState.ExerciseActive)
            {
                var exMgr = FindObjectOfType<ExerciseManager>();
                if (exMgr == null || exMgr.CurrentExercise == null) return;

                ExerciseDefinition ex = exMgr.CurrentExercise;

                if (ex != _lastExercise)
                {
                    _lastExercise = ex;
                    Debug.Log($"[ExerciseAnimatorController] Starting exercise: {ex.ExerciseName}");
                    PlaceInFrontOfPlayer();
                    SetVisible(true);
                    PlayClip(ex);
                    Debug.Log($"[ExerciseAnimatorController] Character positioned at: {transform.position}, Scale: {transform.localScale}");
                }
            }
            else
            {
                if (_lastExercise != null)
                {
                    _lastExercise = null;
                    Debug.Log("[ExerciseAnimatorController] Exercise ended, hiding character");
                    SetVisible(false);
                }
            }
        }

        private void PlaceInFrontOfPlayer()
        {
            if (_cam == null)
            {
                _cam = Camera.main?.transform;
                if (_cam == null)
                {
                    Debug.LogError("[ExerciseAnimatorController] Main camera not found!");
                    return;
                }
            }

            // FIX: Reset scale if it's abnormally large (common Unity issue)
            if (transform.localScale.magnitude > 10f)
            {
                Debug.LogWarning($"[ExerciseAnimatorController] WARNING: Character scale was {transform.localScale}, resetting to (1,1,1)!");
                transform.localScale = Vector3.one;
            }

            // Get camera forward direction (ignore Y to keep character on ground plane)
            Vector3 forward = _cam.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.forward;
            }
            forward.Normalize();

            // Calculate position in front of camera
            Vector3 right     = Vector3.Cross(Vector3.up, forward);
            Vector3 targetPos = _cam.position
                              + forward * DistanceFromPlayer
                              + right   * SideOffset;  // Changed from minus to plus for clarity
            targetPos.y = 0f;

            // Apply position
            transform.position = targetPos;

            // Rotate character to face camera
            Vector3 lookDir = _cam.position - transform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }
            
            Debug.Log($"[ExerciseAnimatorController] Positioned at: {targetPos}, Camera: {_cam.position}, Forward: {forward}, Final Scale: {transform.localScale}");
        }

        private void PlayClip(ExerciseDefinition ex)
        {
            if (CharacterAnimator == null)
            {
                Debug.LogError("[ExerciseAnimatorController] CharacterAnimator atanmamış!");
                return;
            }
            if (ex.ReferenceAnimation == null)
            {
                Debug.LogWarning($"[ExerciseAnimatorController] '{ex.ExerciseName}' için animasyon yok.");
                return;
            }
            CharacterAnimator.speed = 0.7f;
            CharacterAnimator.Play(ex.ReferenceAnimation.name, 0, 0f);
            Debug.Log($"[ExerciseAnimatorController] Oynatılıyor: {ex.ReferenceAnimation.name}");
        }

        private void SetVisible(bool visible)
        {
            if (_renderers == null || _renderers.Length == 0)
            {
                Debug.LogWarning("[ExerciseAnimatorController] No renderers found to toggle visibility!");
                _renderers = GetComponentsInChildren<Renderer>();
            }
            
            foreach (var r in _renderers)
            {
                if (r != null)
                {
                    r.enabled = visible;
                    Debug.Log($"  - {r.gameObject.name} renderer: {(visible ? "ENABLED" : "DISABLED")}");
                }
            }
            
            Debug.Log($"[ExerciseAnimatorController] SetVisible({visible}) - {_renderers.Length} renderers");
        }
    }
}