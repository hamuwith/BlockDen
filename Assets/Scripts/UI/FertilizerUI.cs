using UnityEngine;

public class FertilizerUI : AttachUI
{
    protected override Item.ItemCategory ItemCategory => Item.ItemCategory.Fertilizer;

    public override void Init(ItemManager itemManager)
    {
        base.Init(itemManager);
        boardSize = new Vector2Int(3, 3);
        boardState = new int[9];
        BoardShift(boardSize);
        for (int i = 0; i < boardState.Length; i++) boardState[i] = -1;
    }

    protected override AttachmentShape GetHeldShape()
    {
        var data = itemManager.GetItem(heldItem) as FertilizerData;
        if (data != null) return data.Shape;
        return default;
    }
}
