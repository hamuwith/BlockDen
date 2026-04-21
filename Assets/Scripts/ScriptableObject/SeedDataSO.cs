using UnityEngine;

[CreateAssetMenu(fileName = "SeedDataSO", menuName = "Scriptable Objects/SeedDataSO")]
public class SeedDataSO : ScriptableObject
{
    [SerializeField] public TextAsset csvFile;
    [SerializeField] SeedData[] itemDatas;
    public SeedData[] ItemDatas => itemDatas;

#if UNITY_EDITOR
    public void SetItemDatas(SeedData[] datas)
    {
        itemDatas = datas;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}

[System.Serializable]
public class SeedData : ItemDataBase
{
    public int GrowNum;
    public ItemAccess GrowBlock;
}
