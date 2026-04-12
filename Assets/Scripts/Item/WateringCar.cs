using UnityEngine;

public class WateringCar : BreakTool
{
    int power;
    public override void Init(ItemManager itemManager)
    {
        base.Init(itemManager);
        power = 0;
    }
    public void GetWater()
    {
        power = BreakPower;
    }
    public int UseWater()
    {
        int power = this.power;
        this.power = 0;
        return power;
    }
}
