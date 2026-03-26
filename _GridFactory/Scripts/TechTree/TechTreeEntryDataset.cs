using UnityEngine;

using Esper.SkillWeb;

using GridFactory.Core;

[CreateAssetMenu(fileName = "My Skill Dataset", menuName = "GridFactory/TechtreeDataset")]
public class TechTreeDataset : DefaultSkillDataset
{
    public ResearchItem[] inputItems;
    public int baseCost;
    public int ticksForProcess;
    public string internalId;

    public override string GetDescription()
    {
        return base.GetDescription();
    }

    public override string GetName()
    {
        return base.GetName();
    }

    public override string GetSubtext()
    {
        return base.GetSubtext();
    }

    public ResearchItem[] GetResearchItems()
    {
        return inputItems;
    }

    public int GetBaseCost()
    {
        return baseCost;
    }
}
