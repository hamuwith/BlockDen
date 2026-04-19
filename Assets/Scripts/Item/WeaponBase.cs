using UnityEngine;

public class WeaponBase : PlayerUI
{
    [SerializeField] CraftUI craftUI;
    public override void Init(ItemManager itemManager, Material material, ItemAccess itemAccess)
    {
        base.Init(itemManager, material, itemAccess);
        craftUI.Init(itemManager);
    }
    public override void OpenUI(Player player)
    {
        this.player = player;
        craftUI.OpenUI(player);
    }
    public override void CloseUI()
    {
        craftUI.CloseUI();
    }
    public override void Select(Vector2 vector)
    {
        craftUI.Select(vector);
    }
    public override void Action()
    {
        craftUI.Action();
    }
    public override void Cancel()
    {
        var close = craftUI.Cancel();
        if (close)
        {
            CloseUI();
        }
    }
    public override void UpdateAction()
    {
        craftUI.UpdateAction();
    }
}
