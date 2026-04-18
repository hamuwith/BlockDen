using UnityEngine;

public class BreakTool : Item
{
    int power;
    public override void Init(ItemManager itemManager, Material material, ItemAccess itemAccess)
    {
        base.Init(itemManager, material, itemAccess);
        power = 0;
    }
    public void GetWater()
    {
        power = (itemManager.GetItem(itemAccess) as BreakToolData).BreakPower;
    }
    public int UseWater()
    {
        int power = this.power;
        this.power = 0;
        return power;
    }
}
