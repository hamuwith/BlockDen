using UnityEngine;

[CreateAssetMenu(fileName = "FertilizerData", menuName = "Scriptable Objects/FertilizerData")]
public class FertilizerData : ItemData
{
    [SerializeField] FertilizerStatus fertilizerStatus;
    public FertilizerStatus PlusNum => fertilizerStatus;
}
