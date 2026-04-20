using UnityEngine;

[CreateAssetMenu(fileName = "WeaponBaseDataSO", menuName = "Scriptable Objects/WeaponBaseDataSO")]
public class WeaponBaseDataSO : ScriptableObject
{
    [SerializeField] public TextAsset csvFile;
    [SerializeField] WeaponBaseData[] itemDatas;
    public WeaponBaseData[] ItemDatas => itemDatas;

#if UNITY_EDITOR
    public void SetItemDatas(WeaponBaseData[] datas)
    {
        itemDatas = datas;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}

[System.Serializable]
public class WeaponBaseData : BlockData
{
    public Vector2Int BoardSize;
}
