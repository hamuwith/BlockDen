using UnityEngine;
using System.Collections.Generic;

public class AttachUI : BaseUI
{
    private const int BoardSize = 5;
    private const int ShapeMaxSize = 3;

    protected List<ItemAccess> attachmentItems;
    protected int attachmentIndex;
    protected virtual Item.ItemCategory ItemCategory => Item.ItemCategory.Status;

    private int[] boardState; // -1=empty, >=0=index into attachmentItems
    private ItemAccess[] craftSlotItems; // craft materials displayed on board (non-placeable)
    private ItemAccess heldItem;
    private bool heldFromBoard;
    private int heldFromBoardIndex;
    private List<int> heldOriginalCells;
    private bool isMove;

    public override void Init(ItemManager itemManager)
    {
        InitBase(itemManager);
        attachmentItems = new List<ItemAccess>();
        boardState = new int[BoardSize * BoardSize];
        for (int i = 0; i < boardState.Length; i++) boardState[i] = -1;
        heldOriginalCells = new List<int>();
        highlight.gameObject.SetActive(false);
    }

    public override void OpenUI(Player player)
    {
        OpenUIBase(player);
    }

    protected override void OpenUIBase(Player player)
    {
        this.player = player;
        attachmentIndex = attachmentItems.Count; // lock all previously placed items
        inventoryIndex = (int)Player.InventoryType.Carry;
        index = 0;
        isInventory = true;
        isMove = false;
        UpdateAction();
        _HighLight();
        _Cursor();
        canvas.enabled = true;
    }

    public override void CloseUI()
    {
        // If mid-move from board, restore the piece before closing
        if (isMove && heldFromBoard)
            foreach (int c in heldOriginalCells) boardState[c] = heldFromBoardIndex;

        isMove = false;
        canvas.enabled = false;
        player.CloseToolUI();
        player = null;
    }

    public ItemAccess? GetAttachedItem()
    {
        if (attachmentIndex >= attachmentItems.Count) return null;
        return attachmentItems[attachmentIndex];
    }

    public override void Select(Vector2 vector)
    {
        if (vector.sqrMagnitude <= 0.7f) return;

        if (isInventory)
        {
            // _Select(vector);
        }
        else
        {
            if (isMove)
            {
                var shape = GetHeldShape();
                int maxCol = BoardSize - Mathf.Clamp(shape.width, 1, ShapeMaxSize);
                int maxRow = BoardSize - Mathf.Clamp(shape.height, 1, ShapeMaxSize);
                SelectBoard(vector, maxCol, maxRow);
                UpdateAction();
            }
            else
            {
                SelectBoard(vector, BoardSize - 1, BoardSize - 1);
            }
        }
        _Cursor();
    }

    private void SelectBoard(Vector2 vector, int maxCol, int maxRow)
    {
        var dir = ToDirection8(vector);
        int col = index % BoardSize;
        int row = index / BoardSize;

        bool right = dir == Direction8.Right || dir == Direction8.UpRight || dir == Direction8.DownRight;
        bool left = dir == Direction8.Left || dir == Direction8.UpLeft || dir == Direction8.DownLeft;
        bool up = dir == Direction8.Up || dir == Direction8.UpRight || dir == Direction8.UpLeft;
        bool down = dir == Direction8.Down || dir == Direction8.DownRight || dir == Direction8.DownLeft;

        if (right && col + 1 <= maxCol) col++;
        else if (left && col - 1 >= 0) col--;

        if (up && row + 1 <= maxRow) row++;
        else if (down && row - 1 >= 0) row--;

        index = row * BoardSize + col;
    }

    public override void Action()
    {
        if (isInventory)
        {
            if (player.Bag[inventoryIndex] != null && player.Bag[inventoryIndex].ItemAccess.Category == ItemCategory)
            {
                heldItem = player.Bag[inventoryIndex].ItemAccess;
                heldFromBoard = false;
                isInventory = false;
                isMove = true;
                ClampIndexToShape(GetHeldShape());
                _HighLight();
                UpdateAction();
            }
        }
        else if (isMove)
        {
            var shape = GetHeldShape();
            var (cells, outOfBounds) = GetShapeCellsRaw(index, shape);
            if (!outOfBounds && cells.Count > 0 && IsValidPlacement(cells))
            {
                if (heldFromBoard)
                {
                    foreach (int c in heldOriginalCells) boardState[c] = -1;
                    foreach (int c in cells) boardState[c] = heldFromBoardIndex;
                    isMove = false;
                    // stay on board for further moves
                }
                else
                {
                    int newIdx = attachmentItems.Count;
                    attachmentItems.Add(heldItem);
                    player.BagReduce(1, inventoryIndex);
                    foreach (int c in cells) boardState[c] = newIdx;
                    isMove = false;
                    isInventory = true;
                    _HighLight();
                }
                UpdateAction();
            }
        }
        else
        {
            // Board browsing: pick up an unlocked placed item
            int itemIdx = boardState[index];
            if (itemIdx >= attachmentIndex && itemIdx >= 0)
            {
                heldItem = attachmentItems[itemIdx];
                heldFromBoard = true;
                heldFromBoardIndex = itemIdx;
                heldOriginalCells.Clear();
                for (int i = 0; i < boardState.Length; i++)
                    if (boardState[i] == itemIdx) heldOriginalCells.Add(i);
                foreach (int c in heldOriginalCells) boardState[c] = -1;
                isMove = true;
                if (heldOriginalCells.Count > 0) index = heldOriginalCells[0]; // anchor = bottom-left cell
                ClampIndexToShape(GetHeldShape());
                UpdateAction();
            }
        }
    }

    public override bool Cancel()
    {
        if (!isMove)
        {
            if (isInventory) return true; // close UI
            // Board browsing → back to inventory
            isInventory = true;
            _HighLight();
            UpdateAction();
            return false;
        }

        if (heldFromBoard)
            foreach (int c in heldOriginalCells) boardState[c] = heldFromBoardIndex;
        // else: inventory item stays in bag as-is

        isMove = false;
        if (!heldFromBoard)
        {
            isInventory = true;
            _HighLight();
        }
        UpdateAction();
        return false;
    }

    public override void UpdateAction()
    {
        // Board
        for (int i = 0; i < buttons.Length; i++)
        {
            int itemIdx = boardState[i];
            if (itemIdx >= 0)
            {
                buttons[i].sprite = itemManager.GetItemIcon(attachmentItems[itemIdx]);
                buttons[i].color = itemIdx < attachmentIndex ? Color.gray : Color.white;
            }
            else if (IsCraftCell(i))
            {
                buttons[i].sprite = itemManager.GetItemIcon(craftSlotItems[i]);
                buttons[i].color = Color.gray;
            }
            else
            {
                buttons[i].sprite = null;
                buttons[i].color = Color.white;
            }
            if (itemTexts != null && i < itemTexts.Length) itemTexts[i].text = "";
        }

        // Placement preview
        if (!isInventory && isMove)
        {
            var shape = GetHeldShape();
            var (cells, outOfBounds) = GetShapeCellsRaw(index, shape);
            bool valid = !outOfBounds && cells.Count > 0 && IsValidPlacement(cells);
            Color preview = valid ? new Color(1f, 1f, 1f, 0.4f) : new Color(1f, 0f, 0f, 0.4f);
            foreach (int c in cells)
                if (c >= 0 && c < buttons.Length) buttons[c].color = preview;
        }

        // Inventory
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
            inventoryButtons[i].color = player.Bag[i] != null && player.Bag[i].ItemAccess.Category == ItemCategory ? Color.white : Color.gray;
        }
    }

    private AttachmentShape GetHeldShape()
    {
        var data = itemManager.GetItem(heldItem) as AttachmentData;
        return data != null ? data.Shape : default;
    }

    private void ClampIndexToShape(AttachmentShape shape)
    {
        int maxCol = BoardSize - Mathf.Clamp(shape.width, 1, ShapeMaxSize);
        int maxRow = BoardSize - Mathf.Clamp(shape.height, 1, ShapeMaxSize);
        int col = Mathf.Clamp(index % BoardSize, 0, maxCol);
        int row = Mathf.Clamp(index / BoardSize, 0, maxRow);
        index = row * BoardSize + col;
    }

    private (List<int> cells, bool outOfBounds) GetShapeCellsRaw(int anchorIndex, AttachmentShape shape)
    {
        int anchorCol = anchorIndex % BoardSize;
        int anchorRow = anchorIndex / BoardSize;
        int shapeW = Mathf.Clamp(shape.width, 1, ShapeMaxSize);
        int shapeH = Mathf.Clamp(shape.height, 1, ShapeMaxSize);
        var cells = new List<int>();
        bool outOfBounds = false;

        for (int row = 0; row < shapeH; row++)
        {
            for (int col = 0; col < shapeW; col++)
            {
                if (!shape.GetCell(col, row)) continue;
                int bCol = anchorCol + col;
                int bRow = anchorRow + row;
                if (bCol >= BoardSize || bRow >= BoardSize)
                {
                    outOfBounds = true;
                    continue;
                }
                cells.Add(bRow * BoardSize + bCol);
            }
        }
        return (cells, outOfBounds);
    }

    public void SetCraftSlot(ItemAccess[] craftSlots)
    {
        craftSlotItems = craftSlots;
    }

    private bool IsCraftCell(int cellIndex)
    {
        return craftSlotItems != null && cellIndex < craftSlotItems.Length && craftSlotItems[cellIndex].Id != -1;
    }

    private bool IsValidPlacement(List<int> cells)
    {
        foreach (int c in cells)
            if (boardState[c] != -1 || IsCraftCell(c)) return false;
        return true;
    }
    protected override void _Cursor()
    {
        inventoryHighlight.transform.position = inventoryButtons[inventoryIndex].transform.position;
    }
    protected override void _HighLight()
    {
        inventoryHighlightMaterial.SetFloat(sliceWidthId, isInventory ? 0.1f : 0f);
    }
}
