using System.Collections.Generic;
using UnityEngine;

public class FertilizerUI : AttachUI
{
    private const int FertilizerBoardSize = 5;
    private const int InitialUnlockedCellCount = 9;
    // First 9 cells form the centered 3x3 area, then the outer ring opens clockwise.
    private static readonly IReadOnlyList<int> BoardUnlockOrder = new int[]
    {
        6, 7, 8,
        11, 12, 13,
        16, 17, 18,
        19, 14, 9, 4,
        3, 2, 1, 0,
        5, 10, 15, 20,
        21, 22, 23, 24
    };

    private int unlockedCellCount;

    protected override Item.ItemCategory ItemCategory => Item.ItemCategory.Fertilizer;

    public override void Init(ItemManager itemManager)
    {
        base.Init(itemManager);
        boardSize = new Vector2Int(FertilizerBoardSize, FertilizerBoardSize);
        boardState = new int[boardSize.x * boardSize.y];
        for (int i = 0; i < boardState.Length; i++) boardState[i] = HiddenBoardCellState;

        unlockedCellCount = 0;
        UnlockCellsTo(InitialUnlockedCellCount);
    }

    public void UnlockNextCell()
    {
        UnlockCellsTo(unlockedCellCount + 1);
    }

    public void UnlockCellsTo(int targetUnlockedCellCount)
    {
        int targetCount = Mathf.Clamp(targetUnlockedCellCount, 0, BoardUnlockOrder.Count);
        if (targetCount <= unlockedCellCount)
        {
            RefreshBoardButtonVisibility();
            return;
        }

        for (int i = unlockedCellCount; i < targetCount; i++)
        {
            int cellIndex = BoardUnlockOrder[i];
            if (cellIndex < 0 || cellIndex >= boardState.Length) continue;
            if (boardState[cellIndex] == HiddenBoardCellState)
                boardState[cellIndex] = EmptyBoardCellState;
        }

        unlockedCellCount = targetCount;
        RefreshBoardButtonVisibility();

        if (canvas.enabled && player != null)
            UpdateAction();
    }

    protected override AttachmentShape GetHeldShape()
    {
        var data = itemManager.GetItem(heldItem) as FertilizerData;
        if (data != null) return data.Shape;
        return default;
    }
}
