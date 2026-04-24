using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static Item;
using System.Collections.Generic;

public class MakerUI : BaseUI
{
    [SerializeField] ItemCategory[] makableCategorys;
    [SerializeField] Menu menu;
    [SerializeField] Image[] bagButtons;
    [SerializeField] Canvas inventoryCanvas;
    [SerializeField] Canvas bagCanvas;
    ItemData[] makableItems;
    bool[] makable;
    ItemData makeItem;
    int bagIndex;
    bool isBag;
    TextMeshProUGUI[] bagItemTexts;
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
        inventoryCanvas.enabled = false;
        bagCanvas.enabled = false;
        for (int i = 0; i < makableItems.Length; i++)
        {
            if (i >= buttons.Length) break;
            buttons[i].sprite = makableItems[i].Icon;
        }
        if (bagButtons.Length > 0 && bagButtons[0].GetComponentInChildren<TextMeshProUGUI>() != null)
        {
            bagItemTexts = new TextMeshProUGUI[bagButtons.Length];
            for (int i = 0; i < bagButtons.Length; i++)
            {
                bagItemTexts[i] = bagButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                bagItemTexts[i].text = "";
            }
        }
    }
    public override void OpenUI(Player player)
    {
        this.player = player;
        makable = new bool[makableItems.Length];
        SetEnabled();
        for (int i = 0; i < makableItems.Length; i++)
        {
            if (i >= buttons.Length) break;
            buttons[i].sprite = makableItems[i].Icon;
        }
        inventoryCanvas.enabled = true;
        bagCanvas.enabled = true;
        menu.Init(itemManager);
        _Menu(true);
        OpenUIBase(player);
        _SelectIn(makableItems[index]);
        _Cursor();
    }
    public override void CloseUI()
    {
        canvas.enabled = false;
        inventoryCanvas.enabled = false;
        bagCanvas.enabled = false;
        if (player != null && !isBag) player.BagIndex = inventoryIndex;
        player = null;
    }
    public override void Select(Vector2 vector)
    {
        var change = _GetSelect(vector);
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
            RemoveMakeMaterials(makeItem.ItemMaterials);
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
        var isEnabled = false;
        for (int i = 0; i < makableItems.Length; i++)
        {
            makable[i] = true;
            foreach (var material in makableItems[i].ItemMaterials)
            {
                makable[i] &= GetMaterialNum(material) >= material.Num;
            }
            if (makable[i])
            {
                var type = player.GetInventoryType(makableItems[i].ItemAccess);
                if (type == Player.InventoryType.Material)
                {
                    makable[i] = !IsMaterialBagFull(makableItems[i].ItemAccess);
                }
                else if (type == Player.InventoryType.Food || type == Player.InventoryType.Carry)
                {
                    makable[i] = player.Bag[(int)type] == null;
                }
            }
            isEnabled |= makable[i];
        }
        return isEnabled;
    }
    int GetMaterialNum(ItemAccess material)
    {
        var haveItems = itemManager.GetBoxItemNum();
        var num = haveItems[(int)material.Category][material.Id];
        for (int i = 0; i < player.MaterialBag.Length; i++)
        {
            var bagItem = player.MaterialBag[i];
            if (bagItem.Id == -1) continue;
            if (bagItem.Category == material.Category && bagItem.Id == material.Id)
            {
                num += bagItem.Num;
            }
        }
        return num;
    }
    void RemoveMakeMaterials(List<ItemAccess> materials)
    {
        var haveItems = itemManager.GetBoxItemNum();
        foreach (var material in materials)
        {
            var remain = material.Num;
            var boxNum = haveItems[(int)material.Category][material.Id];
            var boxRemoveNum = Mathf.Min(remain, boxNum);
            haveItems[(int)material.Category][material.Id] -= boxRemoveNum;
            remain -= boxRemoveNum;

            if (remain <= 0) continue;

            for (int i = 0; i < player.MaterialBag.Length; i++)
            {
                var bagItem = player.MaterialBag[i];
                if (bagItem.Id == -1) continue;
                if (bagItem.Category != material.Category || bagItem.Id != material.Id) continue;

                var bagRemoveNum = Mathf.Min(remain, bagItem.Num);
                bagItem.Num -= bagRemoveNum;
                remain -= bagRemoveNum;
                player.MaterialBag[i] = bagItem.Num > 0 ? bagItem : new ItemAccess { Id = -1 };

                if (remain <= 0) break;
            }
        }
    }
    public override void UpdateAction()
    {
        SetEnabled();
        for (int i = 0; i < makableItems.Length; i++)
        {
            if (i >= buttons.Length) break;
            buttons[i].color = makable[i] ? Color.white : Color.gray;
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
        for (int i = 0; i < bagButtons.Length; i++)
        {
            if (i < player.MaterialBag.Length && player.MaterialBag[i].Id != -1)
            {
                bagButtons[i].sprite = itemManager.GetItemIcon(player.MaterialBag[i]);
                if (bagItemTexts != null)
                    bagItemTexts[i].text = player.MaterialBag[i].Num > 1 ? player.MaterialBag[i].Num.ToString() : "";
            }
            else
            {
                bagButtons[i].sprite = null;
                if (bagItemTexts != null)
                    bagItemTexts[i].text = "";
            }
        }
    }
    protected override void _HighLight()
    {
        highlightMaterial.SetFloat(sliceWidthId, isInventory ? 0.0f : 0.1f);
        inventoryHighlightMaterial.SetFloat(sliceWidthId, isInventory ? 0.1f : 0.0f);
    }
    protected void _SelectIn(ItemDataBase item = null)
    {
        var type = player.GetInventoryType(item.ItemAccess);
        if (type == Player.InventoryType.Null)
        {
            inventoryIndex = 0;
            isBag = false;
        }
        else if (type == Player.InventoryType.Material)
        {
            isBag = true;
            bagIndex = FindMaterialBagIndex(item.ItemAccess);
        }
        else if (type == Player.InventoryType.Tool)
        {
            inventoryIndex = (int)type;
            isBag = false;
            player.ChangeTool((item as BreakToolData).BlockType);
        }
        else
        {
            inventoryIndex = (int)type;
            isBag = false;
        }
    }
    protected override void _Cursor()
    {
        if (isBag && bagButtons != null && bagIndex >= 0 && bagIndex < bagButtons.Length)
        {
            inventoryHighlight.transform.position = bagButtons[bagIndex].transform.position;
            inventoryCanvas.sortingOrder = 10;
            bagCanvas.sortingOrder = 11;
        }
        else
        {
            inventoryHighlight.transform.position = inventoryButtons[inventoryIndex].transform.position;
            inventoryCanvas.sortingOrder = 11;
            bagCanvas.sortingOrder = 10;
        }
        highlight.transform.position = buttons[index].transform.position;
    }
    int FindMaterialBagIndex(ItemAccess itemAccess)
    {
        int firstNull = -1;
        for (int i = 0; i < player.MaterialBag.Length; i++)
        {
            if (player.MaterialBag[i].Id == -1)
            {
                if (firstNull == -1) firstNull = i;
            }
            else if (player.MaterialBag[i].Category == itemAccess.Category &&
                     player.MaterialBag[i].Id == itemAccess.Id)
            {
                return i;
            }
        }
        return firstNull != -1 ? firstNull : 0;
    }
    bool IsMaterialBagFull(ItemAccess itemAccess)
    {
        for (int i = 0; i < player.MaterialBag.Length; i++)
        {
            if (player.MaterialBag[i].Id == -1) return false;
            if (player.MaterialBag[i].Category == itemAccess.Category &&
                player.MaterialBag[i].Id == itemAccess.Id) return false;
        }
        return true;
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
