// ============================================================
//  Sağlık360 – ExerciseCardButton.cs  |  UI
// ============================================================
//  Attach this to each exercise card GameObject.
//  Holds a reference to an ExerciseDefinition asset and
//  calls UIManager.OnExerciseSelected() when clicked.
//
//  The card's Button.OnClick simply calls: CardClicked()
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using Saglik360.Data;

namespace Saglik360.UI
{
    public class ExerciseCardButton : MonoBehaviour
    {
        [Header("Assign the matching Exercise asset here")]
        public ExerciseDefinition Exercise;

        private void Start()
        {
            // Auto-wire the button click
            Button btn = GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(CardClicked);
            else
                Debug.LogWarning($"[ExerciseCardButton] {gameObject.name} üzerinde Button bileşeni bulunamadı.");
        }

        public void CardClicked()
        {
            if (Exercise == null)
            {
                Debug.LogError($"[ExerciseCardButton] {gameObject.name} için Exercise atanmamış!");
                return;
            }

            UIManager uiMgr = FindObjectOfType<UIManager>();
            if (uiMgr != null)
                uiMgr.OnExerciseSelected(Exercise);
            else
                Debug.LogError("[ExerciseCardButton] UIManager sahnede bulunamadı.");
        }
    }
}
