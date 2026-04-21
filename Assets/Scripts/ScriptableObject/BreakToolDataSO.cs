using UnityEngine;
using static Block;

[CreateAssetMenu(fileName = "BreakToolDataSO", menuName = "Scriptable Objects/BreakToolDataSO")]
public class BreakToolDataSO : ScriptableObject
{
    [SerializeField] public TextAsset csvFile;
    [SerializeField] BreakToolData[] itemDatas;
    public BreakToolData[] ItemDatas => itemDatas;

#if UNITY_EDITOR
    public void SetItemDatas(BreakToolData[] datas)
    {
        itemDatas = datas;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}

[System.Serializable]
public class BreakToolData : ItemData
{
    public BlockTypeEnum BlockType;
    public int Lv;
    public int BreakPower;
}
