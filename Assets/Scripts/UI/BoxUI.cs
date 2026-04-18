using TMPro;
using UnityEditor.Build;
using UnityEngine;
using static Player;

public class BoxUI : MakerUI
{
    ItemAccess[] boxitems;
    bool isMove;
    public override bool IsMakable => true;
    protected override int MaxIndex => buttons.Length;
    protected virtual InventoryType InventoryType => InventoryType.Carry;
    public override void Init(ItemManager itemManager)
    {
        InitBase(itemManager);
        boxitems = new ItemAccess[buttons.Length];
        for (int i = 0; i < boxitems.Length; i++)
        {
            boxitems[i].Id = -1;
        }
    }
    /// <summary>
    /// ツールのUIを開く際の初期化を行うメソッド
    /// </summary>
    /// <param name="player"></param>
    public override void OpenUI(Player player)
    {
        OpenUIBase(player);
    }
    public override void CloseUI()
    {
        canvas.enabled = false;
        player = null;
    }
    /// <summary>
    /// ツールのUIでボタンを選択するメソッド
    /// </summary>
    /// <param name="vector"></param>
    public override void Select(Vector2 vector)
    {
        var change = _GetSelect(vector);
        change = (change == SelectState.DownOuterChange && isInventory) || (change == SelectState.UpOuterChange && !isInventory) ? SelectState.NoChange : change;
        if (isMove) change = SelectState.NoChange;
        if (change == SelectState.NoChange)
        {
            if (!isInventory)
            {
                _Select(vector);
                _HighLight();                
            }
        }
        else
        {
            if (player.Bag[inventoryIndex] == null) return;
            isInventory = !isInventory;
            _HighLight();
        }
        _Cursor();
    }
    /// <summary>
    /// ツールのアクションを実行するメソッド
    /// </summary>
    public override void Action()
    {
        if (isMove)
        {
            var boxItem = boxitems[index];
            if (player.Bag[inventoryIndex] != null)
            {
                boxitems[index] = player.Bag[inventoryIndex].ItemAccess;
            }
            if (boxItem.Id == -1)
            {
                boxitems[index] = player.Bag[inventoryIndex].ItemAccess;
                player.BagReduce(player.Bag[inventoryIndex].Num, inventoryIndex);
            }
            else if(player.Bag[inventoryIndex] == null)
            {
                boxitems[index].Id = -1;
                player.BagUpdate(boxItem, false);
            }
            else
            {
                if(boxItem.Category == player.Bag[inventoryIndex].ItemAccess.Category && boxItem.Id == player.Bag[inventoryIndex].ItemAccess.Id)
                {
                    if (isInventory)
                    {
                        var num = player.BagUpdate(boxItem, false);
                        boxitems[index].Num -= num;
                        if(boxitems[index].Num <= 0)
                        {
                            boxitems[index].Id = -1;
                        }
                    }
                    else
                    {
                        var maxNum = itemManager.GetItem(boxitems[index]).MaxNum;
                        boxitems[index].Num += player.Bag[inventoryIndex].Num;
                        if(boxitems[index].Num > maxNum)
                        {
                            var num = boxitems[index].Num - maxNum;
                            boxitems[index].Num = maxNum;
                            bool filled = true;
                            for (int i = 0; i < boxitems.Length; i++)
                            {
                                if(boxitems[i].Id == -1)
                                {
                                    boxitems[i] = player.Bag[inventoryIndex].ItemAccess;
                                    boxitems[i].Num = num;
                                    player.BagReduce(player.Bag[inventoryIndex].Num, inventoryIndex);
                                    filled = false;
                                    break;
                                }
                            }
                            if (filled)
                            {
                                player.BagReduce(player.Bag[inventoryIndex].Num - num, inventoryIndex);
                                isInventory = true;
                            }
                        }
                        else
                        {
                            player.BagReduce(player.Bag[inventoryIndex].Num, inventoryIndex);
                        }
                    }
                }
            }
            isMove = false;
            UpdateAction();
        }
        else
        {
            if (isInventory)
            {
                if (player.Bag[inventoryIndex] == null) return;
            }
            else
            {
                if (boxitems[index].Id == -1) return;
            }
            isMove = true;
            isInventory = !isInventory;
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
            if (boxitems[i].Id >= 0)
            {
                buttons[i].sprite = itemManager.GetItemIcon(boxitems[i]);
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
    protected override void OpenUIBase(Player player)
    {
        this.player = player;
        inventoryIndex = (int)InventoryType;
        var item = player.Bag[(int)InventoryType];
        isInventory = item != null;
        index = -1;
        if (item != null)
        {
            var maxNum = itemManager.GetItem(item.ItemAccess).MaxNum;
            for (int i = 0; i < boxitems.Length; i++)
            {
                var boxItem = boxitems[i];
                if (boxItem.Id != -1)
                {
                    if (item.ItemAccess.Category == boxItem.Category && item.ItemAccess.Id == boxItem.Id)
                    {
                        if (item.Num < maxNum)
                        {
                            index = i;
                            isInventory = false;
                            break;
                        }
                        else if (boxItem.Num < maxNum)
                        {
                            index = i;
                            break;
                        }
                    }
                }
                else if (index == -1)
                {
                    index = i;
                }
            }
        }
        if(index == -1)
        {
            index = 0;
        }
        UpdateAction();
        _HighLight();
        _Cursor();
        canvas.enabled = true;
    }
}

