using Esper.SkillWeb.UI.UGUI;

using GridFactory.Core;
using GridFactory.Inventory;
using GridFactory.Tech;

public class TechTreeHovercardUI : SkillHovercardUGUI
{
    public override bool Open(SkillNodeUGUI target)
    {
        var result = base.Open(target);
        if (result)
        {
            var dataset = target.skillNode.skill.dataset as TechTreeDataset;
            descriptionLabel.text += $"\n\nInitial Cost: {dataset.GetBaseCost()} Gold\nDeliver Items: {GetIngredientsForResearch(dataset.GetResearchItems())}";
            if (dataset.baseCost > 0 && InventoryManager.Instance.Gold < dataset.baseCost)
                descriptionLabel.text += $"\n\n<b>Not enough gold.</b>";

            if (TechTreeManager.Instance.IsNodeInResearch(target.skillNode))
                descriptionLabel.text += $"\n\n<b>Already in research.</b>";
        }

        return result;
    }

    private string GetIngredientsForResearch(ResearchItem[] items)
    {
        string myString = "";
        foreach (var entry in items)
        {
            if (entry == null || entry.item == null)
                continue;

            string itemName = entry.item.type.ToString();
            myString += $"{entry.amount}x {itemName} | ";

        }

        return myString;
    }
}