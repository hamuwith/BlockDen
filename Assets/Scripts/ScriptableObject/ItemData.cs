using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Scriptable Objects/ItemData")]
public class ItemData : ScriptableObject
{
    [SerializeField] protected ItemAccess itemAccess;
    [SerializeField] protected ItemAccess[] itemMaterials;
    [SerializeField] protected int unitNum;
    [SerializeField] protected int maxNum;
    [SerializeField] protected Texture2D texture2D;
    [SerializeField] protected Sprite sprite;
    public ItemAccess ItemAccess => itemAccess;
    public ItemAccess[] ItemMaterials => itemMaterials;
    public int UnitNum => unitNum;
    public int MaxNum => maxNum;
    public Texture2D Texture2D => texture2D;
    public Sprite Icon => sprite;
}
