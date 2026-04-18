using UnityEngine;
using System.Collections.Generic;

public class Attachment : Item
{

    static public void SumAttachment(ref AttachmentStatus result, ref Queue<AttachmentStatus> consumables, AttachmentStatus attachment)
    {
        result.Damage += attachment.Damage;
        result.AttackSpeed += attachment.AttackSpeed;
        result.AttackRange += attachment.AttackRange;
        result.Effection += attachment.Effection;
        if (attachment.Ice > 0 || attachment.Poison > 0 || attachment.Lightning > 0 || attachment.Shining > 0 || attachment.Dark > 0 || attachment.Strong > 0)
        {
            consumables.Enqueue(attachment);
        }
    }
}
public struct AttachmentStatus
{
    public int Damage;
    public int AttackSpeed;
    public int AttackRange;
    public int Effection;
    public int Ice;
    public int Poison;
    public int Lightning;
    public int Shining;
    public int Dark;
    public int Strong;
}
