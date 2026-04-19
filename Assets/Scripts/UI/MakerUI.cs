using UnityEngine;
using static Item;
using System.Collections.Generic;

public class MakerUI : BaseUI
{
    [SerializeField] ItemCategory[] makableCategorys;
    [SerializeField] Menu menu;
    ItemData[] makableItems;
    bool[] makable;
    ItemData makeItem;
    protected override int MaxIndex => Mathf.Min(makableItems.Length, buttons.Length);
    public override bool IsMakable => makableItems?.Length > 0;

    public override void Init(ItemManager itemManager)
    {
        var makableItemList = new List<ItemData>();
        foreach (var category in makableCategorys)
        {
            makableItemList.AddRange(itemManager.GetMakableItems(category));
        }
        makableItems = makableItemList.ToArray();
        InitBase(itemManager);
        for (int i = 0; i < makableItems.Length; i++)
        {
            if (i >= buttons.Length) break;
            buttons[i].sprite = makableItems[i].Icon;
        }
    }
    public override void OpenUI(Player player)
    {
        makable = new bool[makableItems.Length];
        SetEnabled();
        for (int i = 0; i < makableItems.Length; i++)
        {
            if (i >= buttons.Length) break;
            buttons[i].sprite = makableItems[i].Icon;
        }
        menu.Init(itemManager);
        _Menu(true);
        OpenUIBase(player);
        _SelectIn(makableItems[index]);
        _Cursor();
    }
    public override void CloseUI()
    {
        canvas.enabled = false;
        if (player != null) player.BagIndex = inventoryIndex;
        player = null;
    }
    public override void Select(Vector2 vector)
    {
        var change = _GetSelect(vector);
        //change = change == SelectState.DownOuterChange && isInventory || change == SelectState.UpOuterChange && !isInventory ? SelectState.NoChange : change;
        change = SelectState.NoChange;
        if (change == SelectState.NoChange)
        {
            if (!isInventory)
            {
                _Select(vector);
                _SelectIn(makableItems[index]);
                _HighLight();
                _Menu(!isInventory);
            }
        }
        else
        {
            isInventory = !isInventory;
            _Menu(!isInventory);
            _HighLight();
        }
        _Cursor();
    }
    public override void Action()
    {
        if (!isInventory)
        {
            makeItem = makable[index] ? makableItems[index] : null;
            if (makeItem != null)
            {
                isInventory = true;
                _Menu(!isInventory);
                _HighLight();
            }
        }
        else
        {
            itemManager.RemoveBoxItem(makeItem.ItemMaterials);
            player.Make(makeItem, makeItem.UnitNum);
            makeItem = null;
            isInventory = false;
            UpdateAction();
            _HighLight();
        }
    }
    public override bool Cancel()
    {
        if (!isInventory)
        {
            return true;
        }
        else
        {
            makeItem = null;
            isInventory = false;
            _Menu(!isInventory);
            _HighLight();
            return false;
        }
    }
    public virtual bool SetEnabled()
    {
        var haveItems = itemManager.GetBoxItemNum();
        var isEnabled = false;
        for (int i = 0; i < makableItems.Length; i++)
        {
            makable[i] = true;
            foreach (var material in makableItems[i].ItemMaterials)
            {
                makable[i] &= (haveItems[(int)material.Category][material.Id]) >= material.Num;
            }
            isEnabled |= makable[i];
        }
        return isEnabled;
    }
    public override void UpdateAction()
    {
        for (int i = 0; i < makableItems.Length; i++)
        {
            if (i >= buttons.Length) break;
            if (makable[i])
            {
                buttons[i].color = Color.white;
            }
            else
            {
                buttons[i].color = Color.gray;
            }
        }
        for (int i = 0; i < inventoryButtons.Length; i++)
        {
            if (player.Bag[i] != null)
            {
                inventoryButtons[i].sprite = itemManager.GetItemIcon(player.Bag[i].ItemAccess);
                inventoryItemTexts[i].text = player.Bag[i].Num > 1 ? player.Bag[i].Num.ToString() : "";
            }
            else
            {
                inventoryButtons[i].sprite = null;
                inventoryItemTexts[i].text = "";
            }
        }
    }
    protected void _SelectIn(ItemData item = null)
    {
        var type = player.GetInventoryType(item.ItemAccess);
        inventoryIndex = (int)type;
        if (type == Player.InventoryType.Null)
        {
            inventoryIndex = 0;
        }
        else if (type == Player.InventoryType.Tool)
        {
            player.ChangeTool((item as BreakToolData).BlockType);
        }
    }
    void _Menu(bool isShow)
    {
        if (isShow)
        {
            menu.ShowMenu(buttons[index].transform, makableItems[index].ItemMaterials);
        }
        else
        {
            menu.HideMenu();
        }
    }
}
