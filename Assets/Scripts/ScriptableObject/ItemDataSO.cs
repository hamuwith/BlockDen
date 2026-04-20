using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDataSO", menuName = "Scriptable Objects/ItemDataSO")]
public class ItemDataSO : ScriptableObject
{
    [SerializeField] public TextAsset csvFile;
    [SerializeField] protected ItemData[] itemDatas;
    public ItemData[] ItemDatas => itemDatas;

#if UNITY_EDITOR
    public void SetItemDatas(ItemData[] datas)
    {
        itemDatas = datas;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}

[System.Serializable]
public class ItemData : ItemDataBase
{
    public List<ItemAccess> ItemMaterials;
}
[System.Serializable]
public class ItemDataBase
{
    public string Name;
    public ItemAccess ItemAccess;
    public int UnitNum;
    public int MaxNum;
    public Texture2D Texture2D;
    public Sprite Icon;
}
