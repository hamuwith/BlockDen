using UnityEngine;

public class BreakTool : Item
{
    public int HasWater { get; private set; }
    public override void Init(ItemManager itemManager, Material material, ItemAccess itemAccess)
    {
        base.Init(itemManager, material, itemAccess);
        HasWater = 0;
    }
    public void GetWater()
    {
        HasWater = (itemManager.GetItem(itemAccess) as BreakToolData).BreakPower;
    }
    public int UseWater()
    {
        int hasWater = HasWater;
        HasWater = 0;
        return hasWater;
    }
}
