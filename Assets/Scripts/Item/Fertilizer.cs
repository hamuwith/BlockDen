using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;
using Unity.VisualScripting;

public class Fertilizer : Item
{
    [SerializeField] float rate;
    [SerializeField] int plusNum;
    [SerializeField] int speed;
    public float Rate => rate;
    public int PlusNum => plusNum;
    public int Speed => speed;

    static public void SumFertilizer(ref Fertilizer result, List<Fertilizer> list)
    {
        foreach (var attachment in list)
        {
            result.rate += attachment.Rate;
            result.plusNum += attachment.PlusNum;
            result.speed += attachment.Speed;
        }
    }
}
