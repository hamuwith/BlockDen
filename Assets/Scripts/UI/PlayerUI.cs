using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : Block
{
    [SerializeField] MakerUI[] uis;
    int tabIndex;
    protected Player player;
    public override void Init(ItemManager itemManager)
    {
        transform.parent = null;
        foreach (var ui in uis)
        {
            ui.Init(itemManager);
        }
    }
    protected void BaseInit(ItemManager itemManager)
    {
        base.Init(itemManager);
    }
    public virtual void OpenUI(Player player)
    {
        tabIndex = 0;
        transform.position = player.transform.position;
        uis[tabIndex].OpenUI(player);
        this.player = player;
    }
    public virtual void CloseUI()
    {
        foreach (var ui in uis)
        {
            ui.CloseUI();
        }
        player.CloseToolUI();
    }
    public virtual void Select(Vector2 vector)
    {
        uis[tabIndex].Select(vector);
    }
    public virtual void Action()
    {
        uis[tabIndex].Action();
    }
    public virtual void Cancel()
    {
        var close = uis[tabIndex].Cancel();
        if (close)
        {
            CloseUI();
        }
    }
    public virtual void UpdateAction()
    {
        uis[tabIndex].UpdateAction();
    }
    public void SelectTab(bool left)
    {
        uis[tabIndex].CloseUI();
        _SelectTab(left);
        uis[tabIndex].OpenUI(player);
    }
    void _SelectTab(bool left)
    {
        tabIndex += left ? -1 : 1;
        if (tabIndex < 0)
        {
            tabIndex = uis.Length - 1;
        }
        else if (tabIndex >= uis.Length)
        {
            tabIndex = 0;
        }
        if (uis[tabIndex].IsMakable)
        {
            return;
        }
        else
        {
            _SelectTab(left);
        }
    }
}
