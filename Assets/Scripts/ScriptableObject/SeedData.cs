using UnityEngine;

[CreateAssetMenu(fileName = "SeedData", menuName = "Scriptable Objects/SeedData")]
public class SeedData : BlockData
{
    [SerializeField] int growNum;
    [SerializeField] ItemData growBlock;
    public int GrowNum => growNum;
    public ItemData GrowBlock => growBlock;
}
