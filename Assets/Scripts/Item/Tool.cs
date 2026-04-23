using UnityEngine;

public class Tool : PlayerUI
{
    [SerializeField] ItemCraftUI itemCraftUI;
    public override void Init(ItemManager itemManager, Material material, ItemAccess itemAccess)
    {
        base.Init(itemManager, material, itemAccess);
        var itemData = itemManager.GetItem(itemAccess) as ToolData;
        var materialType = itemData.MaterialType;
        var craftType = itemData.CraftCategory;
        itemCraftUI.Init(itemManager, materialType, craftType);
    }
    public override void OpenUI(Player player)
    {
        this.player = player;
        itemCraftUI.OpenUI(player);
    }
    public override void CloseUI()
    {
        itemCraftUI.CloseUI();
    }
    public override void Select(Vector2 vector)
    {
        itemCraftUI.Select(vector);
    }
    public override void Action()
    {
        itemCraftUI.Action();
    }
    public override void Cancel()
    {
        var close = itemCraftUI.Cancel();
        if (close)
        {
            CloseUI();
        }
    }
    public override void UpdateAction()
    {
        itemCraftUI.UpdateAction();
    }
    public override void SelectTab(bool left)
    {
    }
}
