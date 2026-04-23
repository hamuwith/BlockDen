using UnityEngine;
using static Item;

[CreateAssetMenu(fileName = "ToolDataSO", menuName = "Scriptable Objects/ToolDataSO")]
public class ToolDataSO : ScriptableObject
{
    [SerializeField] public TextAsset csvFile;
    [SerializeField] ToolData[] itemDatas;
    public ToolData[] ItemDatas => itemDatas;

#if UNITY_EDITOR
    public void SetItemDatas(ToolData[] datas)
    {
        itemDatas = datas;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}

[System.Serializable]
public class ToolData : ItemData
{
    public MaterialType MaterialType;
    public ItemCategory CraftCategory;
}
