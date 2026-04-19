using UnityEngine;

public class BreakCraftUI : CraftUI
{
    public override void Init(ItemManager itemManager)
    {
        InitBase(itemManager);
        craftSlots = new ItemAccess[buttons.Length];
        for (int i = 0; i < craftSlots.Length; i++)
            craftSlots[i].Id = -1;
    }
    protected override void Craft()
    {
        ItemAccess itemAccess = itemManager.CraftToBreakTool(craftSlots);
        player.BagUpdate(itemAccess);
    }
}
