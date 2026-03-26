using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GridFactory.Core;

namespace GridFactory.UI
{
    public class GridShopOfferCardUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text metaText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private Button buyButton;
        [SerializeField] private GridPreviewUI preview;

        private GridShopOfferSaveData offer;
        private Func<string, bool> onBuy;

        public string OfferId => offer != null ? offer.offerId : null;

        public void Bind(GridShopOfferSaveData offerData, Func<string, bool> onBuyCallback, Func<int, int, bool> isLockedFn)
        {
            offer = offerData;
            onBuy = onBuyCallback;

            if (titleText) titleText.text = offer.displayName;
            if (metaText) metaText.text = $"{offer.width} x {offer.height}";
            if (priceText) priceText.text = $"{offer.price} Gold";

            if (preview != null)
            {
                preview.Render(offer.width, offer.height, (x, y) => isLockedFn != null && isLockedFn(x, y));
            }

            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(() =>
                {
                    if (onBuy == null) return;
                    bool success = onBuy.Invoke(offer.offerId);
                    // UI refresh übernimmt parent
                });
                buyButton.onClick.AddListener(AudioManager.Instance.PlayButtonClickSFX);
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (buyButton) buyButton.interactable = interactable;
        }
    }
}
