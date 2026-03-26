using UnityEngine;
using UnityEngine.UI;

using TMPro;

using GridFactory.Core;

public class InventoryUIEntry : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI countText;

    public ItemType Type { get; private set; }

    public void Setup(ItemType type, Sprite icon, string displayName)
    {
        Type = type;

        if (iconImage != null)
            iconImage.sprite = icon;

        if (nameText != null)
            nameText.text = displayName;
    }

    public void SetCount(int count)
    {
        if (countText == null)
            return;

        string formatted;

        if (count < 1000)
        {
            // 1 - 999
            formatted = count.ToString();
        }
        else if (count < 10000)
        {
            // 1000 - 9999 => eine Nachkommastelle
            float kValue = count / 1000f;
            formatted = kValue.ToString("0.#") + "K";
        }
        else
        {
            // ab 10000 => keine Nachkommastelle mehr
            int kValue = count / 1000;
            formatted = kValue.ToString() + "K";
        }

        countText.text = formatted;
    }
}
