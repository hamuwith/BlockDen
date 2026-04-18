using UnityEngine;
using System.Collections.Generic;

public class AttachUI : MakerUI
{
    protected List<ItemAccess> attachmentItems;
    protected bool isMove;
    protected int attachmentIndex;
    protected virtual Item.ItemCategory ItemCategory => Item.ItemCategory.Status;
    public override void Init(ItemManager itemManager)
    {
        InitBase(itemManager);
        attachmentItems = new List<ItemAccess>();
    }
    public override void OpenUI(Player player)
    {
        attachmentIndex = attachmentItems.Count;
        OpenUIBase(player);
        for (int i = 0; i < inventoryButtons.Length; i++)
        {
            if (i == inventoryIndex)
            {
                if (player.Bag[i]?.ItemAccess.Category == ItemCategory)
                {
                    inventoryButtons[i].color = player.Bag[inventoryIndex].ItemAccess.Category != ItemCategory ? Color.gray : Color.white;
                }
            }
            else
            {
                inventoryButtons[i].color = Color.gray;
            }
        }
    }
    public override void CloseUI()
    {
        canvas.enabled = false;
        player.CloseToolUI();
        player = null;
    }
    public ItemAccess? GetAttachedItem()
    {
        if (attachmentIndex == attachmentItems.Count)
        {
            return null;
        }
        else
        {
            return attachmentItems[attachmentIndex];
        }
    }
    public override void Select(Vector2 vector)
    {
    }
    public override void Action()
    {
        if (!isInventory)
        {
            if(isMove)
            {
                ItemAccess boxItem = player.Bag[inventoryIndex].ItemAccess;
                attachmentItems.Add(boxItem);
                player.BagReduce(1, inventoryIndex);
                if (player.Bag[inventoryIndex] != null)
                {
                    isInventory = true;
                    index++;
                }
                UpdateAction();
                isMove = false;
            }
            else
            {
                isInventory = true;
                _HighLight();
                isMove = true;
            }
        }
        else
        {
            if (!isMove)
            {
                if (player.Bag[inventoryIndex]?.ItemAccess.Category == ItemCategory)
                {
                    isInventory = false;
                    _HighLight();
                    isMove = true;
                }
            }
            else
            {
                player.BagUpdate(attachmentItems[attachmentIndex], false);
                attachmentItems.RemoveAt(attachmentItems.Count - 1);
                UpdateAction();
                isMove = false;
            }
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
            if (i < attachmentItems.Count)
            {
                buttons[i].sprite = itemManager.GetItemIcon(attachmentItems[i]);
                itemTexts[i].text = attachmentItems[i].Num > 1 ? attachmentItems[i].Num.ToString() : "";
                buttons[i].color = i < attachmentIndex ? Color.gray : Color.white;
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
            inventoryButtons[i].color = player.Bag[i]?.ItemAccess.Category == ItemCategory ? Color.white : Color.gray;
        }
    }
    protected override void OpenUIBase(Player player)
    {
        this.player = player;
        index = attachmentItems.Count;
        inventoryIndex = (int)Player.InventoryType.Carry;
        isInventory = true;
        UpdateAction();
        _HighLight();
        _Cursor();
        canvas.enabled = true;
    }
}
