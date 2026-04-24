using UnityEngine;

public class Box : PlayerUI
{
    [SerializeField] ItemBoxUI itemBoxUI;
    public override void Init(ItemManager itemManager, Material material, ItemAccess itemAccess)
    {
        base.Init(itemManager, material, itemAccess);
        var itemData = itemManager.GetItem(itemAccess) as BoxData;
        var inventoryType = itemData.InventoryType;
        itemBoxUI.Init(itemManager, inventoryType);
    }
    public override void OpenUI(Player player)
    {
        this.player = player;
        itemBoxUI.OpenUI(player);
    }
    public override void CloseUI()
    {
        itemBoxUI.CloseUI();
    }
    public override void Select(Vector2 vector)
    {
        itemBoxUI.Select(vector);
    }
    public override void Action()
    {
        itemBoxUI.Action();
    }
    public override void Cancel()
    {
        var close = itemBoxUI.Cancel();
        if (close)
        {
            CloseUI();
        }
    }
    public override void UpdateAction()
    {
        itemBoxUI.UpdateAction();
    }
    public override void SelectTab(bool left)
    {
    }
}
