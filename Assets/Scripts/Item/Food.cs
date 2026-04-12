using UnityEngine;

public class Food : Item
{
    [SerializeField] FoodStatus foodStatus;
    public FoodStatus StatusUp => foodStatus;
    [System.Serializable]
    public struct FoodStatus
    {
        public int Duration;
        public float MoveSpeed;
        public int Power;
    }
}
