using UnityEngine;
using System.Collections.Generic;

public class Attachment : Item
{
    [SerializeField] int damage;
    [SerializeField] int attackSpeed;
    [SerializeField] int attackRange;
    [SerializeField] int effection;
    [SerializeField] int ice;
    [SerializeField] int poison;
    [SerializeField] int lightning;
    [SerializeField] int shining;
    [SerializeField] int dark;
    [SerializeField] int strong;
    [SerializeField] int maxNum;
    [SerializeField] bool isUsed;
    public int Damage => damage;
    public int AttackSpeed => attackSpeed;
    public int AttackRange => attackRange;
    public int Effection => effection;
    public int Ice => ice;
    public int Poison => poison;
    public int Lightning => lightning;
    public int Shining => shining;
    public int Dark => dark;
    public int Strong => strong;
    public int MaxNum => maxNum;
    public bool IsUsed => isUsed;

    static public void SumAttachment(ref Attachment result, ref Queue<Attachment> consumables, Attachment attachment)
    {
        result.damage += attachment.Damage;
        result.attackSpeed += attachment.AttackSpeed;
        result.attackRange += attachment.AttackRange;
        result.effection += attachment.Effection;
        if (attachment.ice > 0 || attachment.poison > 0 || attachment.lightning > 0 || attachment.shining > 0 || attachment.dark > 0 || attachment.strong > 0)
        {
            consumables.Enqueue(attachment);
        }
    }
}
