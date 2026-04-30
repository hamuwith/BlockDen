using UnityEngine;
using static Player;
using static Item;

public class BoxUI : BaseUI
{
    bool isMove;

    public override bool IsMakable => true;
    int[] MaterialBoxData => itemManager.GetBoxItemNum()[(int)ItemCategory.Material];

    public override void Init(ItemManager itemManager)
    {
        InitBase(itemManager);
    }

    public override void OpenUI(Player player)
    {
        this.player = player;
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
            if (isInventory)
            {
                _Select(vector);
                if (!isMove && inventoryIndex < player.MaterialBag.Length && player.MaterialBag[inventoryIndex].Id != -1)
                    index = player.MaterialBag[inventoryIndex].Id + 1;
            }
            else if (!isMove)
            {
                _Select(vector);
            }
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
        if (!isMove && !isInventory && index == 0)
        {
            var matData = MaterialBoxData;
            for (int i = 0; i < player.MaterialBag.Length; i++)
            {
                if (player.MaterialBag[i].Id != -1)
                {
                    matData[player.MaterialBag[i].Id] += player.MaterialBag[i].Num;
                    player.MaterialBag[i] = new ItemAccess { Id = -1 };
                }
            }
            UpdateAction();
            return;
        }
        if (isMove)
        {
            if (CompleteMaterialMove()) isMove = false;
            UpdateAction();
        }
        else
        {
            if (!isInventory && index > 0 && MaterialBoxData[index - 1] == 0) return;
            if (isInventory && player.MaterialBag[inventoryIndex].Id == -1) return;
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

    bool CompleteMaterialMove()
    {
        int materialId = index - 1;
        var matData = MaterialBoxData;
        int boxCount = matData[materialId];
        var bagSlot = player.MaterialBag[inventoryIndex];

        if (boxCount > 0 && bagSlot.Id == -1)
        {
            var access = new ItemAccess { Category = ItemCategory.Material, Id = materialId };
            var itemData = itemManager.GetItem(access);
            int transfer = Mathf.Min(boxCount, itemData.UnitNum);
            player.MaterialBag[inventoryIndex] = new ItemAccess { Category = ItemCategory.Material, Id = materialId, Num = transfer };
            matData[materialId] -= transfer;
            return matData[materialId] == 0;
        }
        else if (bagSlot.Id != -1 && bagSlot.Category == ItemCategory.Material && bagSlot.Id == materialId)
        {
            if (boxCount == 0)
            {
                matData[materialId] += bagSlot.Num;
                player.MaterialBag[inventoryIndex] = new ItemAccess { Id = -1 };
                return true;
            }
            else if (isInventory)
            {
                int maxNum = itemManager.GetItem(bagSlot).MaxNum;
                int unitNum = itemManager.GetItem(bagSlot).UnitNum;
                int transfer = Mathf.Min(boxCount, Mathf.Min(unitNum, maxNum - bagSlot.Num));
                if (transfer > 0)
                {
                    player.MaterialBag[inventoryIndex].Num += transfer;
                    matData[materialId] -= transfer;
                }
                return matData[materialId] == 0 || player.MaterialBag[inventoryIndex].Num >= maxNum;
            }
            else
            {
                matData[materialId] += bagSlot.Num;
                player.MaterialBag[inventoryIndex] = new ItemAccess { Id = -1 };
                return true;
            }
        }
        return true;
    }

    public override void UpdateAction()
    {
        var matData = MaterialBoxData;
        buttons[0].sprite = null;
        if (itemTexts != null) itemTexts[0].text = "";
        for (int i = 1; i < buttons.Length; i++)
        {
            int matId = i - 1;
            if (matId < matData.Length && matData[matId] > 0)
            {
                buttons[i].sprite = itemManager.GetItemIcon(new ItemAccess { Category = ItemCategory.Material, Id = matId });
                if (itemTexts != null) itemTexts[i].text = matData[matId] > 1 ? matData[matId].ToString() : "";
            }
            else
            {
                buttons[i].sprite = null;
                if (itemTexts != null) itemTexts[i].text = "";
            }
        }
        for (int i = 0; i < inventoryButtons.Length; i++)
        {
            if (i < player.MaterialBag.Length && player.MaterialBag[i].Id != -1)
            {
                inventoryButtons[i].sprite = itemManager.GetItemIcon(player.MaterialBag[i]);
                if (inventoryItemTexts != null) inventoryItemTexts[i].text = player.MaterialBag[i].Num > 1 ? player.MaterialBag[i].Num.ToString() : "";
            }
            else
            {
                inventoryButtons[i].sprite = null;
                if (inventoryItemTexts != null) inventoryItemTexts[i].text = "";
            }
        }
    }
}
