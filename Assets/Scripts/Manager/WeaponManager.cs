using UnityEditor.SceneManagement;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{     
    [SerializeField] ArrowPool[] arrowPools;
    public ArrowPool[] ArrowPool => arrowPools;
    public void Init(MainManager mainManager)
    {
        foreach (var pool in arrowPools)
        {
            pool.Init();
        }
    }
}
