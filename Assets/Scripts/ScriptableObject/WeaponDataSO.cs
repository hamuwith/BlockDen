using UnityEngine;

[CreateAssetMenu(fileName = "WeaponDataSO", menuName = "Scriptable Objects/WeaponDataSO")]
public class WeaponDataSO : ScriptableObject
{
    [SerializeField] public TextAsset csvFile;
    [SerializeField] WeaponData[] itemDatas;
    public WeaponData[] ItemDatas => itemDatas;

#if UNITY_EDITOR
    public void SetItemDatas(WeaponData[] datas)
    {
        itemDatas = datas;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}

[System.Serializable]
public class WeaponData : CraftItemData
{
    public int ArrowId;
    public int Damage;
    public int AttackSpeed;
    public float Range;
}
