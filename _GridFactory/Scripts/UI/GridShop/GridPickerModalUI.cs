using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GridFactory.Core;
using System.Text;

namespace GridFactory.UI
{
    public class GridPickerModalUI : MonoBehaviour
    {
        [Header("Root")]
        //[SerializeField] private GameObject root;
        //  [SerializeField] private Button closeButton;
        //  [SerializeField] private TMP_Text titleText;
        [SerializeField] private ShopUI shopUi;
        [Header("List")]
        [SerializeField] private Transform listRoot;
        [SerializeField] private GridPickerRowUI rowPrefab;

        private readonly List<GridPickerRowUI> rows = new();

        public Action<GridDefinitionOwned> onPicked;

        private void OnEnable()
        {
            GridShopManager.Instance.OnBuyGrid += Rebuild;
            Rebuild();
        }

        private void OnDisable()
        {
            GridShopManager.Instance.OnBuyGrid -= Rebuild;
        }

        /*
        public void Show(Action<OwnedGridDefinition> onPickedCallback, string title = "Grid auswählen")
        {
            onPicked = onPickedCallback;
            if (titleText) titleText.text = title;

            root.SetActive(true);
            Rebuild();
        }
        */
        /*
                public void Hide()
                {
                    if (root != null)
                        root.SetActive(false);

                    onPicked = null;
                    Clear();
                }
        */
        public void Rebuild()
        {
            Clear();

            var owned = GridDefinitionManager.Instance.Owned;

            for (int i = 0; i < owned.Count; i++)
            {
                var def = owned[i];
                var row = Instantiate(rowPrefab, listRoot);
                row.Bind(def, () =>
                {
                    GameManager.Instance.SetMode(GameMode.Blueprint, false);
                    UIConfirmationManager.Instance.Show(
                   "Grid laden?",
                   () => { onPicked?.Invoke(def); shopUi.Close(); },
                   () => { }
               );

                });
                rows.Add(row);
            }
        }

        private void Clear()
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i] != null) Destroy(rows[i].gameObject);
            }
            rows.Clear();
        }
    }
}
