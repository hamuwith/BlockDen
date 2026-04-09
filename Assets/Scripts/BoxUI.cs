using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BoxUI : MakerUI
{
    Item[] boxitems;
    bool isMove;
    //bool []inventorySelect;
    //bool[] boxSelect;
    protected override int MaxIndex => buttons.Length;
    public override void Init(ItemManager itemManager)
    {
        InitBase(itemManager);
        boxitems = new Item[buttons.Length];
    }
    /// <summary>
    /// ツールのUIを開く際の初期化を行うメソッド
    /// </summary>
    /// <param name="player"></param>
    public override void OpenUI(Player player)
    {
        OpenUIBase(player);
        //for (int i = 0; i < buttons.Length; i++)
        //{
        //    boxSelect[i] = true;
        //}
        //for (int i = 0; i < inventoryButtons.Length; i++)
        //{
        //    var inventoryType = player.GetInventoryType(player.Bag[i]);
        //    if (inventoryType == Player.InventoryType.Food || inventoryType == Player.InventoryType.Carry)
        //    {
        //        inventorySelect[i] = true;
        //    }
        //    else
        //    {
        //        inventorySelect[i] = false;
        //    }
        //}
    }
    /// <summary>
    /// ツールのUIでボタンを選択するメソッド
    /// </summary>
    /// <param name="vector"></param>
    public override SelectState Select(Vector2 vector)
    {
        Debug.Log(isInventory);
        var change = _GetSelect(vector);
        change = (change == SelectState.DownOuterChange && isInventory) || (change == SelectState.UpOuterChange && !isInventory) ? SelectState.NoChange : change;
        if (isMove) change = SelectState.NoChange;
        if (change == SelectState.NoChange)
        {
            if (!isMove || !isInventory)
            {
                _Select(vector);
                _HighLight();                
            }
        }
        else
        {
            isInventory = !isInventory;
            _HighLight();
        }
        _Cursor();
        return change;
    }
    /// <summary>
    /// ツールのアクションを実行するメソッド
    /// </summary>
    public override void Action()
    {
        if (isMove)
        {
            var inventoryType = player.GetInventoryType(player.Bag[inventoryIndex]);
            var boxItemType = player.GetInventoryType(boxitems[index]);
            if (inventoryType == boxItemType || inventoryType == Player.InventoryType.Null || boxItemType == Player.InventoryType.Null)
            {
                Item item = boxitems[index];
                boxitems[index] = player.Bag[inventoryIndex];
                boxitems[index].transform.SetParent(transform);
                player.ChangeItem(item, player.HaveItem);
                isMove = false;
                UpdateAction();
            }
        }
        else
        {
            var item = isInventory ? player.Bag[inventoryIndex] : boxitems[index];
            if (item == null) return;
            isMove = true;
            isInventory = !isInventory;
            if (!isInventory)
            {
                EnableBoxItem();
            }
            else
            {
                _SelectIn();
            }
            _HighLight();
        }
    }
    public override bool Cancel()
    {
        if (!isMove)
        {
            return true;
        }
        else
        {
            EnableBoxItemAll();
            isMove = false;
            isInventory = !isInventory;
            _HighLight();
            return false;
        }
    }
    public override void UpdateAction()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (boxitems[i] != null)
            {
                buttons[i].sprite = boxitems[i].ItemState.Icon;
                itemTexts[i].text = boxitems[i].Num > 1 ? boxitems[i].Num.ToString() : "";
            }
            else
            {
                buttons[i].sprite = null;
                itemTexts[i].text = "";
            }
        }
        for (int i = 0; i < inventoryButtons.Length; i++)
        {
            if (player.Bag[i] != null)
            {
                inventoryButtons[i].sprite = player.Bag[i].ItemState.Icon;
                inventoryItemTexts[i].text = player.Bag[i].Num > 1 ? player.Bag[i].Num.ToString() : "";
            }
            else
            {
                inventoryButtons[i].sprite = null;
                inventoryItemTexts[i].text = "";
            }
        }
    }
    void EnableBoxItem()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            var eqaul = EqualItemType(boxitems[i]);
            if (eqaul)
            {
                buttons[i].color = Color.white;
            }
            else
            {
                buttons[i].color = Color.gray;
            }
        }
    }
    void EnableBoxItemAll()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].color = Color.white;
        }
    }
    bool EqualItemType(Item item)
    {
        var inventoryType = player.GetInventoryType(item);
        var inventoryPlayerType = (Player.InventoryType)inventoryIndex;
        return inventoryType == inventoryPlayerType;
    }
}

