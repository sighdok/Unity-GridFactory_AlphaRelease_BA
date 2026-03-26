
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

using TMPro;

using GridFactory.Blueprints;
using GridFactory.Meta;
using GridFactory.Core;
using GridFactory.UI;

public class BlueprintButtonController : MonoBehaviour
{
    public Button loadBtn;
    public Button deleteButton;
    public Button overwriteButton;
    public TMP_Text titleText;



    private BlueprintUI blueprintUI;


    public void Init(BlueprintDefinition bp)
    {
        blueprintUI = GetComponentInParent<BlueprintUI>();

        if (titleText != null)
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

        loadBtn.onClick.AddListener(AudioManager.Instance.PlayButtonClickSFX);
        deleteButton.onClick.AddListener(AudioManager.Instance.PlayButtonClickSFX);
        overwriteButton.onClick.AddListener(AudioManager.Instance.PlayButtonClickSFX);
    }
}
