using UnityEngine;

public class Character : MonoBehaviour
{
    [SerializeField] protected float moveSpeed = 2f; // ˆÚ“®‘¬“x block per second

    protected Animator animator;
    protected MapManager mapManager;
    protected ItemManager itemManager;
    public float MoveSpeed => moveSpeed;
    public virtual void Init(MainManager mainManager)
    {
        animator = GetComponent<Animator>();
        mapManager = mainManager.MapManager;
        itemManager = mainManager.ItemManager;
    }
}
