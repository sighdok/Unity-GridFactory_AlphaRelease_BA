using System;
using System.Collections;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

namespace GridFactory.Core
{
    public class UIConfirmationManager : MonoBehaviour
    {
        public static UIConfirmationManager Instance { get; private set; }

        [Header("UI Referenzen")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text infoText;
        [SerializeField] private TMP_Text confirmBtnText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        private Coroutine _confirmDelayRoutine;
        private string _originalConfirmText = "";

        private Action _onConfirm;
        private Action _onCancel;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Close();
        }

        public void Show(string message, Action onConfirm, Action onCancel = null, string info = "", bool hideConfirm = false, bool hideCancel = false, float confirmDelay = 0f)
        {
            if (_originalConfirmText == "")
                _originalConfirmText = confirmButton.GetComponentInChildren<TMP_Text>().text;

            _onConfirm = onConfirm;
            _onCancel = onCancel;

            confirmButton.onClick.RemoveAllListeners();
            cancelButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmPressed);
            confirmButton.onClick.AddListener(AudioManager.Instance.PlayButtonClickSFX);
            cancelButton.onClick.AddListener(OnCancelPressed);
            cancelButton.onClick.AddListener(AudioManager.Instance.PlayButtonClickSFX);

            infoText.gameObject.SetActive(true);
            confirmButton.gameObject.SetActive(true);
            cancelButton.gameObject.SetActive(true);

            messageText.SetText(message);

            if (info != "")
                infoText.SetText(info);
            else
                infoText.gameObject.SetActive(false);

            if (hideConfirm)
                confirmButton.gameObject.SetActive(false);
            if (hideCancel)
                cancelButton.gameObject.SetActive(false);

            panel.SetActive(true);
            StartCoroutine(RerenderEnable());

            if (_confirmDelayRoutine != null)
                StopCoroutine(_confirmDelayRoutine);

            if (confirmDelay > 0f)
                _confirmDelayRoutine = StartCoroutine(ConfirmDelayRoutine(confirmDelay));
            else
                confirmButton.interactable = true;
        }

        public void Close()
        {
            if (_confirmDelayRoutine != null)
            {
                StopCoroutine(_confirmDelayRoutine);
                confirmBtnText.SetText(_originalConfirmText);
                _confirmDelayRoutine = null;
            }

            ClearCallbacks();
            panel.SetActive(false);
        }

        private IEnumerator ConfirmDelayRoutine(float delay)
        {
            confirmButton.interactable = false;

            float timeLeft = delay;

            while (timeLeft > 0)
            {
                int seconds = Mathf.CeilToInt(timeLeft);
                confirmBtnText.SetText($"({seconds}) {_originalConfirmText}");

                yield return new WaitForSeconds(1f);
                timeLeft -= 1f;
            }

            confirmBtnText.SetText(_originalConfirmText);
            confirmButton.interactable = true;
        }

        private IEnumerator RerenderEnable()
        {
            panel.GetComponent<VerticalLayoutGroup>().enabled = false;
            yield return new WaitForEndOfFrame();
            panel.GetComponent<VerticalLayoutGroup>().enabled = true;
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

        private void ClearCallbacks()
        {
            _onConfirm = null;
            _onCancel = null;
        }
    }
}