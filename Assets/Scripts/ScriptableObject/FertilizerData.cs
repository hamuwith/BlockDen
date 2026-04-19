using UnityEngine;

[CreateAssetMenu(fileName = "FertilizerData", menuName = "Scriptable Objects/FertilizerData")]
public class FertilizerData : ItemData
{
    [SerializeField] FertilizerStatus fertilizerStatus;
    [SerializeField] bool[,] shape;
    public bool[,] Shape => shape;
    public FertilizerStatus PlusNum => fertilizerStatus;
}
