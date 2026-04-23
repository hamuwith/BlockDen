
public class ItemCraftUI : CraftUI
{
    Item.ItemCategory craftCategory;
    MaterialType materialType;
    public void Init(ItemManager itemManager, MaterialType materialType, Item.ItemCategory craftCategory)
    {
        InitBase(itemManager);
        this.craftCategory = craftCategory;
        this.materialType = materialType;
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
        craftResult = itemManager.CraftToItem(craftSlots, craftCategory);
    }
    public override bool EqualMaterialType(ItemDataBase itemData)
    {
        var itemMaterialType = (itemData as MaterialData).MaterialType;
        return itemMaterialType == materialType;
    }
}
