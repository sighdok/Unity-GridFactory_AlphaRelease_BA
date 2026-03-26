using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GridFactory.Core;

namespace GridFactory.UI
{
    public class GridPickerRowUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text metaText;
        [SerializeField] private Button pickButton;
        [SerializeField] private GridPreviewUI preview;

        public void Bind(GridDefinitionOwned def, System.Action onPick)
        {
            if (def == null) return;

            if (titleText) titleText.text = def.displayName;
            if (metaText) metaText.text = $"{def.width} x {def.height}  |  Free: {def.FreeCells}";

            if (preview != null)
                preview.Render(def.width, def.height, (x, y) => def.IsLocked(new Vector2Int(x, y)));

            if (pickButton != null)
            {
                pickButton.onClick.RemoveAllListeners();
                pickButton.onClick.AddListener(() => onPick?.Invoke());
                pickButton.onClick.AddListener(AudioManager.Instance.PlayButtonClickSFX);

            }
        }
    }
}
