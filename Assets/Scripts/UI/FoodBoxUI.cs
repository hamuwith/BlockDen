using static Player;

public class FoodBoxUI : CarryBoxUI
{
    protected override int BagIndex => (int)InventoryType.Food;
}
