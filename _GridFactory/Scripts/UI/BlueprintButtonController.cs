
using UnityEngine;
using UnityEngine.UI;

using TMPro;

using GridFactory.Blueprints;
using GridFactory.Core;
using GridFactory.UI;

public class BlueprintButtonController : MonoBehaviour
{
    private static AudioManager AM => AudioManager.Instance;

    [SerializeField] private Button loadBtn;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button overwriteButton;
    [SerializeField] private TMP_Text titleText;

    private BlueprintUI blueprintUI;

    public void Init(BlueprintDefinition bp)
    {
        blueprintUI = GetComponentInParent<BlueprintUI>();

        titleText.SetText(bp.displayName);

        loadBtn.onClick.AddListener(() =>
        {
            blueprintUI.OnLoadClicked(bp);
        });

        deleteButton.onClick.AddListener(() =>
        {
            blueprintUI.OnDeleteClicked(bp);
        });

        overwriteButton.onClick.AddListener(() =>
        {
            blueprintUI.OnOverwriteClicked(bp);
        });

        loadBtn.onClick.AddListener(AM.PlayButtonClickSFX);
        deleteButton.onClick.AddListener(AM.PlayButtonClickSFX);
        overwriteButton.onClick.AddListener(AM.PlayButtonClickSFX);
    }
}
