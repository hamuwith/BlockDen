using UnityEngine;

[CreateAssetMenu(fileName = "MaterialDataSO", menuName = "Scriptable Objects/MaterialDataSO")]
public class MaterialDataSO : ScriptableObject
{
    [SerializeField] public TextAsset csvFile;
    [SerializeField] MaterialData[] itemDatas;
    public MaterialData[] ItemDatas => itemDatas;

#if UNITY_EDITOR
    public void SetItemDatas(MaterialData[] datas)
    {
        itemDatas = datas;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}

[System.Serializable]
public class MaterialData : ItemData
{
    public MaterialType MaterialType;
}
