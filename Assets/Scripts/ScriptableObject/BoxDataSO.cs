using UnityEngine;
using static Player;

[CreateAssetMenu(fileName = "BoxDataSO", menuName = "Scriptable Objects/BoxDataSO")]
public class BoxDataSO : ScriptableObject
{
    [SerializeField] public TextAsset csvFile;
    [SerializeField] BoxData[] itemDatas;
    public BoxData[] ItemDatas => itemDatas;

#if UNITY_EDITOR
    public void SetItemDatas(BoxData[] datas)
    {
        itemDatas = datas;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}

[System.Serializable]
public class BoxData : ItemData
{
    public InventoryType InventoryType;
}
