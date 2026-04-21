using UnityEngine;

[CreateAssetMenu(fileName = "AttachmentDataSO", menuName = "Scriptable Objects/AttachmentDataSO")]
public class AttachmentDataSO : ScriptableObject
{
    [SerializeField] public TextAsset csvFile;
    [SerializeField] AttachmentData[] itemDatas;
    public AttachmentData[] ItemDatas => itemDatas;

#if UNITY_EDITOR
    public void SetItemDatas(AttachmentData[] datas)
    {
        itemDatas = datas;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}

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

[System.Serializable]
public class AttachmentData : ItemData
{
    public AttachmentStatus AttachmentStatus;
    public AttachmentShape Shape;
}
