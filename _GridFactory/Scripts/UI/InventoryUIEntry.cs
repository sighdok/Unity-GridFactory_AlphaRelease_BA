using UnityEngine;
using UnityEngine.UI;

using TMPro;

using GridFactory.Core;

public class InventoryUIEntry : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text countText;

    public ItemType Type { get; private set; }

    public void Setup(ItemType type, Sprite icon, string displayName)
    {
        Type = type;

        iconImage.sprite = icon;
        // nameText.SetText(displayName);
    }

    public void SetCount(int count)
    {
        if (countText == null)
            return;

        string formatted;
        if (count < 1000)
        {
            formatted = count.ToString();
        }
        else if (count < 10000)
        {
            float kValue = count / 1000f;
            formatted = kValue.ToString("0.#") + "K";
        }
        else
        {
            int kValue = count / 1000;
            formatted = kValue.ToString() + "K";
        }
        countText.SetText(formatted);
    }
}
