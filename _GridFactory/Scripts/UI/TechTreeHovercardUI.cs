using Esper.SkillWeb.UI.UGUI;
using GridFactory.Core;
using GridFactory.Inventory;
using GridFactory.Tech;

public class TechTreeHovercardUI : SkillHovercardUGUI
{
    public override bool Open(SkillNodeUGUI target)
    {
        // Get the result from the base method (simply a check to see if the hovercard was successfully opened)
        var result = base.Open(target);

        if (result)
        {
            // Get data from a custom dataset
            var dataset = target.skillNode.skill.dataset as TechTreeDataset;

            // Extend the description
            descriptionLabel.text += $"\n\nInitial Cost: {dataset.GetBaseCost()} Gold\nDeliver Items: {GetIngredientsForResearch(dataset.GetResearchItems())}";


            if (dataset.baseCost > 0)
            {
                if (InventoryManager.Instance.Gold < dataset.baseCost)
                {
                    descriptionLabel.text += $"\n\n<b>Not enough gold.</b>";
                }
            }

            if (TechTreeManager.Instance.IsNodeInResearch(target.skillNode))
            {
                descriptionLabel.text += $"\n\n<b>Already in research.</b>";
                // iconColor = new Color(255, 0, 0);
            }
        }

        return result;
    }


    private string GetIngredientsForResearch(ResearchItem[] items)
    {
        string myString = "";

        foreach (var entry in items)
        {
            if (entry == null || entry.item == null) continue;


            // Wenn dein Item ein eigenes Namensfeld hat, hier ersetzen (z.B. entry.item.displayName)
            string itemName = entry.item.type.ToString();
            myString += $"{entry.amount}x {itemName} | ";

        }
        return myString;
    }
}