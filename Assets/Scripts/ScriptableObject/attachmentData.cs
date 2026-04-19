using UnityEngine;

[System.Serializable]
public struct AttachmentShape
{
    // 3x3 cells, index = row*3+col, row 0 = bottom-left
    public bool[] cells;
    public int width;   // 1-3: horizontal cell count
    public int height;  // 1-3: vertical cell count

    public readonly bool GetCell(int col, int row)
    {
        if (col < 0 || col >= 3 || row < 0 || row >= 3) return false;
        if (cells == null || cells.Length < 9) return false;
        return cells[row * 3 + col];
    }
}

[CreateAssetMenu(fileName = "AttachmentData", menuName = "Scriptable Objects/AttachmentData")]
public class AttachmentData : ItemData
{
    [SerializeField] AttachmentStatus attachmentStatus;
    [SerializeField] AttachmentShape shape;
    public AttachmentShape Shape => shape;
    public AttachmentStatus AttachmentStatus => attachmentStatus;
}
