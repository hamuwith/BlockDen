using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static Player;
using static Item;

public class BoxUI : BaseUI
{
    [SerializeField] Image[] materialButtons;
    [SerializeField] Image[] carryButtons;
    // buttons[] from BaseUI = foodButtons
    [SerializeField] Canvas inventoryCanvas;
    [SerializeField] Canvas materialBagCanvas;
    [SerializeField] Image[] materialBagButtons;

    enum BoxSection { Material, Carry, Food }
    BoxSection section;
    ItemAccess[] carryItems;
    ItemAccess[] foodItems;
    bool isMove;
    TextMeshProUGUI[] materialBoxTexts;
    TextMeshProUGUI[] carryBoxTexts;
    TextMeshProUGUI[] materialBagTexts;

    public override bool IsMakable => true;
    protected override int MaxIndex => ActiveBoxButtons.Length;
    protected override Image[] ActiveBoxButtons =>
        section == BoxSection.Material ? materialButtons :
        section == BoxSection.Carry ? carryButtons :
        buttons;

    ItemAccess[] ActiveItems => section == BoxSection.Carry ? carryItems : foodItems;
    int[] MaterialBoxData => itemManager.GetBoxItemNum()[(int)ItemCategory.Material];

    public override void Init(ItemManager itemManager)
    {
        InitBase(itemManager);
        carryItems = new ItemAccess[carryButtons.Length];
        for (int i = 0; i < carryItems.Length; i++) carryItems[i].Id = -1;
        foodItems = new ItemAccess[buttons.Length];
        for (int i = 0; i < foodItems.Length; i++) foodItems[i].Id = -1;
        materialBoxTexts = InitTexts(materialButtons);
        carryBoxTexts = InitTexts(carryButtons);
        materialBagTexts = InitTexts(materialBagButtons);
        inventoryCanvas.enabled = false;
        materialBagCanvas.enabled = false;
    }

    TextMeshProUGUI[] InitTexts(Image[] imgs)
    {
        if (imgs.Length == 0 || imgs[0].GetComponentInChildren<TextMeshProUGUI>() == null) return null;
        var texts = new TextMeshProUGUI[imgs.Length];
        for (int i = 0; i < imgs.Length; i++)
        {
            texts[i] = imgs[i].GetComponentInChildren<TextMeshProUGUI>();
            texts[i].text = "";
        }
        return texts;
    }

    public override void OpenUI(Player player)
    {
        inventoryCanvas.enabled = true;
        materialBagCanvas.enabled = true;
        this.player = player;
        section = BoxSection.Material;
        isMove = false;
        index = 0;
        inventoryIndex = 0;
        isInventory = false;
        UpdateCanvasSorting();
        OpenUIBase(player);
    }

    public override void CloseUI()
    {
        canvas.enabled = false;
        inventoryCanvas.enabled = false;
        materialBagCanvas.enabled = false;
        player = null;
    }

    public override void Select(Vector2 vector)
    {
        var change = _GetSelect(vector);
        if (isMove) change = SelectState.NoChange;
        if (change == SelectState.DownOuterChange && isInventory) change = SelectState.NoChange;
        if (change == SelectState.UpOuterChange && !isInventory) change = SelectState.NoChange;

        if (change == SelectState.NoChange)
        {
            if (!isInventory) _Select(vector);
            _HighLight();
        }
        else if (change == SelectState.DownOuterChange)
        {
            if (section == BoxSection.Material) SwitchSection(BoxSection.Carry);
            else if (section == BoxSection.Carry) SwitchSection(BoxSection.Food);
            else { isInventory = true; _HighLight(); }
        }
        else // UpOuterChange
        {
            if (isInventory) { isInventory = false; _HighLight(); }
            else if (section == BoxSection.Food) SwitchSection(BoxSection.Carry);
            else if (section == BoxSection.Carry) SwitchSection(BoxSection.Material);
        }
        _Cursor();
    }

    void SwitchSection(BoxSection next)
    {
        section = next;
        index = 0;
        isInventory = false;
        isMove = false;
        if (section == BoxSection.Carry) inventoryIndex = (int)InventoryType.Carry;
        else if (section == BoxSection.Food) inventoryIndex = (int)InventoryType.Food;
        else inventoryIndex = 0;
        UpdateCanvasSorting();
        _HighLight();
    }

    void UpdateCanvasSorting()
    {
        inventoryCanvas.sortingOrder = section != BoxSection.Material ? 11 : 10;
        materialBagCanvas.sortingOrder = section == BoxSection.Material ? 11 : 10;
    }

    public override void Action()
    {
        if (isMove)
        {
            if (section == BoxSection.Material) CompleteMaterialMove();
            else CompleteCarryFoodMove();
            isMove = false;
            UpdateAction();
        }
        else
        {
            bool boxEmpty = section == BoxSection.Material
                ? MaterialBoxData[index] == 0
                : ActiveItems[index].Id == -1;
            bool bagEmpty = section == BoxSection.Material
                ? player.MaterialBag[inventoryIndex].Id == -1
                : player.Bag[inventoryIndex] == null;
            if (isInventory && bagEmpty) return;
            if (!isInventory && boxEmpty) return;
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

    void CompleteMaterialMove()
    {
        int materialId = index;
        var matData = MaterialBoxData;
        int boxCount = matData[materialId];
        var bagSlot = player.MaterialBag[inventoryIndex];

        if (boxCount > 0 && bagSlot.Id == -1)
        {
            // Box → Bag (empty slot)
            var access = new ItemAccess { Category = ItemCategory.Material, Id = materialId };
            int maxNum = itemManager.GetItem(access).MaxNum;
            int transfer = Mathf.Min(boxCount, maxNum);
            player.MaterialBag[inventoryIndex] = new ItemAccess { Category = ItemCategory.Material, Id = materialId, Num = transfer };
            matData[materialId] -= transfer;
        }
        else if (bagSlot.Id != -1 && bagSlot.Category == ItemCategory.Material && bagSlot.Id == materialId)
        {
            if (boxCount == 0) // Bag → Box
            {
                matData[materialId] += bagSlot.Num;
                player.MaterialBag[inventoryIndex] = new ItemAccess { Id = -1 };
            }
            else if (isInventory) // came from box → stack into bag
            {
                int maxNum = itemManager.GetItem(bagSlot).MaxNum;
                int transfer = Mathf.Min(boxCount, maxNum - bagSlot.Num);
                if (transfer > 0)
                {
                    player.MaterialBag[inventoryIndex].Num += transfer;
                    matData[materialId] -= transfer;
                }
            }
            else // came from bag → dump into box
            {
                matData[materialId] += bagSlot.Num;
                player.MaterialBag[inventoryIndex] = new ItemAccess { Id = -1 };
            }
        }
    }

    void CompleteCarryFoodMove()
    {
        var items = ActiveItems;
        var boxItem = items[index];
        if (player.Bag[inventoryIndex] != null)
        {
            items[index] = player.Bag[inventoryIndex].ItemAccess;
        }
        if (boxItem.Id == -1)
        {
            items[index] = player.Bag[inventoryIndex].ItemAccess;
            player.BagReduce(player.Bag[inventoryIndex].Num, inventoryIndex);
        }
        else if (player.Bag[inventoryIndex] == null)
        {
            items[index].Id = -1;
            player.BagUpdate(boxItem, false);
        }
        else if (boxItem.Category == player.Bag[inventoryIndex].ItemAccess.Category &&
                 boxItem.Id == player.Bag[inventoryIndex].ItemAccess.Id)
        {
            if (isInventory) // box → bag
            {
                var num = player.BagUpdate(boxItem, false);
                items[index].Num -= num;
                if (items[index].Num <= 0) items[index].Id = -1;
            }
            else // bag → box
            {
                int maxNum = itemManager.GetItem(items[index]).MaxNum;
                items[index].Num += player.Bag[inventoryIndex].Num;
                if (items[index].Num > maxNum)
                {
                    int overflow = items[index].Num - maxNum;
                    items[index].Num = maxNum;
                    bool filled = true;
                    for (int i = 0; i < items.Length; i++)
                    {
                        if (items[i].Id == -1)
                        {
                            items[i] = player.Bag[inventoryIndex].ItemAccess;
                            items[i].Num = overflow;
                            player.BagReduce(player.Bag[inventoryIndex].Num, inventoryIndex);
                            filled = false;
                            break;
                        }
                    }
                    if (filled)
                    {
                        player.BagReduce(player.Bag[inventoryIndex].Num - overflow, inventoryIndex);
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

    public override void UpdateAction()
    {
        var matData = MaterialBoxData;
        for (int i = 0; i < materialButtons.Length; i++)
        {
            if (i < matData.Length && matData[i] > 0)
            {
                materialButtons[i].sprite = itemManager.GetItemIcon(new ItemAccess { Category = ItemCategory.Material, Id = i });
                SetText(materialBoxTexts, i, matData[i] > 1 ? matData[i].ToString() : "");
            }
            else
            {
                materialButtons[i].sprite = null;
                SetText(materialBoxTexts, i, "");
            }
        }
        for (int i = 0; i < carryButtons.Length; i++)
        {
            if (carryItems[i].Id >= 0)
            {
                carryButtons[i].sprite = itemManager.GetItemIcon(carryItems[i]);
                SetText(carryBoxTexts, i, carryItems[i].Num > 1 ? carryItems[i].Num.ToString() : "");
            }
            else
            {
                carryButtons[i].sprite = null;
                SetText(carryBoxTexts, i, "");
            }
        }
        for (int i = 0; i < buttons.Length; i++)
        {
            if (foodItems[i].Id >= 0)
            {
                buttons[i].sprite = itemManager.GetItemIcon(foodItems[i]);
                if (itemTexts != null) itemTexts[i].text = foodItems[i].Num > 1 ? foodItems[i].Num.ToString() : "";
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
                if (inventoryItemTexts != null) inventoryItemTexts[i].text = player.Bag[i].Num > 1 ? player.Bag[i].Num.ToString() : "";
            }
            else
            {
                inventoryButtons[i].sprite = null;
                if (inventoryItemTexts != null) inventoryItemTexts[i].text = "";
            }
        }
        for (int i = 0; i < materialBagButtons.Length; i++)
        {
            if (i < player.MaterialBag.Length && player.MaterialBag[i].Id != -1)
            {
                materialBagButtons[i].sprite = itemManager.GetItemIcon(player.MaterialBag[i]);
                SetText(materialBagTexts, i, player.MaterialBag[i].Num > 1 ? player.MaterialBag[i].Num.ToString() : "");
            }
            else
            {
                materialBagButtons[i].sprite = null;
                SetText(materialBagTexts, i, "");
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
        highlight.transform.position = ActiveBoxButtons[index].transform.position;
        if (section == BoxSection.Material)
        {
            if (inventoryIndex < materialBagButtons.Length)
                inventoryHighlight.transform.position = materialBagButtons[inventoryIndex].transform.position;
        }
        else
        {
            if (inventoryIndex < inventoryButtons.Length)
                inventoryHighlight.transform.position = inventoryButtons[inventoryIndex].transform.position;
        }
    }

    protected override void OpenUIBase(Player player)
    {
        this.player = player;
        AutoSelectIndex();
        UpdateAction();
        _HighLight();
        _Cursor();
        canvas.enabled = true;
    }

    void AutoSelectIndex()
    {
        if (section == BoxSection.Material)
        {
            index = 0;
            inventoryIndex = 0;
            isInventory = false;
            return;
        }
        var invType = section == BoxSection.Carry ? InventoryType.Carry : InventoryType.Food;
        inventoryIndex = (int)invType;
        var bagItem = player.Bag[(int)invType];
        isInventory = bagItem != null;
        var items = ActiveItems;
        index = -1;
        if (bagItem != null)
        {
            int maxNum = itemManager.GetItem(bagItem.ItemAccess).MaxNum;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].Id != -1 && bagItem.ItemAccess.Category == items[i].Category && bagItem.ItemAccess.Id == items[i].Id)
                {
                    if (bagItem.Num < maxNum) { index = i; isInventory = false; break; }
                    else if (items[i].Num < maxNum) { index = i; break; }
                }
                else if (index == -1) index = i;
            }
        }
        if (index == -1) index = 0;
    }

    void SetText(TextMeshProUGUI[] texts, int i, string text)
    {
        if (texts != null && i < texts.Length) texts[i].text = text;
    }
}
