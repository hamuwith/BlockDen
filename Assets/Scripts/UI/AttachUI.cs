using UnityEngine;
using System.Collections.Generic;

public class AttachUI : BaseUI
{
    private const int ShapeMaxSize = 3;
    protected const int HiddenBoardCellState = -2;
    protected const int EmptyBoardCellState = -1;

    protected List<ItemAccess> attachmentItems;
    protected int attachmentIndex;
    protected virtual Item.ItemCategory ItemCategory => Item.ItemCategory.Status;

    protected int[] boardState; // -2=hidden, -1=empty, >=0=index into attachmentItems
    private ItemAccess[] craftSlotItems; // craft materials displayed on board (non-placeable)
    protected ItemAccess heldItem;
    private bool heldFromBoard;
    private int heldFromBoardIndex;
    private List<int> heldOriginalCells;
    private bool isMove;
    protected Vector2Int boardSize;


    public override void Init(ItemManager itemManager)
    {
        InitBase(itemManager);
        attachmentItems = new List<ItemAccess>();
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
                SelectBoard(vector, GetHeldShape(), true);
                UpdateAction();
            }
            else
            {
                SelectBoard(vector, default, false);
            }
        }
        _Cursor();
    }

    private void SelectBoard(Vector2 vector, AttachmentShape shape, bool useShape)
    {
        index = FindNearestSelectableIndex(index, shape, useShape);

        var dir = ToDirection8(vector);
        int col = index % boardSize.x;
        int row = index / boardSize.x;

        bool right = dir == Direction8.Right || dir == Direction8.UpRight || dir == Direction8.DownRight;
        bool left = dir == Direction8.Left || dir == Direction8.UpLeft || dir == Direction8.DownLeft;
        bool up = dir == Direction8.Up || dir == Direction8.UpRight || dir == Direction8.UpLeft;
        bool down = dir == Direction8.Down || dir == Direction8.DownRight || dir == Direction8.DownLeft;

        int nextCol = col;
        int nextRow = row;

        if (right && col + 1 < boardSize.x) nextCol++;
        else if (left && col - 1 >= 0) nextCol--;

        if (up && row + 1 < boardSize.y) nextRow++;
        else if (down && row - 1 >= 0) nextRow--;

        int nextIndex = nextRow * boardSize.x + nextCol;
        if (CanSelectBoardIndex(nextIndex, shape, useShape))
            index = nextIndex;
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
                    foreach (int c in heldOriginalCells) boardState[c] = EmptyBoardCellState;
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
            index = FindNearestSelectableIndex(index, default, false);
            int itemIdx = boardState[index];
            if (itemIdx >= attachmentIndex && itemIdx >= 0)
            {
                heldItem = attachmentItems[itemIdx];
                heldFromBoard = true;
                heldFromBoardIndex = itemIdx;
                heldOriginalCells.Clear();
                for (int i = 0; i < boardState.Length; i++)
                    if (boardState[i] == itemIdx) heldOriginalCells.Add(i);
                foreach (int c in heldOriginalCells) boardState[c] = EmptyBoardCellState;
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
        RefreshBoardButtonVisibility();

        // Board
        for (int i = 0; i < buttons.Length; i++)
        {
            if (i >= boardState.Length)
            {
                buttons[i].sprite = null;
                buttons[i].color = Color.white;
                if (itemTexts != null && i < itemTexts.Length) itemTexts[i].text = "";
                continue;
            }

            int itemIdx = boardState[i];
            if (itemIdx == HiddenBoardCellState)
            {
                buttons[i].sprite = null;
                buttons[i].color = Color.white;
            }
            else if (itemIdx >= 0)
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
            Color preview = valid ? new Color(1f, 1f, 1f, 0.8f) : new Color(1f, 0f, 0f, 0.8f);
            Sprite icon = itemManager.GetItemIcon(heldItem);
            foreach (int c in cells)
                if (c >= 0 && c < buttons.Length && boardState[c] != HiddenBoardCellState)
                {
                    buttons[c].color = preview;
                    buttons[c].sprite = icon;
                }
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

    protected virtual AttachmentShape GetHeldShape()
    {
        var data = itemManager.GetItem(heldItem) as AttachmentData;
        return data != null ? data.Shape : default;
    }

    private void ClampIndexToShape(AttachmentShape shape)
    {
        index = FindNearestSelectableIndex(index, shape, true);
    }

    private (List<int> cells, bool outOfBounds) GetShapeCellsRaw(int anchorIndex, AttachmentShape shape)
    {
        int anchorCol = anchorIndex % boardSize.x;
        int anchorRow = anchorIndex / boardSize.x;
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
                if (bCol >= boardSize.x || bRow >= boardSize.y)
                {
                    outOfBounds = true;
                    continue;
                }
                cells.Add(bRow * boardSize.x + bCol);
            }
        }
        return (cells, outOfBounds);
    }

    public void SetCraftSlot(ItemAccess[] craftSlots, Vector2Int boardSize)
    {
        craftSlotItems = craftSlots;
        this.boardSize = boardSize;
        BoardShift(boardSize);
        boardState = new int[boardSize.x * boardSize.y];
        for (int i = 0; i < boardState.Length; i++) boardState[i] = EmptyBoardCellState;
        RefreshBoardButtonVisibility();
    }

    private bool IsCraftCell(int cellIndex)
    {
        return craftSlotItems != null && cellIndex < craftSlotItems.Length && craftSlotItems[cellIndex].Id != -1;
    }

    private bool IsValidPlacement(List<int> cells)
    {
        foreach (int c in cells)
            if (boardState[c] != EmptyBoardCellState || IsCraftCell(c)) return false;
        return true;
    }

    protected void RefreshBoardButtonVisibility()
    {
        if (buttons == null || boardState == null) return;

        int visibleCellCount = Mathf.Min(buttons.Length, boardState.Length);
        for (int i = 0; i < visibleCellCount; i++)
        {
            bool isActive = boardState[i] != HiddenBoardCellState;
            if (buttons[i].gameObject.activeSelf != isActive)
                buttons[i].gameObject.SetActive(isActive);
        }

        for (int i = visibleCellCount; i < buttons.Length; i++)
        {
            if (buttons[i].gameObject.activeSelf)
                buttons[i].gameObject.SetActive(false);
        }
    }

    private int FindNearestSelectableIndex(int startIndex, AttachmentShape shape, bool useShape)
    {
        if (boardState == null || boardState.Length == 0) return 0;

        int clampedIndex = Mathf.Clamp(startIndex, 0, boardState.Length - 1);
        if (CanSelectBoardIndex(clampedIndex, shape, useShape))
            return clampedIndex;

        int startCol = clampedIndex % boardSize.x;
        int startRow = clampedIndex / boardSize.x;
        int nearestIndex = clampedIndex;
        int nearestDistance = int.MaxValue;
        bool found = false;

        for (int i = 0; i < boardState.Length; i++)
        {
            if (!CanSelectBoardIndex(i, shape, useShape)) continue;

            int col = i % boardSize.x;
            int row = i / boardSize.x;
            int distance = Mathf.Abs(col - startCol) + Mathf.Abs(row - startRow);
            if (!found || distance < nearestDistance)
            {
                nearestIndex = i;
                nearestDistance = distance;
                found = true;
            }
        }

        return found ? nearestIndex : clampedIndex;
    }

    private bool CanSelectBoardIndex(int boardIndex, AttachmentShape shape, bool useShape)
    {
        if (boardIndex < 0 || boardIndex >= boardState.Length) return false;

        if (!useShape)
            return boardState[boardIndex] != HiddenBoardCellState;

        var (cells, outOfBounds) = GetShapeCellsRaw(boardIndex, shape);
        return !outOfBounds && cells.Count > 0 && AreAllBoardCellsVisible(cells);
    }

    private bool AreAllBoardCellsVisible(List<int> cells)
    {
        foreach (int c in cells)
        {
            if (c < 0 || c >= boardState.Length || boardState[c] == HiddenBoardCellState)
                return false;
        }
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
