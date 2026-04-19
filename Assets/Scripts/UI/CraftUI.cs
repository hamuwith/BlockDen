using UnityEngine;

public class CraftUI : BaseUI
{
    ItemAccess[] craftSlots;
    bool isMove;

    public override bool IsMakable => true;
    protected override int MaxIndex => buttons.Length;

    public override void Init(ItemManager itemManager)
    {
        InitBase(itemManager);
        craftSlots = new ItemAccess[buttons.Length];
        for (int i = 0; i < craftSlots.Length; i++)
            craftSlots[i].Id = -1;
    }

    public override void OpenUI(Player player)
    {
        OpenUIBase(player);
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
        change = (change == SelectState.DownOuterChange && isInventory) ||
                 (change == SelectState.UpOuterChange && !isInventory)
                 ? SelectState.NoChange : change;
        if (isMove) change = SelectState.NoChange;

        if (change == SelectState.NoChange)
        {
            _Select(vector);
            if (!isInventory && craftSlots[index].Id != -1)
            {
                var found = FindMaterialBagSlot(craftSlots[index]);
                if (found != -1) inventoryIndex = found;
            }
            _HighLight();
        }
        else
        {
            isInventory = !isInventory;
            _HighLight();
        }
        _Cursor();
    }

    public override void Action()
    {
        if (isInventory)
        {
            // MaterialBag から Craft スロットへ移動開始
            if (player.MaterialBag[inventoryIndex].Id == -1) return;
            isMove = true;
            isInventory = false;
            _HighLight();
            _Cursor();
        }
        else if (isMove)
        {
            // Craft スロットへ配置確定（空スロットのみ）
            if (craftSlots[index].Id != -1) return;
            var source = player.MaterialBag[inventoryIndex];
            craftSlots[index].Category = source.Category;
            craftSlots[index].Id = source.Id;
            craftSlots[index].Num = 1;
            player.MaterialBag[inventoryIndex].Num--;
            if (player.MaterialBag[inventoryIndex].Num <= 0)
            {
                player.MaterialBag[inventoryIndex].Id = -1;
                isMove = false;
                isInventory = true;
            }
            UpdateAction();
            _HighLight();
            _Cursor();
        }
        else
        {
            // Craft スロットから MaterialBag へ返却
            if (craftSlots[index].Id == -1) return;
            var bagSlot = FindMaterialBagSlot(craftSlots[index]);
            if (bagSlot == -1) return;
            inventoryIndex = bagSlot;
            ReturnSlotToBag(index);
            UpdateAction();
            _Cursor();
        }
    }

    public override bool Cancel()
    {
        if (isMove)
        {
            // MaterialBag → Craft への移動をキャンセル
            isMove = false;
            isInventory = true;
            _HighLight();
            _Cursor();
            return false;
        }
        if (!isInventory && craftSlots[index].Id != -1)
        {
            // Craft スロットのアイテムを MaterialBag へ返却
            var bagSlot = FindMaterialBagSlot(craftSlots[index]);
            if (bagSlot != -1)
            {
                inventoryIndex = bagSlot;
                ReturnSlotToBag(index);
                UpdateAction();
                _Cursor();
                return false;
            }
        }
        return true;
    }

    public override void UpdateAction()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (craftSlots[i].Id >= 0)
            {
                buttons[i].sprite = itemManager.GetItemIcon(craftSlots[i]);
            }
            else
            {
                buttons[i].sprite = null;
            }
        }
        for (int i = 0; i < inventoryButtons.Length; i++)
        {
            if (player.MaterialBag[i].Id != -1)
            {
                inventoryButtons[i].sprite = itemManager.GetItemIcon(player.MaterialBag[i]);
                inventoryItemTexts[i].text = player.MaterialBag[i].Num > 1 ? player.MaterialBag[i].Num.ToString() : "";
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
        index = 0;
        inventoryIndex = 0;
        isInventory = true;
        isMove = false;
        UpdateAction();
        _HighLight();
        _Cursor();
        canvas.enabled = true;
    }

    // 同じ素材スロットまたは空きスロットのインデックスを返す。空きなしは -1
    int FindMaterialBagSlot(ItemAccess item)
    {
        int emptySlot = -1;
        for (int i = 0; i < player.MaterialBag.Length; i++)
        {
            if (player.MaterialBag[i].Id != -1)
            {
                if (player.MaterialBag[i].Category == item.Category && player.MaterialBag[i].Id == item.Id)
                    return i;
            }
            else if (emptySlot == -1)
            {
                emptySlot = i;
            }
        }
        return emptySlot;
    }

    void ReturnSlotToBag(int slotIndex)
    {
        if (player.MaterialBag[inventoryIndex].Id == -1)
        {
            player.MaterialBag[inventoryIndex] = craftSlots[slotIndex];
            player.MaterialBag[inventoryIndex].Num = 1;
        }
        else
        {
            player.MaterialBag[inventoryIndex].Num++;
        }
        craftSlots[slotIndex].Id = -1;
    }
}
