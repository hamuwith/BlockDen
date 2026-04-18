using UnityEngine;
using static Block;

[CreateAssetMenu(fileName = "BreakTool", menuName = "Scriptable Objects/BreakTool")]
public class BreakToolData : ItemData
{
    [SerializeField] BlockTypeEnum blockType;
    [SerializeField] int lv;
    [SerializeField] int breakPower;
    public BlockTypeEnum BlockType => blockType;
    public int Lv => lv;
    public int BreakPower => breakPower;
}
