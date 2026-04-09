using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class Box : ToolUI
{
    Item[] boxItems;
    public override void Init(ItemManager itemManager, Tool tool)
    {
        base.Init(itemManager, tool);
        boxItems = new Item[buttons.Length];
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
        SelectOut();
        return boxItems[index];
    }
    public override void SelectIn(Item item = null)
    {
        _HighLight(SelectState.SelectIn);
        _Cursor();
    }
    public Item GetItem()
    {
        return boxItems[index];
    }
    public void SetItem(Item item)
    {
        if (item == null) return;
        boxItems[index] = item;
        buttons[index].sprite = item.ItemState.Icon;
        itemTexts[index].text = item.Num > 1 ? item.Num.ToString() : "";
        item.transform.SetParent(transform);
    }
    public override void UpdateAction()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (boxItems[i] != null)
            {
                buttons[i].sprite = boxItems[i].ItemState.Icon;
                itemTexts[i].text = boxItems[i].Num > 1 ? boxItems[i].Num.ToString() : "";
            }
            else
            {
                //buttons[i].sprite = null;
                itemTexts[i].text = "";
            }
        }
    }
}

