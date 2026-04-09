using System.Net.Mail;
using UnityEngine;
using UnityEngine.UI;
public class Maker : ToolUI
{
    [SerializeField] Item[] makableItems;
    [SerializeField] Menu menu;
    bool[] makable;
    protected override int MaxIndex => makableItems.Length;
    public override void Init(ItemManager itemManager, Tool tool)
    {
        makable = new bool[makableItems.Length];
        base.Init(itemManager, tool);
        for (int i = 0; i < makableItems.Length; i++)
        {
            buttons[i].sprite = makableItems[i].ItemState.Icon;
        }
        menu.Init(itemManager);
    }
    /// <summary>
    /// ツールのUIを開く際の初期化を行うメソッド
    /// </summary>
    /// <param name="player"></param>
    public override void OpenUI(Player player, int index)
    {
        base.OpenUI(player, index);
    }    
    /// <summary>
    /// ツールのUIで外から内に選択を移す際の初期化を行うメソッド
    /// </summary>
    /// <param name="fromUnder"></param>
    public override void SelectIn(Item item = null)
    {
        _HighLight(SelectState.SelectIn);
        _Cursor();
        _Menu(true);
    }
    public override void CloseUI()
    {
        canvas.enabled = false;
    }
    /// <summary>
    /// ツールのアクションを実行するメソッド
    /// </summary>
    public override Item Action()
    {
        SelectState change = SelectState.SelectOut;
        _HighLight(change);
        _Menu(false);
        Item item = makable[index] ? makableItems[index] : null;
        return item;
    }
    /// <summary>
    /// ツールのUIでボタンを選択するメソッド
    /// </summary>
    /// <param name="vector"></param>
    public override SelectState Select(Vector2 vector)
    {
        var change = _GetSelect(vector);
        var isChange = tool.IsOuterChange(change);
        if (!isChange)
        {
            _Select(vector);
            var item = makableItems[index];
            var type = player.GetInventoryType(item);
            var bagIndex = (int)type;
            if (type == Player.InventoryType.Null)
            {
                bagIndex = 0;
            }
            else if (type == Player.InventoryType.Tool)
            {
                player.ChangeTool((item as BreakTool).BlockType);
            }
            player.BagIndex = bagIndex;
            _Menu(true);
        }
        _HighLight(change, isChange);
        _Cursor();
        return change;
    }
    public override void UpdateAction()
    {
        var haveItems = itemManager.GetBoxItemNum();
        for (int i = 0; i < makableItems.Length; i++)
        {
            makable[i] = true;
            foreach (var material in makableItems[i].ItemState.Materials)
            {
                makable[i] &= (haveItems[(int)material.Category][material.Id]) >= material.Num;
            }
            if (makable[i])
            {
                buttons[i].color = Color.white;
            }
            else
            {
                buttons[i].color = Color.gray;
            }
        }
    }
    void _Menu(bool isShow)
    {
        if (isShow)
        {
            menu.ShowMenu(buttons[index].transform, makableItems[index].ItemState.Materials);
        }
        else
        {
            menu.HideMenu();
        }
    }
}
