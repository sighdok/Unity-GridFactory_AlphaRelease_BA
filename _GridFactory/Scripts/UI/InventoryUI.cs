using System;
using System.Collections.Generic;
using GridFactory.Core;
using GridFactory.Inventory;
using TMPro;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InventoryManager inventory;
    [SerializeField] private EnergyManager energy;
    [SerializeField] private Transform itemsParent;          // z.B. ein VerticalLayoutGroup
    [SerializeField] private InventoryUIEntry entryPrefab;   // Prefab für einen UI-Eintrag
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI energyText;
    private readonly List<InventoryUIEntry> entries = new List<InventoryUIEntry>();

    private void Start()
    {
        if (inventory == null)
            inventory = InventoryManager.Instance;

        BuildEntries();
        Refresh();

        if (inventory != null)
            inventory.OnInventoryChanged += Refresh;
        /*
    if (energy != null)
        energy.OnEnergyChanged += RefreshEnergy;
        */
    }

    private void OnDestroy()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= Refresh;
        /*
                if (energy != null)
                    energy.OnEnergyChanged -= RefreshEnergy;
                    */
    }

    private void BuildEntries()
    {
        // Alle ItemTypes außer None durchgehen
        foreach (ItemType type in Enum.GetValues(typeof(ItemType)))
        {
            if (type == ItemType.None)
                continue;

            var def = inventory.GetDefinition(type);

            // Wenn keine Definition vorhanden ist, kannst du entscheiden, ob der Typ trotzdem angezeigt werden soll.
            string displayName = def != null ? def.displayName : type.ToString();
            Sprite icon = def != null ? def.icon : null;

            var entry = Instantiate(entryPrefab, itemsParent);
            entry.Setup(type, icon, displayName);
            entries.Add(entry);
        }
    }

    private void Refresh()
    {
        if (inventory == null) return;

        // Item-Anzahlen setzen
        foreach (var entry in entries)
        {
            int count = inventory.GetItemCount(entry.Type);
            entry.SetCount(count);
        }

        // Gold anzeigen
        if (goldText != null)
        {
            string formatted;

            if (inventory.Gold < 10000)
            {
                // 1000 - 9999 => eine Nachkommastelle
                formatted = inventory.Gold.ToString();
            }
            else
            {
                // ab 10000 => keine Nachkommastelle mehr
                int kValue = inventory.Gold / 1000;
                formatted = kValue.ToString() + "K";
            }

            goldText.text = formatted;
        }

        if (energyText != null)
        {
            energyText.text = EnergyManager.Instance.Energy.ToString();
        }
    }
}
