using UnityEngine;
using static Block;

public class BreakTool : Item
{
    [SerializeField] BlockTypeEnum blockType;
    [SerializeField] int lv;
    [SerializeField] int breakPower;
    [SerializeField] int unmatchPower;
    public BlockTypeEnum BlockType => blockType;
    public int Lv => lv;
    public int BreakPower => breakPower;
    public int UnmatchPower => unmatchPower;
}
