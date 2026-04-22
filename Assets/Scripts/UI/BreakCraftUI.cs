
public class BreakCraftUI : CraftUI
{
    public override void Init(ItemManager itemManager)
    {
        InitBase(itemManager);
        craftResult.Id = -1;
        craftResult.Num = 1;
        craftSlots = new ItemAccess[buttons.Length];
        for (int i = 0; i < craftSlots.Length; i++) craftSlots[i].Id = -1;
    }
    protected override void Craft()
    {
        player.BagUpdate(craftResult);
        for (int i = 0; i < craftSlots.Length; i++) craftSlots[i].Id = -1;
        craftResult.Id = -1;
    }
    protected override void CheckCraft()
    {
        craftResult = itemManager.CraftToBreakTool(craftSlots);
    }
}
