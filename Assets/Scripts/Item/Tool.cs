using UnityEngine;

public class Tool : PlayerUI
{
    [SerializeField] BreakCraftUI breakCraftUI;
    public override void Init(ItemManager itemManager, Material material, ItemAccess itemAccess)
    {
        base.Init(itemManager, material, itemAccess);
        breakCraftUI.Init(itemManager);
    }
    public override void OpenUI(Player player)
    {
        this.player = player;
        breakCraftUI.OpenUI(player);
    }
    public override void CloseUI()
    {
        breakCraftUI.CloseUI();
    }
    public override void Select(Vector2 vector)
    {
        breakCraftUI.Select(vector);
    }
    public override void Action()
    {
        breakCraftUI.Action();
    }
    public override void Cancel()
    {
        var close = breakCraftUI.Cancel();
        if (close)
        {
            CloseUI();
        }
    }
    public override void UpdateAction()
    {
        breakCraftUI.UpdateAction();
    }
    public override void SelectTab(bool left)
    {
    }
}
