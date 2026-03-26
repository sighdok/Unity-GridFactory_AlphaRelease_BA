using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GridFactory.Core;
using GridFactory.Inventory;
using Unity.VisualScripting;
using GridFactory.Grid;

namespace GridFactory.UI
{
    public class GridShopScreenUI : MonoBehaviour
    {
        [Header("Top Bar")]
        //[SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text refreshCostText;
        [SerializeField] private Button refreshButton;

        [Header("Offer List")]
        [SerializeField] private Transform offerRoot;
        [SerializeField] private GridShopOfferCardUI offerCardPrefab;

        [Header("Messages")]
        [SerializeField] private TMP_Text messageText;

        private readonly List<GridShopOfferCardUI> cards = new();

        private void OnEnable()
        {
            Rebuild();

        }


        public void Rebuild()
        {
            ClearCards();

            // UI numbers
            UpdateGoldAndRefresh();

            var offers = GridShopManager.Instance.CurrentOffers;
            for (int i = 0; i < offers.Count; i++)
            {
                var o = offers[i];
                var card = Instantiate(offerCardPrefab, offerRoot);
                cards.Add(card);

                // Für Preview: wir müssen deterministisch die Locks “simulieren”
                // Preset: aus Preset-Asset
                // Random: mit seed generieren
                card.Bind(o, TryBuyOffer, (x, y) => IsLockedForOffer(o, x, y));

                // Disabled, wenn nicht kaufbar (zu wenig Gold / bereits owned)
                card.SetInteractable(CanBuy(o));
            }

            if (refreshButton != null)
            {
                refreshButton.onClick.RemoveAllListeners();
                refreshButton.onClick.AddListener(() =>
                {
                    bool ok = GridShopManager.Instance.TryRefresh(
                        () => InventoryManager.Instance.Gold,
                        cost => InventoryManager.Instance.SetGoldDirect(InventoryManager.Instance.Gold - cost)
                    );

                    if (!ok) SetMessage("Nicht genug Gold für Refresh.");
                    else SetMessage("");

                    // Refresh UI
                    Rebuild();
                });
                refreshButton.onClick.AddListener(AudioManager.Instance.PlayButtonClickSFX);
            }
        }

        private void UpdateGoldAndRefresh()
        {
            //if (goldText) goldText.text = $"Gold: {InventoryManager.Instance.Gold}";

            int refreshCost = GridShopManager.Instance.GetRefreshCost();
            if (refreshCostText) refreshCostText.SetText($"<b>Refresh:</b>\n{refreshCost} Gold");

            if (refreshButton) refreshButton.interactable = InventoryManager.Instance.Gold >= refreshCost;
        }

        private bool TryBuyOffer(string offerId)
        {

            bool ok = GridShopManager.Instance.BuyOfferWithCallbacks(
                offerId,
                () => InventoryManager.Instance.Gold,
                price => InventoryManager.Instance.SetGoldDirect(InventoryManager.Instance.Gold - price)
            );

            if (!ok) SetMessage("Kauf nicht möglich.");
            else SetMessage("");
            Rebuild();
            return ok;
        }

        private bool CanBuy(GridShopOfferSaveData o)
        {

            if (o == null) return false;
            if (InventoryManager.Instance.Gold < o.price) return false;

            if (o.isPreset)
                return !GridDefinitionManager.Instance.IsOwned(o.presetId);

            string rndId = $"rnd_{o.width}x{o.height}_{o.seed}";
            return !GridDefinitionManager.Instance.IsOwned(rndId);
        }

        private bool IsLockedForOffer(GridShopOfferSaveData o, int x, int y)
        {
            if (o == null) return false;

            if (o.isPreset)
            {
                var presets = GridDefinitionManager.Instance.GetPresetPool();
                GridDefinition preset = null;
                for (int i = 0; i < presets.Count; i++)
                {
                    if (presets[i] != null && presets[i].id == o.presetId)
                    {
                        preset = presets[i];
                        break;
                    }
                }

                if (preset == null || preset.unlockableLockedCells == null) return false;

                for (int i = 0; i < preset.unlockableLockedCells.Length; i++)
                {
                    var p = preset.unlockableLockedCells[i];
                    if (p.x == x && p.y == y) return true;
                }
                return false;
            }

            // random: locks aus seed generieren (wie beim Kauf)
            var settings = new RandomGridGenerator.Settings { maxLockedPercent = 0.25f, maxAttempts = 200 };
            // NOTE: maxLockedPercent muss konsistent sein; wenn du im ShopManager einen anderen Wert setzt,
            // mach ihn in beiden Scripts gleich oder expose ihn per getter. Für MVP: gleiche Zahl.
            var locks = RandomGridGenerator.GenerateUnlockableLocks(o.width, o.height, o.seed, settings);
            for (int i = 0; i < locks.Count; i++)
            {
                if (locks[i].x == x && locks[i].y == y) return true;
            }
            return false;
        }

        private void SetMessage(string msg)
        {
            if (messageText) messageText.text = msg;
        }

        private void ClearCards()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] != null) Destroy(cards[i].gameObject);
            }
            cards.Clear();
        }
    }
}
