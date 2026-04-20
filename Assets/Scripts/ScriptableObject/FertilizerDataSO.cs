using UnityEngine;

[CreateAssetMenu(fileName = "FertilizerDataSO", menuName = "Scriptable Objects/FertilizerDataSO")]
public class FertilizerDataSO : ScriptableObject
{
    [SerializeField] public TextAsset csvFile;
    [SerializeField] FertilizerData[] itemDatas;
    public FertilizerData[] ItemDatas => itemDatas;

#if UNITY_EDITOR
    public void SetItemDatas(FertilizerData[] datas)
    {
        itemDatas = datas;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}

[System.Serializable]
public class FertilizerData : ItemDataBase
{
    public FertilizerStatus FertilizerStatus;
    public AttachmentShape Shape;
}
