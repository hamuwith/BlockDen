using TMPro;
using UnityEditor.Build;
using UnityEngine;
using static Player;

public class BoxUI : MakerUI
{
    BoxItem[] boxitems;
    bool isMove;
    public override bool IsMakable => true;
    protected override int MaxIndex => buttons.Length;
    protected virtual InventoryType InventoryType => InventoryType.Carry;
    public override void Init(ItemManager itemManager)
    {
        InitBase(itemManager);
        boxitems = new BoxItem[buttons.Length];
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
                boxitems[index] = new BoxItem
                {
                    ItemState = player.Bag[inventoryIndex].ItemState,
                    Num = player.Bag[inventoryIndex].Num
                };
            }
            if (boxItem == null)
            {
                boxitems[index] = new BoxItem
                {
                    ItemState = player.Bag[inventoryIndex].ItemState,
                    Num = player.Bag[inventoryIndex].Num
                };
                player.BagReduce(player.Bag[inventoryIndex].Num, inventoryIndex);
            }
            else if(player.Bag[inventoryIndex] == null)
            {
                boxitems[index] = null;
                player.BagUpdate(boxItem, false);
            }
            else
            {
                if(boxItem.ItemState.ItemType == player.Bag[inventoryIndex].ItemState.ItemType && boxItem.ItemState.Id == player.Bag[inventoryIndex].ItemState.Id)
                {
                    if (isInventory)
                    {
                        var num = player.BagUpdate(boxItem, false);
                        boxitems[index].Num -= num;
                        if(boxitems[index].Num <= 0)
                        {
                            boxitems[index] = null;
                        }
                    }
                    else
                    {
                        boxitems[index].Num += player.Bag[inventoryIndex].Num;
                        if(boxitems[index].Num > boxitems[index].ItemState.MaxNum)
                        {
                            var num = boxitems[index].Num - boxitems[index].ItemState.MaxNum;
                            boxitems[index].Num = boxitems[index].ItemState.MaxNum;
                            bool filled = true;
                            for (int i = 0; i < boxitems.Length; i++)
                            {
                                if(boxitems[i] == null)
                                {
                                    boxitems[i] = new BoxItem
                                    {
                                        ItemState = player.Bag[inventoryIndex].ItemState,
                                        Num = num
                                    };
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
                if (boxitems[index] == null) return;
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
    protected override void OpenUIBase(Player player)
    {
        this.player = player;
        inventoryIndex = (int)InventoryType;
        var item = player.Bag[(int)InventoryType];
        Debug.Log(inventoryIndex);
        Debug.Log(item);
        isInventory = item != null;
        index = -1;
        if (item != null)
        {
            for (int i = 0; i < boxitems.Length; i++)
            {
                var boxItem = boxitems[i];
                if (boxItem != null)
                {
                    if (item.ItemState.ItemType == boxItem.ItemState.ItemType && item.ItemState.Id == boxItem.ItemState.Id)
                    {
                        if (item.Num < boxItem.ItemState.MaxNum)
                        {
                            index = i;
                            isInventory = false;
                            break;
                        }
                        else if (boxItem.Num < boxItem.ItemState.MaxNum)
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

