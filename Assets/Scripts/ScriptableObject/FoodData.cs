using UnityEngine;

[CreateAssetMenu(fileName = "FoodData", menuName = "Scriptable Objects/FoodData")]
public class FoodData : ItemData
{
    [SerializeField] int duration;
    [SerializeField] float moveSpeed;
    [SerializeField] int power;
    public int Duration => duration;
    public float MoveSpeed => moveSpeed;
    public int Power => power;
}
