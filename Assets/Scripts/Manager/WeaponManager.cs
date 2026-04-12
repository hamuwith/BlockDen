using UnityEditor.SceneManagement;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{     
    [SerializeField] ArrowPool arrowPool;
    public ArrowPool ArrowPool => arrowPool;
    public void Init(MainManager mainManager)
    {
        ArrowPool.Init();
    }
}
