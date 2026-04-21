using UnityEngine;

[CreateAssetMenu(fileName = "FoodDataSO", menuName = "Scriptable Objects/FoodDataSO")]
public class FoodDataSO : ScriptableObject
{
    [SerializeField] public TextAsset csvFile;
    [SerializeField] FoodData[] itemDatas;
    public FoodData[] ItemDatas => itemDatas;

#if UNITY_EDITOR
    public void SetItemDatas(FoodData[] datas)
    {
        itemDatas = datas;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}

[System.Serializable]
public class FoodData : ItemData
{
    public int Duration;
    public float MoveSpeed;
    public int Power;
    public int Damage;
}
