using UnityEngine;

[CreateAssetMenu(fileName = "ItemDataSO", menuName = "Scriptable Objects/ItemDataSO")]
public class ItemDataSO : ScriptableObject
{
    [SerializeField] protected ItemData[] itemDatas;
}

[System.Serializable]
public class ItemData
{
    public ItemAccess ItemAccess;
    public ItemAccess[] ItemMaterials;
    public int UnitNum;
    public int MaxNum;
    public Texture2D Texture2D;
    public Sprite Icon;
}
