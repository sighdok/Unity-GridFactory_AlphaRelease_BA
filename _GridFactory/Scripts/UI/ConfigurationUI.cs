using System;
using UnityEngine;
using UnityEngine.UI;

using TMPro;
using System.Collections;
using System.Collections.Generic;
using GridFactory.Inventory;
using System.Linq;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using GridFactory.Blueprints;
namespace GridFactory.Core
{

    public enum ConfigurationType
    {
        BlueprintSelection,
        RecipeSelection,
        PortSelection
    }
    public class ConfigurationUI : MonoBehaviour
    {


        [Header("UI Referenzen")]
        [SerializeField] private GameObject panel;         // Dein Bestätigungs-Panel
        [SerializeField] private TMP_Text ribbonText;     // Oder Text, falls du kein TMP nutzt
        [SerializeField] private TMP_Text messageText;     // Oder Text, falls du kein TMP nutzt
        [SerializeField] private TMP_Text infoText;     // Oder Text, falls du kein TMP nutzt
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TMP_Dropdown selectionDropdown;

        private bool _selectionHasEmptyOption = false;

        private bool _isOpen = false;

        public bool IsOpen
        {
            get => _isOpen;
        }

        private int _lastSelectedIndex = 0;

        public int LastSelectedIndex
        {
            get => _lastSelectedIndex;
        }

        private Action _onConfirm;
        private Action _onCancel;

        private Action _onSelect;

        private void Awake()
        {
            Close();
        }

        /// <summary>
        /// Zeigt das Bestätigungsfenster.
        /// </summary>
        public void OpenConfiguration(string title, Action onConfirm, Action onCancel = null, string msg = "", string info = "", bool hideConfirm = false, bool hideCancel = false)
        {
            _onConfirm = onConfirm;
            _onCancel = onCancel;

            messageText.transform.parent.gameObject.SetActive(true);
            infoText.gameObject.SetActive(true);
            confirmButton.gameObject.SetActive(true);
            cancelButton.gameObject.SetActive(true);

            ribbonText.SetText(title);

            if (msg != "")
                messageText.SetText(msg);
            else
                messageText.transform.parent.gameObject.SetActive(false);

            if (info != "")
                infoText.SetText(info);
            else
                infoText.gameObject.SetActive(false);

            confirmButton.onClick.RemoveAllListeners();
            cancelButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmPressed);
            confirmButton.onClick.AddListener(AudioManager.Instance.PlayButtonClickSFX);
            cancelButton.onClick.AddListener(OnCancelPressed);
            cancelButton.onClick.AddListener(AudioManager.Instance.PlayButtonClickSFX);
            if (hideConfirm)
                confirmButton.gameObject.SetActive(false);

            if (hideCancel)
                cancelButton.gameObject.SetActive(false);

            panel.SetActive(true);
            StartCoroutine(RerenderEnable());
        }


        private IEnumerator RerenderEnable()
        {
            panel.GetComponent<VerticalLayoutGroup>().enabled = false;
            yield return new WaitForEndOfFrame();
            panel.GetComponent<VerticalLayoutGroup>().enabled = true;
        }


        public void UpdateInfoText(string txt = "")
        {
            if (txt == "")
            {
                messageText.transform.parent.gameObject.SetActive(false);
            }
            else
            {
                messageText.transform.parent.gameObject.SetActive(true);
                messageText.SetText(txt);
            }

            StartCoroutine(RerenderEnable());
        }


        public void Close()
        {
            ClearCallbacks();
            _selectionHasEmptyOption = false;
            panel.SetActive(false);
            _isOpen = false;
        }

        private void OnConfirmPressed()
        {

            _onConfirm?.Invoke();
            Close();

        }

        private void OnCancelPressed()
        {

            _onCancel?.Invoke();
            Close();

        }

        public void SetDropdownValueProgrammatically(int val = 0)
        {
            if (_selectionHasEmptyOption)
            {

                val++;
            }
            selectionDropdown.value = val;
            selectionDropdown.Select(); // optional
            selectionDropdown.RefreshShownValue();
        }

        private void OnSelected(int index)
        {
            if (_selectionHasEmptyOption)
                index--;
            _lastSelectedIndex = index;
            _onSelect?.Invoke();

            //Close();

        }

        private void ClearCallbacks()
        {
            _onConfirm = null;
            _onCancel = null;
            _onSelect = null;
        }










        public void SetupRecipeDropdown(List<RecipeDefinition> definitionsFromMachine, int index = 0, bool emptyFirst = false, Action onSelect = null)
        {
            _selectionHasEmptyOption = emptyFirst;

            selectionDropdown.ClearOptions();
            selectionDropdown.onValueChanged.RemoveAllListeners();

            _onSelect = onSelect;

            var emptyOption = new TMP_Dropdown.OptionData("- select -");
            var options = definitionsFromMachine
                .Select(r => new TMP_Dropdown.OptionData(r != null ? r.displayName : "<missing recipe>"))
                .ToList();
            if (_selectionHasEmptyOption)
                options.Insert(0, emptyOption);

            selectionDropdown.AddOptions(options);

            SetDropdownValueProgrammatically(index);
            selectionDropdown.onValueChanged.AddListener(OnSelected);
        }


        public void SetupBlueprintDropdown(List<BlueprintDefinition> definitions, int index = 0, bool emptyFirst = false, Action onSelect = null)
        {
            _selectionHasEmptyOption = emptyFirst;

            selectionDropdown.ClearOptions();
            selectionDropdown.onValueChanged.RemoveAllListeners();

            _onSelect = onSelect;

            var emptyOption = new TMP_Dropdown.OptionData("- select -");
            var options = definitions
                .Select(r => new TMP_Dropdown.OptionData(r != null ? r.displayName : "<missing recipe>"))
                .ToList();
            if (_selectionHasEmptyOption)
                options.Insert(0, emptyOption);

            selectionDropdown.AddOptions(options);

            SetDropdownValueProgrammatically(index);
            selectionDropdown.onValueChanged.AddListener(OnSelected);
        }

        public void SetupItemDropdown(List<ItemDefinition> definitions, int index = 0, bool emptyFirst = false, Action onSelect = null)
        {
            _selectionHasEmptyOption = emptyFirst;

            selectionDropdown.ClearOptions();
            selectionDropdown.onValueChanged.RemoveAllListeners();

            _onSelect = onSelect;

            var emptyOption = new TMP_Dropdown.OptionData("- select -");

            var options = definitions
                .Select(r => new TMP_Dropdown.OptionData(r != null ? r.displayName : "<missing recipe>"))
                .ToList();


            if (_selectionHasEmptyOption)
                options.Insert(0, emptyOption);

            selectionDropdown.AddOptions(options);

            SetDropdownValueProgrammatically(index);
            selectionDropdown.onValueChanged.AddListener(OnSelected);
        }


    }
}



