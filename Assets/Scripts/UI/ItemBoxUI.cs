using UnityEngine;
using static Player;

public class ItemBoxUI : BaseUI
{
    // buttons[] from BaseUI = box slots
    // inventoryButtons[0] from BaseUI = player.Bag slot

    protected ItemAccess[] boxItems;
    bool isMove;
    InventoryType inventoryType;

    protected virtual int BagIndex => (int)inventoryType;

    public void Init(ItemManager itemManager, InventoryType inventoryType)
    {
        InitBase(itemManager);
        this.inventoryType = inventoryType;
        boxItems = new ItemAccess[buttons.Length];
        for (int i = 0; i < boxItems.Length; i++) boxItems[i].Id = -1;
    }

    public override void OpenUI(Player player)
    {
        this.player = player;
        inventoryIndex = BagIndex;
        Debug.Log($"Open ItemBoxUI with inventoryIndex: {inventoryIndex}");
        isMove = false;
        index = 0;
        isInventory = false;
        UpdateAction();
        _HighLight();
        _Cursor();
        canvas.enabled = true;
    }

    public override void CloseUI()
    {
        canvas.enabled = false;
        player.CloseToolUI();
        player = null;
    }

    public override void Select(Vector2 vector)
    {
        var change = _GetSelect(vector);
        if (isMove) change = SelectState.NoChange;
        if (change == SelectState.UpOuterChange && !isInventory) change = SelectState.NoChange;
        if (change == SelectState.DownOuterChange && isInventory) change = SelectState.NoChange;

        if (change == SelectState.NoChange)
        {
            if (!isInventory) _Select(vector);
            _HighLight();
        }
        else if (change == SelectState.DownOuterChange)
        {
            isInventory = true;
            _HighLight();
        }
        else
        {
            isInventory = false;
            _HighLight();
        }
        _Cursor();
    }

    public override void Action()
    {
        if (isMove)
        {
            CompleteMove();
            isMove = false;
            UpdateAction();
        }
        else
        {
            if (!isInventory && boxItems[index].Id == -1) return;
            if (isInventory && player.Bag[BagIndex] == null) return;
            isMove = true;
            isInventory = !isInventory;
            _HighLight();
        }
    }

    public override bool Cancel()
    {
        if (!isMove) return true;
        isMove = false;
        isInventory = !isInventory;
        _HighLight();
        return false;
    }

    void CompleteMove()
    {
        var boxItem = boxItems[index];
        if (player.Bag[BagIndex] != null)
            boxItems[index] = player.Bag[BagIndex].ItemAccess;
        if (boxItem.Id == -1)
        {
            player.BagReduce(player.Bag[BagIndex].Num, BagIndex);
        }
        else if (player.Bag[BagIndex] == null)
        {
            boxItems[index].Id = -1;
            player.BagUpdate(boxItem, false);
        }
        else if (boxItem.Category == player.Bag[BagIndex].ItemAccess.Category &&
                 boxItem.Id == player.Bag[BagIndex].ItemAccess.Id)
        {
            if (isInventory)
            {
                var num = player.BagUpdate(boxItem, false);
                boxItems[index].Num -= num;
                if (boxItems[index].Num <= 0) boxItems[index].Id = -1;
            }
            else
            {
                int maxNum = itemManager.GetItem(boxItems[index]).MaxNum;
                boxItems[index].Num += player.Bag[BagIndex].Num;
                if (boxItems[index].Num > maxNum)
                {
                    int overflow = boxItems[index].Num - maxNum;
                    boxItems[index].Num = maxNum;
                    bool filled = true;
                    for (int i = 0; i < boxItems.Length; i++)
                    {
                        if (boxItems[i].Id == -1)
                        {
                            boxItems[i] = player.Bag[BagIndex].ItemAccess;
                            boxItems[i].Num = overflow;
                            player.BagReduce(player.Bag[BagIndex].Num, BagIndex);
                            filled = false;
                            break;
                        }
                    }
                    if (filled)
                    {
                        player.BagReduce(player.Bag[BagIndex].Num - overflow, BagIndex);
                        isInventory = true;
                    }
                }
                else
                {
                    player.BagReduce(player.Bag[BagIndex].Num, BagIndex);
                }
            }
        }
    }

    public override void UpdateAction()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (boxItems[i].Id >= 0)
            {
                buttons[i].sprite = itemManager.GetItemIcon(boxItems[i]);
                if (itemTexts != null) itemTexts[i].text = boxItems[i].Num > 1 ? boxItems[i].Num.ToString() : "";
            }
            else
            {
                buttons[i].sprite = null;
                if (itemTexts != null) itemTexts[i].text = "";
            }
        }
        for (int i = 0; i < inventoryButtons.Length; i++)
        {
            if (player.Bag[i] != null)
            {
                inventoryButtons[i].sprite = itemManager.GetItemIcon(player.Bag[i].ItemAccess);
                if (inventoryItemTexts != null && i < inventoryItemTexts.Length)
                    inventoryItemTexts[i].text = player.Bag[i].Num > 1 ? player.Bag[i].Num.ToString() : "";
            }
            else
            {
                inventoryButtons[i].sprite = null;
                if (inventoryItemTexts != null && i < inventoryItemTexts.Length)
                    inventoryItemTexts[i].text = "";
            }
        }
    }

    protected override void _HighLight()
    {
        highlightMaterial.SetFloat(sliceWidthId, isInventory ? 0.0f : 0.1f);
        inventoryHighlightMaterial.SetFloat(sliceWidthId, isInventory ? 0.1f : 0.0f);
    }

    protected override void _Cursor()
    {
        highlight.transform.position = buttons[index].transform.position;
        inventoryHighlight.transform.position = inventoryButtons[inventoryIndex].transform.position;
    }
}
